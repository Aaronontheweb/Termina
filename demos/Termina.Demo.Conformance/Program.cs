using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Termina.Conformance;
using Termina.Diagnostics;
using Termina.Hosting;

var argsSet = args.ToHashSet(StringComparer.OrdinalIgnoreCase);
var rawMode = argsSet.Contains("--raw-alt-scroll") || argsSet.Contains("--kitty-alt-scroll");
var kittyMode = argsSet.Contains("--kitty-alt-scroll")
    ? KittyKeyboardMode.ReportAllKeysPlusDisambiguate
    : KittyKeyboardMode.Off;

var outputDir = Path.Combine(Path.GetTempPath(), "termina-conformance");
Directory.CreateDirectory(outputDir);

var tracePath = Path.Combine(outputDir, "trace.log");
var eventLogPath = Path.Combine(outputDir, "events.jsonl");

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddTerminaFileTracing(tracePath, TerminaTraceCategory.All, TerminaTraceLevel.Trace);
builder.Services.AddSingleton(new ConformanceLogPaths(tracePath, eventLogPath));
builder.Services.AddSingleton<ConformanceRecorder>();

builder.Services.AddTermina("/", termina =>
{
    termina.ConfigureRuntime(options =>
    {
        options.PreferRawInput = rawMode;
        options.ScrollInputMode = rawMode ? ScrollInputMode.AlternateScroll : ScrollInputMode.LegacyMouseTracking;
        options.KittyKeyboardMode = kittyMode;
        options.CtrlCHandlingMode = CtrlCHandlingMode.DoublePressWhenRawInput;
    });

    termina.RegisterRoute<ConformancePage, ConformanceViewModel>("/");
});

await builder.Build().RunAsync();
