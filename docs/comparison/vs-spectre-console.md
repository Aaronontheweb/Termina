# Termina vs Spectre.Console

Both are modern .NET terminal libraries, but with different design goals.

## Overview

| Aspect | Termina | Spectre.Console |
|--------|---------|-----------------|
| **Primary Use** | Interactive TUI apps | Rich console output |
| **UI Model** | Reactive MVVM | Imperative widgets |
| **Rendering** | Surgical updates | Full/partial repaint |
| **AOT** | Full support | Partial (Live) |

## Architecture Comparison

### Termina: Reactive MVVM

```csharp
// State is reactive
[Reactive] private int _count;

// Layout subscribes to state
ViewModel.CountChanged
    .Select(c => new TextNode($"Count: {c}"))
    .AsLayout()

// Changes automatically propagate
Count++;  // UI updates automatically
```

### Spectre.Console: Imperative

```csharp
// Manual rendering
AnsiConsole.Write(new Panel("Count: 0"));

// Live updates require explicit context
await AnsiConsole.Live(panel)
    .StartAsync(async ctx =>
    {
        count++;
        panel.Header = $"Count: {count}";
        ctx.Refresh();  // Manual refresh
    });
```

## Key Differences

### 1. State Management

**Termina** - State lives in ViewModels with observable properties:

```csharp
[Reactive] private List<string> _messages = new();

// Update triggers UI refresh
Messages = Messages.Append("New message").ToList();
```

**Spectre.Console** - State management is manual:

```csharp
var table = new Table();
// Must manually update and refresh
table.AddRow("New message");
ctx.Refresh();
```

### 2. Layout System

**Termina** - Declarative tree with size constraints:

```csharp
Layouts.Vertical()
    .WithChild(header.Height(3))
    .WithChild(content.Fill())
    .WithChild(footer.Height(1))
```

**Spectre.Console** - Widget composition:

```csharp
var layout = new Layout()
    .SplitRows(
        new Layout("Header"),
        new Layout("Content"),
        new Layout("Footer"));
```

### 3. Input Handling

**Termina** - Observable input streams:

```csharp
Input.OfType<KeyPressed>()
    .Where(k => k.KeyInfo.Key == ConsoleKey.Enter)
    .Subscribe(HandleEnter);
```

**Spectre.Console** - Blocking prompts:

```csharp
var name = AnsiConsole.Ask<string>("Name?");
var choice = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .AddChoices("Option 1", "Option 2"));
```

### 4. Streaming Content

**Termina** - Native streaming support:

```csharp
public StreamingTextNode Output { get; } = StreamingTextNode.Create();

await foreach (var chunk in stream)
{
    Output.Append(chunk);  // Character-level updates
}
```

**Spectre.Console** - Status/Progress contexts:

```csharp
await AnsiConsole.Status()
    .StartAsync("Working...", async ctx =>
    {
        // Limited to status updates
    });
```

### 5. Rendering Efficiency

**Termina** - Only changed regions re-render:

```
┌─────────────────┐
│ Header          │  ← Not re-rendered
├─────────────────┤
│ Count: 42       │  ← Only this region updates
├─────────────────┤
│ Footer          │  ← Not re-rendered
└─────────────────┘
```

**Spectre.Console** - Full panel/widget refresh in Live mode.

## When to Use Each

### Use Termina For:

- **Interactive applications** with continuous input handling
- **Streaming content** like LLM output or live logs
- **Complex state** requiring reactive bindings
- **AOT publishing** requirements
- **Multi-page navigation** with routing

### Use Spectre.Console For:

- **CLI tools** with prompts and confirmations
- **Rich output** (tables, trees, charts)
- **Progress bars** and status indicators
- **One-shot rendering** without interactivity
- **Quick prototypes** with simple UI needs

## Migration Considerations

### From Spectre.Console to Termina

1. Replace prompts with pages and ViewModels
2. Convert tables to custom layout nodes
3. Move from Live context to reactive bindings
4. Implement navigation for multi-step flows

### Coexistence

For CLI tools, you might use both:
- **Termina** for the main interactive UI
- **Spectre.Console** for initial setup prompts or output formatting

## Code Comparison

### Counter App

**Termina:**
```csharp
public partial class CounterViewModel : ReactiveViewModel
{
    [Reactive] private int _count;

    public override void OnActivated()
    {
        Input.OfType<KeyPressed>()
            .Subscribe(k =>
            {
                if (k.KeyInfo.Key == ConsoleKey.UpArrow) Count++;
            });
    }
}

public class CounterPage : ReactivePage<CounterViewModel>
{
    public override ILayoutNode BuildLayout() =>
        ViewModel.CountChanged
            .Select(c => new TextNode($"Count: {c}"))
            .AsLayout();
}
```

**Spectre.Console:**
```csharp
var count = 0;
var panel = new Panel($"Count: {count}");

await AnsiConsole.Live(panel)
    .StartAsync(async ctx =>
    {
        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.UpArrow) count++;
                panel = new Panel($"Count: {count}");
                ctx.Refresh();
            }
            await Task.Delay(10);
        }
    });
```
