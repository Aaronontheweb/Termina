using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Termina.Hosting;

/// <summary>
/// Hosted service that runs the Termina TUI application.
/// Automatically starts the application when the host starts and stops it on shutdown.
/// </summary>
internal sealed class TerminaHostedService : IHostedService
{
    private readonly TerminaApplication _app;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<TerminaHostedService>? _logger;
    private Task? _runTask;

    public TerminaHostedService(
        TerminaApplication app,
        IHostApplicationLifetime lifetime,
        ILogger<TerminaHostedService>? logger = null)
    {
        _app = app;
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogDebug("Starting Termina application");

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
        _logger?.LogDebug("Stopping Termina application");

        // Request Termina shutdown
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
    }
}
