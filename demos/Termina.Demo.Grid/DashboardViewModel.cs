// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Demo.Grid;

/// <summary>
/// ViewModel for the GridNode dashboard demo.
/// Demonstrates reactive properties for system metrics simulation.
/// </summary>
public partial class DashboardViewModel : ReactiveViewModel
{
    [Reactive] private int _cpuUsage = 45;
    [Reactive] private int _memoryUsage = 62;
    [Reactive] private int _diskUsage = 28;
    [Reactive] private int _networkUsage = 15;
    [Reactive] private string _statusMessage = "System running normally";
    [Reactive] private List<LogEntry> _logEntries = new();
    [Reactive] private int _selectedLogIndex = -1;

    private readonly Random _random = new();
    private IDisposable? _simulationTimer;

    public override void OnActivated()
    {
        // Initialize with some log entries
        LogEntries = new List<LogEntry>
        {
            new("12:00:00", "INFO", "Application started"),
            new("12:00:01", "DEBUG", "Loading configuration..."),
            new("12:00:02", "INFO", "Connected to database"),
            new("12:00:03", "WARN", "High memory usage detected"),
            new("12:00:04", "INFO", "Background services started"),
        };

        // Subscribe to keyboard input
        Input.OfType<KeyPressed>()
            .Subscribe(HandleKeyPress)
            .DisposeWith(Subscriptions);

        // Start simulation timer to update metrics
        _simulationTimer = Observable.Interval(TimeSpan.FromSeconds(2))
            .Subscribe(_ => UpdateMetrics());

        Subscriptions.Add(_simulationTimer);
    }

    private void UpdateMetrics()
    {
        // Simulate changing metrics
        CpuUsage = Math.Clamp(CpuUsage + _random.Next(-10, 11), 0, 100);
        MemoryUsage = Math.Clamp(MemoryUsage + _random.Next(-5, 6), 0, 100);
        DiskUsage = Math.Clamp(DiskUsage + _random.Next(-2, 3), 0, 100);
        NetworkUsage = Math.Clamp(NetworkUsage + _random.Next(-15, 16), 0, 100);

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

            var newEntries = new List<LogEntry>(LogEntries)
            {
                new(DateTime.Now.ToString("HH:mm:ss"),
                    levels[_random.Next(levels.Length)],
                    messages[_random.Next(messages.Length)])
            };

            // Keep last 8 entries
            if (newEntries.Count > 8)
                newEntries.RemoveAt(0);

            LogEntries = newEntries;
        }

        // Update status based on metrics
        if (CpuUsage > 80 || MemoryUsage > 80)
        {
            StatusMessage = "WARNING: High resource usage!";
        }
        else if (CpuUsage < 20 && MemoryUsage < 40)
        {
            StatusMessage = "System idle";
        }
        else
        {
            StatusMessage = "System running normally";
        }
    }

    private void HandleKeyPress(KeyPressed key)
    {
        switch (key.KeyInfo.Key)
        {
            case ConsoleKey.UpArrow:
                if (SelectedLogIndex > 0)
                {
                    SelectedLogIndex--;
                }
                else if (SelectedLogIndex == -1 && LogEntries.Count > 0)
                {
                    SelectedLogIndex = LogEntries.Count - 1;
                }
                break;

            case ConsoleKey.DownArrow:
                if (SelectedLogIndex < LogEntries.Count - 1)
                {
                    SelectedLogIndex++;
                }
                break;

            case ConsoleKey.Escape:
                Shutdown();
                break;

            case ConsoleKey.R:
                // Force refresh metrics
                UpdateMetrics();
                StatusMessage = "Metrics refreshed";
                break;

            case ConsoleKey.C:
                // Clear log selection
                SelectedLogIndex = -1;
                break;
        }
    }
}

/// <summary>
/// Represents a log entry for the dashboard.
/// </summary>
public record LogEntry(string Time, string Level, string Message);
