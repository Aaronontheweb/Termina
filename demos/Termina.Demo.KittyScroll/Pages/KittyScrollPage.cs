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
/// <c>report_all_keys</c> (CSI &gt; 8 u) + <c>?1007h</c> alternate-scroll, in
/// an AI-chat-style harness so you can verify the focused input doesn't eat
/// wheel ticks and that double-press Ctrl+C exits cleanly even while typing.
///
/// Layout:
/// <list type="bullet">
///   <item>Top: scrollable history panel (StreamingTextNode, pre-populated).</item>
///   <item>Middle: status line (last key + per-class counters).</item>
///   <item>Bottom: TextInputNode (focused). Press Enter to submit the message
///         into the history. Arrows move the cursor; wheel scrolls history.</item>
/// </list>
/// </summary>
public class KittyScrollPage : ReactivePage<KittyScrollViewModel>
{
    private StreamingTextNode _history = null!;
    private TextInputNode _input = null!;

    public KittyScrollPage()
    {
        FocusPolicy = FocusPolicy.FirstFocusable;
    }

    protected override void OnBound()
    {
        base.OnBound();

        _history = StreamingTextNode.Create()
            .WithPrefix("  ", Color.Gray)
            .WithScrollbar();

        for (var i = 1; i <= 200; i++)
        {
            _history.AppendLine(
                $"Line {i,3}: scroll the wheel here — focus stays in the input below.",
                Color.White);
        }

        _input = new TextInputNode()
            .WithPlaceholder("Type a message and press Enter (Ctrl+C twice to quit)…")
            .WithForeground(Color.Cyan);
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        // Echo submitted text into the history (AI-harness style).
        _input.Submitted
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Subscribe(text =>
            {
                _history.AppendLine($"you> {text}", Color.BrightCyan);
                _history.AppendLine($"bot> echo: {text}", Color.BrightGreen);
                _input.Clear();
            })
            .DisposeWith(Subscriptions);

        // Wheel ticks fall through here whenever the focused node isn't IScrollable
        // (the TextInputNode is the typical focus). Under kitty + ?1007h these only
        // arrive on real wheel events — bare arrows go to the focused input via the
        // FocusManager and act as cursor moves.
        ViewModel.Input.OfType<IInputEvent, MouseScrollEvent>()
            .Subscribe(scroll =>
            {
                if (scroll.Delta > 0) ((IScrollable)_history).ScrollUp(3);
                else ((IScrollable)_history).ScrollDown(3);
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
                new TextNode("Wheel scrolls history.  Arrows move input cursor.  Ctrl+C twice to quit.")
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
                new PanelNode()
                    .WithTitle("Input  (Enter to send · ↑/↓ history · Ctrl+C×2 quit)")
                    .WithTitleColor(Color.Cyan)
                    .WithBorder(BorderStyle.Rounded)
                    .WithBorderColor(Color.Cyan)
                    .WithContent(_input)
                    .Height(3));
    }
}

