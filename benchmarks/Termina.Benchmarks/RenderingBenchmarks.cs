// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Benchmarks;

/// <summary>
/// Benchmarks for rendering performance using different layout scenarios.
/// </summary>
/// <remarks>
/// Uses a mock terminal to measure pure rendering and layout calculation overhead
/// without actual console I/O. Each layout scenario is benchmarked independently.
/// </remarks>
[MemoryDiagnoser]
public class RenderingBenchmarks
{
    private BenchmarkTerminal _terminal = null!;
    private DiffingTerminal _diffingTerminal = null!;

    [ParamsSource(nameof(LayoutScenarios))]
    public LayoutScenario Scenario { get; set; } = null!;

    public IEnumerable<LayoutScenario> LayoutScenarios()
    {
        yield return new LayoutScenario(
            "SimpleText",
            () => new TextNode("Hello, World!")
        );

        yield return new LayoutScenario(
            "LargeText",
            () => new TextNode(new string('A', 1000))
        );

        yield return new LayoutScenario(
            "HorizontalLayout_3Items",
            () => new HorizontalLayout(new ILayoutNode[]
            {
                new TextNode("Left"),
                new TextNode("Center"),
                new TextNode("Right")
            })
        );

        yield return new LayoutScenario(
            "VerticalLayout_10Items",
            () =>
            {
                var items = Enumerable.Range(1, 10)
                    .Select(i => new TextNode($"Line {i}") as ILayoutNode)
                    .ToArray();
                return new VerticalLayout(items);
            }
        );

        yield return new LayoutScenario(
            "Panel_WithBorder",
            () => new PanelNode()
                .WithContent("Panel Content")
                .WithTitle("Test Panel")
                .WithBorder(BorderStyle.Single)
        );

        yield return new LayoutScenario(
            "NestedPanels_3Deep",
            () => new PanelNode()
                .WithContent(
                    new PanelNode()
                        .WithContent(
                            new PanelNode()
                                .WithContent("Deeply Nested Content")
                                .WithTitle("Inner")
                                .WithBorder(BorderStyle.Single)
                        )
                        .WithTitle("Middle")
                        .WithBorder(BorderStyle.Single)
                )
                .WithTitle("Outer")
                .WithBorder(BorderStyle.Single)
        );

        yield return new LayoutScenario(
            "ComplexGrid_3x3",
            () => new VerticalLayout(new ILayoutNode[]
            {
                new HorizontalLayout(new ILayoutNode[]
                {
                    new PanelNode().WithContent("1,1").WithBorder(BorderStyle.Single),
                    new PanelNode().WithContent("1,2").WithBorder(BorderStyle.Single),
                    new PanelNode().WithContent("1,3").WithBorder(BorderStyle.Single)
                }),
                new HorizontalLayout(new ILayoutNode[]
                {
                    new PanelNode().WithContent("2,1").WithBorder(BorderStyle.Single),
                    new PanelNode().WithContent("2,2").WithBorder(BorderStyle.Single),
                    new PanelNode().WithContent("2,3").WithBorder(BorderStyle.Single)
                }),
                new HorizontalLayout(new ILayoutNode[]
                {
                    new PanelNode().WithContent("3,1").WithBorder(BorderStyle.Single),
                    new PanelNode().WithContent("3,2").WithBorder(BorderStyle.Single),
                    new PanelNode().WithContent("3,3").WithBorder(BorderStyle.Single)
                })
            })
        );

        yield return new LayoutScenario(
            "SelectionList_20Items",
            () =>
            {
                var items = Enumerable.Range(1, 20).ToList();
                return new SelectionListNode<int>(items, i => $"Item {i}");
            }
        );
    }

    [GlobalSetup]
    public void Setup()
    {
        _terminal = new BenchmarkTerminal(80, 30);
        _diffingTerminal = new DiffingTerminal(_terminal);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Clear terminal state between iterations
        _diffingTerminal.ClearScreen();
        _diffingTerminal.Flush();
    }

    [Benchmark]
    public void MeasureOnly()
    {
        var layoutRoot = Scenario.CreateLayout();
        var available = new Size(_diffingTerminal.Width, _diffingTerminal.Height);
        var measured = layoutRoot.Measure(available);
    }

    [Benchmark]
    public void MeasureAndRender()
    {
        var layoutRoot = Scenario.CreateLayout();

        _diffingTerminal.ClearScreen();

        var available = new Size(_diffingTerminal.Width, _diffingTerminal.Height);
        var measured = layoutRoot.Measure(available);

        var context = new RegionRenderContext(_diffingTerminal, 0, 0, _diffingTerminal.Width, _diffingTerminal.Height);
        var bounds = new Rect(0, 0, _diffingTerminal.Width, _diffingTerminal.Height);

        layoutRoot.Render(context, bounds);
    }

    [Benchmark]
    public void FullRenderCycle()
    {
        var layoutRoot = Scenario.CreateLayout();

        _diffingTerminal.ClearScreen();

        var available = new Size(_diffingTerminal.Width, _diffingTerminal.Height);
        var measured = layoutRoot.Measure(available);

        var context = new RegionRenderContext(_diffingTerminal, 0, 0, _diffingTerminal.Width, _diffingTerminal.Height);
        var bounds = new Rect(0, 0, _diffingTerminal.Width, _diffingTerminal.Height);

        layoutRoot.Render(context, bounds);
        _diffingTerminal.Flush();
    }
}

/// <summary>
/// Represents a layout scenario for benchmarking.
/// </summary>
public class LayoutScenario
{
    public string Name { get; }
    private readonly Func<ILayoutNode> _factory;

    public LayoutScenario(string name, Func<ILayoutNode> factory)
    {
        Name = name;
        _factory = factory;
    }

    public ILayoutNode CreateLayout() => _factory();

    public override string ToString() => Name;
}

/// <summary>
/// A minimal terminal implementation for benchmarking that does no I/O.
/// </summary>
/// <remarks>
/// This terminal accepts all operations but performs minimal work,
/// allowing us to measure the overhead of rendering logic without
/// actual console I/O costs.
/// </remarks>
internal sealed class BenchmarkTerminal : IAnsiTerminal
{
    public int Width { get; }
    public int Height { get; }

    public BenchmarkTerminal(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void MoveTo(int x, int y) { }
    public void Write(string text) { }
    public void Write(char c) { }
    public void SetForeground(Color color) { }
    public void SetBackground(Color color) { }
    public void ResetColors() { }
    public void SaveCursor() { }
    public void RestoreCursor() { }
    public void SetCursorVisible(bool visible) { }
    public void ClearRegion(int x, int y, int width, int height) { }
    public void ClearScreen() { }
    public void Flush() { }
    public void EnterAlternateScreen() { }
    public void ExitAlternateScreen() { }
    public void EnableMouse() { }
    public void DisableMouse() { }
    public void CopyToClipboard(string text) { }
}
