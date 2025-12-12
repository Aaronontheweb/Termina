using Spectre.Console;
using Spectre.Console.Rendering;
using Termina.Components;
using Termina.Components.Streaming;
using Termina.Reactive;

namespace Termina.Demo.Pages;

/// <summary>
/// Demo page showing streaming text components with LLM simulation.
/// Demonstrates both persisted (chat history) and windowed (thinking tokens) modes.
/// </summary>
public class StreamingChatPage : ReactivePage<StreamingChatViewModel>
{
    // Main chat history (persisted, scrollable)
    private readonly StreamingText _chatHistory = StreamingText.CreatePersisted();

    // Thinking indicator (windowed, rolling)
    private readonly StreamingText _thinkingIndicator = StreamingText.CreateWindowed(windowSize: 3);

    // Text input for user prompts
    private readonly TextInput _promptInput = new()
    {
        Label = "You",
        Placeholder = "Enter your question...",
        IsFocused = true,
        FocusedColor = Color.Cyan1
    };

    // Status bar
    private readonly StatusBar _statusBar = new()
    {
        Hints = "[Enter] Send [↑/↓] Scroll [Esc] Cancel [Q] Quit"
    };

    public StreamingChatPage()
    {
        // Configure chat history
        _chatHistory.ViewportHeight = 15;
        _chatHistory.ViewportWidth = 78; // Account for panel borders
        _chatHistory.Prefix = "  ";
        _chatHistory.PrefixStyle = new Style(Color.Grey);

        // Configure thinking indicator
        _thinkingIndicator.ViewportHeight = 3;
        _thinkingIndicator.ViewportWidth = 60;
        _thinkingIndicator.Prefix = "💭 ";
        _thinkingIndicator.PrefixStyle = new Style(Color.Yellow);
        _thinkingIndicator.TextStyle = new Style(Color.Grey);
    }

    protected override void OnBound()
    {
        // Subscribe to chat history changes
        ViewModel.ChatHistoryChanged
            .Subscribe(text =>
            {
                _chatHistory.Clear();
                _chatHistory.Append(text);
            })
            .DisposeWith(Subscriptions);

        // Subscribe to thinking token updates
        ViewModel.ThinkingTextChanged
            .Subscribe(text =>
            {
                _thinkingIndicator.Clear();
                if (!string.IsNullOrEmpty(text))
                {
                    _thinkingIndicator.Append(text);
                }
            })
            .DisposeWith(Subscriptions);

        // Subscribe to current response streaming
        ViewModel.CurrentResponseChanged
            .Subscribe(text =>
            {
                // Append streaming response to chat
            })
            .DisposeWith(Subscriptions);

        // Subscribe to input text changes
        ViewModel.InputTextChanged
            .Subscribe(text =>
            {
                _promptInput.Text = text;
            })
            .DisposeWith(Subscriptions);

        // Subscribe to cursor position changes
        ViewModel.CursorPositionChanged
            .Subscribe(pos =>
            {
                _promptInput.CursorPosition = pos;
            })
            .DisposeWith(Subscriptions);

        // Subscribe to generation state
        ViewModel.IsGeneratingChanged
            .Subscribe(isGenerating =>
            {
                _promptInput.IsFocused = !isGenerating;
                _statusBar.Hints = isGenerating
                    ? "[Esc] Cancel generation [↑/↓] Scroll history [Q] Quit"
                    : "[Enter] Send [↑/↓] Scroll [←/→] Edit [Q] Quit";
            })
            .DisposeWith(Subscriptions);

        // Subscribe to status messages
        ViewModel.StatusMessageChanged
            .Subscribe(msg =>
            {
                _statusBar.Message = msg;
            })
            .DisposeWith(Subscriptions);
    }

    public override IRenderable Render()
    {
        var isGenerating = _thinkingIndicator.Buffer.HasContent;

        // Build the layout
        var elements = new List<IRenderable>
        {
            // Header
            new Markup("[bold cyan]🤖 Streaming Chat Demo[/] - Simulated LLM with Akka.NET"),
            new Text(""),

            // Chat history panel
            new Spectre.Console.Panel(_chatHistory.Render())
            {
                Header = new PanelHeader("[yellow]Chat History[/]"),
                Border = BoxBorder.Rounded,
                Expand = true,
                Height = 17
            }
        };

        // Show thinking indicator when generating
        if (isGenerating)
        {
            elements.Add(new Text(""));
            elements.Add(new Spectre.Console.Panel(_thinkingIndicator.Render())
            {
                Header = new PanelHeader("[yellow]Thinking...[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Yellow)
            });
        }

        // Input area
        elements.Add(new Text(""));
        elements.Add(new Spectre.Console.Panel(_promptInput.Render())
        {
            Header = new PanelHeader("[cyan]Your Prompt[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(_promptInput.IsFocused ? Color.Cyan1 : Color.Grey)
        });

        // Status bar
        elements.Add(_statusBar.Render());

        return new Rows(elements);
    }
}
