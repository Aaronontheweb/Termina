// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Time.Testing;
using Termina.Components.Streaming;
using R3;

namespace Termina.Tests.Components.Streaming;

/// <summary>
/// Tests for the CompositeTextSegment class.
/// </summary>
public class CompositeTextSegmentTests
{
    [Fact]
    public void Constructor_WithStaticSegments_CreatesComposite()
    {
        using var composite = new CompositeTextSegment(
            new StaticTextSegment(new StyledSegment("Hello ")),
            new StaticTextSegment(new StyledSegment("World")));

        Assert.Equal(2, composite.Children.Count);
    }

    [Fact]
    public void GetCurrentSegment_CombinesAllChildText()
    {
        using var composite = new CompositeTextSegment(
            new StaticTextSegment(new StyledSegment("Hello ")),
            new StaticTextSegment(new StyledSegment("World")));

        var segment = composite.GetCurrentSegment();

        Assert.Equal("Hello World", segment.Text);
    }

    [Fact]
    public void Children_ReturnsReadOnlyList()
    {
        using var composite = new CompositeTextSegment(
            new StaticTextSegment(new StyledSegment("A")),
            new StaticTextSegment(new StyledSegment("B")),
            new StaticTextSegment(new StyledSegment("C")));

        Assert.Equal(3, composite.Children.Count);
        Assert.Equal("A", composite.Children[0].GetCurrentSegment().Text);
        Assert.Equal("B", composite.Children[1].GetCurrentSegment().Text);
        Assert.Equal("C", composite.Children[2].GetCurrentSegment().Text);
    }

    [Fact]
    public void IsAnimating_WithoutAnimatedChildren_ReturnsFalse()
    {
        using var composite = new CompositeTextSegment(
            new StaticTextSegment(new StyledSegment("Static 1")),
            new StaticTextSegment(new StyledSegment("Static 2")));

        Assert.False(composite.IsAnimating);
    }

    [Fact]
    public void IsAnimating_WithAnimatedChild_ReturnsTrue()
    {
        using var spinner = new SpinnerSegment(SpinnerStyle.Dots, intervalMs: 1000);
        using var composite = new CompositeTextSegment(
            new StaticTextSegment(new StyledSegment("[")),
            spinner,
            new StaticTextSegment(new StyledSegment("]")));

        Assert.True(composite.IsAnimating);
    }

    [Fact]
    public void Invalidated_PropagatesFromAnimatedChild()
    {
        var timeProvider = new FakeTimeProvider();
        using var spinner = new SpinnerSegment(SpinnerStyle.Line, intervalMs: 80, timeProvider: timeProvider);
        using var composite = new CompositeTextSegment(
            new StaticTextSegment(new StyledSegment("Loading: ")),
            spinner);

        var invalidated = false;
        composite.Invalidated.Subscribe(_ => invalidated = true);

        // Advance time to trigger invalidation from spinner
        timeProvider.Advance(TimeSpan.FromMilliseconds(80));

        Assert.True(invalidated);
    }

    [Fact]
    public void Stop_StopsAllAnimatedChildren()
    {
        using var spinner = new SpinnerSegment(SpinnerStyle.Dots, intervalMs: 100);
        using var composite = new CompositeTextSegment(
            new StaticTextSegment(new StyledSegment("Status: ")),
            spinner);

        Assert.True(spinner.IsAnimating);

        composite.Stop();

        Assert.False(spinner.IsAnimating);
    }

    [Fact]
    public void Start_StartsAllAnimatedChildren()
    {
        using var spinner = new SpinnerSegment(SpinnerStyle.Dots, intervalMs: 100);
        spinner.Stop();

        using var composite = new CompositeTextSegment(
            new StaticTextSegment(new StyledSegment("Status: ")),
            spinner);

        composite.Start();

        Assert.True(spinner.IsAnimating);
    }

    [Fact]
    public void Dispose_DisposesAllChildren()
    {
        var static1 = new StaticTextSegment(new StyledSegment("A"));
        var static2 = new StaticTextSegment(new StyledSegment("B"));
        var composite = new CompositeTextSegment(static1, static2);

        var invalidatedCompleted = false;
        composite.Invalidated.Subscribe(
            onNext: _ => { },
            onCompleted: _ => invalidatedCompleted = true);

        composite.Dispose();

        Assert.True(invalidatedCompleted);
    }

    [Fact]
    public void Constructor_WithEnumerable_CreatesComposite()
    {
        var segments = new ITextSegment[]
        {
            new StaticTextSegment(new StyledSegment("One")),
            new StaticTextSegment(new StyledSegment(" ")),
            new StaticTextSegment(new StyledSegment("Two"))
        };

        using var composite = new CompositeTextSegment(segments);

        Assert.Equal(3, composite.Children.Count);
        Assert.Equal("One Two", composite.GetCurrentSegment().Text);
    }

    [Fact]
    public void NestedComposite_CombinesCorrectly()
    {
        using var inner = new CompositeTextSegment(
            new StaticTextSegment(new StyledSegment("inner")),
            new StaticTextSegment(new StyledSegment("-text")));

        using var outer = new CompositeTextSegment(
            new StaticTextSegment(new StyledSegment("[")),
            inner,
            new StaticTextSegment(new StyledSegment("]")));

        Assert.Equal("[inner-text]", outer.GetCurrentSegment().Text);
    }
}
