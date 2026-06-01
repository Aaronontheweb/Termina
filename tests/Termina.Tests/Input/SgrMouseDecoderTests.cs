// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;

namespace Termina.Tests.Input;

public class SgrMouseDecoderTests
{
    [Theory]
    [InlineData("[<64;5;10M", MouseButton.WheelUp, 5, 10)]
    [InlineData("[<65;5;10M", MouseButton.WheelDown, 5, 10)]
    [InlineData("[<64;200;50M", MouseButton.WheelUp, 200, 50)]
    public void TryDecode_ScrollSequence_ReturnsPointerInput(
        string sequence,
        MouseButton expectedButton,
        int expectedX,
        int expectedY)
    {
        var decoded = SgrMouseDecoder.TryDecode(sequence, out var pointerInput);

        Assert.True(decoded);
        Assert.NotNull(pointerInput);
        Assert.Equal(PointerAction.Wheel, pointerInput!.Action);
        Assert.Equal(expectedButton, pointerInput.Button);
        Assert.Equal(expectedX, pointerInput.X);
        Assert.Equal(expectedY, pointerInput.Y);
    }

    [Theory]
    [InlineData("[<0;5;10M")]
    [InlineData("[<0;5;10m")]
    [InlineData("[<2;10;20M")]
    public void TryDecode_NonScrollMouseSequence_ReturnsTrueWithoutEvent(string sequence)
    {
        var decoded = SgrMouseDecoder.TryDecode(sequence, out var pointerInput);

        Assert.True(decoded);
        Assert.Null(pointerInput);
    }

    [Theory]
    [InlineData("[64;5;10M")]
    [InlineData("[<64M")]
    [InlineData("[<wheel;5;10M")]
    [InlineData("[<64;5;10~")]
    public void TryDecode_NonSgrMouseSequence_ReturnsFalse(string sequence)
    {
        var decoded = SgrMouseDecoder.TryDecode(sequence, out var pointerInput);

        Assert.False(decoded);
        Assert.Null(pointerInput);
    }
}
