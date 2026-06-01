// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;

namespace Termina.Tests.Input;

public class UnknownSequenceFallbackDecoderTests
{
    [Theory]
    [InlineData("[")]
    [InlineData("[2")]
    [InlineData("[200")]
    [InlineData("[13;")]
    [InlineData("[<64;5")]
    public void CouldLeadToRecognizedSequence_ReturnsTrueForRecognizedPrefixes(string sequence)
    {
        Assert.True(UnknownSequenceFallbackDecoder.CouldLeadToRecognizedSequence(sequence));
    }

    [Theory]
    [InlineData("[9~")]
    [InlineData("[25;5~")]
    [InlineData("[x")]
    [InlineData("[?25h")]
    public void CouldLeadToRecognizedSequence_ReturnsFalseForUnknownCompleteOrInvalidPrefixes(string sequence)
    {
        Assert.False(UnknownSequenceFallbackDecoder.CouldLeadToRecognizedSequence(sequence));
    }

    [Fact]
    public void ToRawKeyEvents_PrependsEscapeAndFlushesSequenceCharacters()
    {
        var events = UnknownSequenceFallbackDecoder.ToRawKeyEvents("[9~");

        Assert.Equal(4, events.Count);
        AssertKey(events[0], ConsoleKey.Escape, '\x1b');
        AssertKey(events[1], ConsoleKey.None, '[');
        AssertKey(events[2], ConsoleKey.None, '9');
        AssertKey(events[3], ConsoleKey.None, '~');
    }

    [Fact]
    public void AppendRawKeyEvents_AppendsWithoutClearingExistingEvents()
    {
        var events = new List<IInputEvent>
        {
            new KeyPressed(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false))
        };

        UnknownSequenceFallbackDecoder.AppendRawKeyEvents("[x", events);

        Assert.Equal(4, events.Count);
        AssertKey(events[0], ConsoleKey.X, 'x');
        AssertKey(events[1], ConsoleKey.Escape, '\x1b');
        AssertKey(events[2], ConsoleKey.None, '[');
        AssertKey(events[3], ConsoleKey.None, 'x');
    }

    private static void AssertKey(IInputEvent inputEvent, ConsoleKey key, char keyChar)
    {
        var pressed = Assert.IsType<KeyPressed>(inputEvent);
        Assert.Equal(key, pressed.KeyInfo.Key);
        Assert.Equal(keyChar, pressed.KeyInfo.KeyChar);
    }
}
