# Todo List Tutorial

Build a todo list manager with list selection, item management, and multi-page navigation.

## What You'll Build

A todo list application with:
- Scrollable list with selection highlighting
- Add, toggle, and delete items
- Text input mode for new items
- Navigation to other pages
- Progress tracking

## Project Setup

This tutorial extends from the counter app. If starting fresh:

```bash
dotnet new console -n TodoDemo
cd TodoDemo
dotnet add package Termina
dotnet add package Microsoft.Extensions.Hosting
```

## Step 1: Create the Data Model

First, define a simple record for todo items:

```csharp
public record TodoItem(string Description, bool IsCompleted);
```

## Step 2: Create the ViewModel

The ViewModel manages the list state and handles all input.

::: details View complete TodoListViewModel.cs
<<< @/../demos/Termina.Demo/Pages/TodoListViewModel.cs{csharp}
:::

### Key Points

**Collection State**

```csharp
[Reactive] private IReadOnlyList<TodoItem> _items = new List<TodoItem> { ... };
[Reactive] private int _selectedIndex;
```

Use immutable collections with `[Reactive]`. To update, create a new list:

```csharp
var newItems = Items.ToList();
newItems.Add(new TodoItem("New task", false));
Items = newItems;  // Triggers UI update
```

**Input Mode Handling**

```csharp
[Reactive] private bool _isAddingItem;
[Reactive] private string _newItemText = "";

private void HandleKeyPress(KeyPressed key)
{
    if (IsAddingItem)
    {
        HandleTextEntryKeyPress(key);
        return;
    }
    // Normal mode handling...
}
```

Switch input handling based on current mode.

**Navigation**

```csharp
case ConsoleKey.C:
    Navigate("/counter");
    break;
```

Use `Navigate()` to move between pages.

## Step 3: Create the Page

The Page renders the list and responds to state changes.

::: details View complete TodoListPage.cs
<<< @/../demos/Termina.Demo/Pages/TodoListPage.cs{csharp}
:::

### Key Points

**Combining Observables**

```csharp
ViewModel.ItemsChanged
    .CombineLatest(ViewModel.SelectedIndexChanged, (items, idx) => (items, idx))
    .Select(tuple => BuildTodoList(tuple.items, tuple.idx))
    .AsLayout()
```

Use `CombineLatest` when the UI depends on multiple reactive properties.

**Conditional Rendering**

```csharp
ViewModel.IsAddingItemChanged
    .Select(isAdding => isAdding
        ? BuildTextInputRow()
        : BuildHelpText())
    .AsLayout()
```

Switch layouts based on state.

**Selection Highlighting**

```csharp
if (isSelected)
{
    textNode = textNode
        .WithForeground(Color.Black)
        .WithBackground(Color.Green);
}
```

Style items based on selection state.

**Dynamic List Building**

```csharp
private static ILayoutNode BuildTodoList(IReadOnlyList<TodoItem> items, int selectedIndex)
{
    var container = Layouts.Vertical();

    for (var i = 0; i < items.Count; i++)
    {
        container = container.WithChild(BuildItem(items[i], i == selectedIndex));
    }

    return container;
}
```

Build layout nodes dynamically from collections.

## Step 4: Register Routes

::: details View complete Program.cs
<<< @/../demos/Termina.Demo/Program.cs{csharp}
:::

### Key Points

**Multiple Routes**

```csharp
builder.Services.AddTermina("/counter", termina =>
{
    termina.RegisterRoute<CounterPage, CounterViewModel>("/counter");
    termina.RegisterRoute<TodoListPage, TodoListViewModel>("/todos");
});
```

Register multiple pages with their routes.

**PreserveState**

```csharp
termina.RegisterRoute<TodoListPage, TodoListViewModel>(
    "/todos",
    NavigationBehavior.PreserveState);
```

Use `PreserveState` to keep state when navigating away and back.

## Run the App

```bash
dotnet run
```

Controls:
- `↑` / `↓` - Navigate list
- `Space` - Toggle completion
- `A` - Add new item
- `D` - Delete selected
- `C` - Go to counter page
- `Q` - Quit

## Patterns Demonstrated

### Immutable Updates

Always create new collections to trigger updates:

```csharp
// Bad - mutation doesn't trigger update
Items.Add(newItem);  // Won't update UI!

// Good - replacement triggers update
Items = Items.Append(newItem).ToList();
```

### Mode-Based Input

Use state to control input behavior:

```csharp
private void HandleKeyPress(KeyPressed key)
{
    if (IsAddingItem)
    {
        HandleTextEntryMode(key);
        return;
    }
    HandleNormalMode(key);
}
```

### Progress Tracking

Calculate derived values from state:

```csharp
var completedCount = items.Count(i => i.IsCompleted);
var statsText = $"{completedCount}/{items.Count} completed";
```

## Complete Code

::: code-group
<<< @/../demos/Termina.Demo/Pages/TodoListViewModel.cs [ViewModel]
<<< @/../demos/Termina.Demo/Pages/TodoListPage.cs [Page]
<<< @/../demos/Termina.Demo/Program.cs [Program]
:::

## Next Steps

- Learn about [StreamingTextNode](/components/streaming-text-node) for real-time content
- Build a [streaming chat](/tutorials/streaming-chat) with async data
- Explore [TextInputNode](/components/text-input-node) for proper text input handling
