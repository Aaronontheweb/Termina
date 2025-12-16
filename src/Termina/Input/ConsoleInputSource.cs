using System.Threading.Channels;

namespace Termina.Input;

/// <summary>
/// Real console input source that reads from Console.ReadKey and detects terminal resize.
/// Runs on a background thread since ReadKey is blocking.
/// </summary>
public sealed class ConsoleInputSource : IInputSource
{
    private int _lastWidth;
    private int _lastHeight;

    /// <inheritdoc />
    public async Task RunAsync(ChannelWriter<object> writer, CancellationToken cancellationToken)
    {
        // Initialize dimensions for resize detection
        InitializeDimensions();

        // Run blocking Console.ReadKey on thread pool
        await Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Check if key available to avoid blocking forever on cancellation
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    await writer.WriteAsync(new KeyPressed(key), cancellationToken);
                }
                else
                {
                    // Brief yield to check cancellation
                    await Task.Delay(10, cancellationToken);
                }

                // Check for terminal resize (polling)
                await CheckForResizeAsync(writer, cancellationToken);
            }
        }, cancellationToken);
    }

    private void InitializeDimensions()
    {
        try
        {
            _lastWidth = Console.WindowWidth;
            _lastHeight = Console.WindowHeight;
        }
        catch (IOException)
        {
            // No TTY available - use defaults
            _lastWidth = 80;
            _lastHeight = 24;
        }
    }

    private async Task CheckForResizeAsync(ChannelWriter<object> writer, CancellationToken cancellationToken)
    {
        try
        {
            var width = Console.WindowWidth;
            var height = Console.WindowHeight;

            if (width != _lastWidth || height != _lastHeight)
            {
                _lastWidth = width;
                _lastHeight = height;
                await writer.WriteAsync(new ResizeEvent(width, height), cancellationToken);
            }
        }
        catch (IOException)
        {
            // No TTY available - ignore resize checks
        }
    }
}
