// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;

namespace Termina.Tests.Input;

public class BracketedPasteDecoderTests
{
    [Theory]
    [InlineData("[")]
    [InlineData("[2")]
    [InlineData("[20")]
    [InlineData("[200")]
    [InlineData("[200~")]
    public void CouldBeStartSequence_ReturnsTrueForPartialStart(string sequence)
    {
        Assert.True(BracketedPasteDecoder.CouldBeStartSequence(sequence));
    }

    [Theory]
    [InlineData("[201~")]
    [InlineData("[20x")]
    [InlineData("[200~x")]
    public void CouldBeStartSequence_ReturnsFalseForNonStart(string sequence)
    {
        Assert.False(BracketedPasteDecoder.CouldBeStartSequence(sequence));
    }

    [Fact]
    public void TryConsume_CompletePaste_ReturnsPasteEvent()
    {
        var decoder = new BracketedPasteDecoder();
        decoder.Begin();

        var completed = Feed(decoder, "hello world\x1b[201~", out var pasteEvent);

        Assert.True(completed);
        Assert.NotNull(pasteEvent);
        Assert.Equal("hello world", pasteEvent!.Content);
    }

    [Fact]
    public void TryConsume_PreservesFailedPartialEndSentinel()
    {
        var decoder = new BracketedPasteDecoder();
        decoder.Begin();

        var completed = Feed(decoder, "foo\x1b[20xbar\x1b[201~", out var pasteEvent);

        Assert.True(completed);
        Assert.NotNull(pasteEvent);
        Assert.Equal("foo\x1b[20xbar", pasteEvent!.Content);
    }

    [Fact]
    public void TryConsume_EscapeConsoleKeyStartsEndSentinel()
    {
        var decoder = new BracketedPasteDecoder();
        decoder.Begin();

        Assert.False(decoder.TryConsume(Key('x'), out var pasteEvent));
        Assert.Null(pasteEvent);
        Assert.False(decoder.TryConsume(new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false), out pasteEvent));
        Assert.Null(pasteEvent);

        var completed = Feed(decoder, "[201~", out pasteEvent);

        Assert.True(completed);
        Assert.NotNull(pasteEvent);
        Assert.Equal("x", pasteEvent!.Content);
    }

    private static bool Feed(BracketedPasteDecoder decoder, string input, out PasteEvent? pasteEvent)
    {
        foreach (var c in input)
        {
            if (decoder.TryConsume(Key(c), out pasteEvent))
                return true;
        }

        pasteEvent = null;
        return false;
    }

    private static ConsoleKeyInfo Key(char c) => new(c, c == '\x1b' ? ConsoleKey.Escape : ConsoleKey.None, false, false, false);
}
