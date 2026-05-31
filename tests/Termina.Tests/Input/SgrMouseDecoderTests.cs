// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;

namespace Termina.Tests.Input;

public class SgrMouseDecoderTests
{
    [Theory]
    [InlineData("[<64;5;10M", +1)]
    [InlineData("[<65;5;10M", -1)]
    [InlineData("[<64;200;50M", +1)]
    public void TryDecode_ScrollSequence_ReturnsMouseScrollEvent(string sequence, int expectedDelta)
    {
        var decoded = SgrMouseDecoder.TryDecode(sequence, out var mouseEvent);

        Assert.True(decoded);
        Assert.NotNull(mouseEvent);
        Assert.Equal(expectedDelta, mouseEvent!.Delta);
    }

    [Theory]
    [InlineData("[<0;5;10M")]
    [InlineData("[<0;5;10m")]
    [InlineData("[<2;10;20M")]
    public void TryDecode_NonScrollMouseSequence_ReturnsTrueWithoutEvent(string sequence)
    {
        var decoded = SgrMouseDecoder.TryDecode(sequence, out var mouseEvent);

        Assert.True(decoded);
        Assert.Null(mouseEvent);
    }

    [Theory]
    [InlineData("[64;5;10M")]
    [InlineData("[<64M")]
    [InlineData("[<wheel;5;10M")]
    [InlineData("[<64;5;10~")]
    public void TryDecode_NonSgrMouseSequence_ReturnsFalse(string sequence)
    {
        var decoded = SgrMouseDecoder.TryDecode(sequence, out var mouseEvent);

        Assert.False(decoded);
        Assert.Null(mouseEvent);
    }
}
