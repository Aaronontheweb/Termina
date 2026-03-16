# Clipboard And Feedback

Termina's clipboard support is terminal-native first. It is designed to work in local terminals, remote SSH sessions, and tmux-based workflows without dropping into platform-specific clipboard commands.

## Clipboard Architecture

Clipboard requests flow through `IClipboardService`, which fans out to one or more transports.

- `Osc52ClipboardTransport` sends OSC 52 clipboard sequences through the active terminal
- `TmuxClipboardTransport` integrates with tmux when the `TMUX` environment variable is present

This keeps the application-facing API simple:

```csharp
var copied = clipboardService.Copy(url);
```

The service returns `true` when at least one transport reports success.

## Why Multiple Transports?

Terminal clipboard behavior varies across terminal emulators and multiplexers.

- OSC 52 is the clean terminal-native path
- tmux often needs explicit clipboard handoff even when OSC 52 is emitted correctly

Using transports lets Termina keep those concerns out of pages and components.

## Toast Notifications

`IToastService` exposes a single active toast as an observable. `TerminaApplication` hosts the overlay globally, so pages do not need to render their own popup shell.

```csharp
toastService.Show(
    "Copied to clipboard",
    new ToastOptions(
        Duration: TimeSpan.FromSeconds(2),
        Position: ToastPosition.TopRight));
```

This keeps feedback decoupled from the component that triggered it.

## Inline Feedback

If you do not want a global toast, components like `CopyableTextNode` can show local feedback instead.

Examples:

- a green check mark beside the copied value
- a short `Copied` label near the field
- both inline feedback and a toast

## Diagnostics

When file tracing is enabled, clipboard operations produce trace output through `TerminaTrace`.

Useful trace points include:

- focused copyable node input handling
- clipboard transport selection and success
- toast emission and rendering

The gallery demo enables file tracing and surfaces the trace log path on the Clipboard page for easier troubleshooting.
