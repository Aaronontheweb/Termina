using Akka.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Termina.Demo.Streaming.Actors;
using Termina.Demo.Streaming.Pages;
using Termina.Hosting;
using Termina.Input;

// Check for --test flag (used in CI/CD to run scripted test and exit)
var testMode = args.Contains("--test");

var builder = Host.CreateApplicationBuilder(args);

// Configure logging to only show warnings and errors (avoid cluttering TUI output)
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Set up input source based on mode
VirtualInputSource? scriptedInput = null;
if (testMode)
{
    scriptedInput = new VirtualInputSource();
    builder.Services.AddTerminaVirtualInput(scriptedInput);
}

// Register Akka.NET actor system
builder.Services.AddAkka("termina-streaming-demo", configurationBuilder =>
{
    configurationBuilder.WithActors((system, registry) =>
    {
        var llmActor = system.ActorOf(LlmSimulatorActor.Props(), "llm-simulator");
        registry.Register<LlmSimulatorActor>(llmActor);
    });
});

// Register Termina with reactive pages
builder.Services.AddTermina("/chat", termina =>
{
    termina.RegisterRoute<StreamingChatPage, StreamingChatViewModel>("/chat");
});

var host = builder.Build();

if (testMode && scriptedInput != null)
{
    // Queue up scripted input to test streaming functionality then quit
    _ = Task.Run(async () =>
    {
        await Task.Delay(500); // Wait for initial render

        // Type a prompt: "Hello"
        scriptedInput.EnqueueString("Hello");

        // Submit the prompt
        scriptedInput.EnqueueKey(ConsoleKey.Enter);

        // Wait for streaming to complete (actor produces tokens over ~3-5 seconds)
        await Task.Delay(6000);

        // Test decision point: "decision" prompt forces a decision list to appear
        scriptedInput.EnqueueString("decision");
        scriptedInput.EnqueueKey(ConsoleKey.Enter);

        // Wait for intro text streaming and decision list to appear
        await Task.Delay(4000);

        // Navigate down in the selection list
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow);
        await Task.Delay(200);

        // Select the second option (Enter confirms)
        scriptedInput.EnqueueKey(ConsoleKey.Enter);

        // Wait for follow-up response to stream
        await Task.Delay(5000);

        // Test "Something else..." option - trigger another decision
        scriptedInput.EnqueueString("decision");
        scriptedInput.EnqueueKey(ConsoleKey.Enter);
        await Task.Delay(4000);

        // Navigate to "Something else..." (4th option)
        scriptedInput.EnqueueKey(ConsoleKey.D4); // Quick select option 4
        await Task.Delay(200);

        // Type custom prompt and submit
        scriptedInput.EnqueueString("custom question");
        scriptedInput.EnqueueKey(ConsoleKey.Enter);
        await Task.Delay(5000);

        // Quit with Ctrl+Q
        scriptedInput.EnqueueKey(ConsoleKey.Q, control: true);
        scriptedInput.Complete();
    });
}

await host.RunAsync();
