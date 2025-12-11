using System.Threading.Channels;
using Spectre.Console;
using Spectre.Console.Rendering;
using Termina.Input;
using Termina.Navigation;

namespace Termina;

/// <summary>
/// Orchestrates the event loop, routing events to pages and components,
/// handling input polling, and rendering via Spectre.Console Live display.
/// </summary>
public sealed class EventMediator
{
    private readonly Channel<object> _eventChannel;
    private readonly List<IInputSource> _inputSources;
    private readonly IAnsiConsole _console;
    private readonly NavigationService _navigation;
    private CancellationTokenSource? _shutdownCts;

    /// <summary>
    /// Creates a new event mediator with a single input source.
    /// </summary>
    /// <param name="inputSource">Source for keyboard input.</param>
    /// <param name="console">Spectre.Console instance for rendering.</param>
    /// <param name="navigation">Navigation service for page management.</param>
    public EventMediator(
        IInputSource inputSource,
        IAnsiConsole console,
        NavigationService navigation)
        : this(new[] { inputSource }, console, navigation)
    {
    }

    /// <summary>
    /// Creates a new event mediator with multiple input sources.
    /// All input sources feed into the same event loop.
    /// </summary>
    /// <param name="inputSources">Sources for input (keyboard, mouse, etc.).</param>
    /// <param name="console">Spectre.Console instance for rendering.</param>
    /// <param name="navigation">Navigation service for page management.</param>
    public EventMediator(
        IEnumerable<IInputSource> inputSources,
        IAnsiConsole console,
        NavigationService navigation)
    {
        _eventChannel = Channel.CreateUnbounded<object>();
        _inputSources = inputSources.ToList();
        _console = console;
        _navigation = navigation;

        // Wire up navigation to use our event channel
        _navigation.SetEventWriter(_eventChannel.Writer);
    }

    /// <summary>
    /// Get the event writer for pushing events into the loop.
    /// Used by NavigationService to wire up pages and handlers.
    /// </summary>
    public ChannelWriter<object> EventWriter => _eventChannel.Writer;

    /// <summary>
    /// Request graceful shutdown of the application.
    /// Cancels all input sources and the event loop.
    /// </summary>
    public void Shutdown()
    {
        _shutdownCts?.Cancel();
    }

    /// <summary>
    /// Runs the event loop until cancellation is requested or Shutdown() is called.
    /// Input sources push to the channel, event loop reads and processes sequentially.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Create linked token - cancelled by either external token OR Shutdown()
        _shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var linkedToken = _shutdownCts.Token;

        // Start each input source - they push to the shared channel
        var inputTasks = _inputSources
            .Select(source => source.RunAsync(_eventChannel.Writer, linkedToken))
            .ToList();

        try
        {
            // Initial render
            var rootRenderable = RenderCurrentPage();

            await _console.Live(rootRenderable)
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    // Initial render - don't wait for first event
                    ctx.Refresh();

                    // Single-threaded event loop - all input sources merge here
                    await foreach (var evt in _eventChannel.Reader.ReadAllAsync(linkedToken))
                    {
                        ProcessEvent(evt);

                        // Re-render after event processing
                        ctx.UpdateTarget(RenderCurrentPage());
                        ctx.Refresh();
                    }
                });
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }

        // Wait for all input sources to complete
        try
        {
            await Task.WhenAll(inputTasks);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        finally
        {
            _shutdownCts.Dispose();
            _shutdownCts = null;
        }
    }

    /// <summary>
    /// Process an event by routing it to the appropriate handlers.
    /// </summary>
    private void ProcessEvent(object evt)
    {
        var page = _navigation.CurrentPage;
        var handler = _navigation.CurrentHandler;

        switch (evt)
        {
            // Shutdown request
            case ShutdownRequested:
                Shutdown();
                return; // Don't process further

            // Navigation requests
            case NavigationRequested navReq:
                _navigation.NavigateTo(navReq.PageKey);
                break;

            case NavigationBackRequested:
                _navigation.GoBack();
                break;

            // Low-level input events - route to components
            case KeyPressed keyEvt:
                if (page != null)
                {
                    foreach (var component in page.Components)
                    {
                        component.HandleInput(keyEvt);
                    }
                }
                // Also let handler see raw input if it wants
                handler?.HandleEvent(keyEvt);
                break;

            // Business events - route to components and handler
            default:
                if (page != null)
                {
                    foreach (var component in page.Components)
                    {
                        component.ReceiveEvent(evt);
                    }
                }
                handler?.HandleEvent(evt);
                break;
        }

        // Give handler a chance to push state updates
        handler?.Tick();
    }

    /// <summary>
    /// Renders the current page.
    /// </summary>
    private IRenderable RenderCurrentPage()
    {
        return _navigation.CurrentPage?.Render()
            ?? new Text("No page active");
    }
}
