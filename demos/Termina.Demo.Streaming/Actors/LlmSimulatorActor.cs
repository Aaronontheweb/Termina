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
    public record GenerateRequest(string Prompt, string? DecisionContext = null);

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
    /// A decision point where the user must choose from options.
    /// </summary>
    public record DecisionPointToken(string Question, IReadOnlyList<DecisionChoice> Choices) : StreamToken;

    /// <summary>
    /// A choice for a decision point with title and description.
    /// </summary>
    public record DecisionChoice(string Title, string Description, string Category);

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

    // Decision point scenarios with follow-up responses
    private static readonly DecisionScenario[] DecisionScenarios =
    [
        new DecisionScenario(
            IntroText: "I'd be happy to explain different approaches to building concurrent systems. " +
                       "There are several paradigms to choose from, each with unique strengths.",
            Question: "Which concurrency model would you like to explore?",
            Choices:
            [
                new LlmMessages.DecisionChoice("Actor Model", "Message-passing between isolated actors", "Distributed"),
                new LlmMessages.DecisionChoice("Task Parallel Library", "Task-based async/await patterns", "Threading"),
                new LlmMessages.DecisionChoice("Reactive Extensions", "Observable streams and LINQ operators", "Reactive"),
            ],
            FollowUps: new Dictionary<string, string>
            {
                ["Actor Model"] = "The Actor Model is a powerful paradigm where computation is performed by " +
                    "independent actors that communicate exclusively through messages. Each actor has its own " +
                    "private state and processes messages sequentially, eliminating the need for locks.\n\n" +
                    "Akka.NET implements this model beautifully, providing location transparency, supervision " +
                    "hierarchies for fault tolerance, and clustering for distributed systems. Actors can " +
                    "supervise child actors, automatically restarting them when failures occur.",
                ["Task Parallel Library"] = "The Task Parallel Library (TPL) in .NET provides a robust foundation " +
                    "for parallel and asynchronous programming. Tasks represent units of work that can run " +
                    "concurrently, and async/await syntax makes asynchronous code read like synchronous code.\n\n" +
                    "TPL includes powerful constructs like Parallel.ForEach, Task.WhenAll, and dataflow blocks " +
                    "for building producer-consumer pipelines. It integrates seamlessly with the .NET runtime's " +
                    "thread pool for efficient resource utilization.",
                ["Reactive Extensions"] = "Reactive Extensions (Rx) treats events as streams of data that can be " +
                    "queried using LINQ-style operators. This declarative approach makes complex event processing " +
                    "surprisingly elegant and composable.\n\n" +
                    "With operators like Throttle, Buffer, Merge, and CombineLatest, you can express sophisticated " +
                    "event handling logic concisely. Rx is particularly powerful for UI programming, real-time " +
                    "data processing, and handling multiple asynchronous data sources."
            }),

        new DecisionScenario(
            IntroText: "Great question about software architecture! There are several popular patterns " +
                       "for structuring applications, each suited to different scenarios.",
            Question: "Which architectural pattern interests you most?",
            Choices:
            [
                new LlmMessages.DecisionChoice("Microservices", "Independent deployable services", "Distributed"),
                new LlmMessages.DecisionChoice("Clean Architecture", "Dependency inversion layers", "Monolithic"),
                new LlmMessages.DecisionChoice("Event Sourcing", "Append-only event logs", "Data"),
            ],
            FollowUps: new Dictionary<string, string>
            {
                ["Microservices"] = "Microservices architecture decomposes applications into small, independent " +
                    "services that communicate via APIs. Each service owns its data and can be deployed, " +
                    "scaled, and updated independently.\n\n" +
                    "This approach enables teams to work autonomously, choose appropriate technologies per " +
                    "service, and scale specific components based on demand. However, it introduces complexity " +
                    "in service discovery, distributed transactions, and observability.",
                ["Clean Architecture"] = "Clean Architecture, popularized by Robert C. Martin, organizes code in " +
                    "concentric layers with dependencies pointing inward. The core business logic remains " +
                    "independent of frameworks, databases, and UI concerns.\n\n" +
                    "This separation makes the codebase highly testable and adaptable to change. You can swap " +
                    "out databases, web frameworks, or UI technologies without touching the business rules. " +
                    "The trade-off is additional abstraction layers and boilerplate.",
                ["Event Sourcing"] = "Event Sourcing stores the state of an application as a sequence of events " +
                    "rather than current state snapshots. Every change is captured as an immutable event, " +
                    "providing a complete audit trail and enabling temporal queries.\n\n" +
                    "Combined with CQRS (Command Query Responsibility Segregation), event sourcing enables " +
                    "powerful patterns like event replay, debugging production issues by replaying events, " +
                    "and building multiple read models from the same event stream."
            }),

        new DecisionScenario(
            IntroText: "Terminal UI development has seen a renaissance lately! There are several " +
                       "approaches to building rich console applications.",
            Question: "What aspect of TUI development would you like to learn about?",
            Choices:
            [
                new LlmMessages.DecisionChoice("Layout Systems", "Constraint-based positioning", "Structure"),
                new LlmMessages.DecisionChoice("Input Handling", "Keyboard and mouse events", "Interaction"),
                new LlmMessages.DecisionChoice("Rendering Pipeline", "Efficient screen updates", "Performance"),
            ],
            FollowUps: new Dictionary<string, string>
            {
                ["Layout Systems"] = "Modern TUI frameworks use constraint-based layout systems similar to CSS " +
                    "Flexbox or native mobile layouts. Elements specify size constraints (fixed, fill, auto, " +
                    "percentage) and the layout engine calculates final positions.\n\n" +
                    "This declarative approach handles terminal resizing gracefully and supports nested layouts " +
                    "like vertical/horizontal stacks, grids, and overlapping layers for modals. The key is " +
                    "separating layout logic from rendering for cleaner, more maintainable code.",
                ["Input Handling"] = "Console input handling goes beyond simple ReadLine calls. Modern TUI apps " +
                    "need to handle arrow keys, function keys, mouse events, and key combinations while " +
                    "maintaining responsive UI updates.\n\n" +
                    "Focus management determines which component receives input. A focus stack enables modal " +
                    "dialogs to capture input temporarily, then restore focus when dismissed. Input routing " +
                    "typically flows from focused component up through parent containers.",
                ["Rendering Pipeline"] = "Efficient TUI rendering uses a diff-based approach similar to React's " +
                    "virtual DOM. Instead of clearing and redrawing the entire screen, the renderer compares " +
                    "the new frame against the previous one and only updates changed cells.\n\n" +
                    "This minimizes flickering and terminal escape sequence overhead. Double-buffering with " +
                    "a frame buffer allows compositing complex layouts before flushing to the terminal in a " +
                    "single write operation."
            })
    ];

    private record DecisionScenario(
        string IntroText,
        string Question,
        LlmMessages.DecisionChoice[] Choices,
        Dictionary<string, string> FollowUps);

    // Track pending decision for follow-up responses
    private DecisionScenario? _pendingDecision;

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
        var source = CreateTokenSource(request.Prompt, request.DecisionContext, cts.Token);

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

    private Source<LlmMessages.StreamToken, NotUsed> CreateTokenSource(string prompt, string? decisionContext, CancellationToken ct)
    {
        var thinkingCount = _random.Next(3, 6);

        // Create thinking tokens
        var thinkingSource = Source.From(Enumerable.Range(0, thinkingCount))
            .Select(_ => ThinkingPhrases[_random.Next(ThinkingPhrases.Length)])
            .Select(text => (LlmMessages.StreamToken)new LlmMessages.ThinkingToken(text))
            .Throttle(1, TimeSpan.FromMilliseconds(150), 1, ThrottleMode.Shaping);

        // If we have a decision context, use the follow-up response
        if (decisionContext != null && _pendingDecision != null)
        {
            var followUp = _pendingDecision.FollowUps.GetValueOrDefault(decisionContext)
                ?? $"You selected '{decisionContext}'. That's an interesting choice!";
            _pendingDecision = null;
            return CreateTextStreamSource(thinkingSource, followUp);
        }

        // Check for test trigger: "decision" prompt forces a decision point for headless testing
        var useDecision = prompt.Equals("decision", StringComparison.OrdinalIgnoreCase)
            || _random.Next(100) < 40;

        if (useDecision)
        {
            _pendingDecision = DecisionScenarios[_random.Next(DecisionScenarios.Length)];
            return CreateDecisionSource(thinkingSource, _pendingDecision);
        }
        else
        {
            var response = SampleResponses[_random.Next(SampleResponses.Length)];
            _pendingDecision = null;
            return CreateTextStreamSource(thinkingSource, response);
        }
    }

    private Source<LlmMessages.StreamToken, NotUsed> CreateTextStreamSource(
        Source<LlmMessages.StreamToken, NotUsed> thinkingSource,
        string response)
    {
        // Create text chunks source
        var chunkSize = _random.Next(1, 4);
        var chunks = new List<string>();
        for (var i = 0; i < response.Length; i += chunkSize)
        {
            chunks.Add(response.Substring(i, Math.Min(chunkSize, response.Length - i)));
        }

        var textSource = Source.From(chunks)
            .Select(chunk => (LlmMessages.StreamToken)new LlmMessages.TextChunk(chunk))
            .Throttle(10, TimeSpan.FromMilliseconds(50), 10, ThrottleMode.Shaping);

        var completionSource = Source.Single((LlmMessages.StreamToken)new LlmMessages.GenerationComplete());

        return thinkingSource
            .Concat(textSource)
            .Concat(completionSource);
    }

    private Source<LlmMessages.StreamToken, NotUsed> CreateDecisionSource(
        Source<LlmMessages.StreamToken, NotUsed> thinkingSource,
        DecisionScenario scenario)
    {
        // Stream the intro text first
        var chunkSize = _random.Next(1, 4);
        var chunks = new List<string>();
        for (var i = 0; i < scenario.IntroText.Length; i += chunkSize)
        {
            chunks.Add(scenario.IntroText.Substring(i, Math.Min(chunkSize, scenario.IntroText.Length - i)));
        }

        var introSource = Source.From(chunks)
            .Select(chunk => (LlmMessages.StreamToken)new LlmMessages.TextChunk(chunk))
            .Throttle(10, TimeSpan.FromMilliseconds(50), 10, ThrottleMode.Shaping);

        // Then emit the decision point
        var decisionSource = Source.Single(
            (LlmMessages.StreamToken)new LlmMessages.DecisionPointToken(scenario.Question, scenario.Choices));

        // No completion - wait for user decision
        return thinkingSource
            .Concat(introSource)
            .Concat(decisionSource);
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
