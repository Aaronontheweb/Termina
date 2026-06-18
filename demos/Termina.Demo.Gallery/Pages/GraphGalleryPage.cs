// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Components.Streaming;
using Termina.Extensions;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.Gallery.Pages;

public sealed class GraphGalleryPage : ReactivePage<GraphGalleryViewModel>
{
    private SelectionListNode<GraphStyleItem> _styleList = null!;
    private readonly Random _random = new(42);

    private GraphNode _demoGraph = null!;
    private ProgressBarNode _progressBar = null!;
    private double _progressValue;
    private readonly List<double> _dataPoints = new();

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        KeyBindings.Register(ConsoleKey.Escape, () => Navigate("/menu"));

        KeyBindings.Register(ConsoleKey.Spacebar, () =>
        {
            var highlighted = _styleList.HighlightedItem;
            if (highlighted != null)
                ViewModel.SelectedStyle.Value = highlighted.Value.Style;
        });

        _styleList.SelectionConfirmed
            .Subscribe(items =>
            {
                var item = items.FirstOrDefault();
                if (item != null)
                    ViewModel.SelectedStyle.Value = item.Style;
            })
            .DisposeWith(Subscriptions);

        ViewModel.SelectedStyle
            .ObserveOn(RenderFrameProvider)
            .Subscribe(style => _demoGraph.WithStyle(style))
            .DisposeWith(Subscriptions);

        Observable.Interval(TimeSpan.FromMilliseconds(200), TimeProvider.System)
            .ObserveOn(RenderFrameProvider)
            .Subscribe(_ => PushData())
            .DisposeWith(Subscriptions);

        Focus.PushFocus(_styleList);
    }

    private void PushData()
    {
        _dataPoints.Add(50 + _random.NextDouble() * 50 * Math.Sin(_dataPoints.Count * 0.15) + _random.NextDouble() * 20);
        if (_dataPoints.Count > 120)
            _dataPoints.RemoveAt(0);
        _demoGraph.SetData(_dataPoints.ToArray());

        _progressValue = (_progressValue + 0.008) % 1.01;
        _progressBar.WithValue(_progressValue);
    }

    public override ILayoutNode BuildLayout()
    {
        _styleList = new SelectionListNode<GraphStyleItem>(
            ViewModel.GraphStyles,
            item => new SelectionItemContent()
                .AddLine(
                    new StaticTextSegment(item.Name, Color.BrightCyan, decoration: TextDecoration.Bold),
                    new StaticTextSegment($"  {item.Description}", Color.Gray)))
            .WithMode(SelectionMode.Single)
            .WithShowNumbers(true)
            .WithHighlightColors(Color.Black, Color.Cyan)
            .WithVisibleRows(6);

        var gradient = Gradient.Create(Color.FromRgb(0, 100, 255), Color.FromRgb(0, 255, 100), Color.FromRgb(255, 255, 0));

        _demoGraph = new GraphNode(intervalMs: 0)
            .WithStyle(ViewModel.SelectedStyle.Value)
            .WithGradient(gradient)
            .WithRange(0, 100);

        _progressBar = new ProgressBarNode()
            .WithGradient(Gradient.Create(Color.FromRgb(255, 50, 50), Color.FromRgb(255, 200, 0), Color.FromRgb(50, 255, 50)))
            .WithValue(0)
            .WithLabel("{0:P0}");

        var grid = new GridNode()
            .WithColumns(SizeConstraint.Percentage(30), SizeConstraint.Percentage(70))
            .WithRows(SizeConstraint.FillRemaining())
            .WithGridLines(BorderStyle.Single)
            .WithGridLineColor(Color.BrightBlack);

        grid.SetCell(0, 0,
            Layouts.Vertical()
                .WithChild(
                    new TextNode("Graph Style")
                        .WithForeground(Color.BrightCyan)
                        .Bold()
                        .Height(2))
                .WithChild(_styleList));

        grid.SetCell(0, 1,
            Layouts.Vertical()
                .WithChild(
                    new TextNode("Live Graph")
                        .WithForeground(Color.BrightYellow)
                        .Bold()
                        .Height(2))
                .WithChild(_demoGraph.Fill())
                .WithChild(new EmptyNode().Height(1))
                .WithChild(
                    new TextNode("Progress Bar with Gradient")
                        .WithForeground(Color.BrightYellow)
                        .Bold()
                        .Height(1))
                .WithChild(_progressBar.Height(1)));

        return Layouts.Vertical()
            .WithChild(
                new PanelNode()
                    .WithTitle("Graph & Progress Gallery")
                    .WithBorder(BorderStyle.Double)
                    .WithBorderColor(Color.BrightMagenta)
                    .WithContent(grid)
                    .Fill())
            .WithChild(
                new TextNode("[↑/↓] Navigate  [Enter/Space] Switch Style  [Esc] Menu")
                    .WithForeground(Color.BrightBlack)
                    .NoWrap()
                    .Height(1));
    }
}
