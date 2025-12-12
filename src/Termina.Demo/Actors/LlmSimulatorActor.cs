using Akka.Actor;
using System.Threading.Channels;

namespace Termina.Demo.Actors;

/// <summary>
/// Messages for the LLM simulator actor.
/// </summary>
public static class LlmMessages
{
    /// <summary>
    /// Request to generate a response for a prompt.
    /// </summary>
    public record GenerateRequest(string Prompt, IActorRef ReplyTo);

    /// <summary>
    /// A thinking token (intermediate progress).
    /// </summary>
    public record ThinkingToken(string Text);

    /// <summary>
    /// A chunk of generated text.
    /// </summary>
    public record TextChunk(string Text);

    /// <summary>
    /// Generation is complete.
    /// </summary>
    public record GenerationComplete;

    /// <summary>
    /// Cancel the current generation.
    /// </summary>
    public record CancelGeneration;
}

/// <summary>
/// Actor that simulates LLM streaming behavior with thinking tokens and text generation.
/// </summary>
public class LlmSimulatorActor : ReceiveActor, IWithTimers
{
    public ITimerScheduler Timers { get; set; } = null!;

    private readonly Random _random = new();
    private IActorRef? _currentClient;
    private CancellationTokenSource? _cts;

    // Sample responses for simulation
    private static readonly string[] SampleResponses =
    [
        "Artificial intelligence (AI) is rapidly transforming how we interact with technology. " +
        "From virtual assistants to autonomous vehicles, AI systems are becoming increasingly " +
        "sophisticated and capable of handling complex tasks that were once thought to be " +
        "exclusively human domains.\n\nMachine learning, a subset of AI, enables systems to " +
        "learn and improve from experience without being explicitly programmed. Deep learning, " +
        "using neural networks with many layers, has achieved remarkable breakthroughs in " +
        "image recognition, natural language processing, and game playing.",

        "The history of computing is a fascinating journey from mechanical calculators to " +
        "quantum computers. Charles Babbage's Analytical Engine in the 1830s laid the " +
        "conceptual groundwork for modern computers.\n\nThe ENIAC, completed in 1945, was " +
        "one of the first general-purpose electronic computers. It weighed 30 tons and " +
        "consumed 150 kilowatts of power. Today, a smartphone in your pocket has millions " +
        "of times more computing power than that room-sized machine.",

        "Software development best practices have evolved significantly over the decades. " +
        "The waterfall model gave way to agile methodologies, emphasizing iterative " +
        "development and customer collaboration.\n\nTest-driven development (TDD) and " +
        "continuous integration have become standard practices in modern software teams. " +
        "DevOps culture bridges the gap between development and operations, enabling " +
        "faster and more reliable software delivery.",

        "Terminal user interfaces (TUIs) offer a unique blend of efficiency and nostalgia. " +
        "While graphical interfaces dominate consumer computing, TUIs remain popular among " +
        "developers and system administrators.\n\nModern TUI frameworks like Spectre.Console " +
        "bring rich formatting, animations, and interactive components to the command line. " +
        "These tools prove that text-based interfaces can be both powerful and beautiful.",

        "The actor model, pioneered by Carl Hewitt in 1973, provides a robust foundation " +
        "for building concurrent and distributed systems. Each actor is an independent " +
        "unit of computation that processes messages sequentially.\n\nAkka.NET brings " +
        "the actor model to the .NET ecosystem, enabling developers to build highly " +
        "scalable and fault-tolerant applications. Actors can supervise child actors, " +
        "creating hierarchies that handle failures gracefully."
    ];

    private static readonly string[] ThinkingPhrases =
    [
        "Analyzing the question...",
        "Considering multiple approaches...",
        "Searching knowledge base...",
        "Formulating response structure...",
        "Evaluating relevant context...",
        "Processing semantic meaning...",
        "Generating coherent narrative...",
        "Refining output quality...",
        "Checking factual accuracy...",
        "Optimizing response clarity..."
    ];

    public LlmSimulatorActor()
    {
        Receive<LlmMessages.GenerateRequest>(HandleGenerateRequest);
        Receive<LlmMessages.CancelGeneration>(_ => CancelCurrentGeneration());
    }

    private void HandleGenerateRequest(LlmMessages.GenerateRequest request)
    {
        // Cancel any existing generation
        CancelCurrentGeneration();

        _currentClient = request.ReplyTo;
        _cts = new CancellationTokenSource();

        // Start async generation
        var self = Self;
        _ = Task.Run(async () =>
        {
            try
            {
                await SimulateGeneration(self, request.ReplyTo, request.Prompt, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Generation error: {ex.Message}");
            }
        }, _cts.Token);
    }

    private async Task SimulateGeneration(IActorRef self, IActorRef client, string prompt, CancellationToken ct)
    {
        // Phase 1: Thinking tokens (windowed display)
        var thinkingCount = _random.Next(3, 8);
        for (var i = 0; i < thinkingCount; i++)
        {
            ct.ThrowIfCancellationRequested();
            var phrase = ThinkingPhrases[_random.Next(ThinkingPhrases.Length)];
            client.Tell(new LlmMessages.ThinkingToken(phrase));
            await Task.Delay(_random.Next(200, 600), ct);
        }

        // Small pause before main response
        await Task.Delay(300, ct);

        // Phase 2: Generate response text (persisted display)
        var response = SampleResponses[_random.Next(SampleResponses.Length)];

        // Stream character by character (or small chunks)
        var chunkSize = _random.Next(1, 4);
        for (var i = 0; i < response.Length; i += chunkSize)
        {
            ct.ThrowIfCancellationRequested();
            var chunk = response.Substring(i, Math.Min(chunkSize, response.Length - i));
            client.Tell(new LlmMessages.TextChunk(chunk));

            // Variable delay to simulate typing
            var delay = chunk.Contains('\n') ? 50 : _random.Next(10, 40);
            await Task.Delay(delay, ct);
        }

        // Signal completion
        client.Tell(new LlmMessages.GenerationComplete());
    }

    private void CancelCurrentGeneration()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _currentClient = null;
    }

    protected override void PostStop()
    {
        CancelCurrentGeneration();
        base.PostStop();
    }

    public static Props Props() => Akka.Actor.Props.Create<LlmSimulatorActor>();
}
