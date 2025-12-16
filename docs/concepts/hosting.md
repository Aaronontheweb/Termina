# Hosting & Dependency Injection

Termina integrates with Microsoft.Extensions.Hosting for dependency injection and application lifecycle management.

## Basic Setup

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTermina("/", termina =>
{
    termina.RegisterRoute<HomePage, HomeViewModel>("/");
    termina.RegisterRoute<SettingsPage, SettingsViewModel>("/settings");
});

var app = builder.Build();
await app.RunAsync();
```

## Service Registration

### AddTermina

The primary extension method for configuring Termina:

```csharp
// With start page
builder.Services.AddTermina("/dashboard", termina =>
{
    termina.RegisterRoute<DashboardPage, DashboardViewModel>("/dashboard");
});

// Without start page (navigate manually)
builder.Services.AddTermina(termina =>
{
    termina.RegisterRoute<MainPage, MainViewModel>("main");
});
```

### RegisterRoute

Register pages with route templates:

```csharp
termina.RegisterRoute<TodoPage, TodoViewModel>("/todos");
termina.RegisterRoute<DetailPage, DetailViewModel>("/todos/{id:int}");
```

### Navigation Behavior

Control page lifecycle during navigation:

```csharp
// Default: create new instances each time
termina.RegisterRoute<Page, ViewModel>("/path");

// Preserve state across navigations
termina.RegisterRoute<Page, ViewModel>(
    "/path",
    NavigationBehavior.PreserveState);
```

## Dependency Injection

Pages and ViewModels support constructor injection:

```csharp
public class TodoViewModel : ReactiveViewModel
{
    private readonly ITodoService _todoService;
    private readonly ILogger<TodoViewModel> _logger;

    public TodoViewModel(
        ITodoService todoService,
        ILogger<TodoViewModel> logger)
    {
        _todoService = todoService;
        _logger = logger;
    }
}
```

Register your services normally:

```csharp
builder.Services.AddSingleton<ITodoService, TodoService>();
builder.Services.AddSingleton<IDataStore, JsonDataStore>();
```

## Custom Input Sources

### Console Input (Default)

Standard keyboard input from the terminal:

```csharp
builder.Services.AddTerminaConsoleInput();
```

### Virtual Input (Testing)

For automated testing with simulated input:

```csharp
var inputSource = new VirtualInputSource();
builder.Services.AddTerminaVirtualInput(inputSource);

// Later, in tests:
inputSource.SendKey(ConsoleKey.Enter);
inputSource.SendKey(ConsoleKey.UpArrow);
```

## Custom Factories

For complex initialization beyond DI:

```csharp
termina.RegisterRoute<CustomPage, CustomViewModel>(
    "/custom",
    pageFactory: sp => new CustomPage(sp.GetService<IConfig>()),
    viewModelFactory: sp => new CustomViewModel(
        sp.GetRequiredService<IDataService>(),
        customSetting: true));
```

## What Gets Registered

`AddTermina` registers these services:

| Service | Lifetime | Description |
|---------|----------|-------------|
| `IAnsiTerminal` | Singleton | Terminal abstraction |
| `TerminaApplication` | Singleton | Main application |
| `TerminaHostedService` | HostedService | Runs the app |
| `TPage` | Transient | Each page type |
| `TViewModel` | Transient | Each ViewModel type |

## Application Lifecycle

1. **Host Starts** - `IHostedService.StartAsync` called
2. **Initial Navigation** - Route to start page
3. **Input Loop** - Process keyboard/mouse events
4. **Navigation** - Create/activate pages as needed
5. **Shutdown** - `Shutdown()` called or Ctrl+C received
6. **Host Stops** - Graceful shutdown

## Source Code

::: details View TerminaServiceCollectionExtensions
<<< @/../src/Termina/Hosting/TerminaServiceCollectionExtensions.cs{csharp}
:::

::: details View TerminaBuilder
<<< @/../src/Termina/Hosting/TerminaBuilder.cs{csharp}
:::
