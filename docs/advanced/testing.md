# Testing

Termina provides `VirtualInputSource` for automated testing without a real terminal.

## Overview

Testing TUI applications typically requires:
1. Simulating keyboard/mouse input
2. Verifying state changes
3. Running without a physical terminal

`VirtualInputSource` solves these by providing programmatic input injection.

## Setting Up Tests

### Register Virtual Input

```csharp
var inputSource = new VirtualInputSource();
builder.Services.AddTerminaVirtualInput(inputSource);
```

### Enqueue Input Events

```csharp
// Single keys
inputSource.EnqueueKey(ConsoleKey.Enter);
inputSource.EnqueueKey(ConsoleKey.UpArrow);

// Keys with modifiers
inputSource.EnqueueKey(ConsoleKey.C, control: true);  // Ctrl+C

// Characters and strings
inputSource.EnqueueChar('a');
inputSource.EnqueueString("Hello World");

// Mouse events
inputSource.EnqueueClick(10, 5, MouseButton.Left);
inputSource.EnqueueScroll(10, 5, up: true);

// Resize events
inputSource.EnqueueResize(120, 40);

// Signal completion
inputSource.Complete();
```

## VirtualInputSource API

::: details View VirtualInputSource implementation
<<< @/../src/Termina/Input/VirtualInputSource.cs{csharp}
:::

### Key Methods

| Method | Description |
|--------|-------------|
| `EnqueueKey(ConsoleKey)` | Send a key without character |
| `EnqueueKey(ConsoleKey, shift, alt, control)` | Send key with modifiers |
| `EnqueueChar(char)` | Send a character key |
| `EnqueueString(string)` | Send multiple characters |
| `EnqueueClick(x, y, button)` | Send mouse click |
| `EnqueueScroll(x, y, up)` | Send scroll event |
| `EnqueueResize(width, height)` | Send resize event |
| `Complete()` | Signal end of input |

## Example: Testing Counter App

```csharp
[Fact]
public async Task Counter_IncrementDecrement_UpdatesState()
{
    // Arrange
    var inputSource = new VirtualInputSource();
    var builder = Host.CreateApplicationBuilder();
    builder.Services.AddTerminaVirtualInput(inputSource);
    builder.Services.AddTermina("/counter", termina =>
    {
        termina.RegisterRoute<CounterPage, CounterViewModel>("/counter");
    });

    var host = builder.Build();

    // Act - queue input before starting
    _ = Task.Run(async () =>
    {
        await Task.Delay(100);  // Wait for startup

        inputSource.EnqueueKey(ConsoleKey.UpArrow);
        inputSource.EnqueueKey(ConsoleKey.UpArrow);
        inputSource.EnqueueKey(ConsoleKey.DownArrow);

        await Task.Delay(50);
        inputSource.EnqueueKey(ConsoleKey.Escape);
        inputSource.Complete();
    });

    await host.RunAsync();

    // Assert - ViewModel state
    // (Access via service provider or exposed test hooks)
}
```

## Demo Test Mode

The demos include a `--test` flag for CI/CD:

```csharp
var testMode = args.Contains("--test");

if (testMode)
{
    var scriptedInput = new VirtualInputSource();
    builder.Services.AddTerminaVirtualInput(scriptedInput);

    _ = Task.Run(async () =>
    {
        await Task.Delay(100);

        scriptedInput.EnqueueKey(ConsoleKey.UpArrow);
        scriptedInput.EnqueueString("Test message");
        scriptedInput.EnqueueKey(ConsoleKey.Enter);
        scriptedInput.EnqueueKey(ConsoleKey.Escape);
        scriptedInput.Complete();
    });
}
```

Run in CI:

```bash
dotnet run --project demos/Termina.Demo.RegionBased -- --test
```

## Testing Patterns

### Unit Testing ViewModels

Test ViewModels in isolation:

```csharp
[Fact]
public void HandleKeyPress_UpArrow_IncrementsCount()
{
    // Arrange
    var vm = new CounterViewModel();
    vm.OnActivated();

    // Act - simulate via reflection or test hooks
    // vm.HandleKeyPress(new KeyPressed(...));

    // Assert
    Assert.Equal(1, vm.Count);
}
```

### Integration Testing

Test full page/ViewModel interaction:

```csharp
[Fact]
public async Task TodoList_AddItem_AppearsInList()
{
    var inputSource = new VirtualInputSource();
    // Setup host...

    _ = Task.Run(async () =>
    {
        await Task.Delay(100);
        inputSource.EnqueueKey(ConsoleKey.A);  // Start adding
        inputSource.EnqueueString("New Task");
        inputSource.EnqueueKey(ConsoleKey.Enter);
        inputSource.EnqueueKey(ConsoleKey.Escape);
        inputSource.Complete();
    });

    await host.RunAsync();

    // Verify via ViewModel state
}
```

### Timing Considerations

Input processing is asynchronous. Use delays between related actions:

```csharp
// Good - allows processing time
inputSource.EnqueueKey(ConsoleKey.A);
await Task.Delay(50);
inputSource.EnqueueString("text");

// Risky - might race
inputSource.EnqueueKey(ConsoleKey.A);
inputSource.EnqueueString("text");  // Might process before mode change
```

## Continuous Integration

Example GitHub Actions workflow:

```yaml
- name: Run Demo Tests
  run: |
    dotnet run --project demos/Termina.Demo.RegionBased -- --test
    dotnet run --project demos/Termina.Demo.Streaming -- --test
```

The `--test` flag ensures demos exit automatically after scripted input.
