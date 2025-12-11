using Spectre.Console;
using Termina;
using Termina.Input;
using Termina.Navigation;
using Termina.Pages;
using Termina.Spike.Pages;

// Two-Tier Event Architecture - Full Vertical Slice Demo
// ========================================================
//
// This demonstrates:
// - Page + Handler architecture (MVVM-like)
// - Navigation with ResetOnNavigation vs PreserveState
// - SelectList and TextInput components
// - Low-level input handling (keyboard) vs business events
// - Multiple input sources (console + virtual)

// Check for --test flag (used in CI/CD to run scripted test and exit)
var testMode = args.Contains("--test");

// Create input sources
List<IInputSource> inputSources;
VirtualInputSource? scriptedInput = null;

if (testMode)
{
    // Test mode: use only scripted input that navigates to Exit
    scriptedInput = new VirtualInputSource();
    inputSources = [scriptedInput];
}
else
{
    // Normal mode: keyboard + programmatic input for LLM
    var keyboard = new ConsoleInputSource();
    var programmaticInput = new VirtualInputSource();
    inputSources = [keyboard, programmaticInput];
}

// Create navigation service
var navigation = new NavigationService();

// Create mediator - wires up navigation automatically
var mediator = new EventMediator(
    inputSources,
    AnsiConsole.Console,
    navigation
);

// Register pages
// MainMenu: ResetOnNavigation - always starts fresh
navigation.RegisterPage<MainMenuPage, MainMenuHandler>(
    "main-menu",
    NavigationBehavior.ResetOnNavigation);

// Settings: PreserveState - form data persists across visits
navigation.RegisterPage<SettingsPage, SettingsHandler>(
    "settings",
    NavigationBehavior.PreserveState);

// About: ResetOnNavigation - static content
navigation.RegisterPage<AboutPage, AboutHandler>(
    "about",
    NavigationBehavior.ResetOnNavigation);

// Navigate to initial page
navigation.NavigateTo("main-menu");

// In test mode, queue up scripted input to navigate to Exit and select it
if (testMode && scriptedInput != null)
{
    // Small delay to let the app initialize, then navigate to Exit (3rd option)
    _ = Task.Run(async () =>
    {
        await Task.Delay(500); // Wait for initial render
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow); // Settings -> About
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow); // About -> Exit
        scriptedInput.EnqueueKey(ConsoleKey.Enter);     // Select Exit
        scriptedInput.Complete();
    });
}

// Run the event loop
using var cts = new CancellationTokenSource();

// Handle Ctrl+C gracefully
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await mediator.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // Normal shutdown
}

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[green]Thanks for trying Termina![/]");
