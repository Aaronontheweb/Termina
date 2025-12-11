using System.Threading.Channels;

namespace Termina.Input;

/// <summary>
/// Abstraction for input sources.
/// Input sources push events to a channel - no polling required.
/// </summary>
public interface IInputSource
{
    /// <summary>
    /// Start the input source, pushing events to the provided channel.
    /// Returns a task that completes when the source stops (e.g., on cancellation).
    /// </summary>
    /// <param name="writer">Channel to push input events to.</param>
    /// <param name="cancellationToken">Token to stop the input source.</param>
    Task RunAsync(ChannelWriter<object> writer, CancellationToken cancellationToken);
}
