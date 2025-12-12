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

        // Type another prompt to test second interaction
        scriptedInput.EnqueueString("Test");

        // Submit
        scriptedInput.EnqueueKey(ConsoleKey.Enter);

        // Wait a bit then cancel mid-stream
        await Task.Delay(1500);
        scriptedInput.EnqueueKey(ConsoleKey.Escape);
        await Task.Delay(500);

        // Quit with Ctrl+Q
        scriptedInput.EnqueueKey(ConsoleKey.Q, control: true);
        scriptedInput.Complete();
    });
}

await host.RunAsync();
