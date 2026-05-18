using R3;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Demo.WheelAmbiguity.Pages;

/// <summary>
/// ViewModel for the wheel-vs-arrow ambiguity demo.
/// Tracks the most recent keypress and mouse-scroll event so the user can see
/// what events the framework is actually receiving when they spin the wheel.
/// </summary>
public class WheelAmbiguityViewModel : ReactiveViewModel
{
    public ReactiveProperty<string> LastKey { get; } = new("(none yet)");
    public ReactiveProperty<int> ArrowUpCount { get; } = new(0);
    public ReactiveProperty<int> ArrowDownCount { get; } = new(0);
    public ReactiveProperty<int> MouseScrollCount { get; } = new(0);

    public override void OnActivated()
    {
        Input.OfType<IInputEvent, KeyPressed>()
            .Subscribe(HandleKey)
            .DisposeWith(Subscriptions);

        Input.OfType<IInputEvent, MouseScrollEvent>()
            .Subscribe(_ => MouseScrollCount.Value++)
            .DisposeWith(Subscriptions);
    }

    private void HandleKey(KeyPressed key)
    {
        var info = key.KeyInfo;
        LastKey.Value = info.Modifiers == 0
            ? info.Key.ToString()
            : $"{info.Modifiers}+{info.Key}";

        if (info.Key == ConsoleKey.UpArrow && info.Modifiers == 0)
            ArrowUpCount.Value++;
        else if (info.Key == ConsoleKey.DownArrow && info.Modifiers == 0)
            ArrowDownCount.Value++;

        if (info.Key == ConsoleKey.Q && info.Modifiers.HasFlag(ConsoleModifiers.Control))
            RequestShutdown();
    }

    public override void Dispose()
    {
        LastKey.Dispose();
        ArrowUpCount.Dispose();
        ArrowDownCount.Dispose();
        MouseScrollCount.Dispose();
        base.Dispose();
    }
}
