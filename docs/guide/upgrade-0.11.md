# Upgrading to Termina 0.11.0

Termina 0.11.0 improves terminal input reliability and adds conformance coverage for native selection, clipboard copy, and paste into focused Termina inputs. This is not a breaking public API release: existing applications continue to receive the same public input event types.

This advisory is most relevant if your application needs users to copy text from the terminal screen and paste it back into a Termina input, especially when running inside tmux.

## Summary

| Area | Existing default | Recommended for native selection |
|------|------------------|-----------------------------------|
| Input transport | Standard console input | Raw input |
| Scroll mode | `LegacyMouseTracking` | `AlternateScroll` |
| Native terminal selection | Captured by mouse tracking | Preserved by the terminal |
| Wheel input | SGR mouse events | Alternate-scroll sequences |
| Public events | `KeyPressed`, `MouseScrollEvent`, `PasteEvent`, `ResizeEvent` | Same public events |

Existing apps keep the compatibility-first `LegacyMouseTracking` default. Apps that need native selection and clipboard workflows should opt into raw input plus alternate-scroll.

## Recommended Runtime Configuration

Configure Termina at startup:

```csharp
using Termina.Hosting;

builder.Services.AddTermina("/", termina =>
{
    termina.ConfigureRuntime(options =>
    {
        options.PreferRawInput = true;
        options.ScrollInputMode = ScrollInputMode.AlternateScroll;
        options.KittyKeyboardMode = KittyKeyboardMode.ReportAllKeysPlusDisambiguate;
        options.CtrlCHandlingMode = CtrlCHandlingMode.DoublePressWhenRawInput;
    });

    termina.RegisterRoute<HomePage, HomeViewModel>("/");
});
```

`AlternateScroll` only activates when raw input is active. If raw input is unavailable, Termina falls back to legacy mouse tracking rather than starting in a partially working mode.

## What Changed From Earlier Versions

Earlier Termina versions used legacy mouse tracking by default. That mode enables terminal mouse reporting so Termina can receive wheel events, but it also causes the terminal to stop owning normal click-drag text selection.

The recommended 0.11 configuration uses xterm alternate-scroll instead. In this mode Termina does not enable full mouse tracking, so the terminal keeps ownership of screen text selection and clipboard copy. Wheel input still reaches Termina through alternate-scroll sequences when raw input is active.

Termina 0.11 also hardens the internal input path:

- protocol-specific decoders now handle SGR mouse input, SS3 keys, CSI function keys, legacy CSI keys, Kitty keyboard formats, bracketed paste, and unknown sequence fallback
- decoded input is adapted back to the existing public event surface
- real Linux conformance tests now cover baseline PTY, tmux, kitty, and kitty plus tmux workflows
- conformance coverage verifies screen selection, explicit clipboard copy, clipboard paste into focused input, and bracketed paste delivery

## Paste Handling

Built-in Termina text inputs already handle bracketed paste. `TextInputNode` and `TextAreaNode` implement `IPasteReceiver`, so pasted content is routed directly to the focused input.

If you have a custom focusable input component, implement `IPasteReceiver`:

```csharp
using Termina.Input;

public sealed class CustomInputNode : LayoutNode, IFocusable, IPasteReceiver
{
    public bool HandlePaste(PasteEvent paste)
    {
        if (string.IsNullOrEmpty(paste.Content))
            return false;

        InsertTextAtCursor(paste.Content);
        return true;
    }
}
```

If no focused component handles paste, `PasteEvent` still falls through to the ViewModel input observable.

## tmux Configuration

Enable tmux passthrough so bracketed-paste and Kitty setup sequences can reach the outer terminal:

```bash
set -g allow-passthrough on
```

For the simplest native-selection behavior, keep tmux mouse mode off:

```bash
set -g mouse off
```

If tmux mouse mode is on, selection may require the terminal override gesture:

```text
Shift-drag select -> Ctrl+Shift+C -> Ctrl+Shift+V
```

That workflow uses native terminal selection. Pressing `Enter` after Shift-drag does not copy because you are not in tmux copy-mode. `Enter` copies only when you start selection from tmux copy-mode with `prefix + [`.

See [Running Termina Inside tmux](/concepts/tmux-configuration) for the full tmux setup.

## Validation Checklist

After upgrading, test the workflows your users actually use:

1. Run the app directly in your normal terminal.
2. Select text from the Termina screen and copy it with the terminal copy shortcut.
3. Paste into a focused Termina input and submit.
4. Repeat inside tmux.
5. If your app has custom input components, verify they implement `IPasteReceiver` or intentionally handle `PasteEvent` in the ViewModel.

If native selection still does not work, confirm that the app is using `PreferRawInput = true` and `ScrollInputMode.AlternateScroll`, then check whether tmux or the terminal emulator is intercepting mouse selection.

## Compatibility

No public input event migration is required for existing apps. These event types remain the supported public surface:

- `KeyPressed`
- `MouseScrollEvent`
- `PasteEvent`
- `ResizeEvent`

The new semantic input model is internal and exists to make terminal protocol decoding more reliable without forcing consumers to rewrite existing input handling code.
