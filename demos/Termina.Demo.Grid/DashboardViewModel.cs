// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Demo.Grid;

/// <summary>
/// ViewModel for the GridNode dashboard demo.
/// Demonstrates reactive properties for system metrics simulation.
/// </summary>
public class DashboardViewModel : ReactiveViewModel
{
    public ReactiveProperty<int> CpuUsage { get; } = new(45);
    public ReactiveProperty<int> MemoryUsage { get; } = new(62);
    public ReactiveProperty<int> DiskUsage { get; } = new(28);
    public ReactiveProperty<int> NetworkUsage { get; } = new(15);
    public ReactiveProperty<string> StatusMessage { get; } = new("System running normally");
    public ReactiveProperty<List<LogEntry>> LogEntries { get; } = new(new());
    public ReactiveProperty<int> SelectedLogIndex { get; } = new(-1);

    private readonly Random _random = new();
    private IDisposable? _simulationTimer;

    public override void OnActivated()
    {
        // Initialize with some log entries
        LogEntries.Value = new List<LogEntry>
        {
            new("12:00:00", "INFO", "Application started"),
            new("12:00:01", "DEBUG", "Loading configuration..."),
            new("12:00:02", "INFO", "Connected to database"),
            new("12:00:03", "WARN", "High memory usage detected"),
            new("12:00:04", "INFO", "Background services started"),
        };

        // Subscribe to keyboard input
        Input.OfType<IInputEvent, KeyPressed>()
            .Subscribe(HandleKeyPress)
            .DisposeWith(Subscriptions);

        // Start simulation timer to update metrics
        _simulationTimer = Observable.Interval(TimeSpan.FromSeconds(2))
            .ObserveOn(RenderFrameProvider)
            .Subscribe(_ => UpdateMetrics());

        Subscriptions.Add(_simulationTimer);
    }

    private void UpdateMetrics()
    {
        // Simulate changing metrics
        CpuUsage.Value = Math.Clamp(CpuUsage.Value + _random.Next(-10, 11), 0, 100);
        MemoryUsage.Value = Math.Clamp(MemoryUsage.Value + _random.Next(-5, 6), 0, 100);
        DiskUsage.Value = Math.Clamp(DiskUsage.Value + _random.Next(-2, 3), 0, 100);
        NetworkUsage.Value = Math.Clamp(NetworkUsage.Value + _random.Next(-15, 16), 0, 100);

        // Add a new log entry occasionally
        if (_random.Next(100) < 30)
        {
            var levels = new[] { "INFO", "DEBUG", "WARN", "ERROR" };
            var messages = new[]
            {
                "Request processed successfully",
                "Cache miss detected",
                "Connection pool adjusted",
                "Background task completed",
                "Memory pressure event",
                "Network latency spike"
            };

            var newEntries = new List<LogEntry>(LogEntries.Value)
            {
                new(DateTime.Now.ToString("HH:mm:ss"),
                    levels[_random.Next(levels.Length)],
                    messages[_random.Next(messages.Length)])
            };

            // Keep last 8 entries
            if (newEntries.Count > 8)
                newEntries.RemoveAt(0);

            LogEntries.Value = newEntries;
        }

        // Update status based on metrics
        if (CpuUsage.Value > 80 || MemoryUsage.Value > 80)
        {
            StatusMessage.Value = "WARNING: High resource usage!";
        }
        else if (CpuUsage.Value < 20 && MemoryUsage.Value < 40)
        {
            StatusMessage.Value = "System idle";
        }
        else
        {
            StatusMessage.Value = "System running normally";
        }
    }

    private void HandleKeyPress(KeyPressed key)
    {
        switch (key.KeyInfo.Key)
        {
            case ConsoleKey.UpArrow:
                if (SelectedLogIndex.Value > 0)
                {
                    SelectedLogIndex.Value--;
                }
                else if (SelectedLogIndex.Value == -1 && LogEntries.Value.Count > 0)
                {
                    SelectedLogIndex.Value = LogEntries.Value.Count - 1;
                }
                break;

            case ConsoleKey.DownArrow:
                if (SelectedLogIndex.Value < LogEntries.Value.Count - 1)
                {
                    SelectedLogIndex.Value++;
                }
                break;

            case ConsoleKey.Escape:
                Shutdown();
                break;

            case ConsoleKey.R:
                // Force refresh metrics
                UpdateMetrics();
                StatusMessage.Value = "Metrics refreshed";
                break;

            case ConsoleKey.C:
                // Clear log selection
                SelectedLogIndex.Value = -1;
                break;
        }
    }

    public override void Dispose()
    {
        CpuUsage.Dispose();
        MemoryUsage.Dispose();
        DiskUsage.Dispose();
        NetworkUsage.Dispose();
        StatusMessage.Dispose();
        LogEntries.Dispose();
        SelectedLogIndex.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Represents a log entry for the dashboard.
/// </summary>
public record LogEntry(string Time, string Level, string Message);
