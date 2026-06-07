# Terminal Input Modes

Termina supports two runtime input strategies:

- legacy mouse tracking, which is the compatibility-first default
- raw input plus alternate-scroll, which is the higher-fidelity mode for apps like NetClaw

This page explains when to use each mode, how to configure them, and what to expect in tmux and kitty-capable terminals.

::: tip Upgrading to 0.11.0
If you are upgrading an app that needs native terminal selection, clipboard copy, or paste into focused inputs, start with the [0.11.0 upgrade advisory](/guide/upgrade-0.11).
:::

## Recommended Defaults

The framework default remains `LegacyMouseTracking`.

That means:

- Termina enables bracketed paste automatically
- Termina enables SGR mouse tracking for wheel events
- apps get `MouseScrollEvent` support without enabling raw input
- native terminal text selection is captured while mouse mode is active

This is the safest default because it works on the normal `Console.ReadKey()` path and does not depend on raw-byte parsing.

## When To Use Alternate Scroll

Use `AlternateScroll` when your app needs all of the following:

- mouse wheel scrolling
- native terminal selection and copy behavior
- clear separation between wheel ticks and arrow keys

This mode works by combining:

- raw input transport
- xterm alternate-scroll (`CSI ?1007h`)
- optionally kitty keyboard reporting for richer key disambiguation

It is a good fit for chat-style interfaces, logs, and scroll-heavy panes where the user still wants the terminal to own click-drag selection.

## Configure Runtime Options

Use `ConfigureRuntime()` from `AddTermina()`:

```csharp
using Termina.Hosting;

builder.Services.AddTermina("/chat", termina =>
{
    termina.ConfigureRuntime(options =>
    {
        options.PreferRawInput = true;
        options.ScrollInputMode = ScrollInputMode.AlternateScroll;
        options.KittyKeyboardMode = KittyKeyboardMode.ReportAllKeysPlusDisambiguate;
        options.CtrlCHandlingMode = CtrlCHandlingMode.DoublePressWhenRawInput;
    });

    termina.RegisterRoute<ChatPage, ChatViewModel>("/chat");
});
```

## Option Reference

### `PreferRawInput`

When `true`, Termina prefers the raw-byte platform console implementation when one is available.

Raw input is required for:

- alternate-scroll wheel disambiguation
- kitty `report_all_keys`
- structured byte-level parsing of CSI-u input on Unix and Windows Terminal

If raw input is requested on Unix and stdin is not a TTY, Termina falls back cleanly instead of throwing during startup.

### `ScrollInputMode`

`LegacyMouseTracking`

- default
- most compatible
- captures wheel via SGR mouse events
- can interfere with native terminal selection while active

`AlternateScroll`

- opt-in
- only activated when raw input is actually active
- preserves native selection and clipboard integration
- falls back to legacy mouse tracking when raw input is unavailable

### `KittyKeyboardMode`

Termina can negotiate kitty keyboard protocol flags during app startup:

- `Off`
- `DisambiguateOnly` (`1`)
- `ReportAllKeys` (`8`)
- `ReportAllKeysPlusDisambiguate` (`9`)
- `ReportAllKeysWithEventTypes` (`11`)

Use `ReportAllKeys` or `ReportAllKeysPlusDisambiguate` when you want the parser to treat real arrows and wheel ticks as structurally distinct byte streams under alternate-scroll.

### `CtrlCHandlingMode`

`DoublePressWhenRawInput` is the default.

That means Termina only intercepts `Ctrl+C` globally when raw input is active. Normal console-mode apps keep their prior behavior.

## tmux Notes

If the app runs inside tmux, Termina forwards bracketed-paste and kitty setup sequences to the outer terminal using tmux passthrough.

Enable this in tmux:

```bash
set -g allow-passthrough on
```

Without that setting, tmux silently drops the forwarded setup sequences.

See [tmux Configuration](/concepts/tmux-configuration) for complete setup instructions, mouse mode considerations, and troubleshooting.

## Terminal Support Expectations

Best results for raw input + alternate-scroll + kitty keyboard:

- Ghostty
- kitty
- WezTerm
- foot
- iTerm2 3.5+
- Windows Terminal with raw input enabled

Older or unsupported terminals still work, but Termina may fall back to legacy mouse tracking or reduced key fidelity.

## Safe Rollout Pattern

For existing apps, keep defaults.

For apps like NetClaw, opt in explicitly and test:

1. direct terminal session
2. tmux session
3. supported kitty-capable terminal
4. unsupported terminal fallback

That lets you ship the better mode where it matters without changing behavior for every existing Termina app.
