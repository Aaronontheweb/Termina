# Termina vs Terminal.Gui

Both are full TUI frameworks, but with fundamentally different approaches.

## Overview

| Aspect | Termina | Terminal.Gui |
|--------|---------|--------------|
| **UI Model** | Reactive MVVM | Event-driven widgets |
| **Layout** | Declarative tree | Computed/Absolute |
| **Widgets** | Minimal, composable | Rich built-in set |
| **Windows** | Single-page focused | Multi-window support |

## Architecture Comparison

### Termina: Reactive Declarative

```csharp
// State drives UI automatically
[Reactive] private string _text = "";

// Layout is a function of state
ViewModel.TextChanged
    .Select(t => new TextNode(t))
    .AsLayout()
```

### Terminal.Gui: Event-Driven Widgets

```csharp
// Create widgets
var label = new Label("Text") { X = 0, Y = 0 };
var button = new Button("Click") { X = 0, Y = 1 };

// Wire up events
button.Clicked += () => label.Text = "Clicked!";

// Add to window
window.Add(label, button);
```

## Key Differences

### 1. Layout Philosophy

**Termina** - Tree-based with constraints:

```csharp
Layouts.Vertical()
    .WithChild(
        new PanelNode()
            .WithContent(content)
            .Fill())  // Take remaining space
    .WithChild(
        statusBar.Height(1))  // Fixed height
```

**Terminal.Gui** - Position and size:

```csharp
var view = new View
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),      // Fill parent
    Height = Dim.Percent(80) // 80% of parent
};
```

### 2. State Management

**Termina** - Reactive observables:

```csharp
[Reactive] private List<TodoItem> _items = new();

// UI automatically updates when Items changes
ViewModel.ItemsChanged
    .Select(items => BuildList(items))
    .AsLayout()
```

**Terminal.Gui** - Property binding or manual:

```csharp
var listView = new ListView(items);

// Manual update
items.Add(newItem);
listView.SetSource(items);  // Explicit refresh
```

### 3. Input Handling

**Termina** - Observable streams:

```csharp
Input.OfType<KeyPressed>()
    .Where(k => k.KeyInfo.Key == ConsoleKey.F1)
    .Subscribe(_ => ShowHelp());
```

**Terminal.Gui** - Event handlers:

```csharp
Application.Top.KeyPress += (e) =>
{
    if (e.KeyEvent.Key == Key.F1)
    {
        ShowHelp();
        e.Handled = true;
    }
};
```

### 4. Built-in Widgets

**Terminal.Gui** has more built-in widgets:

- Windows and Dialogs
- Menu bars
- Tree views
- Tab views
- File dialogs
- Message boxes

**Termina** provides primitives:

- TextNode, PanelNode
- TextInputNode
- StreamingTextNode
- ScrollableContainer
- Layout containers

### 5. Multi-Window Support

**Terminal.Gui** - Native windowing:

```csharp
var mainWindow = new Window("Main");
var dialog = new Dialog("Settings");
Application.Run(mainWindow);
dialog.Run();  // Modal dialog
```

**Termina** - Page-based navigation:

```csharp
Navigate("/settings");  // Full page replacement
// Dialogs would be custom overlay components
```

## When to Use Each

### Use Termina For:

- **Reactive data-driven apps** with streaming updates
- **Single-focus interfaces** like chat, dashboards
- **AOT compilation** requirements
- **Akka.NET integration** or complex async flows
- **Custom rendering** needs

### Use Terminal.Gui For:

- **Traditional GUI applications** with windows/dialogs
- **File managers** or tree-based UIs
- **Menu-driven applications**
- **Rich widget requirements** out of the box
- **Existing WinForms/WPF developers** (familiar patterns)

## Rendering Comparison

### Termina

Direct ANSI rendering with surgical updates:

```
// Only changed regions re-render
┌─────────────────┐
│ Static Header   │  ← Cached
├─────────────────┤
│ Dynamic: 42     │  ← Re-rendered
└─────────────────┘
```

### Terminal.Gui

View-based rendering with partial updates:

```
// View hierarchy determines update scope
Application
└── TopLevel
    └── Window
        └── Label  ← Marked dirty, redraws
```

## Code Comparison

### Todo List

**Termina:**

```csharp
public partial class TodoViewModel : ReactiveViewModel
{
    [Reactive] private IReadOnlyList<TodoItem> _items = new List<TodoItem>();
    [Reactive] private int _selectedIndex;
}

public class TodoPage : ReactivePage<TodoViewModel>
{
    public override ILayoutNode BuildLayout() =>
        ViewModel.ItemsChanged
            .CombineLatest(ViewModel.SelectedIndexChanged,
                (items, idx) => BuildList(items, idx))
            .AsLayout();

    private ILayoutNode BuildList(IReadOnlyList<TodoItem> items, int selected)
    {
        var container = Layouts.Vertical();
        for (int i = 0; i < items.Count; i++)
        {
            var node = new TextNode(items[i].Text);
            if (i == selected)
                node = node.WithBackground(Color.Blue);
            container = container.WithChild(node.Height(1));
        }
        return container;
    }
}
```

**Terminal.Gui:**

```csharp
public class TodoWindow : Window
{
    private ListView _listView;
    private List<string> _items = new();

    public TodoWindow()
    {
        _listView = new ListView(_items)
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _listView.SelectedItemChanged += OnSelectionChanged;
        Add(_listView);
    }

    public void AddItem(string text)
    {
        _items.Add(text);
        _listView.SetSource(_items);
    }

    private void OnSelectionChanged(ListViewItemEventArgs e)
    {
        // Handle selection
    }
}
```

## Migration Path

### From Terminal.Gui to Termina

1. Convert Window classes to Page + ViewModel pairs
2. Replace event handlers with observable subscriptions
3. Convert widget properties to reactive properties
4. Implement custom nodes for unsupported widgets
5. Replace dialogs with navigation or overlay patterns

### Incremental Adoption

Terminal.Gui applications can't easily embed Termina or vice versa - they manage the terminal differently. Choose one framework per application.
