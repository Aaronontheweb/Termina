// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Extensions;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.Grid;

/// <summary>
/// Dashboard page demonstrating GridNode capabilities.
/// Shows a system dashboard with nested grids, metrics, and log display.
/// </summary>
public class DashboardPage : ReactivePage<DashboardViewModel>
{
    public override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            // Main content area using GridNode
            .WithChild(BuildDashboardGrid().Fill())
            // Status bar at bottom
            .WithChild(BuildStatusBar());
    }

    private GridNode BuildDashboardGrid()
    {
        // Main dashboard: 2 columns (metrics panel left, logs right)
        // With a header row spanning both columns
        var dashboard = new GridNode()
            .WithColumns(
                SizeConstraint.Exactly(35),      // Left column: fixed width for metrics
                SizeConstraint.FillRemaining())  // Right column: remaining space for logs
            .WithRows(
                SizeConstraint.Exactly(3),       // Row 0: Header
                SizeConstraint.FillRemaining())  // Row 1: Content
            .WithGridLines(BorderStyle.Double)
            .WithGridLineColor(Color.Blue);

        // Header spanning both columns
        dashboard.SetCell(0, 0,
            new TextNode("System Dashboard")
                .AlignCenter()
                .Bold()
                .WithForeground(Color.BrightCyan),
            colSpan: 2);

        // Left column: Metrics panel with nested grid
        dashboard.SetCell(1, 0, BuildMetricsPanel());

        // Right column: Log entries panel
        dashboard.SetCell(1, 1, BuildLogsPanel());

        return dashboard;
    }

    private ILayoutNode BuildMetricsPanel()
    {
        return new PanelNode()
            .WithTitle("System Metrics")
            .WithBorder(BorderStyle.Single)
            .WithBorderColor(Color.Green)
            .WithContent(BuildMetricsGrid());
    }

    private ILayoutNode BuildMetricsGrid()
    {
        // Metrics grid: 2 columns (label, progress bar)
        var metricsGrid = new GridNode()
            .WithColumns(
                SizeConstraint.Exactly(10),      // Label column
                SizeConstraint.FillRemaining())  // Progress bar column
            .WithGridLines(BorderStyle.None);

        // CPU row
        metricsGrid.AddRow(
            new TextNode("CPU:").WithForeground(Color.Gray),
            ViewModel.CpuUsage
                .Select<int, ILayoutNode>(cpu => BuildProgressBar(cpu, GetMetricColor(cpu)))
                .AsLayout());

        // Memory row
        metricsGrid.AddRow(
            new TextNode("Memory:").WithForeground(Color.Gray),
            ViewModel.MemoryUsage
                .Select<int, ILayoutNode>(mem => BuildProgressBar(mem, GetMetricColor(mem)))
                .AsLayout());

        // Disk row
        metricsGrid.AddRow(
            new TextNode("Disk:").WithForeground(Color.Gray),
            ViewModel.DiskUsage
                .Select<int, ILayoutNode>(disk => BuildProgressBar(disk, GetMetricColor(disk)))
                .AsLayout());

        // Network row
        metricsGrid.AddRow(
            new TextNode("Network:").WithForeground(Color.Gray),
            ViewModel.NetworkUsage
                .Select<int, ILayoutNode>(net => BuildProgressBar(net, GetMetricColor(net)))
                .AsLayout());

        return metricsGrid;
    }

    private static TextNode BuildProgressBar(int percentage, Color color)
    {
        const int barWidth = 12;
        var filled = (int)(percentage / 100.0 * barWidth);
        var empty = barWidth - filled;
        var bar = new string('█', filled) + new string('░', empty);
        return new TextNode($"{bar} {percentage,3}%")
            .WithForeground(color)
            .NoWrap();
    }

    private static Color GetMetricColor(int percentage)
    {
        return percentage switch
        {
            >= 80 => Color.Red,
            >= 60 => Color.Yellow,
            _ => Color.Green
        };
    }

    private ILayoutNode BuildLogsPanel()
    {
        return new PanelNode()
            .WithTitle("Recent Events")
            .WithBorder(BorderStyle.Single)
            .WithBorderColor(Color.Magenta)
            .WithContent(BuildLogsGrid());
    }

    private ILayoutNode BuildLogsGrid()
    {
        // Logs grid: 3 columns (time, level, message)
        // Header row + data rows
        return ViewModel.LogEntries
            .CombineLatest(ViewModel.SelectedLogIndex, (logs, selectedIndex) => (logs, selectedIndex))
            .Select(state =>
            {
                var (logs, selectedIndex) = state;

                var logsGrid = new GridNode()
                    .WithColumns(
                        SizeConstraint.Exactly(10),  // Time
                        SizeConstraint.Exactly(7),   // Level
                        SizeConstraint.FillRemaining()) // Message
                    .WithGridLines(BorderStyle.None);

                // Header row
                logsGrid.AddRow(
                    new TextNode("Time").Bold().WithForeground(Color.Cyan),
                    new TextNode("Level").Bold().WithForeground(Color.Cyan),
                    new TextNode("Message").Bold().WithForeground(Color.Cyan));

                // Data rows
                for (var i = 0; i < logs.Count; i++)
                {
                    var entry = logs[i];
                    var isSelected = i == selectedIndex;
                    var levelColor = GetLevelColor(entry.Level);

                    if (isSelected)
                    {
                        logsGrid.AddRow(
                            new TextNode(entry.Time).WithBackground(Color.Blue).WithForeground(Color.White),
                            new TextNode(entry.Level).WithBackground(Color.Blue).WithForeground(Color.White),
                            new TextNode(entry.Message).WithBackground(Color.Blue).WithForeground(Color.White).NoWrap());
                    }
                    else
                    {
                        logsGrid.AddRow(
                            new TextNode(entry.Time).WithForeground(Color.Gray),
                            new TextNode(entry.Level).WithForeground(levelColor),
                            new TextNode(entry.Message).WithForeground(Color.White).NoWrap());
                    }
                }

                return (ILayoutNode)logsGrid;
            })
            .AsLayout();
    }

    private static Color GetLevelColor(string level)
    {
        return level switch
        {
            "ERROR" => Color.Red,
            "WARN" => Color.Yellow,
            "INFO" => Color.Green,
            "DEBUG" => Color.Gray,
            _ => Color.White
        };
    }

    private HorizontalLayout BuildStatusBar()
    {
        var statusBar = Layouts.Horizontal()
            .WithChild(
                ViewModel.StatusMessage
                    .Select<string, ILayoutNode>(status => new TextNode(status)
                        .WithForeground(Color.BrightYellow)
                        .NoWrap())
                    .AsLayout()
                    .Fill())
            .WithChild(
                new TextNode("[↑/↓] Select  [R] Refresh  [C] Clear  [Esc] Quit")
                    .WithForeground(Color.BrightBlack)
                    .NoWrap()
                    .WidthAuto());
        statusBar.Height(1);
        return statusBar;
    }
}
