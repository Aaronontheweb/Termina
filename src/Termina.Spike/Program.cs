using System.Threading.Channels;
using Spectre.Console;

// Week 1 Validation Spike: Minimal Event Loop + Keyboard Input + Spectre/ANSI Coexistence

// Create unbounded channel for events (will be bounded in later phases)
var eventChannel = Channel.CreateUnbounded<string>();

// Track if we should exit
var cts = new CancellationTokenSource();

// Background task: Non-blocking keyboard input polling
var keyboardTask = Task.Run(async () =>
{
    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);

                // Send key event to channel
                await eventChannel.Writer.WriteAsync($"Key: {key.Key}", cts.Token);

                // Exit on Escape
                if (key.Key == ConsoleKey.Escape)
                {
                    await eventChannel.Writer.WriteAsync("Exit", cts.Token);
                }
            }

            // 10ms polling interval (non-blocking)
            await Task.Delay(10, cts.Token);
        }
    }
    catch (OperationCanceledException)
    {
        // Normal shutdown
    }
}, cts.Token);

// Background task: Simulate app events
var simulatorTask = Task.Run(async () =>
{
    try
    {
        var counter = 0;
        while (!cts.Token.IsCancellationRequested)
        {
            await Task.Delay(2000, cts.Token); // Every 2 seconds
            await eventChannel.Writer.WriteAsync($"Timer: {++counter}", cts.Token);
        }
    }
    catch (OperationCanceledException)
    {
        // Normal shutdown
    }
}, cts.Token);

// Main event loop
Console.Clear();
AnsiConsole.MarkupLine("[bold yellow]Termina Week 1 Validation Spike[/]");
AnsiConsole.WriteLine();

// Test Spectre.Console rendering
var panel = new Panel("This is a [green]Spectre.Console[/] Panel!\nPress any key to see events. Press [red]ESC[/] to exit.")
{
    Header = new PanelHeader("Spectre.Console + Manual ANSI Coexistence"),
    Border = BoxBorder.Rounded
};
AnsiConsole.Write(panel);

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[dim]Event Log:[/]");

// Remember cursor position for event log (manual ANSI positioning)
var logStartRow = Console.CursorTop;
var eventCount = 0;

try
{
    await foreach (var evt in eventChannel.Reader.ReadAllAsync(cts.Token))
    {
        eventCount++;

        // Manual ANSI cursor positioning (coexisting with Spectre.Console above)
        Console.SetCursorPosition(0, logStartRow + (eventCount - 1) % 10);

        // Clear line and write event with ANSI color codes
        Console.Write($"\x1b[2K"); // Clear line
        Console.Write($"\x1b[36m[{DateTime.Now:HH:mm:ss}]\x1b[0m {evt}");

        if (evt == "Exit")
        {
            break;
        }
    }
}
catch (OperationCanceledException)
{
    // Normal shutdown
}
finally
{
    cts.Cancel();
    await Task.WhenAll(keyboardTask, simulatorTask);

    // Clean exit
    Console.SetCursorPosition(0, logStartRow + 11);
    AnsiConsole.MarkupLine("\n[green]✓[/] Week 1 Validation Complete: Channels + Keyboard + Spectre/ANSI coexistence confirmed!");
}
