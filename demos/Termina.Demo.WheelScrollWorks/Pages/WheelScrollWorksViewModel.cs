using R3;
using Termina.Input;
using Termina.Reactive;
using Termina.Terminal;

namespace Termina.Demo.WheelScrollWorks.Pages;

/// <summary>
/// ViewModel for the "wheel scroll works" demo. Opts in to full mouse mode so
/// that wheel ticks arrive as <see cref="MouseScrollEvent"/> rather than bare
/// arrow keypresses.
/// </summary>
public class WheelScrollWorksViewModel : ReactiveViewModel
{
    private readonly IAnsiTerminal _terminal;

    public ReactiveProperty<string> LastKey { get; } = new("(none yet)");
    public ReactiveProperty<int> MouseScrollCount { get; } = new(0);

    public WheelScrollWorksViewModel(IAnsiTerminal terminal)
    {
        _terminal = terminal;
    }

    public override void OnActivated()
    {
        // Opt back in to full SGR mouse mode. This is on top of the framework's
        // default ?1007h, so we still also get wheel-as-arrow keypresses; the
        // ?1000h MouseScrollEvent path simply takes precedence in our handler.
        _terminal.EnableMouse();

        Input.OfType<IInputEvent, KeyPressed>()
            .Subscribe(k =>
            {
                var info = k.KeyInfo;
                LastKey.Value = info.Modifiers == 0
                    ? info.Key.ToString()
                    : $"{info.Modifiers}+{info.Key}";

                if (info.Key == ConsoleKey.Q && info.Modifiers.HasFlag(ConsoleModifiers.Control))
                    RequestShutdown();
            })
            .DisposeWith(Subscriptions);

        Input.OfType<IInputEvent, MouseScrollEvent>()
            .Subscribe(_ => MouseScrollCount.Value++)
            .DisposeWith(Subscriptions);
    }

    public override void Dispose()
    {
        // Pair with EnableMouse() so the terminal isn't left in mouse-tracking
        // mode when the host exits.
        try { _terminal.DisableMouse(); } catch { /* terminal may be torn down */ }

        LastKey.Dispose();
        MouseScrollCount.Dispose();
        base.Dispose();
    }
}
