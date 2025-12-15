// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.App;
using Termina.Demo.V2;
using Termina.Input;
using Termina.Terminal;

// Check for --test flag (used in CI/CD to run scripted test and exit)
var testMode = args.Contains("--test");

// Create app - headless for testing, console for interactive
ReactiveTerminaApp app;
VirtualInputSource? scriptedInput = null;

if (testMode)
{
    var terminal = new VirtualTerminal(80, 24);
    scriptedInput = new VirtualInputSource();
    app = ReactiveTerminaApp.CreateHeadless(terminal, scriptedInput);
}
else
{
    app = ReactiveTerminaApp.CreateConsole();
}

// Create page and view model
var viewModel = new CounterViewModel();
var page = new CounterPage();

// Set up the page with ViewModel
app.SetPage(page, viewModel);

// Test mode - queue up scripted input then quit
if (testMode && scriptedInput != null)
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(100); // Wait for initial render

        // Increment counter
        scriptedInput.EnqueueKey(ConsoleKey.UpArrow);
        scriptedInput.EnqueueKey(ConsoleKey.UpArrow);
        scriptedInput.EnqueueKey(ConsoleKey.UpArrow);
        await Task.Delay(50);

        // Decrement counter
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow);
        await Task.Delay(50);

        // Type a message
        scriptedInput.EnqueueString("Hello from test!");
        await Task.Delay(50);
        scriptedInput.EnqueueKey(ConsoleKey.Enter);
        await Task.Delay(50);

        // Type another message
        scriptedInput.EnqueueString("Testing reactive v2");
        scriptedInput.EnqueueKey(ConsoleKey.Enter);
        await Task.Delay(50);

        // Quit
        scriptedInput.EnqueueKey(ConsoleKey.Escape);
        scriptedInput.Complete();
    });
}

// Run the app
await app.RunAsync();

// Print final state for test verification
if (testMode)
{
    Console.WriteLine($"\n--- Test Results ---");
    Console.WriteLine($"Counter: {viewModel.Count}");
    Console.WriteLine($"Messages: {viewModel.Messages.Count}");
    foreach (var msg in viewModel.Messages)
        Console.WriteLine($"  {msg}");
}

await app.DisposeAsync();
