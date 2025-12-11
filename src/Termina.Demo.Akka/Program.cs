using Akka.Actor;
using Akka.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Termina;
using Termina.Demo.Akka.Actors;
using Termina.Demo.Akka.Pages;
using Termina.Input;
using Termina.Pages;

// Check for --test flag (used in CI/CD to run scripted test and exit)
var testMode = args.Contains("--test");

// Build the host with services
var builder = Host.CreateApplicationBuilder(args);

// Configure logging (disable most for clean TUI output)
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Register Termina application (needed before Akka so Bus is available)
builder.Services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);

// Create virtual input source for test mode
VirtualInputSource? scriptedInput = testMode ? new VirtualInputSource() : null;

builder.Services.AddSingleton<TerminaApplication>(sp =>
{
    var console = sp.GetRequiredService<IAnsiConsole>();
    var app = new TerminaApplication(console);

    // Set up input source based on mode
    if (scriptedInput != null)
    {
        app.AddInputSource(scriptedInput);
    }
    else
    {
        app.AddInputSource(new ConsoleInputSource());
    }

    return app;
});

// Register Akka.NET actor system using Akka.Hosting
builder.Services.AddAkka("TaskManagerSystem", (akkaBuilder, sp) =>
{
    var terminaApp = sp.GetRequiredService<TerminaApplication>();

    // Create TaskManagerActor with IApplicationBus injected
    akkaBuilder.WithActors((system, registry) =>
    {
        var taskManager = system.ActorOf(
            Props.Create(() => new TaskManagerActor(terminaApp.Bus)),
            "task-manager");
        registry.Register<TaskManagerActor>(taskManager);
    });
});

// Register hosted service to run Termina
builder.Services.AddHostedService<TerminaHostedService>();

var host = builder.Build();

// If in test mode, queue up scripted input
if (testMode && scriptedInput != null)
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(500); // Wait for initial render

        // Add a task
        scriptedInput.EnqueueKey(ConsoleKey.A);           // Enter add mode
        scriptedInput.EnqueueString("Test task from CI");  // Type description
        scriptedInput.EnqueueKey(ConsoleKey.Enter);        // Confirm add

        await Task.Delay(300); // Wait for task to be added

        // Start timer on the task
        scriptedInput.EnqueueKey(ConsoleKey.S);            // Start timer

        await Task.Delay(1000); // Let timer run for a second

        // Stop timer
        scriptedInput.EnqueueKey(ConsoleKey.S);            // Stop timer

        await Task.Delay(300);

        // Toggle complete
        scriptedInput.EnqueueKey(ConsoleKey.Spacebar);     // Mark complete

        await Task.Delay(300);

        // Add another task
        scriptedInput.EnqueueKey(ConsoleKey.A);
        scriptedInput.EnqueueString("Second task");
        scriptedInput.EnqueueKey(ConsoleKey.Enter);

        await Task.Delay(300);

        // Navigate down and delete
        scriptedInput.EnqueueKey(ConsoleKey.DownArrow);
        scriptedInput.EnqueueKey(ConsoleKey.D);            // Delete task

        await Task.Delay(300);

        // Exit
        scriptedInput.EnqueueKey(ConsoleKey.Q);
        scriptedInput.Complete();
    });
}

// Run the host
await host.RunAsync();

/// <summary>
/// Hosted service that runs the Termina TUI application.
/// Coordinates between Akka.NET actors and the Termina UI.
/// </summary>
public sealed class TerminaHostedService : IHostedService
{
    private readonly TerminaApplication _app;
    private readonly IRequiredActor<TaskManagerActor> _taskManagerProvider;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<TerminaHostedService> _logger;
    private Task? _runTask;

    public TerminaHostedService(
        TerminaApplication app,
        IRequiredActor<TaskManagerActor> taskManagerProvider,
        IHostApplicationLifetime lifetime,
        ILogger<TerminaHostedService> logger)
    {
        _app = app;
        _taskManagerProvider = taskManagerProvider;
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Termina Task Manager demo");

        // Get the task manager actor from the registry
        var taskManager = _taskManagerProvider.ActorRef;

        // Register pages with custom handler factory for DI
        _app.RegisterPage<TaskListHandler>("tasks", () =>
        {
            var handler = new TaskListHandler();
            handler.SetTaskManager(taskManager);
            return handler;
        }, NavigationBehavior.PreserveState);

        // Navigate to the task list page
        _app.NavigateTo("tasks");

        // Run Termina in a background task
        _runTask = Task.Run(async () =>
        {
            try
            {
                await _app.RunAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
            finally
            {
                // Signal the host to stop when Termina exits
                _lifetime.StopApplication();
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Termina Task Manager demo");

        // Shutdown Termina
        _app.Shutdown();

        // Wait for Termina to finish
        if (_runTask != null)
        {
            try
            {
                await _runTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        // Actor system shutdown is handled by Akka.Hosting
    }
}
