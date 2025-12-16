# Counter App Tutorial

Build a reactive counter application that demonstrates Termina's core concepts.

## What You'll Build

A counter application with:
- Increment/decrement with arrow keys
- Text input for messages
- Real-time status updates
- Escape to quit

## Project Setup

Create a new console application:

```bash
dotnet new console -n CounterDemo
cd CounterDemo
dotnet add package Termina
dotnet add package Microsoft.Extensions.Hosting
```

## Step 1: Create the ViewModel

The ViewModel holds your application state and handles input.

::: details View complete CounterViewModel.cs
<<< @/../demos/Termina.Demo.RegionBased/CounterViewModel.cs{csharp}
:::

### Key Points

**Reactive Properties**

```csharp
[Reactive] private int _count;
[Reactive] private string _statusMessage = "Initial status";
```

The `[Reactive]` attribute generates:
- A `Count` property with getter/setter
- A `CountChanged` observable for UI binding
- A `BehaviorSubject<int>` backing field

**Input Handling**

```csharp
public override void OnActivated()
{
    Input.OfType<KeyPressed>()
        .Subscribe(HandleKeyPress)
        .DisposeWith(Subscriptions);
}
```

Subscribe to the `Input` observable in `OnActivated()`. Use `DisposeWith(Subscriptions)` for automatic cleanup.

**Partial Class**

The class must be `partial` for the source generator to work:

```csharp
public partial class CounterViewModel : ReactiveViewModel
```

## Step 2: Create the Page

The Page builds the layout from ViewModel state.

::: details View complete CounterPage.cs
<<< @/../demos/Termina.Demo.RegionBased/CounterPage.cs{csharp}
:::

### Key Points

**Reactive Bindings**

```csharp
ViewModel.CountChanged
    .Select(count => new TextNode($"Count: {count}"))
    .AsLayout()
```

The `AsLayout()` extension creates a `ReactiveLayoutNode` that automatically updates when the observable emits.

**Layout Composition**

```csharp
return Layouts.Vertical()
    .WithChild(header.Height(3))
    .WithChild(counter.Height(3))
    .WithChild(messages.Fill())
    .WithChild(status.Height(1));
```

Build complex UIs by nesting layouts with size constraints.

**Styling**

```csharp
new TextNode("Title")
    .WithForeground(Color.Cyan)
    .Bold()
```

Chain style methods for formatted text.

## Step 3: Wire Up the Host

::: details View complete Program.cs
<<< @/../demos/Termina.Demo.RegionBased/Program.cs{csharp}
:::

### Key Points

**Service Registration**

```csharp
builder.Services.AddTermina("/counter", termina =>
{
    termina.RegisterRoute<CounterPage, CounterViewModel>("/counter");
});
```

Register your page and ViewModel with a route template.

**Initial Route**

The first argument to `AddTermina` specifies the starting route.

## Run the App

```bash
dotnet run
```

Use:
- `↑` / `↓` to change the counter
- Type text and press `Enter` to add messages
- `Escape` to quit

## Complete Code

::: code-group
<<< @/../demos/Termina.Demo.RegionBased/CounterViewModel.cs [ViewModel]
<<< @/../demos/Termina.Demo.RegionBased/CounterPage.cs [Page]
<<< @/../demos/Termina.Demo.RegionBased/Program.cs [Program]
:::

## Next Steps

- Add more pages and [navigation](/concepts/navigation)
- Learn about [size constraints](/layout/size-constraints) for responsive layouts
- Build a [todo list](/tutorials/todo-list) with list management
