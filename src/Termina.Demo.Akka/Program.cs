using Akka.Actor;
using Akka.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Termina.Demo.Akka.Actors;
using Termina.Demo.Akka.Pages;
using Termina.Hosting;
using Termina.Input;
using Termina.Pages;

// Check for --test flag (used in CI/CD to run scripted test and exit)
var testMode = args.Contains("--test");

// Build the host with services
var builder = Host.CreateApplicationBuilder(args);

// Configure logging (disable most for clean TUI output)
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Create virtual input source for test mode
VirtualInputSource? scriptedInput = testMode ? new VirtualInputSource() : null;

// Register Akka.NET actor system
builder.Services.AddAkka("TaskManagerSystem", (akkaBuilder, _) =>
{
    // Create TaskManagerActor (no longer needs IApplicationBus)
    akkaBuilder.WithActors((system, registry) =>
    {
        var taskManager = system.ActorOf(
            Props.Create(() => new TaskManagerActor()),
            "task-manager");
        registry.Register<TaskManagerActor>(taskManager);
    });
});

// Register Termina with pages using the new DI pattern
builder.Services.AddTermina("tasks", termina =>
{
    // Task list page - PreserveState so we don't lose selection
    termina.RegisterPage<TaskListHandler>("tasks", NavigationBehavior.PreserveState);

    // Task detail page - Reset each time we view a different task
    termina.RegisterPage<TaskDetailHandler>("task-detail", NavigationBehavior.ResetOnNavigation);
});

// Add input sources
if (scriptedInput != null)
{
    builder.Services.AddTerminaVirtualInput(scriptedInput);
}
else
{
    builder.Services.AddTerminaConsoleInput();
}

var host = builder.Build();

// If in test mode, queue up scripted input
if (testMode && scriptedInput != null)
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(500); // Wait for initial render with pre-stocked tasks

        // Navigate through pre-stocked tasks (there are 6)
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow);
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow);
        await Task.Delay(200);

        // Change priority on current task (P cycles through priorities)
        scriptedInput.EnqueueKey(ConsoleKey.P);
        await Task.Delay(200);
        scriptedInput.EnqueueKey(ConsoleKey.P);
        await Task.Delay(200);

        // View task detail (Enter)
        scriptedInput.EnqueueKey(ConsoleKey.Enter);
        await Task.Delay(500); // View detail page

        // Toggle timer on detail page
        scriptedInput.EnqueueKey(ConsoleKey.S);
        await Task.Delay(300);

        // Change priority on detail page
        scriptedInput.EnqueueKey(ConsoleKey.P);
        await Task.Delay(200);

        // Go back to list
        scriptedInput.EnqueueKey(ConsoleKey.Escape);
        await Task.Delay(300);

        // Add a new high-priority task
        scriptedInput.EnqueueKey(ConsoleKey.A);           // Enter add mode
        scriptedInput.EnqueueKey(ConsoleKey.Tab);          // Cycle priority to High
        scriptedInput.EnqueueKey(ConsoleKey.Tab);          // Cycle priority to Critical
        scriptedInput.EnqueueString("CI Test Task");
        scriptedInput.EnqueueKey(ConsoleKey.Enter);        // Confirm add
        await Task.Delay(300);

        // Start timer on the new task (should be at top due to critical priority)
        scriptedInput.EnqueueKey(ConsoleKey.Home);         // Go to top (if supported)
        scriptedInput.EnqueueKey(ConsoleKey.UpArrow);      // Navigate up
        scriptedInput.EnqueueKey(ConsoleKey.UpArrow);
        scriptedInput.EnqueueKey(ConsoleKey.UpArrow);
        scriptedInput.EnqueueKey(ConsoleKey.UpArrow);
        scriptedInput.EnqueueKey(ConsoleKey.UpArrow);
        scriptedInput.EnqueueKey(ConsoleKey.UpArrow);
        scriptedInput.EnqueueKey(ConsoleKey.S);            // Start timer
        await Task.Delay(500);

        // Stop timer
        scriptedInput.EnqueueKey(ConsoleKey.S);
        await Task.Delay(200);

        // Toggle complete
        scriptedInput.EnqueueKey(ConsoleKey.Spacebar);
        await Task.Delay(200);

        // Delete the task
        scriptedInput.EnqueueKey(ConsoleKey.D);
        await Task.Delay(200);

        // Exit
        scriptedInput.EnqueueKey(ConsoleKey.Q);
        scriptedInput.Complete();
    });
}

// Run the host
await host.RunAsync();
