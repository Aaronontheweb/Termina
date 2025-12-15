// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Termina.Demo.V2;
using Termina.Hosting;
using Termina.Input;

// Check for --test flag (used in CI/CD to run scripted test and exit)
var testMode = args.Contains("--test");

var builder = Host.CreateApplicationBuilder(args);

// Set up input source based on mode
VirtualInputSource? scriptedInput = null;
if (testMode)
{
    scriptedInput = new VirtualInputSource();
    builder.Services.AddTerminaVirtualInput(scriptedInput);
}

// Register Termina with the tree-based reactive page
builder.Services.AddTermina("/counter", termina =>
{
    termina.RegisterRoute<CounterPage, CounterViewModel>("/counter");
});

var host = builder.Build();

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

await host.RunAsync();
