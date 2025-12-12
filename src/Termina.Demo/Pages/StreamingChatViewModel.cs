using System.Reactive.Linq;
using System.Text;
using Akka.Actor;
using Akka.Hosting;
using Termina.Demo.Actors;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Demo.Pages;

/// <summary>
/// ViewModel for the streaming chat demo.
/// Demonstrates integration with Akka.NET actors for simulated LLM streaming.
/// </summary>
public partial class StreamingChatViewModel : ReactiveViewModel
{
    private readonly IActorRef _llmActor;
    private readonly StringBuilder _chatHistoryBuilder = new();
    private readonly StringBuilder _currentResponseBuilder = new();
    private readonly List<string> _thinkingLines = new();

    // Reactive properties (backing fields - actual state managed by generated code)
    [Reactive] private string _chatHistory = string.Empty;
    [Reactive] private string _thinkingText = string.Empty;
    [Reactive] private string _currentResponse = string.Empty;
    [Reactive] private string _inputText = string.Empty;
    [Reactive] private int _cursorPosition = 0;
    [Reactive] private bool _isGenerating = false;
    [Reactive] private string _statusMessage = "Ready. Enter a question to begin.";

    public StreamingChatViewModel(IRequiredActor<LlmSimulatorActor> llmActorProvider)
    {
        _llmActor = llmActorProvider.ActorRef;

        // Add initial welcome message
        _chatHistoryBuilder.AppendLine("🤖 Assistant: Hello! I'm a simulated LLM demo.");
        _chatHistoryBuilder.AppendLine("   Ask me anything and watch the streaming response!");
        _chatHistoryBuilder.AppendLine();
        ChatHistory = _chatHistoryBuilder.ToString();
    }

    public override void OnActivated()
    {
        // Subscribe to keyboard input
        Input.OfType<KeyPressed>()
            .Subscribe(HandleKeyPress)
            .DisposeWith(Subscriptions);

        // Subscribe to LLM messages via the actor bridge
        Input.OfType<ActorMessageWrapper>()
            .Select(w => w.Message)
            .Subscribe(HandleActorMessage)
            .DisposeWith(Subscriptions);
    }

    private void HandleKeyPress(KeyPressed key)
    {
        var keyInfo = key.KeyInfo;

        // Global keys
        switch (keyInfo.Key)
        {
            case ConsoleKey.Q when !_isGenerating || keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control):
                Shutdown();
                return;

            case ConsoleKey.Escape when _isGenerating:
                CancelGeneration();
                return;

            case ConsoleKey.UpArrow when _isGenerating:
                // TODO: Scroll up in chat history
                StatusMessage = "Scroll up (not yet implemented)";
                return;

            case ConsoleKey.DownArrow when _isGenerating:
                // TODO: Scroll down in chat history
                StatusMessage = "Scroll down (not yet implemented)";
                return;
        }

        // Input editing (only when not generating)
        if (_isGenerating) return;

        switch (keyInfo.Key)
        {
            case ConsoleKey.Enter:
                SubmitPrompt();
                break;

            case ConsoleKey.Backspace:
                if (_cursorPosition > 0)
                {
                    InputText = InputText.Remove(_cursorPosition - 1, 1);
                    CursorPosition--;
                }
                break;

            case ConsoleKey.Delete:
                if (_cursorPosition < InputText.Length)
                {
                    InputText = InputText.Remove(_cursorPosition, 1);
                }
                break;

            case ConsoleKey.LeftArrow:
                if (_cursorPosition > 0)
                    CursorPosition--;
                break;

            case ConsoleKey.RightArrow:
                if (_cursorPosition < InputText.Length)
                    CursorPosition++;
                break;

            case ConsoleKey.Home:
                CursorPosition = 0;
                break;

            case ConsoleKey.End:
                CursorPosition = InputText.Length;
                break;

            default:
                // Insert character
                if (keyInfo.KeyChar >= 32 && keyInfo.KeyChar < 127)
                {
                    InputText = InputText.Insert(_cursorPosition, keyInfo.KeyChar.ToString());
                    CursorPosition++;
                }
                break;
        }
    }

    private void HandleActorMessage(object message)
    {
        switch (message)
        {
            case LlmMessages.ThinkingToken token:
                _thinkingLines.Add(token.Text);
                // Keep only last 3 thinking lines
                while (_thinkingLines.Count > 3)
                    _thinkingLines.RemoveAt(0);
                ThinkingText = string.Join("\n", _thinkingLines) + "\n";
                break;

            case LlmMessages.TextChunk chunk:
                // Clear thinking when text starts
                if (_thinkingLines.Count > 0)
                {
                    _thinkingLines.Clear();
                    ThinkingText = string.Empty;
                }

                _currentResponseBuilder.Append(chunk.Text);
                CurrentResponse = _currentResponseBuilder.ToString();

                // Also append to chat history
                _chatHistoryBuilder.Append(chunk.Text);
                ChatHistory = _chatHistoryBuilder.ToString();
                break;

            case LlmMessages.GenerationComplete:
                _chatHistoryBuilder.AppendLine();
                _chatHistoryBuilder.AppendLine();
                ChatHistory = _chatHistoryBuilder.ToString();

                _currentResponseBuilder.Clear();
                CurrentResponse = string.Empty;

                _thinkingLines.Clear();
                ThinkingText = string.Empty;

                IsGenerating = false;
                StatusMessage = "Ready. Enter another question.";
                break;
        }
    }

    private void SubmitPrompt()
    {
        var prompt = InputText.Trim();
        if (string.IsNullOrEmpty(prompt))
            return;

        // Add user message to chat
        _chatHistoryBuilder.AppendLine($"👤 You: {prompt}");
        _chatHistoryBuilder.AppendLine();
        _chatHistoryBuilder.Append("🤖 Assistant: ");
        ChatHistory = _chatHistoryBuilder.ToString();

        // Clear input
        InputText = string.Empty;
        CursorPosition = 0;

        // Start generation
        IsGenerating = true;
        StatusMessage = "Generating response...";

        // Create a bridge actor to forward messages to the ViewModel
        var bridge = CreateActorBridge();
        _llmActor.Tell(new LlmMessages.GenerateRequest(prompt, bridge));
    }

    private void CancelGeneration()
    {
        _llmActor.Tell(new LlmMessages.CancelGeneration());

        // Clean up state
        if (_currentResponseBuilder.Length > 0)
        {
            _chatHistoryBuilder.AppendLine(" [cancelled]");
            _chatHistoryBuilder.AppendLine();
        }

        _currentResponseBuilder.Clear();
        CurrentResponse = string.Empty;

        _thinkingLines.Clear();
        ThinkingText = string.Empty;

        ChatHistory = _chatHistoryBuilder.ToString();
        IsGenerating = false;
        StatusMessage = "Generation cancelled.";
    }

    /// <summary>
    /// Creates an actor that bridges messages from the actor system to the ViewModel's Input stream.
    /// </summary>
    private IActorRef CreateActorBridge()
    {
        // For now, we'll use the actor system to create a simple forwarding actor
        // In a real implementation, this would be cleaner with a dedicated bridge
        return Context.ActorOf(Props.Create(() => new BridgeActor(this)));
    }

    // Simple actor that forwards messages to the ViewModel
    private class BridgeActor : ReceiveActor
    {
        public BridgeActor(StreamingChatViewModel viewModel)
        {
            ReceiveAny(msg =>
            {
                // Forward to ViewModel's input stream
                viewModel.SendInput(new ActorMessageWrapper(msg));
            });
        }
    }

    /// <summary>
    /// Gets the actor system context (set by the page).
    /// </summary>
    internal ActorSystem Context { get; set; } = null!;

    /// <summary>
    /// Exposes SendInput for the bridge actor.
    /// </summary>
    internal void SendInput(IInputEvent evt)
    {
        // This needs to be wired up properly through the reactive system
        // For now, directly call the handler
        if (evt is ActorMessageWrapper wrapper)
        {
            HandleActorMessage(wrapper.Message);
        }
    }
}

/// <summary>
/// Wrapper for actor messages to flow through the input stream.
/// </summary>
public record ActorMessageWrapper(object Message) : IInputEvent;
