using R3;
using Termina.Extensions;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.WheelScrollWorks.Pages;

/// <summary>
/// Working counterpart to <c>Termina.Demo.WheelAmbiguity</c>. The ViewModel
/// re-enables full mouse tracking, so wheel ticks arrive as MouseScrollEvent
/// and we can forward them to the StreamingTextNode.
/// </summary>
public class WheelScrollWorksPage : ReactivePage<WheelScrollWorksViewModel>
{
    private StreamingTextNode _history = null!;
    private TextInputNode _input = null!;

    protected override void OnBound()
    {
        base.OnBound();

        _history = StreamingTextNode.Create()
            .WithPrefix("  ", Color.Gray)
            .WithScrollbar();

        for (var i = 1; i <= 100; i++)
        {
            _history.AppendLine(
                $"Line {i,3}: wheel-scroll over this panel — it should actually scroll now.",
                Color.White);
        }

        _input = new TextInputNode()
            .WithPlaceholder("Focused input — wheel goes to the history, not here.")
            .WithForeground(Color.Cyan);

        foreach (var c in "abcdefghijklmnopqrstuvwxyz")
            _input.HandleInput(new ConsoleKeyInfo(c, ConsoleKey.A, false, false, false));
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        // Keyboard: route navigation keys to the history, everything else to the input.
        ViewModel.Input.OfType<IInputEvent, KeyPressed>()
            .Subscribe(k =>
            {
                var key = k.KeyInfo.Key;
                if (key == ConsoleKey.PageUp || key == ConsoleKey.PageDown ||
                    (k.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) &&
                     (key == ConsoleKey.Home || key == ConsoleKey.End)))
                {
                    _history.HandleInput(k.KeyInfo, viewportHeight: 10, viewportWidth: 80);
                    return;
                }

                if (key == ConsoleKey.Q && k.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                    return;

                _input.HandleInput(k.KeyInfo);
            })
            .DisposeWith(Subscriptions);

        // Mouse wheel: forward MouseScrollEvent to the (non-focusable) history.
        // This is the path that lights up because the ViewModel called EnableMouse().
        ViewModel.Input.OfType<IInputEvent, MouseScrollEvent>()
            .Subscribe(scroll =>
            {
                IScrollable s = _history;
                if (scroll.Delta > 0) s.ScrollUp(3);
                else s.ScrollDown(3);
            })
            .DisposeWith(Subscriptions);
    }

    public override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            .WithChild(
                new TextNode("Wheel-Scroll Works Demo  (EnableMouse() ?1000h re-enabled)")
                    .WithForeground(Color.Green)
                    .Bold()
                    .Height(1))
            .WithChild(
                new TextNode("Wheel over the history below scrolls it. Hold Shift (Option on macOS) to text-select.")
                    .WithForeground(Color.BrightBlack)
                    .Height(1))
            .WithChild(new EmptyNode().Height(1))
            .WithChild(
                new PanelNode()
                    .WithTitle("History (wheel scrolls — try it)")
                    .WithTitleColor(Color.Green)
                    .WithBorder(BorderStyle.Rounded)
                    .WithBorderColor(Color.Green)
                    .WithContent(_history.Fill())
                    .Fill())
            .WithChild(
                new PanelNode()
                    .WithTitle("Focused input (wheel does NOT reach here)")
                    .WithTitleColor(Color.Cyan)
                    .WithBorder(BorderStyle.Rounded)
                    .WithBorderColor(Color.Cyan)
                    .WithContent(_input)
                    .Height(3))
            .WithChild(
                Observable.CombineLatest(
                        ViewModel.LastKey,
                        ViewModel.MouseScrollCount,
                        (lastKey, scrolls) =>
                            $"  Last key: {lastKey,-20}   MouseScrollEvents: {scrolls,3}")
                    .Select<string, ILayoutNode>(s => new TextNode(s).WithForeground(Color.White).NoWrap())
                    .AsLayout()
                    .Height(1))
            .WithChild(
                new TextNode("  [Wheel] scroll history   [PgUp/PgDn] keyboard scroll   [Ctrl+Q] quit")
                    .WithForeground(Color.BrightBlack)
                    .Height(1));
    }
}
