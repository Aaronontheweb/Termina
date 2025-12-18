# SelectionListNode

An interactive list selection component with keyboard navigation, supporting single and multi-select modes. Supports both simple text items and rich content with multiple lines and styled/animated segments.

## Basic Usage

```csharp
// String list with factory method
var list = Layouts.SelectionList("Option 1", "Option 2", "Option 3");

// Or from an enumerable
var list = Layouts.SelectionList(options);

// Typed list with custom display
var list = Layouts.SelectionList(items, item => item.Name);
```

## Rich Content

For items that need multiple lines, styled text, or animated elements (like spinners), use the constructor that accepts a `Func<T, SelectionItemContent>`:

```csharp
var list = new SelectionListNode<ServerInfo>(servers, server =>
    new SelectionItemContent()
        .AddLine(server.Name, Color.White, decoration: TextDecoration.Bold)
        .AddLine($"   {server.Address}:{server.Port}", Color.BrightBlack)
);
```

### Multi-line Items with Animations

Rich content items can include animated segments like spinners:

```csharp
var list = new SelectionListNode<ConnectionState>(connections, conn =>
{
    var content = new SelectionItemContent()
        .AddLine(conn.ServerName, Color.Cyan, decoration: TextDecoration.Bold);

    if (conn.IsConnecting)
    {
        content.AddLine(
            new StaticTextSegment("   "),
            new SpinnerSegment(SpinnerStyle.Dots, Color.Yellow),
            new StaticTextSegment(" Connecting...", Color.Yellow)
        );
    }
    else
    {
        content.AddLine($"   Status: {conn.Status}", Color.Green);
    }

    return content;
});
```

### SelectionItemContent API

`SelectionItemContent` provides a fluent API for building multi-line, styled content:

```csharp
var content = new SelectionItemContent()
    // Add a simple text line
    .AddLine("First line")

    // Add a styled text line
    .AddLine("Bold and Blue", Color.Blue, decoration: TextDecoration.Bold)

    // Add a line with multiple segments
    .AddLine(
        new StaticTextSegment("Status: ", Color.White),
        new StaticTextSegment("Active", Color.Green, decoration: TextDecoration.Bold)
    )

    // Add a line with an animated spinner
    .AddLine(
        new SpinnerSegment(SpinnerStyle.Line, Color.Cyan),
        new StaticTextSegment(" Loading...")
    );
```

### CompositeTextSegment

Use `CompositeTextSegment` to combine multiple segments into a single unit:

```csharp
var composite = new CompositeTextSegment(
    new StaticTextSegment("["),
    new SpinnerSegment(SpinnerStyle.Dots, Color.Blue),
    new StaticTextSegment("] Processing...")
);

content.AddLine(composite);
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

SelectionListNode works seamlessly with ModalNode. The **Page owns both nodes** and **manages Focus**:

```csharp
public class MyPage : ReactivePage<MyViewModel>
{
    private SelectionListNode<string> _priorityList = null!;
    private ModalNode _priorityModal = null!;

    protected override void OnBound()
    {
        _priorityList = Layouts.SelectionList("High", "Medium", "Low")
            .WithMode(SelectionMode.Single)
            .WithShowNumbers(true)
            .WithHighlightColors(Color.Black, Color.Cyan)
            .WithOtherOption("Custom...");

        _priorityModal = Layouts.Modal()
            .WithTitle("Select Priority")
            .WithBorder(BorderStyle.Rounded)
            .WithBorderColor(Color.Yellow)
            .WithBackdrop(BackdropStyle.Dim)
            .WithContent(_priorityList);

        // Handle selections - call ViewModel methods
        _priorityList.SelectionConfirmed.Subscribe(selected =>
            ViewModel.OnPrioritySelected(selected.First()))
            .DisposeWith(Subscriptions);

        _priorityList.OtherSelected.Subscribe(custom =>
            ViewModel.OnPrioritySelected(custom))
            .DisposeWith(Subscriptions);

        _priorityList.Cancelled.Subscribe(_ =>
            ViewModel.OnPriorityCancelled())
            .DisposeWith(Subscriptions);

        // React to ViewModel state to manage Focus
        ViewModel.ShowPriorityModalChanged
            .Subscribe(show => {
                if (show)
                {
                    Focus.PushFocus(_priorityModal);
                    Focus.PushFocus(_priorityList);
                }
                else
                {
                    Focus.ClearFocus();
                }
            })
            .DisposeWith(Subscriptions);
    }
}
```

## Complete Example

Here's the recommended pattern where **ViewModel handles state** and **Page owns layout nodes and Focus**:

**ViewModel** - State and business logic:

```csharp
public partial class SettingsViewModel : ReactiveViewModel
{
    [Reactive] private bool _showThemeSelector;
    [Reactive] private string _currentTheme = "System Default";

    public void OnThemeSelected(string theme)
    {
        CurrentTheme = theme;
        ApplyTheme(theme);
        ShowThemeSelector = false;
    }

    public void OnThemeSelectorCancelled()
    {
        ShowThemeSelector = false;
    }

    public void OpenThemeSelector()
    {
        ShowThemeSelector = true;
    }
}
```

**Page** - Owns nodes and manages Focus:

```csharp
public class SettingsPage : ReactivePage<SettingsViewModel>
{
    private SelectionListNode<string> _themeList = null!;
    private ModalNode _themeModal = null!;

    protected override void OnBound()
    {
        _themeList = Layouts.SelectionList("Light", "Dark", "System Default")
            .WithMode(SelectionMode.Single)
            .WithShowNumbers(true)
            .WithHighlightColors(Color.Black, Color.White);

        _themeList.SelectionConfirmed
            .Subscribe(selected => ViewModel.OnThemeSelected(selected.First()))
            .DisposeWith(Subscriptions);

        _themeList.Cancelled
            .Subscribe(_ => ViewModel.OnThemeSelectorCancelled())
            .DisposeWith(Subscriptions);

        _themeModal = Layouts.Modal()
            .WithTitle("Choose Theme")
            .WithBorder(BorderStyle.Rounded)
            .WithContent(_themeList);

        _themeModal.Dismissed
            .Subscribe(_ => ViewModel.OnThemeSelectorCancelled())
            .DisposeWith(Subscriptions);

        // React to ViewModel state to manage Focus
        ViewModel.ShowThemeSelectorChanged
            .Subscribe(show => {
                if (show)
                {
                    Focus.PushFocus(_themeModal);
                    Focus.PushFocus(_themeList);
                }
                else
                {
                    Focus.ClearFocus();
                }
            })
            .DisposeWith(Subscriptions);
    }

    public override ILayoutNode BuildLayout()
    {
        return Layouts.Stack()
            .WithChild(mainContent)
            .WithChild(
                ViewModel.ShowThemeSelectorChanged
                    .Select(show => show ? (ILayoutNode)_themeModal : Layouts.Empty())
                    .AsLayout());
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

### Constructors

| Constructor | Description |
|-------------|-------------|
| `SelectionListNode(IEnumerable<T>, Func<T, string>)` | Create with plain text display |
| `SelectionListNode(IEnumerable<T>, Func<T, SelectionItemContent>)` | Create with rich content display |

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
| `DisplayText` | `string` | First line text (plain text) |
| `Content` | `SelectionItemContent` | Full rich content with all lines and styling |
| `LineCount` | `int` | Number of lines this item occupies |
| `IsSelected` | `bool` | Whether item is selected |
| `IsOther` | `bool` | Whether this is the "Other" option |

### SelectionItemContent Properties

| Property | Type | Description |
|----------|------|-------------|
| `Lines` | `IReadOnlyList<IReadOnlyList<ITextSegment>>` | All lines of content |
| `LineCount` | `int` | Number of lines |
| `HasAnimations` | `bool` | Whether content has animated segments |
| `Invalidated` | `IObservable<Unit>` | Fires when animated content changes |

### SelectionItemContent Methods

| Method | Description |
|--------|-------------|
| `.AddLine(params ITextSegment[])` | Add a line with multiple segments |
| `.AddLine(string, Color?, Color?, TextDecoration)` | Add a simple styled text line |
| `.ToPlainText()` | Get all lines as plain text |
| `.GetFirstLineText()` | Get first line as plain text |
| `FromString(string)` | Static factory for single-line content |

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

::: details View SelectionItemContent implementation
<<< @/../src/Termina/Layout/SelectionItemContent.cs{csharp}
:::

::: details View SelectionItem implementation
<<< @/../src/Termina/Layout/SelectionItem.cs{csharp}
:::

::: details View CompositeTextSegment implementation
<<< @/../src/Termina/Components/Streaming/CompositeTextSegment.cs{csharp}
:::
