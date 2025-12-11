using System.Threading.Channels;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Termina;

/// <summary>
/// Orchestrates the event loop, routing events from the channel to components
/// and rendering the UI via Spectre.Console Live display.
/// </summary>
public sealed class EventMediator
{
    private readonly List<Component> _components = new();
    private readonly ChannelReader<object> _eventReader;
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Creates a new event mediator.
    /// </summary>
    /// <param name="eventReader">Channel reader for incoming events of any type.</param>
    /// <param name="console">Spectre.Console instance for rendering.</param>
    public EventMediator(ChannelReader<object> eventReader, IAnsiConsole console)
    {
        _eventReader = eventReader;
        _console = console;
    }

    /// <summary>
    /// Register a component to receive events and be rendered.
    /// </summary>
    public void RegisterComponent(Component component)
    {
        _components.Add(component);
    }

    /// <summary>
    /// Runs the event loop until cancellation is requested.
    /// Events are processed sequentially on a single thread for thread safety.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Initial render
        var rootRenderable = RenderComponents();

        await _console.Live(rootRenderable)
            .StartAsync(async ctx =>
            {
                // Single-threaded event loop
                await foreach (var evt in _eventReader.ReadAllAsync(cancellationToken))
                {
                    // 1. Distribute event to ALL components
                    foreach (var component in _components)
                    {
                        component.ReceiveEvent(evt);
                    }

                    // 2. Re-render after event processing
                    var rendered = RenderComponents();
                    ctx.UpdateTarget(rendered);
                    ctx.Refresh();
                }
            });
    }

    /// <summary>
    /// Renders all components into a single renderable.
    /// </summary>
    private IRenderable RenderComponents()
    {
        if (_components.Count == 0)
            return new Text("No components registered");

        if (_components.Count == 1)
            return _components[0].Render();

        // Stack components vertically using Rows
        var rows = new Rows(_components.Select(c => c.Render()));
        return rows;
    }
}
