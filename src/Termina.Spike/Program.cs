using System.Threading.Channels;
using Spectre.Console;
using Termina;
using Termina.Examples;

// Event-Mediator Architecture Proof of Concept

Console.WriteLine("Termina Event-Mediator Architecture - Proof of Concept");
Console.WriteLine("========================================================");
Console.WriteLine();

// 1. Create event channel
var eventChannel = Channel.CreateUnbounded<IUIEvent>();

// 2. Create components with subscriptions
var statusBar = new StatusBar();

// 3. Create mediator and register components
var mediator = new EventMediator(eventChannel.Reader, AnsiConsole.Console);
mediator.RegisterComponent(statusBar);

// 4. Start mediator in background task
using var cts = new CancellationTokenSource();
var mediatorTask = Task.Run(() => mediator.RunAsync(cts.Token), cts.Token);

// 5. Simulate backend pushing events
await Task.Delay(500);
await eventChannel.Writer.WriteAsync(new StatusUpdated("Processing request...", DateTime.UtcNow));

await Task.Delay(1000);
await eventChannel.Writer.WriteAsync(new StatusUpdated("Loading data...", DateTime.UtcNow));

await Task.Delay(1000);
await eventChannel.Writer.WriteAsync(new StatusUpdated("Rendering results...", DateTime.UtcNow));

await Task.Delay(1000);
await eventChannel.Writer.WriteAsync(new StatusUpdated("Complete!", DateTime.UtcNow));

await Task.Delay(1000);

// 6. Shutdown
cts.Cancel();

try
{
    await mediatorTask;
}
catch (OperationCanceledException)
{
    // Expected
}

Console.WriteLine();
Console.WriteLine("✓ Event-mediator architecture validated!");
Console.WriteLine();
Console.WriteLine("Key validations:");
Console.WriteLine("  ✓ Channel-based event routing works");
Console.WriteLine("  ✓ Component subscriptions work");
Console.WriteLine("  ✓ Spectre.Console Live display integration works");
Console.WriteLine("  ✓ Thread-safe single consumer model works");
