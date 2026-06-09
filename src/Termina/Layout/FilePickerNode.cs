// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.IO.Enumeration;
using R3;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Layout;

/// <summary>
/// An interactive file/folder picker with breadcrumb navigation, scrolling, and fuzzy filtering.
/// </summary>
public sealed class FilePickerNode : IFocusable, IInvalidatingNode
{
    private readonly Subject<Unit> _invalidated = new();
    private readonly Subject<IReadOnlyList<string>> _selectionConfirmed = new();
    private readonly Subject<Unit> _cancelled = new();
    private readonly Subject<string> _directoryChanged = new();

    private IFileSystemProvider _fileSystem = DefaultFileSystemProvider.Instance;
    private string _currentPath;
    private List<FileSystemEntry> _entries = new();
    private List<FileSystemEntry> _filteredEntries = new();
    private readonly HashSet<string> _selectedPaths = new();

    private int _highlightedIndex;
    private int _scrollOffset;
    private int _visibleRows = 10;
    private bool _hasFocus;
    private bool _disposed;

    // Filter state
    private bool _isFiltering;
    private TextInputNode? _filterInput;

    // Configuration
    private FilePickerMode _mode = FilePickerMode.Files;
    private FilePickerSelectionMode _selectionMode = FilePickerSelectionMode.Single;
    private bool _showHidden;
    private string? _fileFilter;
    private bool _fillHeight;
    private Color _highlightForeground = Color.Black;
    private Color _highlightBackground = Color.White;
    private Color _directoryColor = Color.BrightCyan;
    private Color _fileColor = Color.Default;

    // ASCII icons (avoids surrogate-pair/column-width issues with emoji)
    private const string DirIcon = "[D] ";
    private const string FileIcon = "    ";
    private const string FilterPrefix = "/ ";
    private const string BreadcrumbPrefix = ">> ";

    public FilePickerNode(string? startPath = null)
    {
        _currentPath = startPath ?? Environment.CurrentDirectory;
    }

    public Observable<Unit> Invalidated => _invalidated.AsObservable();

    /// <summary>
    /// Emits selected full paths when the user confirms a selection.
    /// </summary>
    public Observable<IReadOnlyList<string>> SelectionConfirmed => _selectionConfirmed.AsObservable();

    /// <summary>
    /// Emits when the user presses Escape (at root level or without active filter).
    /// </summary>
    public Observable<Unit> Cancelled => _cancelled.AsObservable();

    /// <summary>
    /// Emits the new directory path whenever the user navigates to a different directory.
    /// </summary>
    public Observable<string> DirectoryChanged => _directoryChanged.AsObservable();

    public SizeConstraint WidthConstraint => SizeConstraint.FillRemaining();

    public SizeConstraint HeightConstraint => _fillHeight ? SizeConstraint.FillRemaining() : SizeConstraint.AutoSize();

    public bool CanFocus => true;

    public bool HasFocus => _hasFocus;

    public int FocusPriority => 10;

    /// <summary>
    /// Gets the current directory path being displayed.
    /// </summary>
    public string CurrentPath => _currentPath;

    #region Fluent Configuration

    public FilePickerNode WithStartPath(string path)
    {
        _currentPath = path;
        return this;
    }

    public FilePickerNode WithMode(FilePickerMode mode)
    {
        _mode = mode;
        return this;
    }

    public FilePickerNode WithSelectionMode(FilePickerSelectionMode mode)
    {
        _selectionMode = mode;
        return this;
    }

    public FilePickerNode WithShowHidden(bool show = true)
    {
        _showHidden = show;
        return this;
    }

    public FilePickerNode WithFileFilter(string? globPattern)
    {
        _fileFilter = globPattern;
        return this;
    }

    public FilePickerNode WithHighlightColors(Color foreground, Color background)
    {
        _highlightForeground = foreground;
        _highlightBackground = background;
        return this;
    }

    public FilePickerNode WithDirectoryColor(Color color)
    {
        _directoryColor = color;
        return this;
    }

    public FilePickerNode WithFileColor(Color color)
    {
        _fileColor = color;
        return this;
    }

    public FilePickerNode WithVisibleRows(int rows)
    {
        _visibleRows = Math.Max(1, rows);
        _fillHeight = false;
        return this;
    }

    public FilePickerNode WithFillHeight(bool fill = true)
    {
        _fillHeight = fill;
        return this;
    }

    public FilePickerNode WithFileSystemProvider(IFileSystemProvider provider)
    {
        _fileSystem = provider ?? throw new ArgumentNullException(nameof(provider));
        return this;
    }

    #endregion

    #region Focus Lifecycle

    public void OnFocused()
    {
        _hasFocus = true;

        // Load entries on first focus if not yet loaded
        if (_entries.Count == 0)
            LoadDirectory(_currentPath);

        Invalidate();
    }

    public void OnBlurred()
    {
        _hasFocus = false;
        if (_isFiltering)
            StopFiltering();
        Invalidate();
    }

    public void OnActivate()
    {
        if (_entries.Count == 0)
            LoadDirectory(_currentPath);

        _filterInput?.OnActivate();
    }

    public void OnDeactivate()
    {
        _filterInput?.OnDeactivate();
    }

    #endregion

    #region Input Handling

    public bool HandleInput(ConsoleKeyInfo key)
    {
        if (_isFiltering)
            return HandleFilterInput(key);

        return HandleBrowsingInput(key);
    }

    private bool HandleBrowsingInput(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
            case ConsoleKey.Home:
            case ConsoleKey.End:
                return HandleNavigationKey(key);

            case ConsoleKey.Enter:
                ActivateHighlighted();
                return true;

            case ConsoleKey.Backspace:
                NavigateUp();
                return true;

            case ConsoleKey.Escape:
                OnEscapePressed();
                return true;

            case ConsoleKey.Spacebar:
                HandleSpacebar();
                return true;

            default:
                // '/' activates filter; any printable char also activates filter + types it
                if (key.KeyChar == '/')
                {
                    StartFiltering("");
                    return true;
                }

                if (!char.IsControl(key.KeyChar) && key.KeyChar != '\0')
                {
                    StartFiltering(key.KeyChar.ToString());
                    return true;
                }

                return false;
        }
    }

    private bool HandleFilterInput(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                StopFiltering();
                return true;

            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
            case ConsoleKey.Home:
            case ConsoleKey.End:
                return HandleNavigationKey(key);

            case ConsoleKey.Enter:
                ActivateHighlighted();
                return true;

            case ConsoleKey.Spacebar:
                HandleSpacebar();
                return true;

            default:
                if (_filterInput != null)
                {
                    var handled = _filterInput.HandleInput(key);
                    if (handled)
                    {
                        ApplyFilter();

                        if (string.IsNullOrEmpty(_filterInput.Text))
                            StopFiltering();
                    }
                    return handled;
                }
                return false;
        }
    }

    private bool HandleNavigationKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                MoveHighlight(-1);
                return true;
            case ConsoleKey.DownArrow:
                MoveHighlight(1);
                return true;
            case ConsoleKey.Home:
                _highlightedIndex = 0;
                EnsureVisible();
                Invalidate();
                return true;
            case ConsoleKey.End:
                _highlightedIndex = Math.Max(0, _filteredEntries.Count - 1);
                EnsureVisible();
                Invalidate();
                return true;
            default:
                return false;
        }
    }

    private void HandleSpacebar()
    {
        if (_highlightedIndex < 0 || _highlightedIndex >= _filteredEntries.Count)
            return;

        var entry = _filteredEntries[_highlightedIndex];

        if (_selectionMode == FilePickerSelectionMode.Multi)
        {
            ToggleSelection();
        }
        else
        {
            // Single mode: Space selects the item if it's selectable per mode
            if (IsSelectable(entry))
                _selectionConfirmed.OnNext(new[] { entry.FullPath });
        }
    }

    private void OnEscapePressed()
    {
        _cancelled.OnNext(Unit.Default);
    }

    #endregion

    #region Navigation

    private void LoadDirectory(string path)
    {
        if (!_fileSystem.DirectoryExists(path))
            return;

        _currentPath = path;
        var allEntries = _fileSystem.GetEntries(path);

        _entries = new List<FileSystemEntry>(allEntries.Count);
        foreach (var e in allEntries)
        {
            if (!_showHidden && e.Name.StartsWith('.'))
                continue;
            if (!e.IsDirectory && _fileFilter != null && !MatchesGlob(e.Name, _fileFilter))
                continue;
            if (_mode == FilePickerMode.Directories && !e.IsDirectory)
                continue;
            _entries.Add(e);
        }

        _filteredEntries = new List<FileSystemEntry>(_entries);
        _highlightedIndex = 0;
        _scrollOffset = 0;
        _isFiltering = false;

        EmitDirectoryChanged(_currentPath);
        Invalidate();
    }

    private void NavigateUp()
    {
        var parent = _fileSystem.GetParentDirectory(_currentPath);
        if (parent != null)
            LoadDirectory(parent);
    }

    private void ActivateHighlighted()
    {
        if (_highlightedIndex < 0 || _highlightedIndex >= _filteredEntries.Count)
            return;

        var entry = _filteredEntries[_highlightedIndex];

        if (entry.IsDirectory)
        {
            // Enter on a directory always navigates into it, regardless of mode
            LoadDirectory(entry.FullPath);
            return;
        }

        // File selected
        if (_selectionMode == FilePickerSelectionMode.Single)
        {
            _selectionConfirmed.OnNext(new[] { entry.FullPath });
        }
        else
        {
            // In multi-select, Enter confirms all toggled items
            var selected = _selectedPaths.Count > 0
                ? _selectedPaths.ToList()
                : new List<string> { entry.FullPath };
            _selectionConfirmed.OnNext(selected);
        }
    }

    private void ToggleSelection()
    {
        if (_highlightedIndex < 0 || _highlightedIndex >= _filteredEntries.Count)
            return;

        var entry = _filteredEntries[_highlightedIndex];

        if (!IsSelectable(entry))
            return;

        if (!_selectedPaths.Remove(entry.FullPath))
            _selectedPaths.Add(entry.FullPath);

        MoveHighlight(1);
    }

    private bool IsSelectable(FileSystemEntry entry)
    {
        return _mode switch
        {
            FilePickerMode.Files => !entry.IsDirectory,
            FilePickerMode.Directories => entry.IsDirectory,
            FilePickerMode.All => true,
            _ => false
        };
    }

    #endregion

    #region Filtering

    private void StartFiltering(string initialText)
    {
        _isFiltering = true;
        _filterInput ??= new TextInputNode()
            .WithPlaceholder("Type to filter...");
        _filterInput.OnActivate();
        _filterInput.Clear();

        // Seed with initial text if provided
        if (!string.IsNullOrEmpty(initialText))
        {
            foreach (var c in initialText)
            {
                _filterInput.HandleInput(new ConsoleKeyInfo(c, ConsoleKey.None, false, false, false));
            }
        }

        ApplyFilter();
        Invalidate();
    }

    private void StopFiltering()
    {
        _isFiltering = false;
        _filterInput?.OnDeactivate();
        _filteredEntries = new List<FileSystemEntry>(_entries);
        _highlightedIndex = Math.Min(_highlightedIndex, Math.Max(0, _filteredEntries.Count - 1));
        _scrollOffset = 0;
        EnsureVisible();
        Invalidate();
    }

    private void ApplyFilter()
    {
        var filterText = _filterInput?.Text ?? "";
        if (string.IsNullOrEmpty(filterText))
        {
            _filteredEntries = new List<FileSystemEntry>(_entries);
        }
        else
        {
            _filteredEntries = _entries
                .Where(e => e.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        _highlightedIndex = 0;
        _scrollOffset = 0;
        Invalidate();
    }

    #endregion

    #region Scrolling

    private void MoveHighlight(int delta)
    {
        if (_filteredEntries.Count == 0)
            return;

        _highlightedIndex = Math.Clamp(_highlightedIndex + delta, 0, _filteredEntries.Count - 1);
        EnsureVisible();
        Invalidate();
    }

    private void EnsureVisible()
    {
        if (_highlightedIndex < _scrollOffset)
        {
            _scrollOffset = _highlightedIndex;
        }
        else if (_highlightedIndex >= _scrollOffset + _visibleRows)
        {
            _scrollOffset = _highlightedIndex - _visibleRows + 1;
        }
    }

    #endregion

    #region Measure & Render

    public Size Measure(Size available)
    {
        // Layout: breadcrumb (1) + optional filter (1) + file list + hints (1)
        var headerLines = 1 + (_isFiltering ? 1 : 0);
        var footerLines = 1;

        if (_fillHeight)
        {
            _visibleRows = Math.Max(1, available.Height - headerLines - footerLines);
            return new Size(available.Width, available.Height);
        }

        var listHeight = Math.Min(_filteredEntries.Count, _visibleRows);
        var totalHeight = headerLines + listHeight + footerLines;
        return new Size(available.Width, totalHeight);
    }

    public void Render(IRenderContext context, Rect bounds)
    {
        if (!bounds.HasArea)
            return;

        var sub = context.CreateSubContext(bounds);
        var currentRow = 0;
        var width = bounds.Width;

        // Breadcrumb
        RenderBreadcrumb(sub, currentRow, width);
        currentRow++;

        // Filter bar (if active)
        if (_isFiltering)
        {
            RenderFilterBar(sub, currentRow, width);
            currentRow++;
        }

        // Recalculate visible rows in fill mode
        var footerLines = 1;
        var availableForList = bounds.Height - currentRow - footerLines;
        if (_fillHeight)
            _visibleRows = Math.Max(1, availableForList);

        var listHeight = Math.Min(_filteredEntries.Count, Math.Min(_visibleRows, availableForList));

        // File list
        RenderFileList(sub, currentRow, width, listHeight);
        currentRow += Math.Max(listHeight, 1);

        // Key hints footer
        if (currentRow < bounds.Height)
            RenderFooter(sub, currentRow, width);
    }

    private void RenderBreadcrumb(IRenderContext context, int row, int width)
    {
        context.SetForeground(Color.BrightYellow);
        context.SetDecoration(TextDecoration.Bold);

        var pathDisplay = _currentPath;
        var maxPathWidth = width - BreadcrumbPrefix.Length - 1;
        if (pathDisplay.Length > maxPathWidth)
            pathDisplay = "..." + pathDisplay[(pathDisplay.Length - maxPathWidth + 3)..];

        context.WriteAt(1, row, BreadcrumbPrefix + pathDisplay);
        context.SetDecoration(TextDecoration.None);
        context.ResetColors();
    }

    private void RenderFilterBar(IRenderContext context, int row, int width)
    {
        context.SetForeground(Color.BrightMagenta);
        var prefixLen = FilterPrefix.Length + 1; // +1 for the leading space at x=1
        context.WriteAt(1, row, FilterPrefix);

        if (_filterInput != null)
        {
            var inputX = prefixLen;
            var inputBounds = new Rect(inputX, row, Math.Max(1, width - inputX - 1), 1);
            var inputContext = context.CreateSubContext(inputBounds);
            _filterInput.Render(inputContext, new Rect(0, 0, inputBounds.Width, 1));
        }

        context.ResetColors();
    }

    private void RenderFileList(IRenderContext context, int startRow, int width, int listHeight)
    {
        if (_filteredEntries.Count == 0)
        {
            context.SetForeground(Color.BrightBlack);
            context.WriteAt(2, startRow, _isFiltering ? "No matches" : "Empty directory");
            context.ResetColors();
            return;
        }

        var needsScrollbar = _filteredEntries.Count > listHeight;
        var contentWidth = needsScrollbar ? width - 1 : width;

        for (var i = 0; i < listHeight && (_scrollOffset + i) < _filteredEntries.Count; i++)
        {
            var entryIndex = _scrollOffset + i;
            var entry = _filteredEntries[entryIndex];
            var isHighlighted = entryIndex == _highlightedIndex && _hasFocus;
            var isSelected = _selectedPaths.Contains(entry.FullPath);
            var row = startRow + i;

            RenderEntry(context, entry, row, contentWidth, isHighlighted, isSelected);
        }

        if (needsScrollbar)
            RenderScrollbar(context, width - 1, startRow, listHeight);
    }

    private void RenderEntry(IRenderContext context, FileSystemEntry entry, int row, int width,
        bool isHighlighted, bool isSelected)
    {
        // Build prefix: cursor + checkbox (multi) + icon
        var cursor = isHighlighted ? "> " : "  ";
        var checkbox = "";
        if (_selectionMode == FilePickerSelectionMode.Multi)
        {
            checkbox = IsSelectable(entry) ? (isSelected ? "[x] " : "[ ] ") : "    ";
        }

        var icon = entry.IsDirectory ? DirIcon : FileIcon;
        var name = entry.IsDirectory ? entry.Name + "/" : entry.Name;

        var prefix = cursor + checkbox + icon;
        var displayText = prefix + name;

        // Truncate if needed (safe — all chars are BMP, no surrogates)
        if (displayText.Length > width)
            displayText = displayText[..(width - 1)] + "~";

        // Apply highlight or normal colors
        if (isHighlighted)
        {
            context.SetBackground(_highlightBackground);
            context.Fill(0, row, width, 1, ' ');
            context.SetForeground(_highlightForeground);
        }
        else if (isSelected)
        {
            context.SetForeground(Color.BrightGreen);
        }
        else
        {
            context.SetForeground(entry.IsDirectory ? _directoryColor : _fileColor);
        }

        context.WriteAt(0, row, displayText);
        context.ResetColors();
    }

    private void RenderScrollbar(IRenderContext context, int x, int startRow, int height)
    {
        var totalItems = _filteredEntries.Count;
        if (totalItems <= height)
            return;

        var thumbSize = Math.Max(1, height * height / totalItems);
        var thumbPos = _scrollOffset * (height - thumbSize) / Math.Max(1, totalItems - height);

        context.SetForeground(Color.BrightBlack);
        for (var row = 0; row < height; row++)
        {
            var isThumb = row >= thumbPos && row < thumbPos + thumbSize;
            context.WriteAt(x, startRow + row, isThumb ? '#' : '|');
        }
        context.ResetColors();
    }

    private void RenderFooter(IRenderContext context, int row, int width)
    {
        context.SetForeground(Color.BrightBlack);

        string hints;
        if (_isFiltering)
        {
            hints = "[Enter] Open  [Esc] Clear filter";
        }
        else if (_selectionMode == FilePickerSelectionMode.Multi)
        {
            hints = "[Space] Toggle  [Enter] Open/Confirm  [BS] Up  [/] Filter  [Esc] Cancel";
        }
        else if (_mode is FilePickerMode.Directories or FilePickerMode.All)
        {
            hints = "[Enter] Open  [Space] Select  [BS] Up  [/] Filter  [Esc] Cancel";
        }
        else
        {
            hints = "[Enter] Open/Select  [BS] Up  [/] Filter  [Esc] Cancel";
        }

        if (hints.Length > width)
            hints = hints[..width];

        context.WriteAt(1, row, hints);
        context.ResetColors();
    }

    #endregion

    #region Utilities

    private static bool MatchesGlob(string fileName, string pattern)
    {
        return FileSystemName.MatchesSimpleExpression(pattern, fileName, ignoreCase: true);
    }

    private void EmitDirectoryChanged(string path)
    {
        if (!_disposed)
            _directoryChanged.OnNext(path);
    }

    private void Invalidate()
    {
        if (!_disposed)
            _invalidated.OnNext(Unit.Default);
    }

    #endregion

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _invalidated.OnCompleted();
        _invalidated.Dispose();
        _selectionConfirmed.OnCompleted();
        _selectionConfirmed.Dispose();
        _cancelled.OnCompleted();
        _cancelled.Dispose();
        _directoryChanged.OnCompleted();
        _directoryChanged.Dispose();

        _filterInput?.Dispose();
    }
}
