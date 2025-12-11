using Spectre.Console;
using Termina;
using Termina.Input;
using Termina.Pages;
using Termina.Spike.Pages;

// Termina Two-Tier Event Architecture Demo
// ==========================================
//
// This demonstrates:
// - TerminaApplication orchestrator with duplex event loop
// - PageBase + PageHandler architecture
// - Stateless handlers (no mutable state in handlers)
// - Typed UI events transformed from raw input
// - Commands from handler to page for UI updates
// - Model events from backend to handler (via IApplicationBus)
// - Navigation with ResetOnNavigation vs PreserveState

// Check for --test flag (used in CI/CD to run scripted test and exit)
var testMode = args.Contains("--test");

// Create the application
var app = new TerminaApplication(AnsiConsole.Console);

// Register pages with their handlers
// MainMenu: ResetOnNavigation - always starts fresh
app.RegisterPage<MainMenuHandler>("main-menu");

// Settings: PreserveState - form data persists across visits
app.RegisterPage<SettingsHandler>("settings", NavigationBehavior.PreserveState);

// About: ResetOnNavigation - static content
app.RegisterPage<AboutHandler>("about");

// Set up input sources
if (testMode)
{
    // Test mode: use scripted input that navigates to Exit
    var scriptedInput = new VirtualInputSource();
    app.AddInputSource(scriptedInput);

    // Queue up scripted input to navigate to Exit and select it
    _ = Task.Run(async () =>
    {
        await Task.Delay(500); // Wait for initial render
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow); // Settings -> About
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow); // About -> Exit
        scriptedInput.EnqueueKey(ConsoleKey.Enter);     // Select Exit
        scriptedInput.Complete();
    });
}
else
{
    // Normal mode: keyboard input
    app.AddInputSource(new ConsoleInputSource());
}

// Optional: Subscribe to dead letter events for debugging
app.Bus.DeadLetter += (evt, reason) =>
{
    // In a real app, you'd log this
    // Console.WriteLine($"[DeadLetter] {evt.GetType().Name}: {reason}");
};

// Navigate to initial page
app.NavigateTo("main-menu");

// Run the application
using var cts = new CancellationTokenSource();

// Handle Ctrl+C gracefully
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await app.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // Normal shutdown
}

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[green]Thanks for trying Termina![/]");
