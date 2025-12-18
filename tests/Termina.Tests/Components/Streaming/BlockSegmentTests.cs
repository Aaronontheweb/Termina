// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Components.Streaming;
using Termina.Terminal;
using Xunit;

namespace Termina.Tests.Components.Streaming;

public class BlockSegmentTests
{
    [Fact]
    public void AsBlock_WrapsSegment()
    {
        var inner = new StaticTextSegment("test", TextStyle.Default);
        var block = inner.AsBlock();

        Assert.IsType<BlockSegment>(block);
        Assert.Same(inner, block.Inner);
    }

    [Fact]
    public void AsBlock_DoesNotDoubleWrap()
    {
        var inner = new StaticTextSegment("test", TextStyle.Default);
        var block1 = inner.AsBlock();
        var block2 = block1.AsBlock();

        Assert.Same(block1, block2);
    }

    [Fact]
    public void BlockSegment_DelegatesGetCurrentSegment()
    {
        var inner = new StaticTextSegment("test", Color.Red);
        var block = new BlockSegment(inner);

        var result = block.GetCurrentSegment();

        Assert.Equal("test", result.Text);
        Assert.Equal(Color.Red, result.Style.Foreground);
    }

    [Fact]
    public async Task BlockSegment_PropagatesInvalidation_FromAnimatedInner()
    {
        var spinner = new SpinnerSegment(SpinnerStyle.Dots, intervalMs: 10);
        var block = new BlockSegment(spinner);

        var invalidated = false;
        block.Invalidated.Subscribe(_ => invalidated = true);

        // Wait for spinner animation
        await spinner.Invalidated.FirstAsync().Timeout(TimeSpan.FromSeconds(1));

        Assert.True(invalidated);

        spinner.Dispose();
    }

    [Fact]
    public void BlockSegment_IsAnimating_ReturnsFalse_ForStaticInner()
    {
        var inner = new StaticTextSegment("test", TextStyle.Default);
        var block = new BlockSegment(inner);

        Assert.False(block.IsAnimating);
    }

    [Fact]
    public void BlockSegment_IsAnimating_ReturnsTrue_ForAnimatedInner()
    {
        var spinner = new SpinnerSegment(SpinnerStyle.Dots);
        var block = new BlockSegment(spinner);

        Assert.True(block.IsAnimating);

        spinner.Dispose();
    }

    [Fact]
    public void BlockSegment_Dispose_DisposesInner()
    {
        var spinner = new SpinnerSegment(SpinnerStyle.Dots);
        var block = new BlockSegment(spinner);

        block.Dispose();

        // After dispose, spinner should stop animating
        Assert.False(spinner.IsAnimating);
    }
}
