using Microsoft.Extensions.Hosting;
using Termina.Demo.Pages;
using Termina.Hosting;
using Termina.Input;

// Check for --test flag (used in CI/CD to run scripted test and exit)
var testMode = args.Contains("--test");

var builder = Host.CreateApplicationBuilder(args);

// Set up input source based on mode
VirtualInputSource? scriptedInput = null;
if (testMode)
{
    scriptedInput = new VirtualInputSource();
    builder.Services.AddTerminaVirtualInput(scriptedInput);
}

// Register Termina with reactive pages
builder.Services.AddTermina("counter", termina =>
{
    termina.RegisterPage<CounterPage, CounterViewModel>("counter");
    termina.RegisterPage<TodoListPage, TodoListViewModel>("todo-list");
});

var host = builder.Build();

if (testMode && scriptedInput != null)
{
    // Queue up scripted input to test basic functionality then quit
    _ = Task.Run(async () =>
    {
        await Task.Delay(500); // Wait for initial render

        // Test counter: increment twice
        scriptedInput.EnqueueKey(ConsoleKey.UpArrow);
        scriptedInput.EnqueueKey(ConsoleKey.UpArrow);

        // Navigate to todo list
        scriptedInput.EnqueueKey(ConsoleKey.T);
        await Task.Delay(200);

        // Navigate in todo list
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow);
        scriptedInput.EnqueueKey(ConsoleKey.Spacebar); // Toggle item

        // Quit
        scriptedInput.EnqueueKey(ConsoleKey.Q);
        scriptedInput.Complete();
    });
}

await host.RunAsync();
