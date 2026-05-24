// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Time.Testing;
using R3;
using Termina.Components.Streaming;
using Termina.Layout;
using Termina.Terminal;

namespace Termina.Tests.Layout;

/// <summary>
/// Tests for BlockSegment functionality in StreamingTextNode.
/// </summary>
public class StreamingTextNodeBlockSegmentTests
{
    [Fact]
    public void AppendTracked_BlockSegment_StartsOnNewLine_WhenCurrentLineHasContent()
    {
        var node = StreamingTextNode.Create();
        node.Append("First line content");

        var id = new SegmentId(1);
        node.AppendTracked(id, new StaticTextSegment("Block content", TextStyle.Default).AsBlock());

        var lines = node.Buffer.GetAllStyledLines();
        Assert.Equal(2, lines.Count);
        Assert.Equal("First line content", lines[0].ToPlainText());
        Assert.Equal("Block content", lines[1].ToPlainText());
    }

    [Fact]
    public void AppendTracked_BlockSegment_DoesNotAddExtraLine_WhenCurrentLineEmpty()
    {
        var node = StreamingTextNode.Create();

        var id = new SegmentId(1);
        node.AppendTracked(id, new StaticTextSegment("Block content", TextStyle.Default).AsBlock());

        var lines = node.Buffer.GetAllStyledLines();
        Assert.Single(lines);
        Assert.Equal("Block content", lines[0].ToPlainText());
    }

    [Fact]
    public void AppendTracked_BlockSegment_CanBeRemoved()
    {
        var node = StreamingTextNode.Create();
        var id = new SegmentId(1);
        node.AppendTracked(id, new StaticTextSegment("Thinking...", TextStyle.Default).AsBlock());

        var removed = node.Remove(id);

        Assert.True(removed);
        Assert.False(node.Buffer.HasContent);
    }

    [Fact]
    public void AppendTracked_MultipleBlockSegments_EachStartsOnNewLine()
    {
        var node = StreamingTextNode.Create();
        node.Append("Line 1");

        var id1 = new SegmentId(1);
        node.AppendTracked(id1, new StaticTextSegment("Block 1", TextStyle.Default).AsBlock());

        var id2 = new SegmentId(2);
        node.AppendTracked(id2, new StaticTextSegment("Block 2", TextStyle.Default).AsBlock());

        var lines = node.Buffer.GetAllStyledLines();
        Assert.Equal(3, lines.Count);
        Assert.Equal("Line 1", lines[0].ToPlainText());
        Assert.Equal("Block 1", lines[1].ToPlainText());
        Assert.Equal("Block 2", lines[2].ToPlainText());
    }

    [Fact]
    public void Replace_WithBlockSegment_StartsOnNewLine()
    {
        var node = StreamingTextNode.Create();
        node.Append("Prefix ");

        var id = new SegmentId(1);
        node.AppendTracked(id, new StaticTextSegment("Original", TextStyle.Default));

        // Replace with block segment
        node.Replace(id, new StaticTextSegment("Replaced Block", TextStyle.Default).AsBlock());

        var lines = node.Buffer.GetAllStyledLines();
        Assert.Equal(2, lines.Count);
        Assert.Equal("Prefix ", lines[0].ToPlainText());
        Assert.Equal("Replaced Block", lines[1].ToPlainText());
    }

    [Fact]
    public void AppendTracked_BlockSegment_AfterAppendLine()
    {
        var node = StreamingTextNode.Create();
        node.AppendLine("Complete line");

        var id = new SegmentId(1);
        node.AppendTracked(id, new StaticTextSegment("Block on new line", TextStyle.Default).AsBlock());

        var lines = node.Buffer.GetAllStyledLines();
        Assert.Equal(2, lines.Count);
        Assert.Equal("Complete line", lines[0].ToPlainText());
        Assert.Equal("Block on new line", lines[1].ToPlainText());
    }

    [Fact]
    public void AppendTracked_BlockWithCompositeSegment()
    {
        var node = StreamingTextNode.Create();
        node.Append("Before");

        var composite = new CompositeTextSegment(
            new StaticTextSegment("[", TextStyle.Default),
            new StaticTextSegment("Status", Color.Green),
            new StaticTextSegment("]", TextStyle.Default)
        ).AsBlock();

        var id = new SegmentId(1);
        node.AppendTracked(id, composite);

        var lines = node.Buffer.GetAllStyledLines();
        Assert.Equal(2, lines.Count);
        Assert.Equal("Before", lines[0].ToPlainText());
        Assert.Equal("[Status]", lines[1].ToPlainText());
    }

    [Fact]
    public void AppendTracked_BlockWithAnimatedSegment_UpdatesInPlace()
    {
        // Deterministic timing via FakeTimeProvider (per CLAUDE.md convention) —
        // no Thread.Sleep, no real timer, no async/await flakiness.
        var timeProvider = new FakeTimeProvider();
        var node = StreamingTextNode.Create();
        node.Append("Static line");

        var spinner = new SpinnerSegment(
            Termina.Components.Streaming.SpinnerStyle.Dots,
            intervalMs: 80,
            timeProvider: timeProvider);
        var blockSpinner = spinner.AsBlock();

        var id = new SegmentId(2);
        node.AppendTracked(id, blockSpinner);

        var frame1 = node.Buffer.GetAllStyledLines()[1].ToPlainText();

        // Deterministically trigger the next animation frame.
        timeProvider.Advance(TimeSpan.FromMilliseconds(80));

        var frame2 = node.Buffer.GetAllStyledLines()[1].ToPlainText();

        Assert.NotEqual(frame1, frame2);
        Assert.Equal(2, node.Buffer.GetAllStyledLines().Count); // Still 2 lines

        spinner.Dispose();
    }
}
