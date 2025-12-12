using Spectre.Console;
using Spectre.Console.Rendering;
using Termina.Components;
using Termina.Reactive;

namespace Termina.Demo.Streaming.Pages;

/// <summary>
/// Demo page showing streaming text components with LLM simulation.
/// Components own their state - Page just renders them.
/// </summary>
public class StreamingChatPage : ReactivePage<StreamingChatViewModel>
{
    /// <summary>
    /// Use scrolling mode for chat - natural terminal scrollback instead of fixed layout.
    /// </summary>
    public override RenderMode RenderMode => RenderMode.Scrolling;

    // Status bar (owned by page for layout)
    private readonly StatusBar _statusBar = new()
    {
        Hints = "[Enter] Send [Esc] Clear/Quit [Ctrl+Q] Quit"
    };

    protected override void OnBound()
    {
        // Bind generation state for status bar hints
        ViewModel.IsGeneratingChanged
            .Subscribe(isGenerating =>
            {
                _statusBar.Hints = isGenerating
                    ? "[Esc] Cancel generation [Ctrl+Q] Quit"
                    : "[Enter] Send [←/→] Edit [Esc] Clear/Quit [Ctrl+Q] Quit";
            })
            .DisposeWith(Subscriptions);

        // Bind status messages
        ViewModel.StatusMessageChanged
            .Subscribe(msg => _statusBar.Message = msg)
            .DisposeWith(Subscriptions);
    }

    public override IRenderable Render()
    {
        var isThinking = ViewModel.ThinkingIndicator.Buffer.HasContent;

        // Build the layout
        var elements = new List<IRenderable>
        {
            // Header
            new Markup("[bold cyan]🤖 Streaming Chat Demo[/] - Simulated LLM with Akka.NET"),
            new Text(""),

            // Chat history panel - grows with content, terminal scrollback handles scrolling
            new Panel(ViewModel.ChatHistory.Render())
            {
                Header = new PanelHeader("[yellow]Chat History[/]"),
                Border = BoxBorder.Rounded,
                Expand = true
            }
        };

        // Show thinking indicator when generating
        if (isThinking)
        {
            elements.Add(new Text(""));
            elements.Add(new Panel(ViewModel.ThinkingIndicator.Render())
            {
                Header = new PanelHeader("[yellow]Thinking...[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Yellow)
            });
        }

        // Input area - component renders itself
        elements.Add(new Text(""));
        elements.Add(new Panel(ViewModel.PromptInput.Render())
        {
            Header = new PanelHeader("[cyan]Your Prompt[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(ViewModel.PromptInput.IsFocused ? Color.Cyan1 : Color.Grey),
            Expand = true
        });

        // Status bar
        elements.Add(_statusBar.Render());

        return new Rows(elements);
    }
}
