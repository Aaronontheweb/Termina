using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Termina.Demo.Gallery;
using Termina.Diagnostics;
using Termina.Demo.Gallery.Pages;
using Termina.Hosting;
using Termina.Input;
using Termina.Pages;

// Check for --test flag (used in CI/CD to run scripted test and exit)
var testMode = args.Contains("--test");

var builder = Host.CreateApplicationBuilder(args);

var traceDir = Path.Combine(Path.GetTempPath(), "termina-logs");
Directory.CreateDirectory(traceDir);
var traceFile = Path.Combine(traceDir, $"gallery-trace-{DateTime.Now:yyyyMMdd-HHmmss}.log");

builder.Services.AddTerminaFileTracing(traceFile, TerminaTraceCategory.All, TerminaTraceLevel.Trace);
builder.Services.AddSingleton(new TraceFileInfo(traceFile));

// Set up input source based on mode
VirtualInputSource? scriptedInput = null;
if (testMode)
{
    scriptedInput = new VirtualInputSource();
    builder.Services.AddTerminaVirtualInput(scriptedInput);
}

// Register Termina with gallery pages using route-based navigation
builder.Services.AddTermina("/menu", termina =>
{
    termina.RegisterRoute<GalleryMenuPage, GalleryMenuViewModel>("/menu", NavigationBehavior.PreserveState);
    termina.RegisterRoute<SelectionListGalleryPage, SelectionListGalleryViewModel>("/selection", NavigationBehavior.PreserveState);
    termina.RegisterRoute<TextInputGalleryPage, TextInputGalleryViewModel>("/textinput", NavigationBehavior.PreserveState);
    termina.RegisterRoute<ClipboardGalleryPage, ClipboardGalleryViewModel>("/clipboard", NavigationBehavior.PreserveState);
    termina.RegisterRoute<LayoutGalleryPage, LayoutGalleryViewModel>("/layouts", NavigationBehavior.PreserveState);
    termina.RegisterRoute<AnimationsGalleryPage, AnimationsGalleryViewModel>("/animations", NavigationBehavior.PreserveState);
});

var host = builder.Build();

if (testMode && scriptedInput != null)
{
    // Queue up scripted input to test basic functionality then quit
    _ = Task.Run(async () =>
    {
        await Task.Delay(500); // Wait for initial render

        // Navigate through menu
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow);
        scriptedInput.EnqueueKey(ConsoleKey.Enter); // Enter SelectionList gallery
        await Task.Delay(300);

        // Navigate in selection list
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow);
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow);
        scriptedInput.EnqueueKey(ConsoleKey.Spacebar);

        // Return to menu
        scriptedInput.EnqueueKey(ConsoleKey.Escape);
        await Task.Delay(200);

        // Quit
        scriptedInput.EnqueueKey(ConsoleKey.Q);
        scriptedInput.Complete();
    });
}

await host.RunAsync();
