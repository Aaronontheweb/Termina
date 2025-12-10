using System.Threading.Channels;
using Spectre.Console;

// Week 1 Validation Spike: Minimal Event Loop + Spectre/ANSI Coexistence (Non-Interactive)

Console.WriteLine("Termina Week 1 Validation Spike");
Console.WriteLine("================================");
Console.WriteLine();

// Create unbounded channel for events (will be bounded in later phases)
var eventChannel = Channel.CreateUnbounded<string>();

// Track if we should exit
var cts = new CancellationTokenSource();

// Background task: Simulate app events
var simulatorTask = Task.Run(async () =>
{
    try
    {
        for (int i = 1; i <= 5; i++)
        {
            await Task.Delay(500, cts.Token);
            await eventChannel.Writer.WriteAsync($"Event {i}: Timer tick", cts.Token);
        }

        // Signal completion
        await eventChannel.Writer.WriteAsync("Exit", cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Normal shutdown
    }
}, cts.Token);

// Test Spectre.Console rendering
var panel = new Panel("This validates that Spectre.Console and ANSI coexistence works.")
{
    Header = new PanelHeader("Spectre.Console + Manual ANSI Test"),
    Border = BoxBorder.Rounded
};
AnsiConsole.Write(panel);
AnsiConsole.WriteLine();

Console.WriteLine("Event Log:");
Console.WriteLine("----------");

try
{
    await foreach (var evt in eventChannel.Reader.ReadAllAsync(cts.Token))
    {
        // Write event with ANSI color codes
        Console.WriteLine($"\x1b[36m[{DateTime.Now:HH:mm:ss}]\x1b[0m {evt}");

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
    await simulatorTask;

    Console.WriteLine();
    AnsiConsole.MarkupLine("[green]✓ Week 1 Validation Complete: Channels + Spectre/ANSI coexistence confirmed![/]");
}
