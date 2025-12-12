using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace Termina.Demo.Streaming.Actors;

/// <summary>
/// Messages for the LLM simulator actor.
/// </summary>
public static class LlmMessages
{
    /// <summary>
    /// Request to generate a response for a prompt.
    /// Returns an IAsyncEnumerable of StreamToken via a channel.
    /// </summary>
    public record GenerateRequest(string Prompt);

    /// <summary>
    /// Response containing the async stream of tokens.
    /// </summary>
    public record GenerateResponse(IAsyncEnumerable<StreamToken> TokenStream, CancellationTokenSource Cancellation);

    /// <summary>
    /// A token from the stream (either thinking or text).
    /// </summary>
    public abstract record StreamToken;

    /// <summary>
    /// A thinking token (intermediate progress).
    /// </summary>
    public record ThinkingToken(string Text) : StreamToken;

    /// <summary>
    /// A chunk of generated text.
    /// </summary>
    public record TextChunk(string Text) : StreamToken;

    /// <summary>
    /// Signal that generation is complete.
    /// </summary>
    public record GenerationComplete : StreamToken;
}

/// <summary>
/// Actor that simulates LLM streaming behavior using Akka Streams.
/// Returns an IAsyncEnumerable via Channel for consumption by StreamingText components.
/// </summary>
public class LlmSimulatorActor : ReceiveActor
{
    private readonly ActorMaterializer _materializer;
    private readonly Random _random = new();

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
        _materializer = Context.Materializer();

        Receive<LlmMessages.GenerateRequest>(HandleGenerateRequest);
    }

    private void HandleGenerateRequest(LlmMessages.GenerateRequest request)
    {
        var cts = new CancellationTokenSource();
        var channel = Channel.CreateUnbounded<LlmMessages.StreamToken>();

        // Create Akka Stream source that generates tokens
        var source = CreateTokenSource(request.Prompt, cts.Token);

        // Run the stream, writing to the channel
        source
            .RunForeach(token => channel.Writer.TryWrite(token), _materializer)
            .ContinueWith(_ =>
            {
                channel.Writer.Complete();
            }, TaskContinuationOptions.ExecuteSynchronously);

        // Return the async enumerable wrapping the channel
        var asyncEnumerable = ReadFromChannelAsync(channel.Reader, cts.Token);
        Sender.Tell(new LlmMessages.GenerateResponse(asyncEnumerable, cts));
    }

    private Source<LlmMessages.StreamToken, NotUsed> CreateTokenSource(string prompt, CancellationToken ct)
    {
        var thinkingCount = _random.Next(3, 8);
        var response = SampleResponses[_random.Next(SampleResponses.Length)];

        // Create thinking tokens with delays using Throttle instead of Delay
        // Throttle ensures elements come out at a specific rate (1 token per 600ms for visibility)
        var thinkingSource = Source.From(Enumerable.Range(0, thinkingCount))
            .Select(_ => ThinkingPhrases[_random.Next(ThinkingPhrases.Length)])
            .Select(text => (LlmMessages.StreamToken)new LlmMessages.ThinkingToken(text))
            .Throttle(1, TimeSpan.FromMilliseconds(600), 1, ThrottleMode.Shaping);

        // Create text chunks source (character by character with small chunks)
        var chunkSize = _random.Next(1, 4);
        var chunks = new List<string>();
        for (var i = 0; i < response.Length; i += chunkSize)
        {
            chunks.Add(response.Substring(i, Math.Min(chunkSize, response.Length - i)));
        }

        var textSource = Source.From(chunks)
            .Select(chunk => (LlmMessages.StreamToken)new LlmMessages.TextChunk(chunk))
            .Throttle(10, TimeSpan.FromMilliseconds(50), 10, ThrottleMode.Shaping);

        // Completion signal
        var completionSource = Source.Single((LlmMessages.StreamToken)new LlmMessages.GenerationComplete());

        // Combine: thinking -> text -> complete
        return thinkingSource
            .Concat(textSource)
            .Concat(completionSource);
    }

    private static async IAsyncEnumerable<LlmMessages.StreamToken> ReadFromChannelAsync(
        ChannelReader<LlmMessages.StreamToken> reader,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var token in reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            yield return token;
        }
    }

    protected override void PostStop()
    {
        _materializer.Dispose();
        base.PostStop();
    }

    public static Props Props() => Akka.Actor.Props.Create<LlmSimulatorActor>();
}
