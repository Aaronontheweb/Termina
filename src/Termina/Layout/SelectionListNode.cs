// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Layout;

/// <summary>
/// An interactive list selection component with keyboard navigation.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
/// <remarks>
/// <para>
/// SelectionListNode provides keyboard-driven item selection with:
/// - Arrow key navigation (Up/Down)
/// - Home/End to jump to first/last item
/// - Space to toggle selection (in Multi mode)
/// - Enter to confirm selection
/// - Number keys (1-9) for quick selection
/// - Escape to cancel
/// </para>
/// <para>
/// Optionally supports an "Other" option for custom text input.
/// </para>
/// </remarks>
public sealed class SelectionListNode<T> : IFocusable, IInvalidatingNode
{
    private readonly Subject<Unit> _invalidated = new();
    private readonly Subject<IReadOnlyList<T>> _selectionConfirmed = new();
    private readonly Subject<string> _otherSelected = new();
    private readonly Subject<Unit> _cancelled = new();
    private readonly List<SelectionItem<T>> _items = new();
    private readonly Func<T, string> _displaySelector;

    private int _highlightedIndex;
    private int _scrollOffset;
    private int _visibleRows = 10;
    private bool _isEditingOther;
    private TextInputNode? _otherInput;
    private string? _otherLabel;
    private Action<string>? _otherCallback;
    private bool _hasFocus;
    private bool _disposed;

    private SelectionMode _mode = SelectionMode.Single;
    private Color _highlightForeground = Color.Black;
    private Color _highlightBackground = Color.White;
    private Color? _foreground;
    private Color? _selectedForeground;
    private bool _showNumbers = true;

    /// <summary>
    /// Creates a new SelectionListNode with the specified items.
    /// </summary>
    /// <param name="items">The items to display.</param>
    /// <param name="displaySelector">A function to convert items to display text.</param>
    public SelectionListNode(IEnumerable<T> items, Func<T, string> displaySelector)
    {
        _displaySelector = displaySelector ?? throw new ArgumentNullException(nameof(displaySelector));

        foreach (var item in items)
        {
            _items.Add(new SelectionItem<T>(item, displaySelector(item)));
        }

        if (_items.Count > 0)
            _highlightedIndex = 0;
    }

    /// <inheritdoc />
    public IObservable<Unit> Invalidated => _invalidated.AsObservable();

    /// <summary>
    /// Observable that emits the selected items when Enter is pressed.
    /// </summary>
    public IObservable<IReadOnlyList<T>> SelectionConfirmed => _selectionConfirmed.AsObservable();

    /// <summary>
    /// Observable that emits the custom text when "Other" is selected and confirmed.
    /// </summary>
    public IObservable<string> OtherSelected => _otherSelected.AsObservable();

    /// <summary>
    /// Observable that emits when Escape is pressed.
    /// </summary>
    public IObservable<Unit> Cancelled => _cancelled.AsObservable();

    /// <inheritdoc />
    public SizeConstraint WidthConstraint => SizeConstraint.FillRemaining();

    /// <inheritdoc />
    public SizeConstraint HeightConstraint => SizeConstraint.AutoSize();

    /// <inheritdoc />
    public bool CanFocus => true;

    /// <inheritdoc />
    public bool HasFocus => _hasFocus;

    /// <summary>
    /// Selection list has medium priority (lower than modal).
    /// </summary>
    public int FocusPriority => 10;

    /// <summary>
    /// Gets the currently highlighted item.
    /// </summary>
    public SelectionItem<T>? HighlightedItem =>
        _highlightedIndex >= 0 && _highlightedIndex < _items.Count ? _items[_highlightedIndex] : null;

    /// <summary>
    /// Gets all selected items.
    /// </summary>
    public IReadOnlyList<T> SelectedItems =>
        _items.Where(i => i.IsSelected && !i.IsOther).Select(i => i.Value).ToList();

    /// <summary>
    /// Gets the items in the list.
    /// </summary>
    public IReadOnlyList<SelectionItem<T>> Items => _items;

    /// <summary>
    /// Sets the selection mode (Single or Multi).
    /// </summary>
    public SelectionListNode<T> WithMode(SelectionMode mode)
    {
        _mode = mode;
        return this;
    }

    /// <summary>
    /// Sets the highlight colors for the selected row.
    /// </summary>
    public SelectionListNode<T> WithHighlightColors(Color foreground, Color background)
    {
        _highlightForeground = foreground;
        _highlightBackground = background;
        return this;
    }

    /// <summary>
    /// Sets the default foreground color.
    /// </summary>
    public SelectionListNode<T> WithForeground(Color color)
    {
        _foreground = color;
        return this;
    }

    /// <summary>
    /// Sets the color for selected (checked) items.
    /// </summary>
    public SelectionListNode<T> WithSelectedForeground(Color color)
    {
        _selectedForeground = color;
        return this;
    }

    /// <summary>
    /// Sets whether to show number prefixes (1-9) for quick selection.
    /// </summary>
    public SelectionListNode<T> WithShowNumbers(bool show = true)
    {
        _showNumbers = show;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of visible rows before scrolling.
    /// </summary>
    public SelectionListNode<T> WithVisibleRows(int rows)
    {
        _visibleRows = Math.Max(1, rows);
        return this;
    }

    /// <summary>
    /// Adds an "Other..." option that allows custom text input.
    /// </summary>
    /// <param name="label">The label for the "Other" option (default: "Other...").</param>
    /// <param name="onOther">Callback when custom text is submitted.</param>
    public SelectionListNode<T> WithOtherOption(string label = "Other...", Action<string>? onOther = null)
    {
        _otherLabel = label;
        _otherCallback = onOther;

        // Add Other as a special item at the end
        // We use default(T)! as the value since it won't be used
        _items.Add(new SelectionItem<T>(default!, label, isOther: true));

        return this;
    }

    /// <inheritdoc />
    public void OnFocused()
    {
        _hasFocus = true;
        Invalidate();
    }

    /// <inheritdoc />
    public void OnBlurred()
    {
        _hasFocus = false;
        _isEditingOther = false;
        Invalidate();
    }

    /// <inheritdoc />
    public bool HandleInput(ConsoleKeyInfo key)
    {
        // If editing "Other" text, forward to text input
        if (_isEditingOther && _otherInput != null)
        {
            if (key.Key == ConsoleKey.Enter)
            {
                var text = _otherInput.Text;
                _isEditingOther = false;
                _otherCallback?.Invoke(text);
                _otherSelected.OnNext(text);
                Invalidate();
                return true;
            }

            if (key.Key == ConsoleKey.Escape)
            {
                _isEditingOther = false;
                Invalidate();
                return true;
            }

            return _otherInput.HandleInput(key);
        }

        // Handle navigation and selection
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
                _highlightedIndex = _items.Count - 1;
                EnsureVisible();
                Invalidate();
                return true;

            case ConsoleKey.Spacebar:
                ToggleSelection();
                return true;

            case ConsoleKey.Enter:
                ConfirmSelection();
                return true;

            case ConsoleKey.Escape:
                _cancelled.OnNext(Unit.Default);
                return true;

            // Number keys for quick selection (1-9)
            case >= ConsoleKey.D1 and <= ConsoleKey.D9 when _showNumbers:
                var index = key.Key - ConsoleKey.D1;
                if (index < _items.Count)
                {
                    _highlightedIndex = index;
                    var selectedItem = _items[index];

                    // If selecting "Other", start text input immediately
                    if (selectedItem.IsOther)
                    {
                        StartOtherInput();
                    }
                    else if (_mode == SelectionMode.Single)
                    {
                        // In single mode, number key confirms immediately
                        SelectItem(index);
                        ConfirmSelection();
                    }
                    else
                    {
                        // In multi mode, number key toggles selection
                        ToggleSelection();
                    }
                }
                return true;

            default:
                return false;
        }
    }

    private void MoveHighlight(int delta)
    {
        if (_items.Count == 0)
            return;

        _highlightedIndex = Math.Clamp(_highlightedIndex + delta, 0, _items.Count - 1);
        EnsureVisible();

        // Auto-start text input when navigating to "Other" option
        var item = _items[_highlightedIndex];
        if (item.IsOther && !_isEditingOther)
        {
            StartOtherInput();
        }
        else
        {
            Invalidate();
        }
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

    private void ToggleSelection()
    {
        if (_highlightedIndex < 0 || _highlightedIndex >= _items.Count)
            return;

        var item = _items[_highlightedIndex];

        if (item.IsOther)
        {
            // Start editing "Other" text
            StartOtherInput();
            return;
        }

        if (_mode == SelectionMode.Single)
        {
            // Clear other selections
            foreach (var i in _items)
                i.IsSelected = false;
        }

        item.IsSelected = !item.IsSelected;
        Invalidate();
    }

    private void SelectItem(int index)
    {
        if (index < 0 || index >= _items.Count)
            return;

        if (_mode == SelectionMode.Single)
        {
            foreach (var i in _items)
                i.IsSelected = false;
        }

        _items[index].IsSelected = true;
        Invalidate();
    }

    private void StartOtherInput()
    {
        _otherInput ??= new TextInputNode()
            .WithPlaceholder("Enter custom value...");

        _otherInput.Clear();
        _isEditingOther = true;
        Invalidate();
    }

    private void ConfirmSelection()
    {
        if (_highlightedIndex >= 0 && _highlightedIndex < _items.Count)
        {
            var item = _items[_highlightedIndex];

            if (item.IsOther)
            {
                StartOtherInput();
                return;
            }

            // In single mode, always select the highlighted item on Enter
            if (_mode == SelectionMode.Single)
            {
                // Clear other selections first
                foreach (var i in _items)
                    i.IsSelected = false;
                item.IsSelected = true;
            }
        }

        var selected = _items
            .Where(i => i.IsSelected && !i.IsOther)
            .Select(i => i.Value)
            .ToList();

        _selectionConfirmed.OnNext(selected);
    }

    /// <inheritdoc />
    public Size Measure(Size available)
    {
        var height = Math.Min(_items.Count, _visibleRows);

        // Calculate width based on content
        var maxItemWidth = _items.Count > 0
            ? _items.Max(i => GetItemDisplayLength(i, _items.IndexOf(i)))
            : 10;

        var width = Math.Min(maxItemWidth + 2, available.Width);

        return new Size(width, height);
    }

    private int GetItemDisplayLength(SelectionItem<T> item, int index)
    {
        var prefix = GetItemPrefix(item, index);
        return prefix.Length + item.DisplayText.Length;
    }

    private string GetItemPrefix(SelectionItem<T> item, int index)
    {
        var parts = new List<string>();

        // Number prefix
        if (_showNumbers && index < 9)
        {
            parts.Add($"{index + 1}.");
        }

        // Checkbox for multi-select
        if (_mode == SelectionMode.Multi && !item.IsOther)
        {
            parts.Add(item.IsSelected ? "[x]" : "[ ]");
        }
        else if (_mode == SelectionMode.Single && item.IsSelected && !item.IsOther)
        {
            parts.Add("●");
        }

        return parts.Count > 0 ? string.Join(" ", parts) + " " : "";
    }

    /// <inheritdoc />
    public void Render(IRenderContext context, Rect bounds)
    {
        if (!bounds.HasArea || _items.Count == 0)
            return;

        var visibleCount = Math.Min(_visibleRows, bounds.Height);
        var needsScrollbar = _items.Count > visibleCount;
        var contentWidth = needsScrollbar ? bounds.Width - 1 : bounds.Width;

        for (var row = 0; row < visibleCount; row++)
        {
            var itemIndex = _scrollOffset + row;
            if (itemIndex >= _items.Count)
                break;

            var item = _items[itemIndex];

            // If this is the "Other" item and we're editing, render the text input instead
            if (item.IsOther && _isEditingOther && _otherInput != null)
            {
                RenderOtherInput(context, row, contentWidth, itemIndex);
            }
            else
            {
                var isHighlighted = itemIndex == _highlightedIndex && _hasFocus && !_isEditingOther;
                RenderItem(context, item, itemIndex, row, contentWidth, isHighlighted);
            }
        }

        // Render scrollbar if needed
        if (needsScrollbar)
        {
            RenderScrollbar(context, bounds.Width - 1, visibleCount);
        }
    }

    private void RenderOtherInput(IRenderContext context, int row, int width, int itemIndex)
    {
        if (_otherInput == null)
            return;

        // Render the number prefix (e.g., "4. ") in dimmed color
        var prefix = _showNumbers && itemIndex < 9 ? $"{itemIndex + 1}. " : "";
        if (prefix.Length > 0)
        {
            context.SetForeground(Color.BrightBlack);
            context.WriteAt(0, row, prefix);
            context.ResetColors();
        }

        // Render the text input after the prefix
        var inputX = prefix.Length;
        var inputWidth = Math.Max(1, width - inputX);
        var inputBounds = new Rect(inputX, row, inputWidth, 1);
        var inputContext = context.CreateSubContext(inputBounds);
        var innerBounds = new Rect(0, 0, inputWidth, 1);
        _otherInput.Render(inputContext, innerBounds);
    }

    private void RenderItem(IRenderContext context, SelectionItem<T> item, int index, int row,
        int width, bool isHighlighted)
    {
        // Set colors
        if (isHighlighted)
        {
            context.SetForeground(_highlightForeground);
            context.SetBackground(_highlightBackground);
        }
        else if (item.IsSelected && _selectedForeground.HasValue)
        {
            context.SetForeground(_selectedForeground.Value);
        }
        else if (_foreground.HasValue)
        {
            context.SetForeground(_foreground.Value);
        }

        // Build display string
        var prefix = GetItemPrefix(item, index);
        var displayText = prefix + item.DisplayText;

        // Truncate if needed
        if (displayText.Length > width)
        {
            displayText = displayText[..(width - 1)] + "…";
        }

        // Pad to full width for highlight background
        if (isHighlighted)
        {
            displayText = displayText.PadRight(width);
        }

        context.WriteAt(0, row, displayText);
        context.ResetColors();
    }

    private void RenderScrollbar(IRenderContext context, int x, int height)
    {
        if (_items.Count <= height)
            return;

        var thumbSize = Math.Max(1, height * height / _items.Count);
        var thumbPos = _scrollOffset * (height - thumbSize) / (_items.Count - height);

        context.SetForeground(Color.BrightBlack);

        for (var row = 0; row < height; row++)
        {
            var isThumb = row >= thumbPos && row < thumbPos + thumbSize;
            context.WriteAt(x, row, isThumb ? '█' : '░');
        }

        context.ResetColors();
    }

    private void Invalidate()
    {
        if (!_disposed)
            _invalidated.OnNext(Unit.Default);
    }

    /// <summary>
    /// Called when the node becomes active (page navigated to).
    /// Activates the embedded TextInputNode if it exists.
    /// </summary>
    public void OnActivate()
    {
        // Activate the embedded "Other" input if it exists
        _otherInput?.OnActivate();
    }

    /// <summary>
    /// Called when the node becomes inactive (navigating away from page).
    /// Deactivates the embedded TextInputNode to conserve resources.
    /// </summary>
    public void OnDeactivate()
    {
        // Deactivate the embedded "Other" input if it exists
        _otherInput?.OnDeactivate();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _invalidated.OnCompleted();
        _invalidated.Dispose();
        _selectionConfirmed.OnCompleted();
        _selectionConfirmed.Dispose();
        _otherSelected.OnCompleted();
        _otherSelected.Dispose();
        _cancelled.OnCompleted();
        _cancelled.Dispose();

        _otherInput?.Dispose();
    }
}
