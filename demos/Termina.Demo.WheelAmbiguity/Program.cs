using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Termina.Demo.WheelAmbiguity.Pages;
using Termina.Hosting;

// Termina.Demo.WheelAmbiguity
// -----------------------------------------------------------------------------
// Reproduces the wheel-vs-arrow ambiguity introduced by switching the framework
// from SGR mouse tracking (CSI ?1000h + ?1006h) to xterm alternate-scroll mode
// (CSI ?1007h). See: feature/alternate-scroll-mode branch and draft PR #215.
//
// The terminal translates wheel events into bare UpArrow/DownArrow keypresses,
// which are indistinguishable from real keyboard arrows. Apps with an
// always-focused TextInputNode (chat-style UIs) will see the wheel move the
// text cursor instead of scrolling the surrounding view.
//
// What to try once it's running:
//   1. The TextInputNode at the bottom is focused on startup. Type a few
//      characters so the cursor isn't at position 0.
//   2. Hover the mouse over the upper (yellow) history panel and scroll the
//      wheel. Expected: the history scrolls. Actual: the text-input cursor
//      jumps left/right because each wheel tick is delivered as Up/Down arrow,
//      which the focused TextInputNode consumes as cursor movement.
//   3. The right-hand "Last events" panel logs every KeyPressed and any
//      MouseScrollEvent (the latter only fires if you've manually re-enabled
//      full mouse mode), so you can correlate physical actions with the
//      events the framework actually receives.
//   4. PgUp / PgDn always scrolls the history — that's the documented
//      keyboard fallback. Ctrl+Q quits.
//
// Compare against the previous default (?1000h mouse tracking):
//   - Wheel produces MouseScrollEvent, which TerminaApplication routes to the
//     focused IScrollable. But the host terminal stops handling click-drag
//     selection for the duration of the run.
// -----------------------------------------------------------------------------

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddTermina("/wheel", termina =>
{
    termina.RegisterRoute<WheelAmbiguityPage, WheelAmbiguityViewModel>("/wheel");
});

await builder.Build().RunAsync();
