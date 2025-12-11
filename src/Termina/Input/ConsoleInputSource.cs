using System.Threading.Channels;

namespace Termina.Input;

/// <summary>
/// Real console input source that reads from Console.ReadKey.
/// Runs on a background thread since ReadKey is blocking.
/// </summary>
public sealed class ConsoleInputSource : IInputSource
{
    /// <inheritdoc />
    public async Task RunAsync(ChannelWriter<object> writer, CancellationToken cancellationToken)
    {
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
            }
        }, cancellationToken);
    }
}
