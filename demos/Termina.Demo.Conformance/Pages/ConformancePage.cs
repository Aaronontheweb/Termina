using R3;
using Termina.Extensions;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Conformance;

public sealed class ConformancePage : ReactivePage<ConformanceViewModel>
{
    private StreamingTextNode _history = null!;
    private TextInputNode _input = null!;
    private TextNode _selectionTarget = null!;

    public ConformancePage()
    {
        FocusPolicy = FocusPolicy.FirstFocusable;
    }

    protected override void OnBound()
    {
        base.OnBound();

        _history = StreamingTextNode.Create()
            .WithPrefix("  ", Color.Gray)
            .WithScrollbar();

        for (var i = 1; i <= 160; i++)
        {
            _history.AppendLine($"History line {i,3}: wheel should scroll this pane without breaking text selection.", Color.White);
        }

        _selectionTarget = new TextNode("Selection sentinel: SELECTABLE SENTINEL 12345")
            .WithForeground(Color.Yellow)
            .NoWrap();

        _input = new TextInputNode()
            .WithPlaceholder("Type here, press Enter to submit. Up/Down should still be keyboard input.")
            .WithForeground(Color.Cyan);
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        _input.Submitted
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Subscribe(text =>
            {
                ViewModel.RecordSubmitted(text);
                _history.AppendLine($"submit> {text}", Color.BrightGreen);
                _input.Clear();
            })
            .DisposeWith(Subscriptions);

        _input.TextChanged
            .Subscribe(ViewModel.RecordInputTextChanged)
            .DisposeWith(Subscriptions);

        ViewModel.Input.OfType<IInputEvent, MouseScrollEvent>()
            .Subscribe(scroll =>
            {
                IScrollable scrollable = _history;
                if (scroll.Delta > 0)
                    scrollable.ScrollUp(3);
                else
                    scrollable.ScrollDown(3);
            })
            .DisposeWith(Subscriptions);

        KeyBindings.Register(ConsoleKey.Q, ConsoleModifiers.Control, Shutdown);
    }

    public override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            .WithChild(new TextNode("Termina terminal conformance harness").WithForeground(Color.Yellow).Bold().Height(1))
            .WithChild(new TextNode("Goals: preserve native selection, keep wheel distinct from arrows, and leave input behavior intact.")
                .WithForeground(Color.BrightBlack)
                .Height(1))
            .WithChild(new EmptyNode().Height(1))
            .WithChild(_selectionTarget.Height(1))
            .WithChild(
                new PanelNode()
                    .WithTitle("History")
                    .WithTitleColor(Color.Yellow)
                    .WithBorder(BorderStyle.Rounded)
                    .WithBorderColor(Color.Yellow)
                    .WithContent(_history.Fill())
                    .Fill())
            .WithChild(
                Observable.CombineLatest(
                        ViewModel.LastKeyboardEvent,
                        ViewModel.ArrowUpCount,
                        ViewModel.ArrowDownCount,
                        ViewModel.WheelUpCount,
                        ViewModel.WheelDownCount,
                        ViewModel.SubmittedCount,
                        ViewModel.LastSubmitted,
                        (last, au, ad, wu, wd, submitted, lastSubmitted) =>
                            $"LastKey:{last,-16} ArrowUp:{au,3} ArrowDown:{ad,3} WheelUp:{wu,3} WheelDown:{wd,3} Submitted:{submitted,3} LastSubmit:{lastSubmitted}")
                    .Select<string, ILayoutNode>(line => new TextNode(line).WithForeground(Color.White).NoWrap())
                    .AsLayout()
                    .Height(1))
            .WithChild(
                Observable.CombineLatest(
                        ViewModel.TracePath,
                        ViewModel.EventLogPath,
                        (tracePath, eventLogPath) => $"Trace: {tracePath}   Events: {eventLogPath}")
                    .Select<string, ILayoutNode>(line => new TextNode(line).WithForeground(Color.BrightBlack).NoWrap())
                    .AsLayout()
                    .Height(1))
            .WithChild(
                new PanelNode()
                    .WithTitle("Input")
                    .WithTitleColor(Color.Cyan)
                    .WithBorder(BorderStyle.Rounded)
                    .WithBorderColor(Color.Cyan)
                    .WithContent(_input)
                    .Height(3));
    }
}
