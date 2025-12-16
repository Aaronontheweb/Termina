# SelectionListNode

An interactive list selection component with keyboard navigation, supporting single and multi-select modes.

## Basic Usage

```csharp
// String list with factory method
var list = Layouts.SelectionList("Option 1", "Option 2", "Option 3");

// Or from an enumerable
var list = Layouts.SelectionList(options);

// Typed list with custom display
var list = Layouts.SelectionList(items, item => item.Name);
```

## Features

- Arrow key navigation (Up/Down)
- Home/End to jump to first/last item
- Space to toggle selection (multi-select mode)
- Enter to confirm selection
- Number keys (1-9) for quick selection
- Escape to cancel
- Scrolling for long lists
- Optional "Other" option for custom text input

## Selection Modes

### Single Select (Default)

Only one item can be selected at a time:

```csharp
var list = Layouts.SelectionList("Low", "Medium", "High")
    .WithMode(SelectionMode.Single);

list.SelectionConfirmed.Subscribe(selected => {
    var choice = selected.FirstOrDefault();
    Console.WriteLine($"Selected: {choice}");
});
```

### Multi-Select

Multiple items can be selected with Space, confirmed with Enter:

```csharp
var list = Layouts.SelectionList("Feature A", "Feature B", "Feature C")
    .WithMode(SelectionMode.Multi);

list.SelectionConfirmed.Subscribe(selected => {
    foreach (var item in selected)
    {
        Console.WriteLine($"Enabled: {item}");
    }
});
```

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `↑/↓` | Move highlight |
| `Home` | Jump to first item |
| `End` | Jump to last item |
| `Space` | Toggle selection (multi-select) |
| `Enter` | Confirm selection |
| `1-9` | Quick select by number |
| `Escape` | Cancel |

## "Other" Option for Custom Input

Add a custom text input option that appears at the end of the list:

```csharp
var list = Layouts.SelectionList("High", "Medium", "Low")
    .WithOtherOption("Custom priority...");

// Handle standard selections
list.SelectionConfirmed.Subscribe(selected => {
    ProcessPriority(selected.First());
});

// Handle custom "Other" input
list.OtherSelected.Subscribe(customText => {
    ProcessPriority(customText);
});
```

When the user navigates to or selects the "Other" option, a text input appears immediately inline, allowing them to type without pressing Enter first.

## Styling

```csharp
Layouts.SelectionList("A", "B", "C")
    .WithHighlightColors(Color.Black, Color.Cyan)  // Highlighted row colors
    .WithForeground(Color.White)                    // Default text color
    .WithSelectedForeground(Color.Green)            // Checked items color
    .WithShowNumbers(true)                          // Show 1. 2. 3. prefixes
    .WithVisibleRows(5)                             // Max visible before scrolling
```

## With Typed Items

Use custom types with a display selector:

```csharp
public record Priority(int Level, string Name);

var priorities = new[]
{
    new Priority(1, "Critical"),
    new Priority(2, "High"),
    new Priority(3, "Medium"),
    new Priority(4, "Low")
};

var list = Layouts.SelectionList(priorities, p => p.Name)
    .WithMode(SelectionMode.Single);

list.SelectionConfirmed.Subscribe(selected => {
    Priority priority = selected.First();
    Console.WriteLine($"Selected level {priority.Level}");
});
```

## Inside a Modal

SelectionListNode works seamlessly with ModalNode:

```csharp
// Create selection list
var priorityList = Layouts.SelectionList("High", "Medium", "Low")
    .WithMode(SelectionMode.Single)
    .WithShowNumbers(true)
    .WithHighlightColors(Color.Black, Color.Cyan)
    .WithOtherOption("Custom...");

// Create modal with selection list
var modal = Layouts.Modal()
    .WithTitle("Select Priority")
    .WithBorder(BorderStyle.Rounded)
    .WithBorderColor(Color.Yellow)
    .WithBackdrop(BackdropStyle.Dim)
    .WithContent(priorityList);

// Handle selections
priorityList.SelectionConfirmed.Subscribe(selected => {
    HandleSelection(selected.First());
    Focus.PopFocus();
});

priorityList.OtherSelected.Subscribe(custom => {
    HandleSelection(custom);
    Focus.PopFocus();
});

priorityList.Cancelled.Subscribe(_ => {
    Focus.PopFocus();
});

// Show modal
Focus.PushFocus(modal);
Focus.PushFocus(priorityList);  // List needs focus for keyboard input
```

## Complete Example

```csharp
public partial class SettingsViewModel : ReactiveViewModel
{
    [Reactive] private bool _showThemeSelector;

    private SelectionListNode<string>? _themeList;
    private ModalNode? _themeModal;

    public ModalNode? ThemeModal => _themeModal;

    public override void OnActivated()
    {
        _themeList = Layouts.SelectionList("Light", "Dark", "System Default")
            .WithMode(SelectionMode.Single)
            .WithShowNumbers(true)
            .WithHighlightColors(Color.Black, Color.White);

        _themeList.SelectionConfirmed
            .Subscribe(selected => {
                ApplyTheme(selected.First());
                HideThemeSelector();
            })
            .DisposeWith(Subscriptions);

        _themeList.Cancelled
            .Subscribe(_ => HideThemeSelector())
            .DisposeWith(Subscriptions);

        _themeModal = Layouts.Modal()
            .WithTitle("Choose Theme")
            .WithBorder(BorderStyle.Rounded)
            .WithContent(_themeList);

        _themeModal.Dismissed
            .Subscribe(_ => HideThemeSelector())
            .DisposeWith(Subscriptions);
    }

    private void ShowThemeSelector()
    {
        ShowThemeSelector = true;
        Focus.PushFocus(_themeModal!);
        Focus.PushFocus(_themeList!);
    }

    private void HideThemeSelector()
    {
        ShowThemeSelector = false;
        Focus.ClearFocus();
    }
}
```

## Observables

| Observable | Type | Description |
|------------|------|-------------|
| `SelectionConfirmed` | `IObservable<IReadOnlyList<T>>` | Emits selected items on Enter |
| `OtherSelected` | `IObservable<string>` | Emits custom text from "Other" option |
| `Cancelled` | `IObservable<Unit>` | Emits when Escape is pressed |
| `Invalidated` | `IObservable<Unit>` | Emits when redraw is needed |

## API Reference

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Items` | `IReadOnlyList<SelectionItem<T>>` | All items in the list |
| `SelectedItems` | `IReadOnlyList<T>` | Currently selected items |
| `HighlightedItem` | `SelectionItem<T>?` | Currently highlighted item |
| `CanFocus` | `bool` | Always `true` |
| `HasFocus` | `bool` | Whether list has focus |
| `FocusPriority` | `int` | `10` (lower than modal) |

### Fluent Methods

| Method | Description |
|--------|-------------|
| `.WithMode(SelectionMode)` | Set Single or Multi select mode |
| `.WithHighlightColors(fg, bg)` | Set highlight row colors |
| `.WithForeground(Color)` | Set default text color |
| `.WithSelectedForeground(Color)` | Set checked item color |
| `.WithShowNumbers(bool)` | Show/hide number prefixes |
| `.WithVisibleRows(int)` | Set max visible rows before scroll |
| `.WithOtherOption(string, Action?)` | Add custom input option |

### SelectionItem Properties

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `T` | The item value |
| `DisplayText` | `string` | Text shown in the list |
| `IsSelected` | `bool` | Whether item is selected |
| `IsOther` | `bool` | Whether this is the "Other" option |

### Enums

**SelectionMode**
| Value | Description |
|-------|-------------|
| `Single` | Only one item can be selected |
| `Multi` | Multiple items can be selected |

## Source Code

::: details View SelectionListNode implementation
<<< @/../src/Termina/Layout/SelectionListNode.cs{csharp}
:::
