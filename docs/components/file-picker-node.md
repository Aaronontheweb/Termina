# FilePickerNode

An interactive file and folder picker with breadcrumb navigation, keyboard-driven directory traversal, scrolling, and type-to-filter search. Supports file-only, directory-only, or mixed selection in single or multi-select modes.

![FilePickerNode demo: browsing directories, filtering, and selecting files and folders](/gallery/gallery-file-picker.gif)

*Side-by-side file and folder pickers in the gallery — navigation, filtering, and selection.*

## Basic Usage

```csharp
// Pick a single file, starting in the current directory
// (or pass a path: Layouts.FilePicker("/home/user/projects"))
var picker = Layouts.FilePicker()
    .WithMode(FilePickerMode.Files)
    .WithSelectionMode(FilePickerSelectionMode.Single);

picker.SelectionConfirmed.Subscribe(paths => {
    var file = paths[0];
    Console.WriteLine($"Selected: {file}");
});

picker.Cancelled.Subscribe(_ => {
    // User pressed Escape — close the picker
});
```

The picker loads its directory lazily on first render or focus, so constructing one is cheap.

## Features

- Breadcrumb header showing the current directory
- Enter to descend into folders, Backspace to go up
- Type-to-filter: any printable character (or `/`) opens an inline filter bar
- File-only, directory-only, or mixed selection modes
- Single and multi-select (Space toggles, Enter confirms)
- Optional glob filter (e.g. `*.cs`) applied to files
- Hidden-file filtering (dotfiles excluded by default)
- Scrolling with scrollbar for large directories
- Pluggable `IFileSystemProvider` for deterministic testing

## Picker Modes

`FilePickerMode` controls which entries are *selectable*. Directories are always shown for navigation (except files are hidden entirely in `Directories` mode):

```csharp
// Only files can be selected; folders are navigation-only
Layouts.FilePicker().WithMode(FilePickerMode.Files);

// Only folders are shown and selectable (Space selects, Enter opens)
Layouts.FilePicker().WithMode(FilePickerMode.Directories);

// Both files and folders are selectable
Layouts.FilePicker().WithMode(FilePickerMode.All);
```

Because Enter always opens a directory, **Space** is the selection gesture for folders. This keeps deep navigation possible in directory-only mode.

## Selection Modes

### Single Select (Default)

```csharp
var picker = Layouts.FilePicker("/home/user/projects")
    .WithSelectionMode(FilePickerSelectionMode.Single);

picker.SelectionConfirmed.Subscribe(paths => {
    OpenFile(paths[0]);
});
```

- Enter on a file confirms it immediately.
- Space on a selectable entry (per the picker mode) also confirms it.

### Multi-Select

```csharp
var picker = Layouts.FilePicker()
    .WithSelectionMode(FilePickerSelectionMode.Multi);

picker.SelectionConfirmed.Subscribe(paths => {
    foreach (var path in paths)
        Stage(path);
});
```

- Space toggles the highlighted entry and moves the cursor down.
- Enter confirms all toggled entries. If the highlighted entry is a selectable file, it is included in the confirmation.
- Enter on a directory with toggled items confirms the toggled set; with nothing toggled it navigates into the directory.
- Selections are scoped to the current directory — navigating clears any toggled items.

## Keyboard Shortcuts

### Browsing

| Key | Action |
|-----|--------|
| `↑/↓` | Move highlight |
| `Home` / `End` | Jump to first/last entry |
| `Enter` | Open directory / confirm selection |
| `Backspace` | Go up one directory |
| `Space` | Select (single) or toggle (multi) |
| `/` or any letter | Open the filter bar |
| `Escape` | Cancel the picker (emits `Cancelled`) |

### Filtering

| Key | Action |
|-----|--------|
| Printable keys | Type into the filter (spaces allowed) |
| `↑/↓` / `Home` / `End` | Navigate the filtered list |
| `Enter` | Open / confirm the highlighted match |
| `Backspace` | Delete a character (exits filter when empty) |
| `Escape` | Clear the filter and return to browsing |

The filter is a case-insensitive substring match against entry names. Filtering resets when the picker loses focus or the user navigates to another directory.

## Glob File Filter

Restrict the files shown with a glob pattern. Directories always pass through so navigation still works:

```csharp
var picker = Layouts.FilePicker()
    .WithFileFilter("*.cs");   // only .cs files are listed
```

## Hidden Files

Dotfiles (names starting with `.`) are hidden by default:

```csharp
var picker = Layouts.FilePicker()
    .WithShowHidden();   // include .gitignore, .config, etc.
```

## Sizing

By default the picker auto-sizes with a maximum of 10 visible rows. Use `WithFillHeight()` to expand into the parent's available space, or `WithVisibleRows()` for a fixed row count:

```csharp
// Fill a panel
var picker = Layouts.FilePicker()
    .WithFillHeight();

// Or a fixed window of 15 rows
var picker = Layouts.FilePicker()
    .WithVisibleRows(15);
```

## Styling

```csharp
Layouts.FilePicker()
    .WithHighlightColors(Color.Black, Color.Cyan)  // Highlighted row colors
    .WithDirectoryColor(Color.BrightCyan)          // Directory entry color
    .WithFileColor(Color.Default);                 // File entry color
```

## Testing with IFileSystemProvider

The picker reads the filesystem through `IFileSystemProvider`, so tests can supply a fake provider and never touch the disk:

```csharp
public class FakeFileSystemProvider : IFileSystemProvider
{
    private readonly Dictionary<string, List<FileSystemEntry>> _dirs = new();

    public FakeFileSystemProvider AddDirectory(string path, params FileSystemEntry[] entries)
    {
        _dirs[path] = entries.ToList();
        return this;
    }

    public IReadOnlyList<FileSystemEntry> GetEntries(string path) =>
        _dirs.TryGetValue(path, out var e) ? e : [];

    public bool DirectoryExists(string path) => _dirs.ContainsKey(path);

    public string? GetParentDirectory(string path)
    {
        var i = path.LastIndexOf('/');
        return i <= 0 ? (i == 0 ? "/" : null) : path[..i];
    }
}

[Fact]
public void Enter_On_Directory_NavigatesInto()
{
    var fs = new FakeFileSystemProvider()
        .AddDirectory("/root", new FileSystemEntry("src", "/root/src", true))
        .AddDirectory("/root/src");

    using var picker = new FilePickerNode("/root").WithFileSystemProvider(fs);
    picker.OnFocused();

    picker.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));

    Assert.Equal("/root/src", picker.CurrentPath);
}
```

The default provider (`DefaultFileSystemProvider`) wraps `System.IO`, skips entries it cannot access (`UnauthorizedAccessException`, `IOException`), and sorts directories first, then files, both alphabetically.

## Complete Example

The recommended pattern — **ViewModel handles state**, **Page owns nodes and Focus**:

**ViewModel:**

```csharp
public class OpenFileViewModel : ReactiveViewModel
{
    public ReactiveProperty<string> StatusMessage { get; } = new("Pick a file");

    public void OnFileSelected(IReadOnlyList<string> paths)
    {
        StatusMessage.Value = $"Opened {paths[0]}";
        // load the file...
    }

    public override void Dispose()
    {
        StatusMessage.Dispose();
        base.Dispose();
    }
}
```

**Page:**

```csharp
public class OpenFilePage : ReactivePage<OpenFileViewModel>
{
    private FilePickerNode _picker = null!;

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        // Don't register Escape at the page level — the picker needs it
        // for clearing filters. Use the Cancelled observable instead.
        _picker.SelectionConfirmed
            .Subscribe(paths => ViewModel.OnFileSelected(paths))
            .DisposeWith(Subscriptions);

        _picker.Cancelled
            .Subscribe(_ => Navigate("/menu"))
            .DisposeWith(Subscriptions);

        Focus.SetFocus(_picker);
    }

    public override ILayoutNode BuildLayout()
    {
        _picker = Layouts.FilePicker(Environment.CurrentDirectory)
            .WithMode(FilePickerMode.Files)
            .WithFileFilter("*.json")
            .WithFillHeight();

        return Layouts.Vertical()
            .WithChild(_picker)
            .WithChild(
                ViewModel.StatusMessage
                    .Select<string, ILayoutNode>(msg => new TextNode(msg))
                    .AsLayout()
                    .Height(1));
    }
}
```

::: warning Escape and page key bindings
Page-level `KeyBindings` run in the capture phase, *before* the focused component sees the key. Registering Escape at the page level will prevent the picker from clearing its filter. Subscribe to `Cancelled` instead.
:::

## Observables

| Observable | Type | Description |
|------------|------|-------------|
| `SelectionConfirmed` | `Observable<IReadOnlyList<string>>` | Emits selected full paths on confirmation |
| `Cancelled` | `Observable<Unit>` | Emits when Escape is pressed while browsing |
| `DirectoryChanged` | `Observable<string>` | Emits the new path on every directory load (including the initial one) |
| `Invalidated` | `Observable<Unit>` | Emits when a redraw is needed |

## API Reference

### Constructor

| Constructor | Description |
|-------------|-------------|
| `FilePickerNode(string? startPath = null)` | Create a picker; defaults to `Environment.CurrentDirectory` |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CurrentPath` | `string` | The directory currently displayed |
| `CanFocus` | `bool` | Always `true` |
| `HasFocus` | `bool` | Whether the picker has focus |
| `FocusPriority` | `int` | `10` (lower than modal) |

### Fluent Methods

| Method | Description |
|--------|-------------|
| `.WithStartPath(string)` | Set the initial directory |
| `.WithMode(FilePickerMode)` | What is selectable: `Files`, `Directories`, or `All` |
| `.WithSelectionMode(FilePickerSelectionMode)` | `Single` or `Multi` |
| `.WithShowHidden(bool)` | Include dotfiles (default `false`) |
| `.WithFileFilter(string?)` | Glob pattern applied to files (e.g. `*.cs`) |
| `.WithHighlightColors(fg, bg)` | Highlighted row colors |
| `.WithDirectoryColor(Color)` | Directory entry color |
| `.WithFileColor(Color)` | File entry color |
| `.WithVisibleRows(int)` | Fixed visible row count (disables fill) |
| `.WithFillHeight(bool)` | Fill available vertical space |
| `.WithFileSystemProvider(IFileSystemProvider)` | Override filesystem access (testing) |

### Enums

**FilePickerMode**
| Value | Description |
|-------|-------------|
| `Files` | Only files are selectable; directories are navigation-only |
| `Directories` | Only directories are shown and selectable |
| `All` | Both files and directories are selectable |

**FilePickerSelectionMode**
| Value | Description |
|-------|-------------|
| `Single` | Confirm exactly one entry |
| `Multi` | Toggle entries with Space, confirm with Enter |

### FileSystemEntry

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Entry name (no path) |
| `FullPath` | `string` | Absolute path |
| `IsDirectory` | `bool` | Whether the entry is a directory |
| `Size` | `long?` | File size in bytes (null for directories) |
| `LastModified` | `DateTimeOffset?` | Last write time |

## Source Code

::: details View FilePickerNode implementation
<<< @/../src/Termina/Layout/FilePickerNode.cs{csharp}
:::

::: details View IFileSystemProvider implementation
<<< @/../src/Termina/Layout/IFileSystemProvider.cs{csharp}
:::

::: details View FileSystemEntry implementation
<<< @/../src/Termina/Layout/FileSystemEntry.cs{csharp}
:::
