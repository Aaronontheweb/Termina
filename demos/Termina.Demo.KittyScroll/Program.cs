using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Termina.Demo.KittyScroll.Pages;
using Termina.Hosting;

// Termina.Demo.KittyScroll
// -----------------------------------------------------------------------------
// Prototype of "Option B" from PR #215: combine the kitty keyboard protocol
// (CSI > 8 u — report_all_keys) with xterm alternate-scroll (CSI ?1007h) so
// real arrow keys and mouse-wheel ticks arrive as structurally different byte
// sequences, while preserving native click-drag text selection (no ?1000h
// mouse tracking is enabled).
//
// With kitty active:
//   - Real Up/Down arrow keys  → CSI A/B (or CSI 1;mods A) or CSI <PUA> u,
//                                 dispatched as KeyPressed(UpArrow/DownArrow).
//   - Mouse wheel up/down       → SS3 OA/OB (DECCKM on) — Ghostty's mouse
//                                 subsystem ignores the kitty enhancement.
//                                 Dispatched as MouseScrollEvent.
//
// Prerequisites for this demo to behave as advertised:
//   1. TERMINA_UNIX_RAW_INPUT=1 (so the raw-stdin UnixConsole is used —
//      Console.ReadKey would otherwise fold both forms to UpArrow before our
//      parser ever sees the bytes).
//   2. TERMINA_KITTY_KEYBOARD=8 (or 9 / 11 if you want disambiguate + report
//      bits as well — see Termina.Demo.RawStdinProbe for the spec).
//   3. A terminal that implements the kitty keyboard protocol. Confirmed:
//      Ghostty, kitty, foot, wezterm, iTerm2 ≥ 3.5. macOS Terminal.app does
//      NOT support it and will degrade to today's ambiguity.
//
// What to look for once it's running:
//   • Scroll the wheel over the history panel — the history should scroll
//     AND the "MouseScrollEvents" counter should increment.
//   • Press the bare arrow keys — the "Arrow ↑/↓" counter should increment
//     and history should NOT move (arrows are intentionally not routed to
//     the scrollable in this demo, to make the disambiguation visible).
//   • Click and drag with the mouse to select text — your terminal's native
//     selection should appear, because we never enabled mouse tracking.
//   • Ctrl+Q quits.
//
// Run:
//   TERMINA_UNIX_RAW_INPUT=1 TERMINA_KITTY_KEYBOARD=8 \
//     dotnet run --project demos/Termina.Demo.KittyScroll
// -----------------------------------------------------------------------------

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddTermina("/kitty", termina =>
{
    termina.RegisterRoute<KittyScrollPage, KittyScrollViewModel>("/kitty");
});

await builder.Build().RunAsync();
