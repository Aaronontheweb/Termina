using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Termina.Demo.WheelScrollWorks.Pages;
using Termina.Hosting;

// Termina.Demo.WheelScrollWorks
// -----------------------------------------------------------------------------
// Counterpart to Termina.Demo.WheelAmbiguity. This demo opts back in to full
// SGR mouse mode (CSI ?1000h + ?1006h) via IAnsiTerminal.EnableMouse(), so the
// EscapeSequenceParser produces MouseScrollEvent for wheel ticks. The page
// subscribes to those events and forwards them to the StreamingTextNode --
// scrolling the history works as a user would expect.
//
// Tradeoff (the reason the framework default on this branch is ?1007h):
//   While EnableMouse() is active, the host terminal stops handling click-drag
//   text selection. Hold Shift (Option on macOS Terminal/iTerm) to force the
//   terminal's native selection back on while dragging.
// -----------------------------------------------------------------------------

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddTermina("/wheel-works", termina =>
{
    termina.RegisterRoute<WheelScrollWorksPage, WheelScrollWorksViewModel>("/wheel-works");
});

await builder.Build().RunAsync();
