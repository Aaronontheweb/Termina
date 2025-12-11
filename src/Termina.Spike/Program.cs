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

// Create input sources
var keyboard = new ConsoleInputSource();
var programmaticInput = new VirtualInputSource(); // For LLM or other programmatic input

// Create navigation service
var navigation = new NavigationService();

// Create mediator - wires up navigation automatically
var mediator = new EventMediator(
    [keyboard, programmaticInput],
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
