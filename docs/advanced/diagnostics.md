# Diagnostic Tracing

Termina includes a built-in diagnostic tracing system for debugging and monitoring your TUI applications.

## Overview

The tracing system is designed with these goals:

- **Zero-cost when disabled**: No allocations, minimal CPU overhead (a single boolean check)
- **Deferred formatting**: Messages are only formatted when a listener actually needs them
- **Lock-free I/O**: File output uses a Channel-based design that never blocks the UI thread
- **Category filtering**: Focus on specific subsystems (Focus, Input, Layout, Render, etc.)
- **Level filtering**: Control verbosity from Trace to Error

## Default Behavior

**Tracing is completely disabled by default.** When no listener is configured, all trace calls are no-ops with sub-nanosecond overhead and zero allocations.

## Enabling Tracing

### File Tracing (Recommended for Debugging)

```csharp
using Termina.Diagnostics;

var builder = Host.CreateApplicationBuilder(args);

// Enable file tracing
builder.Services.AddTerminaFileTracing("termina-trace.log");

// Or with category and level filtering
builder.Services.AddTerminaFileTracing(
    "termina-trace.log",
    TerminaTraceCategory.Focus | TerminaTraceCategory.Input,
    TerminaTraceLevel.Debug);
```

### Microsoft.Extensions.Logging Integration

Route Termina traces to your existing logging infrastructure:

```csharp
builder.Services
    .AddLogging(logging => logging.AddConsole())
    .AddTerminaLoggerTracing();

// With filtering
builder.Services.AddTerminaLoggerTracing(
    TerminaTraceCategory.All,
    TerminaTraceLevel.Info);
```

### Stderr Output (Quick Debugging)

For quick debugging without file I/O:

```csharp
TerminaTrace.Configure(
    FileTraceListener.CreateStdErr(),
    TerminaTraceCategory.All,
    TerminaTraceLevel.Debug);
```

### Manual Configuration (Without DI)

```csharp
var listener = new FileTraceListener("trace.log");
TerminaTrace.Configure(listener, TerminaTraceCategory.All, TerminaTraceLevel.Debug);

// When done, properly dispose to drain pending events
await listener.DisposeAsync();
```

## Categories

Filter traces by subsystem using the `TerminaTraceCategory` flags enum:

| Category | Description |
|----------|-------------|
| `Focus` | Focus management, focus stack operations |
| `Input` | Keyboard and mouse input processing |
| `Layout` | Layout measurement and arrangement |
| `Page` | Page navigation and lifecycle |
| `Reactive` | Observable subscriptions and updates |
| `Render` | Rendering operations and ANSI output |
| `Platform` | Platform-specific console operations |
| `All` | All categories enabled |
| `None` | No categories (disabled) |

Combine categories with bitwise OR:

```csharp
var categories = TerminaTraceCategory.Focus | TerminaTraceCategory.Input;
```

## Levels

Control verbosity with `TerminaTraceLevel`:

| Level | Description |
|-------|-------------|
| `Trace` | Most verbose, fine-grained diagnostic info |
| `Debug` | Debugging information (default) |
| `Info` | General informational messages |
| `Warning` | Potential issues or unexpected conditions |
| `Error` | Errors that don't crash the application |

Setting a level includes all higher severity levels (e.g., `Debug` includes `Info`, `Warning`, and `Error`).

## Output Format

The `FileTraceListener` produces output in this format:

```
2024-01-15 10:30:45.123 [DEBUG] [Focus] FocusManager#12345678 - PushFocus: TextInputNode, stack depth=1
2024-01-15 10:30:45.125 [TRACE] [Input] TextInputNode#87654321 - HandleInput: key=A, char='A'
2024-01-15 10:30:45.130 [INFO] [Page] TerminaApplication#11111111 - Navigating to: /todos
```

Format: `{timestamp} [{level}] [{category}] {source_type}#{hash} - {message}`

## Adding Traces to Your Code

Use the category-specific loggers on `TerminaTrace`:

```csharp
// In a ViewModel or component
public void HandleKeyPress(KeyPressed key)
{
    TerminaTrace.Input.Trace(this, "HandleKeyPress: key={0}", key.KeyInfo.Key);

    // ... your logic ...

    if (someCondition)
    {
        TerminaTrace.Input.Debug(this, "Special condition triggered");
    }
}
```

### Available Loggers

| Logger | Category |
|--------|----------|
| `TerminaTrace.Focus` | Focus operations |
| `TerminaTrace.Input` | Input handling |
| `TerminaTrace.Layout` | Layout operations |
| `TerminaTrace.Page` | Page navigation |
| `TerminaTrace.Reactive` | Reactive subscriptions |
| `TerminaTrace.Render` | Rendering |
| `TerminaTrace.Platform` | Platform operations |

### Method Overloads

Each logger supports 0-3 arguments with deferred formatting:

```csharp
// No arguments
TerminaTrace.Focus.Debug(this, "Focus changed");

// One argument
TerminaTrace.Focus.Debug(this, "Focused: {0}", nodeName);

// Two arguments
TerminaTrace.Input.Trace(this, "Key: {0}, Char: {1}", key.Key, key.KeyChar);

// Three arguments
TerminaTrace.Render.Trace(this, "Render at ({0},{1}) size {2}", x, y, size);
```

## Custom Listeners

Implement `ITerminaTraceListener` for custom output:

```csharp
public interface ITerminaTraceListener
{
    bool IsEnabled(TerminaTraceLevel level, TerminaTraceCategory category);
    void Write(in TraceEvent evt);
}
```

Example: Console listener with colors:

```csharp
public class ColoredConsoleListener : ITerminaTraceListener
{
    private readonly TerminaTraceCategory _categories;
    private readonly TerminaTraceLevel _minLevel;

    public ColoredConsoleListener(
        TerminaTraceCategory categories = TerminaTraceCategory.All,
        TerminaTraceLevel minLevel = TerminaTraceLevel.Debug)
    {
        _categories = categories;
        _minLevel = minLevel;
    }

    public bool IsEnabled(TerminaTraceLevel level, TerminaTraceCategory category)
        => level >= _minLevel && (_categories & category) != 0;

    public void Write(in TraceEvent evt)
    {
        var color = evt.Level switch
        {
            TerminaTraceLevel.Error => ConsoleColor.Red,
            TerminaTraceLevel.Warning => ConsoleColor.Yellow,
            TerminaTraceLevel.Info => ConsoleColor.White,
            _ => ConsoleColor.Gray
        };

        var original = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Error.WriteLine($"[{evt.Level}] [{evt.Category}] {evt.FormatMessage()}");
        Console.ForegroundColor = original;
    }
}
```

## Performance

The tracing system is designed for minimal overhead:

| Scenario | Time | Allocations |
|----------|------|-------------|
| Disabled (no listener) | ~1 ns | 0 B |
| Enabled, no args | ~29 ns | 0 B |
| Enabled, 1 arg | ~32 ns | 24 B |
| Enabled, 2 args | ~32 ns | 24 B |
| Enabled, 3 args | ~37 ns | 48 B |
| With formatting | ~64-110 ns | 80-128 B |

The small allocations when enabled come from boxing arguments. String formatting only occurs when the listener actually processes the event.

## Best Practices

### Use Appropriate Levels

```csharp
// Trace: Very frequent operations, detailed debugging
TerminaTrace.Input.Trace(this, "Mouse move: {0},{1}", x, y);

// Debug: Less frequent, useful for debugging
TerminaTrace.Focus.Debug(this, "Focus pushed: {0}", nodeName);

// Info: Significant events
TerminaTrace.Page.Info(this, "Navigating to: {0}", path);

// Warning: Unexpected but recoverable
TerminaTrace.Layout.Warning(this, "Node has zero size: {0}", node);

// Error: Problems that need attention
TerminaTrace.Render.Error(this, "Render failed: {0}", exception.Message);
```

### Filter in Production

Only enable categories and levels you need:

```csharp
// Development: Everything
builder.Services.AddTerminaFileTracing("trace.log",
    TerminaTraceCategory.All,
    TerminaTraceLevel.Trace);

// Production: Errors and warnings only
builder.Services.AddTerminaFileTracing("trace.log",
    TerminaTraceCategory.All,
    TerminaTraceLevel.Warning);
```

### Dispose Listeners Properly

The `FileTraceListener` uses async I/O. Always dispose properly to flush pending events:

```csharp
// With DI - automatic disposal
builder.Services.AddTerminaFileTracing("trace.log");

// Manual usage
var listener = new FileTraceListener("trace.log");
try
{
    TerminaTrace.Configure(listener, ...);
    // ... run application ...
}
finally
{
    await listener.DisposeAsync(); // Drains pending events
}
```

## Disabling Tracing

```csharp
// Disable at runtime
TerminaTrace.Disable();
```

This immediately stops all trace output and returns to the zero-overhead state.
