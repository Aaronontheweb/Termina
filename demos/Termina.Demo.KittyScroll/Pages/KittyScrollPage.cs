using R3;
using Termina.Extensions;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.KittyScroll.Pages;

/// <summary>
/// Prototype of "Option B" wheel-vs-arrow disambiguation: kitty keyboard
/// <c>report_all_keys</c> (CSI &gt; 8 u) + <c>?1007h</c> alternate-scroll.
///
/// Wheel ticks → <see cref="MouseScrollEvent"/> → scrolls the history. Real
/// arrow keys → <see cref="KeyPressed"/> → only update the on-screen counters
/// (intentionally NOT routed to the history, so the disambiguation is visible
/// to the eye). Native click-drag text selection keeps working because we
/// never enable full mouse tracking.
/// </summary>
public class KittyScrollPage : ReactivePage<KittyScrollViewModel>
{
    private StreamingTextNode _history = null!;

    protected override void OnBound()
    {
        base.OnBound();

        _history = StreamingTextNode.Create()
            .WithPrefix("  ", Color.Gray)
            .WithScrollbar();

        for (var i = 1; i <= 200; i++)
        {
            _history.AppendLine(
                $"Line {i,3}: scroll the wheel here — the history should move; arrows should NOT.",
                Color.White);
        }
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        // Wheel scrolls the history. Under kitty keyboard + ?1007h, this fires
        // for wheel ticks but NOT for arrow keypresses — that's the whole point.
        ViewModel.Input.OfType<IInputEvent, MouseScrollEvent>()
            .Subscribe(scroll =>
            {
                if (scroll.Delta > 0) ((IScrollable)_history).ScrollUp(3);
                else ((IScrollable)_history).ScrollDown(3);
            })
            .DisposeWith(Subscriptions);

        // Arrows deliberately do nothing to the history here, so you can verify
        // that pressing them physically does not move the scrollback — only the
        // arrow counters in the status line tick.
        ViewModel.Input.OfType<IInputEvent, KeyPressed>()
            .Subscribe(k =>
            {
                var key = k.KeyInfo.Key;
                // PgUp/PgDn keyboard fallback for terminals without kitty support.
                if (key == ConsoleKey.PageUp || key == ConsoleKey.PageDown)
                    _history.HandleInput(k.KeyInfo, viewportHeight: 10, viewportWidth: 80);
            })
            .DisposeWith(Subscriptions);
    }

    public override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            .WithChild(
                new TextNode("Kitty-keyboard wheel-scroll prototype  (CSI > 8 u + ?1007h)")
                    .WithForeground(Color.Yellow)
                    .Bold()
                    .Height(1))
            .WithChild(
                new TextNode("Wheel → scrolls history.  Arrows → counters only.  Click-drag → native selection.")
                    .WithForeground(Color.BrightBlack)
                    .Height(1))
            .WithChild(new EmptyNode().Height(1))
            .WithChild(
                new PanelNode()
                    .WithTitle("History (200 lines — try the mouse wheel)")
                    .WithTitleColor(Color.Yellow)
                    .WithBorder(BorderStyle.Rounded)
                    .WithBorderColor(Color.Yellow)
                    .WithContent(_history.Fill())
                    .Fill())
            .WithChild(
                Observable.CombineLatest(
                        ViewModel.LastKey,
                        ViewModel.ArrowUpCount,
                        ViewModel.ArrowDownCount,
                        ViewModel.OtherKeyCount,
                        ViewModel.WheelUpCount,
                        ViewModel.WheelDownCount,
                        (last, au, ad, ok, wu, wd) =>
                            $"  Last key: {last,-18}  Arrow ↑:{au,3} ↓:{ad,3}  Other:{ok,3}   Wheel ↑:{wu,3} ↓:{wd,3}")
                    .Select<string, ILayoutNode>(s => new TextNode(s).WithForeground(Color.White).NoWrap())
                    .AsLayout()
                    .Height(1))
            .WithChild(
                new TextNode("  [PgUp/PgDn] keyboard scroll fallback   [Ctrl+Q] quit")
                    .WithForeground(Color.BrightBlack)
                    .Height(1));
    }
}
