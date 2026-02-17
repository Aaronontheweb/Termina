// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Components.Streaming;
using Termina.Layout;
using Termina.Terminal;
using SpinnerStyle = Termina.Components.Streaming.SpinnerStyle;

namespace Termina.Tests.Layout;

/// <summary>
/// Tests for tracked segment functionality in StreamingTextNode.
/// </summary>
public class StreamingTextNodeTrackedSegmentTests
{
    [Fact]
    public void AppendTracked_WithStaticSegment_AddsToBuffer()
    {
        var node = StreamingTextNode.Create();
        var id = new SegmentId(1);
        var segment = new StaticTextSegment("tracked", TextStyle.Default);

        node.AppendTracked(id, segment);

        var lines = node.Buffer.GetAllStyledLines();
        Assert.Single(lines);
        Assert.Equal("tracked", lines[0].Segments[0].Text);
    }

    [Fact]
    public void AppendTracked_WithNoneId_ThrowsArgumentException()
    {
        var node = StreamingTextNode.Create();
        var segment = new StaticTextSegment("test", TextStyle.Default);

        var ex = Assert.Throws<ArgumentException>(() => node.AppendTracked(SegmentId.None, segment));
        Assert.Contains("SegmentId.None cannot be used", ex.Message);
    }

    [Fact]
    public void AppendTracked_WithDuplicateId_ThrowsArgumentException()
    {
        var node = StreamingTextNode.Create();
        var id = new SegmentId(1);

        node.AppendTracked(id, new StaticTextSegment("first", TextStyle.Default));

        var ex = Assert.Throws<ArgumentException>(() =>
            node.AppendTracked(id, new StaticTextSegment("second", TextStyle.Default)));
        Assert.Contains("already in use", ex.Message);
    }

    [Fact]
    public void Remove_ExistingSegment_RemovesFromBuffer()
    {
        var node = StreamingTextNode.Create();
        var id = new SegmentId(1);

        node.Append("Before ");
        node.AppendTracked(id, new StaticTextSegment("tracked", TextStyle.Default));
        node.Append(" After");

        var result = node.Remove(id);

        Assert.True(result);
        var lines = node.Buffer.GetAllStyledLines();
        Assert.Single(lines);
        Assert.Equal("Before  After", lines[0].Segments[0].Text);
    }

    [Fact]
    public void Remove_NonExistentSegment_ReturnsFalse()
    {
        var node = StreamingTextNode.Create();
        var result = node.Remove(new SegmentId(999));
        Assert.False(result);
    }

    [Fact]
    public void Remove_DisposesSegment()
    {
        var node = StreamingTextNode.Create();
        var id = new SegmentId(1);
        var segment = new SpinnerSegment(SpinnerStyle.Dots);

        node.AppendTracked(id, segment);
        Assert.True(segment.IsAnimating);

        node.Remove(id);

        // Spinner should be disposed and stopped
        Assert.False(segment.IsAnimating);
    }

    [Fact]
    public void Replace_WithTrackedSegment_ReplacesContent()
    {
        var node = StreamingTextNode.Create();
        var id = new SegmentId(1);

        node.AppendTracked(id, new StaticTextSegment("original", TextStyle.Default));

        var result = node.Replace(id, new StaticTextSegment("replaced", TextStyle.Default), keepTracked: true);

        Assert.True(result);
        var lines = node.Buffer.GetAllStyledLines();
        Assert.Equal("replaced", lines[0].Segments[0].Text);
    }

    [Fact]
    public void Replace_WithUntrackedSegment_ConvertsToStatic()
    {
        var node = StreamingTextNode.Create();
        var id = new SegmentId(1);

        node.AppendTracked(id, new StaticTextSegment("tracked", TextStyle.Default));

        // Replace with untracked
        var result = node.Replace(id, new StaticTextSegment("untracked", TextStyle.Default), keepTracked: false);

        Assert.True(result);

        // ID should now be free for reuse
        node.AppendTracked(id, new StaticTextSegment("reused", TextStyle.Default));
        var lines = node.Buffer.GetAllStyledLines();
        Assert.Equal("untrackedreused", lines[0].Segments[0].Text);
    }

    [Fact]
    public void Replace_NonExistentSegment_ReturnsFalse()
    {
        var node = StreamingTextNode.Create();
        var result = node.Replace(new SegmentId(999), new StaticTextSegment("test", TextStyle.Default));
        Assert.False(result);
    }

    [Fact]
    public void Replace_DisposesOldSegment()
    {
        var node = StreamingTextNode.Create();
        var id = new SegmentId(1);
        var oldSpinner = new SpinnerSegment(SpinnerStyle.Dots);

        node.AppendTracked(id, oldSpinner);
        Assert.True(oldSpinner.IsAnimating);

        node.Replace(id, new StaticTextSegment("static", TextStyle.Default));

        Assert.False(oldSpinner.IsAnimating);
    }

    [Fact]
    public void AppendTracked_MixedWithUntracked_MaintainsOrder()
    {
        var node = StreamingTextNode.Create();

        node.Append("A");
        node.AppendTracked(new SegmentId(1), new StaticTextSegment("B", TextStyle.Default));
        node.Append("C");
        node.AppendTracked(new SegmentId(2), new StaticTextSegment("D", TextStyle.Default));
        node.Append("E");

        var lines = node.Buffer.GetAllStyledLines();
        Assert.Equal("ABCDE", lines[0].Segments[0].Text);
    }

    [Fact]
    public void Remove_UpdatesIndicesCorrectly()
    {
        var node = StreamingTextNode.Create();

        node.AppendTracked(new SegmentId(1), new StaticTextSegment("A", TextStyle.Default));
        node.AppendTracked(new SegmentId(2), new StaticTextSegment("B", TextStyle.Default));
        node.AppendTracked(new SegmentId(3), new StaticTextSegment("C", TextStyle.Default));

        // Remove middle element
        node.Remove(new SegmentId(2));

        // Should be able to remove element 3 (indices were updated)
        var result = node.Remove(new SegmentId(3));
        Assert.True(result);

        var lines = node.Buffer.GetAllStyledLines();
        Assert.Equal("A", lines[0].Segments[0].Text);
    }

    [Fact]
    public async Task SpinnerSegment_AnimationFramesChange()
    {
        var spinner = new SpinnerSegment(SpinnerStyle.Line, Color.Yellow, intervalMs: 10);

        // Get initial frame
        var frame1 = spinner.GetCurrentSegment().Text;

        // Wait for actual invalidation event instead of sleeping
        await spinner.Invalidated
            .FirstAsync()
            .WaitAsync(TimeSpan.FromSeconds(1));

        var frame2 = spinner.GetCurrentSegment().Text;

        // Frames should differ (animation progressed)
        Assert.NotEqual(frame1, frame2);

        spinner.Dispose();
    }

    [Fact]
    public async Task SpinnerSegment_InvalidatedEventFires()
    {
        var spinner = new SpinnerSegment(SpinnerStyle.Dots, intervalMs: 10);

        // Wait for actual invalidation event instead of sleeping
        await spinner.Invalidated
            .FirstAsync()
            .WaitAsync(TimeSpan.FromSeconds(1));

        // If we get here without timeout, the event fired
        spinner.Dispose();
    }

    [Fact]
    public void SpinnerSegment_StopPausesAnimation()
    {
        var spinner = new SpinnerSegment(SpinnerStyle.Dots);

        Assert.True(spinner.IsAnimating);

        spinner.Stop();

        Assert.False(spinner.IsAnimating);

        spinner.Dispose();
    }

    [Fact]
    public void StaticTextSegment_WithStyle_RendersCorrectly()
    {
        var segment = new StaticTextSegment("styled", Color.Red, Color.Blue, TextDecoration.Bold);

        var styled = segment.GetCurrentSegment();

        Assert.Equal("styled", styled.Text);
        Assert.Equal(Color.Red, styled.Style.Foreground);
        Assert.Equal(Color.Blue, styled.Style.Background);
        Assert.Equal(TextDecoration.Bold, styled.Style.Decoration);
    }

    [Fact]
    public void Clear_RemovesAllTrackedSegments()
    {
        var node = StreamingTextNode.Create();
        var spinner = new SpinnerSegment(SpinnerStyle.Dots);

        node.AppendTracked(new SegmentId(1), spinner);
        node.Append("text");
        node.AppendTracked(new SegmentId(2), new StaticTextSegment("tracked", TextStyle.Default));

        node.Clear();

        // Buffer should be empty
        var lines = node.Buffer.GetAllStyledLines();
        Assert.Empty(lines);

        // Spinner should be disposed
        Assert.False(spinner.IsAnimating);
    }

    [Fact]
    public void Dispose_DisposesAllTrackedSegments()
    {
        var node = StreamingTextNode.Create();
        var spinner1 = new SpinnerSegment(SpinnerStyle.Dots);
        var spinner2 = new SpinnerSegment(SpinnerStyle.Line);

        node.AppendTracked(new SegmentId(1), spinner1);
        node.AppendTracked(new SegmentId(2), spinner2);

        node.Dispose();

        Assert.False(spinner1.IsAnimating);
        Assert.False(spinner2.IsAnimating);
    }

    [Fact]
    public async Task Replace_KeepTracked_SubscribesToNewAnimation()
    {
        var node = StreamingTextNode.Create();
        var id = new SegmentId(1);

        node.AppendTracked(id, new StaticTextSegment("static", TextStyle.Default));

        // Replace with animated segment
        var newSpinner = new SpinnerSegment(SpinnerStyle.Dots, intervalMs: 10);
        node.Replace(id, newSpinner, keepTracked: true);

        // Wait for actual content change event instead of sleeping
        await node.ContentChanged
            .FirstAsync()
            .WaitAsync(TimeSpan.FromSeconds(1));

        // If we get here without timeout, the event fired
        newSpinner.Dispose();
    }
}
