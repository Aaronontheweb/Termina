using R3;
using Termina.Extensions;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.WheelAmbiguity.Pages;

/// <summary>
/// Demonstrates the wheel-vs-arrow ambiguity caused by alternate-scroll mode
/// (CSI ?1007h). Layout:
/// <list type="bullet">
///   <item>Top: scrollable history panel (StreamingTextNode, pre-populated).</item>
///   <item>Middle: always-focused TextInputNode that consumes Up/Down arrows.</item>
///   <item>Bottom: live status of last key + arrow / scroll counters.</item>
/// </list>
/// Spin the mouse wheel over the history panel and observe that the text-input
/// cursor moves instead of the history scrolling.
/// </summary>
public class WheelAmbiguityPage : ReactivePage<WheelAmbiguityViewModel>
{
    private StreamingTextNode _history = null!;
    private TextInputNode _input = null!;

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        _history = StreamingTextNode.Create()
            .WithPrefix("  ", Color.Gray)
            .WithScrollbar();

        for (var i = 1; i <= 100; i++)
        {
            _history.AppendLine(
                $"Line {i,3}: scroll the wheel over this panel and watch the input cursor below.",
                Color.White);
        }

        _input = new TextInputNode()
            .WithPlaceholder("Type here, then scroll the wheel over the history above…")
            .WithForeground(Color.Cyan);

        // Pre-fill the input so cursor motion is visible.
        foreach (var c in "abcdefghijklmnopqrstuvwxyz")
            _input.HandleInput(new ConsoleKeyInfo(c, ConsoleKey.A, false, false, false));

        // Route keys: PgUp/PgDn -> history; everything else -> focused input.
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

                // Quit shortcut handled by ViewModel.
                if (key == ConsoleKey.Q && k.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                    return;

                // Everything else (including bare Up/Down from the wheel under ?1007h)
                // goes to the focused TextInputNode — this is where the ambiguity bites.
                _input.HandleInput(k.KeyInfo);
            })
            .DisposeWith(Subscriptions);

        // If the renderer still has full mouse mode enabled (e.g. EnableMouse()
        // called manually), MouseScrollEvents will arrive here too. With
        // ?1007h-only they will not — and that's the whole point of the demo.
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
                new TextNode("Wheel-vs-Arrow Ambiguity Demo  (?1007h alternate-scroll mode)")
                    .WithForeground(Color.Yellow)
                    .Bold()
                    .Height(1))
            .WithChild(
                new TextNode("Scroll the wheel over the history below. Expected: history scrolls. Actual: input cursor moves.")
                    .WithForeground(Color.BrightBlack)
                    .Height(1))
            .WithChild(new EmptyNode().Height(1))
            .WithChild(
                new PanelNode()
                    .WithTitle("History (try mouse wheel here)")
                    .WithTitleColor(Color.Yellow)
                    .WithBorder(BorderStyle.Rounded)
                    .WithBorderColor(Color.Yellow)
                    .WithContent(_history.Fill())
                    .Fill())
            .WithChild(
                new PanelNode()
                    .WithTitle("Focused input — wheel ticks land HERE as Up/Down arrows")
                    .WithTitleColor(Color.Cyan)
                    .WithBorder(BorderStyle.Rounded)
                    .WithBorderColor(Color.Cyan)
                    .WithContent(_input)
                    .Height(3))
            .WithChild(
                Observable.CombineLatest(
                        ViewModel.LastKey,
                        ViewModel.ArrowUpCount,
                        ViewModel.ArrowDownCount,
                        ViewModel.MouseScrollCount,
                        (lastKey, up, down, scrolls) =>
                            $"  Last key: {lastKey,-20}  ↑ bare-arrow: {up,3}   ↓ bare-arrow: {down,3}   MouseScrollEvents: {scrolls,3}")
                    .Select<string, ILayoutNode>(s => new TextNode(s).WithForeground(Color.White).NoWrap())
                    .AsLayout()
                    .Height(1))
            .WithChild(
                new TextNode("  [PgUp/PgDn] scroll history   [Ctrl+Home/End] jump   [Ctrl+Q] quit")
                    .WithForeground(Color.BrightBlack)
                    .Height(1));
    }
}
