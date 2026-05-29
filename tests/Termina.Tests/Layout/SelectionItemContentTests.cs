// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Time.Testing;
using R3;
using Termina.Components.Streaming;
using Termina.Layout;
using Termina.Terminal;
using Streaming = Termina.Components.Streaming;

namespace Termina.Tests.Layout;

/// <summary>
/// Tests for the SelectionItemContent builder class.
/// </summary>
public class SelectionItemContentTests
{
    [Fact]
    public void AddLine_WithSegments_CreatesLine()
    {
        using var content = new SelectionItemContent()
            .AddLine(new StaticTextSegment(new StyledSegment("Hello World")));

        Assert.Equal(1, content.LineCount);
        Assert.Single(content.Lines);
        Assert.Single(content.Lines[0]);
    }

    [Fact]
    public void AddLine_Multiple_CreatesMultipleLines()
    {
        using var content = new SelectionItemContent()
            .AddLine(new StaticTextSegment(new StyledSegment("Line 1")))
            .AddLine(new StaticTextSegment(new StyledSegment("Line 2")))
            .AddLine(new StaticTextSegment(new StyledSegment("Line 3")));

        Assert.Equal(3, content.LineCount);
    }

    [Fact]
    public void AddLine_WithMultipleSegments_StoresAll()
    {
        using var content = new SelectionItemContent()
            .AddLine(
                new StaticTextSegment("Status: ", Color.Gray),
                new StaticTextSegment("Connected", Color.Green));

        Assert.Equal(1, content.LineCount);
        Assert.Equal(2, content.Lines[0].Count);
    }

    [Fact]
    public void AddLine_WithString_CreatesStaticSegment()
    {
        using var content = new SelectionItemContent()
            .AddLine("Simple text");

        Assert.Equal(1, content.LineCount);
        Assert.Equal("Simple text", content.GetFirstLineText());
    }

    [Fact]
    public void AddLine_WithStringAndColor_AppliesStyle()
    {
        using var content = new SelectionItemContent()
            .AddLine("Colored text", Color.Red);

        var segment = content.Lines[0][0].GetCurrentSegment();
        Assert.Equal("Colored text", segment.Text);
        Assert.Equal(Color.Red, segment.Style.Foreground);
    }

    [Fact]
    public void ToPlainText_SingleLine_ReturnsText()
    {
        using var content = new SelectionItemContent()
            .AddLine(new StaticTextSegment(new StyledSegment("Hello World")));

        Assert.Equal("Hello World", content.ToPlainText());
    }

    [Fact]
    public void ToPlainText_MultipleLines_JoinsWithNewlines()
    {
        using var content = new SelectionItemContent()
            .AddLine(new StaticTextSegment(new StyledSegment("Line 1")))
            .AddLine(new StaticTextSegment(new StyledSegment("Line 2")));

        Assert.Equal("Line 1\nLine 2", content.ToPlainText());
    }

    [Fact]
    public void ToPlainText_MultipleSegmentsPerLine_Concatenates()
    {
        using var content = new SelectionItemContent()
            .AddLine(
                new StaticTextSegment(new StyledSegment("A")),
                new StaticTextSegment(new StyledSegment("B")),
                new StaticTextSegment(new StyledSegment("C")));

        Assert.Equal("ABC", content.ToPlainText());
    }

    [Fact]
    public void GetFirstLineText_ReturnsFirstLineOnly()
    {
        using var content = new SelectionItemContent()
            .AddLine(new StaticTextSegment(new StyledSegment("First")))
            .AddLine(new StaticTextSegment(new StyledSegment("Second")));

        Assert.Equal("First", content.GetFirstLineText());
    }

    [Fact]
    public void GetFirstLineText_Empty_ReturnsEmptyString()
    {
        using var content = new SelectionItemContent();

        Assert.Equal(string.Empty, content.GetFirstLineText());
    }

    [Fact]
    public void FromString_CreatesSimpleContent()
    {
        using var content = SelectionItemContent.FromString("Simple");

        Assert.Equal(1, content.LineCount);
        Assert.Equal("Simple", content.GetFirstLineText());
    }

    [Fact]
    public void HasAnimations_WithoutAnimated_ReturnsFalse()
    {
        using var content = new SelectionItemContent()
            .AddLine(new StaticTextSegment(new StyledSegment("Static content")));

        Assert.False(content.HasAnimations);
    }

    [Fact]
    public void HasAnimations_WithAnimated_ReturnsTrue()
    {
        using var content = new SelectionItemContent()
            .AddLine(new SpinnerSegment(Streaming.SpinnerStyle.Dots, intervalMs: 1000));

        Assert.True(content.HasAnimations);
    }

    [Fact]
    public void Invalidated_EmitsOnAnimationChange()
    {
        var timeProvider = new FakeTimeProvider();
        using var content = new SelectionItemContent()
            .AddLine(new SpinnerSegment(Streaming.SpinnerStyle.Line, intervalMs: 10, timeProvider: timeProvider));

        var invalidationCount = 0;
        using var subscription = content.Invalidated.Subscribe(_ => invalidationCount++);

        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        Assert.Equal(1, invalidationCount);

        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        Assert.Equal(2, invalidationCount);
    }

    [Fact]
    public void Dispose_CompletesInvalidated()
    {
        var content = new SelectionItemContent()
            .AddLine(new StaticTextSegment(new StyledSegment("Test")));

        var completed = false;
        content.Invalidated.Subscribe(
            onNext: _ => { },
            onCompleted: _ => completed = true);

        content.Dispose();

        Assert.True(completed);
    }

    [Fact]
    public void AddLine_ReturnsSelf_ForFluent()
    {
        using var content = new SelectionItemContent();

        var result = content.AddLine(new StaticTextSegment(new StyledSegment("Test")));

        Assert.Same(content, result);
    }

    [Fact]
    public void ComplexContent_MultiLineMultiSegment()
    {
        using var content = new SelectionItemContent()
            .AddLine(
                new StaticTextSegment("Server: ", Color.Gray),
                new StaticTextSegment("Production", Color.White, decoration: TextDecoration.Bold))
            .AddLine(
                new StaticTextSegment("   Status: ", Color.Gray),
                new StaticTextSegment("Connected", Color.Green))
            .AddLine(
                new StaticTextSegment("   Latency: ", Color.Gray),
                new StaticTextSegment("3ms", Color.Cyan));

        Assert.Equal(3, content.LineCount);
        Assert.Equal("Server: Production", content.GetFirstLineText());
        Assert.Contains("Connected", content.ToPlainText());
        Assert.Contains("3ms", content.ToPlainText());
    }
}
