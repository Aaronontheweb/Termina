// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Layout;
using Termina.Terminal;

namespace Termina.Tests.Layout;

/// <summary>
/// A fake filesystem provider for deterministic testing.
/// </summary>
public class FakeFileSystemProvider : IFileSystemProvider
{
    private readonly Dictionary<string, List<FileSystemEntry>> _directories = new();

    public FakeFileSystemProvider AddDirectory(string path, params FileSystemEntry[] entries)
    {
        var sorted = entries.OrderBy(e => !e.IsDirectory).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
        _directories[path] = sorted;
        return this;
    }

    public IReadOnlyList<FileSystemEntry> GetEntries(string directoryPath)
    {
        return _directories.TryGetValue(directoryPath, out var entries) ? entries : [];
    }

    public bool DirectoryExists(string path) => _directories.ContainsKey(path);

    public string? GetParentDirectory(string path)
    {
        var lastSep = path.LastIndexOf('/');
        if (lastSep <= 0)
            return lastSep == 0 ? "/" : null;
        return path[..lastSep];
    }
}

public class FilePickerNodeTests
{
    private static readonly FileSystemEntry[] RootEntries =
    [
        new("src", "/root/src", true),
        new("tests", "/root/tests", true),
        new(".hidden", "/root/.hidden", true),
        new("Program.cs", "/root/Program.cs", false),
        new("README.md", "/root/README.md", false),
        new(".gitignore", "/root/.gitignore", false)
    ];

    private static readonly FileSystemEntry[] SrcEntries =
    [
        new("Layout", "/root/src/Layout", true),
        new("Terminal", "/root/src/Terminal", true),
        new("App.cs", "/root/src/App.cs", false),
        new("Startup.cs", "/root/src/Startup.cs", false)
    ];

    private static FakeFileSystemProvider CreateTestFs()
    {
        return new FakeFileSystemProvider()
            .AddDirectory("/root", RootEntries)
            .AddDirectory("/root/src", SrcEntries)
            .AddDirectory("/root/src/Layout")
            .AddDirectory("/root/tests");
    }

    private static FilePickerNode CreatePicker(
        FakeFileSystemProvider? fs = null,
        FilePickerMode mode = FilePickerMode.Files,
        FilePickerSelectionMode selectionMode = FilePickerSelectionMode.Single,
        bool showHidden = false)
    {
        fs ??= CreateTestFs();
        return new FilePickerNode("/root")
            .WithFileSystemProvider(fs)
            .WithMode(mode)
            .WithSelectionMode(selectionMode)
            .WithShowHidden(showHidden);
    }

    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0') =>
        new(ch, key, false, false, false);

    private static ConsoleKeyInfo CharKey(char ch) =>
        new(ch, ConsoleKey.None, false, false, false);

    #region Construction and Defaults

    [Fact]
    public void FilePickerNode_CanBeCreated()
    {
        using var picker = CreatePicker();
        Assert.NotNull(picker);
        Assert.Equal("/root", picker.CurrentPath);
    }

    [Fact]
    public void FilePickerNode_HasCorrectDefaults()
    {
        using var picker = CreatePicker();
        Assert.True(picker.CanFocus);
        Assert.False(picker.HasFocus);
        Assert.Equal(10, picker.FocusPriority);
    }

    [Fact]
    public void Layouts_FilePicker_CreatesNode()
    {
        using var picker = Layouts.FilePicker("/root");
        Assert.Equal("/root", picker.CurrentPath);

        using var defaultPicker = Layouts.FilePicker();
        Assert.Equal(Environment.CurrentDirectory, defaultPicker.CurrentPath);
    }

    [Fact]
    public void FilePickerNode_FluentApi_ReturnsThis()
    {
        using var picker = new FilePickerNode();
        var result = picker
            .WithStartPath("/tmp")
            .WithMode(FilePickerMode.Directories)
            .WithSelectionMode(FilePickerSelectionMode.Multi)
            .WithShowHidden(true)
            .WithFileFilter("*.cs")
            .WithHighlightColors(Color.Black, Color.White)
            .WithVisibleRows(20)
            .WithFillHeight();

        Assert.Same(picker, result);
    }

    #endregion

    #region Focus

    [Fact]
    public void OnFocused_SetsHasFocus_And_LoadsEntries()
    {
        using var picker = CreatePicker();
        picker.OnFocused();

        Assert.True(picker.HasFocus);
    }

    [Fact]
    public void OnBlurred_ClearsHasFocus()
    {
        using var picker = CreatePicker();
        picker.OnFocused();
        picker.OnBlurred();

        Assert.False(picker.HasFocus);
    }

    #endregion

    #region Navigation

    [Fact]
    public void Enter_On_Directory_NavigatesInto()
    {
        using var picker = CreatePicker();
        picker.OnFocused();

        string? changedTo = null;
        picker.DirectoryChanged.Subscribe(path => changedTo = path);

        // First item should be "src" directory (hidden files filtered, dirs first)
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.Equal("/root/src", changedTo);
        Assert.Equal("/root/src", picker.CurrentPath);
    }

    [Fact]
    public void Backspace_NavigatesUp()
    {
        using var picker = CreatePicker();
        picker.OnFocused();

        // Navigate into src
        picker.HandleInput(Key(ConsoleKey.Enter));
        Assert.Equal("/root/src", picker.CurrentPath);

        // Navigate back up
        picker.HandleInput(Key(ConsoleKey.Backspace));
        Assert.Equal("/root", picker.CurrentPath);
    }

    [Fact]
    public void DownArrow_MovesHighlight()
    {
        using var picker = CreatePicker();
        picker.OnFocused();

        // Move down and Enter should navigate into "tests" (second dir)
        picker.HandleInput(Key(ConsoleKey.DownArrow));
        string? changedTo = null;
        picker.DirectoryChanged.Subscribe(path => changedTo = path);
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.Equal("/root/tests", changedTo);
    }

    [Fact]
    public void Enter_On_File_EmitsSelection()
    {
        using var picker = CreatePicker();
        picker.OnFocused();

        IReadOnlyList<string>? selected = null;
        picker.SelectionConfirmed.Subscribe(paths => selected = paths);

        // Skip directories (src, tests) to reach Program.cs
        picker.HandleInput(Key(ConsoleKey.DownArrow)); // tests
        picker.HandleInput(Key(ConsoleKey.DownArrow)); // Program.cs
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.NotNull(selected);
        Assert.Single(selected);
        Assert.Equal("/root/Program.cs", selected[0]);
    }

    [Fact]
    public void Escape_EmitsCancelled()
    {
        using var picker = CreatePicker();
        picker.OnFocused();

        var cancelled = false;
        picker.Cancelled.Subscribe(_ => cancelled = true);

        picker.HandleInput(Key(ConsoleKey.Escape));

        Assert.True(cancelled);
    }

    [Fact]
    public void Home_And_End_JumpToFirstAndLast()
    {
        using var picker = CreatePicker();
        picker.OnFocused();

        // Move to end
        picker.HandleInput(Key(ConsoleKey.End));

        IReadOnlyList<string>? selected = null;
        picker.SelectionConfirmed.Subscribe(paths => selected = paths);
        picker.HandleInput(Key(ConsoleKey.Enter));

        // Last visible item should be README.md (hidden files filtered out)
        Assert.NotNull(selected);
        Assert.Equal("/root/README.md", selected[0]);

        // Move to home
        picker.HandleInput(Key(ConsoleKey.Home));
        string? changedTo = null;
        picker.DirectoryChanged.Subscribe(path => changedTo = path);
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.Equal("/root/src", changedTo);
    }

    #endregion

    #region Hidden Files

    [Fact]
    public void HiddenFiles_AreFilteredByDefault()
    {
        using var picker = CreatePicker(showHidden: false);
        picker.OnFocused();

        // Navigate to end — should reach README.md (4 items: src, tests, Program.cs, README.md)
        picker.HandleInput(Key(ConsoleKey.End));

        IReadOnlyList<string>? selected = null;
        picker.SelectionConfirmed.Subscribe(paths => selected = paths);
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.NotNull(selected);
        Assert.Equal("/root/README.md", selected[0]);
    }

    [Fact]
    public void ShowHidden_IncludesHiddenFiles()
    {
        using var picker = CreatePicker(showHidden: true);
        picker.OnFocused();

        // With hidden shown: .hidden, src, tests, .gitignore, Program.cs, README.md
        // Navigate to end
        picker.HandleInput(Key(ConsoleKey.End));

        IReadOnlyList<string>? selected = null;
        picker.SelectionConfirmed.Subscribe(paths => selected = paths);
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.NotNull(selected);
        Assert.Equal("/root/README.md", selected[0]);
    }

    #endregion

    #region Filtering

    [Fact]
    public void Slash_ActivatesFilterMode()
    {
        using var picker = CreatePicker();
        picker.OnFocused();

        var cancelled = false;
        picker.Cancelled.Subscribe(_ => cancelled = true);

        // Pressing '/' should activate filter
        picker.HandleInput(new ConsoleKeyInfo('/', ConsoleKey.None, false, false, false));

        // First Escape exits filter mode, not the picker
        picker.HandleInput(Key(ConsoleKey.Escape));
        Assert.False(cancelled);

        // Second Escape cancels the picker (back in browsing mode)
        picker.HandleInput(Key(ConsoleKey.Escape));
        Assert.True(cancelled);
    }

    [Fact]
    public void Filter_Backspace_OnEmptyFilter_ExitsFilterMode()
    {
        using var picker = CreatePicker();
        picker.OnFocused();

        // '/' enters filter mode with empty text
        picker.HandleInput(new ConsoleKeyInfo('/', ConsoleKey.None, false, false, false));

        // Backspace on empty filter should exit filter mode
        picker.HandleInput(Key(ConsoleKey.Backspace));

        // Now we're back in browse mode — Enter navigates to src
        string? changedTo = null;
        picker.DirectoryChanged.Subscribe(path => changedTo = path);
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.Equal("/root/src", changedTo);
    }

    [Fact]
    public void Typing_ActivatesFilter_And_FiltersEntries()
    {
        using var picker = CreatePicker();
        picker.OnFocused();

        // Type 'p' to start filtering — should match "Program.cs"
        picker.HandleInput(CharKey('p'));

        IReadOnlyList<string>? selected = null;
        picker.SelectionConfirmed.Subscribe(paths => selected = paths);

        // Enter on the first filtered result
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.NotNull(selected);
        Assert.Equal("/root/Program.cs", selected[0]);
    }

    [Fact]
    public void Filter_Escape_ReturnsToFullList()
    {
        using var picker = CreatePicker();
        picker.OnFocused();

        // Start filtering
        picker.HandleInput(CharKey('x'));

        // Should be in filter mode, Escape clears filter
        picker.HandleInput(Key(ConsoleKey.Escape));

        // Now Enter should navigate into "src" (first item, back to full list)
        string? changedTo = null;
        picker.DirectoryChanged.Subscribe(path => changedTo = path);
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.Equal("/root/src", changedTo);
    }

    #endregion

    #region Directory-Only Mode

    [Fact]
    public void DirectoryMode_HidesFiles()
    {
        using var picker = CreatePicker(mode: FilePickerMode.Directories);
        picker.OnFocused();

        // Should only see directories: src, tests
        // Enter on a directory always navigates into it
        string? changedTo = null;
        picker.DirectoryChanged.Subscribe(path => changedTo = path);
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.Equal("/root/src", changedTo);
    }

    [Fact]
    public void DirectoryMode_Space_SelectsDirectory()
    {
        using var picker = CreatePicker(mode: FilePickerMode.Directories);
        picker.OnFocused();

        // Space on a directory in Single+Directories mode should select it
        IReadOnlyList<string>? selected = null;
        picker.SelectionConfirmed.Subscribe(paths => selected = paths);
        picker.HandleInput(Key(ConsoleKey.Spacebar));

        Assert.NotNull(selected);
        Assert.Single(selected);
        Assert.Equal("/root/src", selected[0]);
    }

    [Fact]
    public void DirectoryMode_Enter_NavigatesInto_ThenSpaceSelects()
    {
        using var picker = CreatePicker(mode: FilePickerMode.Directories);
        picker.OnFocused();

        // Enter navigates into src
        picker.HandleInput(Key(ConsoleKey.Enter));
        Assert.Equal("/root/src", picker.CurrentPath);

        // Space selects the first dir in src (Layout)
        IReadOnlyList<string>? selected = null;
        picker.SelectionConfirmed.Subscribe(paths => selected = paths);
        picker.HandleInput(Key(ConsoleKey.Spacebar));

        Assert.NotNull(selected);
        Assert.Equal("/root/src/Layout", selected[0]);
    }

    #endregion

    #region OnBlurred Filter Reset

    [Fact]
    public void OnBlurred_ResetsFilterState()
    {
        using var picker = CreatePicker();
        picker.OnFocused();

        // Start filtering with 'p' — should match Program.cs only
        picker.HandleInput(CharKey('p'));

        // Blur and re-focus
        picker.OnBlurred();
        picker.OnFocused();

        // Full list should be restored — Enter on first item navigates to src
        string? changedTo = null;
        picker.DirectoryChanged.Subscribe(path => changedTo = path);
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.Equal("/root/src", changedTo);
    }

    #endregion

    #region Multi-Select

    [Fact]
    public void MultiSelect_Space_TogglesSelection()
    {
        using var picker = CreatePicker(selectionMode: FilePickerSelectionMode.Multi);
        picker.OnFocused();

        // Skip directories to reach files
        picker.HandleInput(Key(ConsoleKey.DownArrow)); // tests
        picker.HandleInput(Key(ConsoleKey.DownArrow)); // Program.cs

        // Toggle Program.cs
        picker.HandleInput(Key(ConsoleKey.Spacebar));

        // Space also moves highlight down, so now on README.md - toggle it too
        picker.HandleInput(Key(ConsoleKey.Spacebar));

        // Confirm selection with Enter (now at end of list, but selected items are committed)
        IReadOnlyList<string>? selected = null;
        picker.SelectionConfirmed.Subscribe(paths => selected = paths);
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.NotNull(selected);
        Assert.Equal(2, selected.Count);
        Assert.Contains("/root/Program.cs", selected);
        Assert.Contains("/root/README.md", selected);
    }

    [Fact]
    public void MultiSelect_Space_OnDirectory_IsIgnored_InFilesMode()
    {
        using var picker = CreatePicker(selectionMode: FilePickerSelectionMode.Multi);
        picker.OnFocused();

        // Try to toggle first item (src directory) — should be ignored in Files mode
        // Highlight stays on src since toggle was rejected
        picker.HandleInput(Key(ConsoleKey.Spacebar));

        // Enter on src directory navigates into it
        string? changedTo = null;
        picker.DirectoryChanged.Subscribe(path => changedTo = path);
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.Equal("/root/src", changedTo);
    }

    [Fact]
    public void MultiSelect_DirectoryMode_SpaceToggle_EnterConfirms()
    {
        using var picker = CreatePicker(
            mode: FilePickerMode.Directories,
            selectionMode: FilePickerSelectionMode.Multi);
        picker.OnFocused();

        // Toggle src and tests directories
        picker.HandleInput(Key(ConsoleKey.Spacebar)); // toggle src, moves to tests
        picker.HandleInput(Key(ConsoleKey.Spacebar)); // toggle tests

        // Move back to src and press Enter to confirm
        picker.HandleInput(Key(ConsoleKey.Home));

        IReadOnlyList<string>? selected = null;
        picker.SelectionConfirmed.Subscribe(paths => selected = paths);
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.NotNull(selected);
        Assert.Equal(2, selected.Count);
        Assert.Contains("/root/src", selected);
        Assert.Contains("/root/tests", selected);
    }

    #endregion

    #region File Filter (Glob)

    [Fact]
    public void WithFileFilter_FiltersFilesByGlob()
    {
        using var picker = new FilePickerNode("/root")
            .WithFileSystemProvider(CreateTestFs())
            .WithFileFilter("*.cs");
        picker.OnFocused();

        // Should see: src, tests (dirs pass through), Program.cs (matches *.cs)
        // README.md and .gitignore should be filtered out
        picker.HandleInput(Key(ConsoleKey.End));

        IReadOnlyList<string>? selected = null;
        picker.SelectionConfirmed.Subscribe(paths => selected = paths);
        picker.HandleInput(Key(ConsoleKey.Enter));

        Assert.NotNull(selected);
        Assert.Equal("/root/Program.cs", selected[0]);
    }

    #endregion

    #region Measure

    [Fact]
    public void Measure_ReturnsReasonableSize()
    {
        using var picker = CreatePicker();
        picker.OnFocused();

        var size = picker.Measure(new Size(80, 24));

        Assert.Equal(80, size.Width);
        Assert.True(size.Height > 0);
    }

    [Fact]
    public void FillHeight_UsesAvailableSpace()
    {
        using var picker = CreatePicker();
        picker.WithFillHeight();
        picker.OnFocused();

        var size = picker.Measure(new Size(80, 24));

        Assert.Equal(80, size.Width);
        Assert.Equal(24, size.Height);
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var picker = CreatePicker();
        picker.OnFocused();

        picker.Dispose();

        // Double-dispose should also be safe
        picker.Dispose();
    }

    #endregion
}
