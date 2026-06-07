using System.Diagnostics;
using R3;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Conformance;

public sealed class ConformanceViewModel : ReactiveViewModel
{
    private readonly ConformanceRecorder _recorder;
    private readonly ConformanceLogPaths _paths;

    public ReactiveProperty<string> LastKeyboardEvent { get; } = new("(none)");
    public ReactiveProperty<int> ArrowUpCount { get; } = new(0);
    public ReactiveProperty<int> ArrowDownCount { get; } = new(0);
    public ReactiveProperty<int> WheelUpCount { get; } = new(0);
    public ReactiveProperty<int> WheelDownCount { get; } = new(0);
    public ReactiveProperty<int> SubmittedCount { get; } = new(0);
    public ReactiveProperty<string> LastSubmitted { get; } = new("(none)");
    public ReactiveProperty<string> TracePath { get; }
    public ReactiveProperty<string> EventLogPath { get; }

    public ConformanceViewModel(ConformanceRecorder recorder, ConformanceLogPaths paths)
    {
        _recorder = recorder;
        _paths = paths;
        TracePath = new ReactiveProperty<string>(paths.TracePath);
        EventLogPath = new ReactiveProperty<string>(paths.EventLogPath);
    }

    public override void OnActivated()
    {
        var inTmux = Environment.GetEnvironmentVariable("TMUX") is not null;
        var tmuxMouse = inTmux ? ProbeTmuxMouse() : "n/a";

        _recorder.RecordLine(
            JsonLine(
                ("phase", "startup"),
                ("tracePath", _paths.TracePath),
                ("eventLogPath", _paths.EventLogPath),
                ("envRawInput", Environment.GetEnvironmentVariable("TERMINA_RAW_INPUT") ?? ""),
                ("envKittyKeyboard", Environment.GetEnvironmentVariable("TERMINA_KITTY_KEYBOARD") ?? ""),
                ("tmux", inTmux.ToString().ToLowerInvariant()),
                ("tmuxMouse", tmuxMouse),
                ("term", Environment.GetEnvironmentVariable("TERM") ?? ""),
                ("termProgram", Environment.GetEnvironmentVariable("TERM_PROGRAM") ?? "")));

        Input.OfType<IInputEvent, KeyPressed>()
            .Subscribe(HandleKey)
            .DisposeWith(Subscriptions);

        Input.OfType<IInputEvent, MouseScrollEvent>()
            .Subscribe(scroll =>
            {
                if (scroll.Delta > 0)
                    WheelUpCount.Value++;
                else
                    WheelDownCount.Value++;

                _recorder.RecordLine(
                    JsonLine(
                        ("phase", "input"),
                        ("kind", "MouseScrollEvent"),
                        ("delta", scroll.Delta.ToString())));
            })
            .DisposeWith(Subscriptions);
    }

    public void RecordSubmitted(string text)
    {
        SubmittedCount.Value++;
        LastSubmitted.Value = text;
        _recorder.RecordLine(
            JsonLine(
                ("phase", "submit"),
                ("text", text),
                ("count", SubmittedCount.Value.ToString())));
    }

    public void RecordInputTextChanged(string text)
    {
        _recorder.RecordLine(
            JsonLine(
                ("phase", "text"),
                ("kind", "TextChanged"),
                ("text", text)));
    }

    private void HandleKey(KeyPressed key)
    {
        var info = key.KeyInfo;
        LastKeyboardEvent.Value = info.Modifiers == 0
            ? info.Key.ToString()
            : $"{info.Modifiers}+{info.Key}";

        if (info.Key == ConsoleKey.UpArrow && info.Modifiers == 0)
            ArrowUpCount.Value++;
        else if (info.Key == ConsoleKey.DownArrow && info.Modifiers == 0)
            ArrowDownCount.Value++;

        _recorder.RecordLine(
            JsonLine(
                ("phase", "input"),
                ("kind", "KeyPressed"),
                ("key", info.Key.ToString()),
                ("modifiers", info.Modifiers.ToString()),
                ("keyChar", info.KeyChar == '\0' ? "" : info.KeyChar.ToString())));
    }

    private static string JsonLine(params (string Key, string Value)[] fields)
    {
        return "{" + string.Join(",", fields.Select(field => $"\"{Escape(field.Key)}\":\"{Escape(field.Value)}\"")) + "}";
    }

    private static string ProbeTmuxMouse()
    {
        var envMouse = Environment.GetEnvironmentVariable("TMUX_EXPECTED_MOUSE");
        if (!string.IsNullOrEmpty(envMouse))
            return envMouse;

        try
        {
            using var proc = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "tmux",
                    ArgumentList = { "show", "-gv", "mouse" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            proc.Start();
            proc.WaitForExit(2000);
            if (proc.ExitCode == 0)
            {
                var line = proc.StandardOutput.ReadLine()?.Trim();
                if (line is not null)
                {
                    var idx = line.LastIndexOf(' ');
                    if (idx >= 0)
                        return line[(idx + 1)..];
                }
            }
        }
        catch
        {
            // Ignore probe failures; fall through to "unknown"
        }

        return "unknown";
    }

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }

    public override void Dispose()
    {
        LastKeyboardEvent.Dispose();
        ArrowUpCount.Dispose();
        ArrowDownCount.Dispose();
        WheelUpCount.Dispose();
        WheelDownCount.Dispose();
        SubmittedCount.Dispose();
        LastSubmitted.Dispose();
        TracePath.Dispose();
        EventLogPath.Dispose();
        base.Dispose();
    }
}
