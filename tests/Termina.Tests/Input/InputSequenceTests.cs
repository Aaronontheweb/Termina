// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;

namespace Termina.Tests.Input;

public class InputSequenceTests
{
    [Fact]
    public void Constructor_CapturesTextAndLength()
    {
        var sequence = new InputSequence("[13;5u");

        Assert.Equal("[13;5u", sequence.Text);
        Assert.Equal(6, sequence.Length);
        Assert.Equal('[', sequence[0]);
        Assert.False(sequence.IsEmpty);
        Assert.Equal("[13;5u", sequence.ToString());
    }

    [Fact]
    public void EmptyText_IsEmpty()
    {
        var sequence = new InputSequence(string.Empty);

        Assert.True(sequence.IsEmpty);
        Assert.Equal(0, sequence.Length);
    }

    [Fact]
    public void Default_IsEmpty()
    {
        var sequence = default(InputSequence);

        Assert.True(sequence.IsEmpty);
        Assert.Equal(string.Empty, sequence.Text);
        Assert.Equal(string.Empty, sequence.ToString());
    }

    [Fact]
    public void Constructor_RejectsNullText()
    {
        Assert.Throws<ArgumentNullException>(() => new InputSequence(null!));
    }
}
