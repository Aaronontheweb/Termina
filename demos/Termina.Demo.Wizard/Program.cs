// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Termina.Demo.Wizard.Pages;
using Termina.Diagnostics;
using Termina.Hosting;
using Termina.Input;
using Termina.Pages;

// Check for --test flag (used in CI/CD to run scripted test and exit)
var testMode = args.Contains("--test");

var builder = Host.CreateApplicationBuilder(args);

// Suppress host/console logging so it doesn't bleed into the TUI's rendered output.
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Enable file tracing for debugging
var traceFile = Path.Combine(Path.GetTempPath(), "termina-wizard-trace.log");
builder.Services.AddTerminaFileTracing(traceFile, TerminaTraceCategory.All, TerminaTraceLevel.Trace);
Console.Error.WriteLine($"Trace log: {traceFile}");

// Set up input source based on mode
VirtualInputSource? scriptedInput = null;
if (testMode)
{
    scriptedInput = new VirtualInputSource();
    builder.Services.AddTerminaVirtualInput(scriptedInput);
}

// Register Termina with the wizard page
builder.Services.AddTermina("/wizard", termina =>
{
    termina.RegisterRoute<SetupWizardPage, SetupWizardViewModel>("/wizard", NavigationBehavior.PreserveState);
});

var host = builder.Build();

if (testMode && scriptedInput != null)
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(500); // Wait for initial render

        // Step 1: Provider selection
        // Navigate to "Azure" (second item)
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow);
        await Task.Delay(100);
        // Select it
        scriptedInput.EnqueueKey(ConsoleKey.Enter);
        await Task.Delay(200);

        // Step 2: Auth - type a username
        scriptedInput.EnqueueString("admin");
        await Task.Delay(100);
        scriptedInput.EnqueueKey(ConsoleKey.Enter);
        await Task.Delay(200);

        // Step 3: Confirm - press Enter to complete
        scriptedInput.EnqueueKey(ConsoleKey.Enter);
        await Task.Delay(300);

        // Quit
        scriptedInput.EnqueueKey(ConsoleKey.Q);
        scriptedInput.Complete();
    });
}

await host.RunAsync();
