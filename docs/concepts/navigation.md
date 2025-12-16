# Navigation

Termina provides navigation actions within ViewModels to move between pages.

## Navigation Actions

ViewModels have access to protected navigation methods wired up by the framework:

### Navigate

Navigate directly by path:

```csharp
Navigate("/");           // Go to home
Navigate("/settings");   // Go to settings
Navigate("/items/42");   // Go to item 42
```

### NavigateWithParams

Navigate using a template with parameters:

```csharp
NavigateWithParams("/items/{id}", new { id = 42 });
NavigateWithParams("/users/{name}", new { name = "alice" });
```

### Shutdown

Request graceful application shutdown:

```csharp
Shutdown();  // Exits the application
```

## Navigation Example

```csharp
public partial class MenuViewModel : ReactiveViewModel
{
    [Reactive] private int _selectedIndex;

    public override void OnActivated()
    {
        Input.OfType<KeyPressed>()
            .Subscribe(HandleKey)
            .DisposeWith(Subscriptions);
    }

    private void HandleKey(KeyPressed key)
    {
        switch (key.KeyInfo.Key)
        {
            case ConsoleKey.Enter:
                NavigateToSelected();
                break;
            case ConsoleKey.Escape:
                Shutdown();
                break;
        }
    }

    private void NavigateToSelected()
    {
        var route = SelectedIndex switch
        {
            0 => "/counter",
            1 => "/todo",
            2 => "/settings",
            _ => "/"
        };
        Navigate(route);
    }
}
```

## Navigation Behavior

When registering routes, you can control how navigation behaves:

```csharp
termina.RegisterRoute<DetailPage, DetailViewModel>(
    "/items/{id}",
    NavigationBehavior.PreserveState);  // Keep ViewModel state
```

### ResetOnNavigation (Default)

- Creates new Page and ViewModel on each navigation
- State is reset each time
- Use for pages that should start fresh

### PreserveState

- Reuses existing Page and ViewModel
- State persists across navigations
- `OnDeactivating()` disposes subscriptions
- `OnActivated()` recreates subscriptions
- Use for pages with expensive state or data caching

## Lifecycle Methods

### OnActivated

Called when navigating to the page:

```csharp
public override void OnActivated()
{
    // Setup subscriptions
    Input.OfType<KeyPressed>()
        .Subscribe(HandleKey)
        .DisposeWith(Subscriptions);

    // Load data
    LoadItems();
}
```

### OnDeactivating

Called when navigating away:

```csharp
public override void OnDeactivating()
{
    // Subscriptions are auto-disposed
    base.OnDeactivating();  // Important: call base

    // Optional: save state, cancel operations
    SaveDraft();
}
```

## RequestRedraw

For asynchronous content updates (like streaming), request a UI refresh:

```csharp
private async Task StreamDataAsync()
{
    await foreach (var chunk in dataStream)
    {
        Messages = Messages.Append(chunk).ToList();
        RequestRedraw();  // Trigger UI update
    }
}
```

## ViewModel Source Code

::: details View ReactiveViewModel implementation
<<< @/../src/Termina/Reactive/ReactiveViewModel.cs{csharp}
:::
