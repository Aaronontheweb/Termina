using R3;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Demo.KittyScroll.Pages;

/// <summary>
/// ViewModel for the kitty-keyboard wheel-scroll prototype demo. Tracks the most
/// recent keypress and per-class counters so the user can visually confirm that
/// wheel ticks arrive as <see cref="MouseScrollEvent"/> while arrows arrive as
/// <see cref="KeyPressed"/>.
/// </summary>
public class KittyScrollViewModel : ReactiveViewModel
{
    public ReactiveProperty<string> LastKey { get; } = new("(none yet)");
    public ReactiveProperty<int> ArrowUpCount { get; } = new(0);
    public ReactiveProperty<int> ArrowDownCount { get; } = new(0);
    public ReactiveProperty<int> OtherKeyCount { get; } = new(0);
    public ReactiveProperty<int> WheelUpCount { get; } = new(0);
    public ReactiveProperty<int> WheelDownCount { get; } = new(0);

    public override void OnActivated()
    {
        Input.OfType<IInputEvent, KeyPressed>()
            .Subscribe(HandleKey)
            .DisposeWith(Subscriptions);

        Input.OfType<IInputEvent, MouseScrollEvent>()
            .Subscribe(scroll =>
            {
                if (scroll.Delta > 0) WheelUpCount.Value++;
                else WheelDownCount.Value++;
            })
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
        else
            OtherKeyCount.Value++;

        if (info.Key == ConsoleKey.Q && info.Modifiers.HasFlag(ConsoleModifiers.Control))
            RequestShutdown();
    }

    public override void Dispose()
    {
        LastKey.Dispose();
        ArrowUpCount.Dispose();
        ArrowDownCount.Dispose();
        OtherKeyCount.Dispose();
        WheelUpCount.Dispose();
        WheelDownCount.Dispose();
        base.Dispose();
    }
}
