namespace Termina.Conformance;

public sealed class ConformanceRecorder : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly Lock _gate = new();

    public ConformanceRecorder(ConformanceLogPaths paths)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(paths.EventLogPath)!);
        _writer = new StreamWriter(paths.EventLogPath, append: false) { AutoFlush = true };
    }

    public void RecordLine(string payload)
    {
        lock (_gate)
        {
            _writer.WriteLine(payload);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _writer.Dispose();
        }
    }
}
