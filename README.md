# Termina

![Termina Logo](https://raw.githubusercontent.com/Aaronontheweb/Termina/refs/heads/dev/assets/termina-icon.png)

[![NuGet Downloads](https://img.shields.io/nuget/dt/Termina)](https://www.nuget.org/packages/Termina) ![GitHub License](https://img.shields.io/github/license/Aaronontheweb/Termina) ![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/Aaronontheweb/Termina/pr_validation.yml) ![GitHub Release](https://img.shields.io/github/v/release/Aaronontheweb/Termina)

**Termina** is a reactive terminal UI (TUI) framework for .NET built on top of [Spectre.Console](https://spectreconsole.net/). It provides an MVVM architecture with source-generated reactive properties, ASP.NET Core-style routing, and seamless integration with Microsoft.Extensions.Hosting.

## Features

- **Reactive MVVM Architecture** - ViewModels with `[Reactive]` attribute for source-generated observable properties
- **ASP.NET Core-Style Routing** - Route templates with parameters (`/tasks/{id:int}`) and type constraints
- **Source Generators** - AOT-compatible code generation for reactive properties and route parameter injection
- **Dependency Injection** - Full integration with `Microsoft.Extensions.DependencyInjection`
- **Hosting Integration** - Works with `Microsoft.Extensions.Hosting` for clean application lifecycle management
- **Spectre.Console Rendering** - Beautiful terminal UIs with full Spectre.Console component support

## Installation

```bash
dotnet add package Termina
```

## Quick Start

### 1. Define a ViewModel

```csharp
using Termina.Reactive;

public partial class CounterViewModel : ReactiveViewModel
{
    [Reactive] private int _count;
    [Reactive] private string _message = "Press Up/Down to change count";

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
            case ConsoleKey.UpArrow:
                Count++;
                Message = $"Count: {Count}";
                break;
            case ConsoleKey.DownArrow:
                Count--;
                Message = $"Count: {Count}";
                break;
            case ConsoleKey.Q:
                Shutdown();
                break;
        }
    }
}
```

The `[Reactive]` attribute generates:
- A `BehaviorSubject<T>` backing field
- A public property `Count` with get/set
- An `IObservable<T>` property `CountChanged` for subscriptions

### 2. Define a Page

```csharp
using Spectre.Console;
using Spectre.Console.Rendering;
using Termina.Pages;

public class CounterPage : ReactivePage<CounterViewModel>
{
    protected override IRenderable Render(CounterViewModel vm)
    {
        return new Panel(
            new Rows(
                new FigletText(vm.Count.ToString()).Color(Color.Cyan1),
                new Text(vm.Message)
            ))
            .Header("Counter Demo")
            .Border(BoxBorder.Rounded);
    }
}
```

### 3. Configure and Run

```csharp
using Microsoft.Extensions.Hosting;
using Termina.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTermina("/counter", termina =>
{
    termina.RegisterRoute<CounterPage, CounterViewModel>("/counter");
    termina.RegisterRoute<TodoListPage, TodoListViewModel>("/todos");
    termina.RegisterRoute<TodoDetailPage, TodoDetailViewModel>("/todos/{id:int}");
});

await builder.Build().RunAsync();
```

## Routing

Termina uses ASP.NET Core-style route templates with parameter support.

### Route Templates

```csharp
// Simple routes
termina.RegisterRoute<HomePage, HomeViewModel>("/");
termina.RegisterRoute<TasksPage, TasksViewModel>("/tasks");

// Routes with parameters
termina.RegisterRoute<TaskDetailPage, TaskDetailViewModel>("/tasks/{id:int}");
termina.RegisterRoute<UserPage, UserViewModel>("/users/{name}");
termina.RegisterRoute<DocumentPage, DocumentViewModel>("/docs/{id:guid}");
```

### Supported Type Constraints

| Constraint | C# Type | Example |
|------------|---------|---------|
| `:int` | `int` | `/tasks/{id:int}` |
| `:guid` | `Guid` | `/docs/{id:guid}` |
| `:bool` | `bool` | `/items/{active:bool}` |
| (none) | `string` | `/users/{name}` |

### Route Parameter Injection

Use `[FromRoute]` to automatically inject route parameters into your ViewModel:

```csharp
public partial class TaskDetailViewModel : ReactiveViewModel
{
    [FromRoute] private int _id;  // Injected from route before OnActivated

    public override void OnActivated()
    {
        // Id property is already populated
        LoadTask(Id);
    }
}
```

### Navigation

```csharp
// Navigate by path
Navigate("/tasks/42");

// Navigate with route values (type-safe)
NavigateWithParams("/tasks/{id}", new { id = 42 });

// Go back
// (handled by framework when CanGoBack is true)
```

## Reactive Properties

The `[Reactive]` attribute on private fields generates observable properties:

```csharp
public partial class MyViewModel : ReactiveViewModel
{
    [Reactive] private string _name = "default";
    [Reactive] private int _count;
    [Reactive] private IReadOnlyList<Item> _items = Array.Empty<Item>();
}
```

Generated code provides:
- `Name`, `Count`, `Items` - public properties with get/set
- `NameChanged`, `CountChanged`, `ItemsChanged` - `IObservable<T>` for subscriptions

Pages automatically re-render when any reactive property changes.

## Page Lifecycle

```csharp
public partial class MyViewModel : ReactiveViewModel
{
    public override void OnActivated()
    {
        // Called when navigating TO this page
        // Set up subscriptions here
    }

    public override void OnDeactivating()
    {
        // Called when navigating AWAY from this page
        // Clean up if needed (Subscriptions auto-dispose)
    }
}
```

## Navigation Behavior

Control how pages behave when navigated to:

```csharp
// Reset state each time (default)
termina.RegisterRoute<MyPage, MyViewModel>("/page", NavigationBehavior.ResetOnNavigation);

// Preserve state across navigations
termina.RegisterRoute<MyPage, MyViewModel>("/page", NavigationBehavior.PreserveState);
```

## Testing

Termina includes `VirtualInputSource` for automated testing:

```csharp
var scriptedInput = new VirtualInputSource();
builder.Services.AddTerminaVirtualInput(scriptedInput);

// Queue up scripted input
scriptedInput.EnqueueKey(ConsoleKey.UpArrow);
scriptedInput.EnqueueKey(ConsoleKey.Enter);
scriptedInput.EnqueueKey(ConsoleKey.Q);
scriptedInput.Complete();

await host.RunAsync();
```

## Requirements

- .NET 10.0 or later
- AOT-compatible (Native AOT publishing supported)

## License

Apache 2.0 - See [LICENSE](LICENSE) for details.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.
