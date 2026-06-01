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
    public void TryConsume_CompletePaste_ReturnsPasteInput()
    {
        var decoder = new BracketedPasteDecoder();
        decoder.Begin();

        var completed = Feed(decoder, "hello world\x1b[201~", out var pasteInput);

        Assert.True(completed);
        Assert.NotNull(pasteInput);
        Assert.Equal("hello world", pasteInput!.Text);
    }

    [Fact]
    public void TryConsume_PreservesFailedPartialEndSentinel()
    {
        var decoder = new BracketedPasteDecoder();
        decoder.Begin();

        var completed = Feed(decoder, "foo\x1b[20xbar\x1b[201~", out var pasteInput);

        Assert.True(completed);
        Assert.NotNull(pasteInput);
        Assert.Equal("foo\x1b[20xbar", pasteInput!.Text);
    }

    [Fact]
    public void TryConsume_EscapeConsoleKeyStartsEndSentinel()
    {
        var decoder = new BracketedPasteDecoder();
        decoder.Begin();

        Assert.False(decoder.TryConsume(Key('x'), out var pasteInput));
        Assert.Null(pasteInput);
        Assert.False(decoder.TryConsume(new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false), out pasteInput));
        Assert.Null(pasteInput);

        var completed = Feed(decoder, "[201~", out pasteInput);

        Assert.True(completed);
        Assert.NotNull(pasteInput);
        Assert.Equal("x", pasteInput!.Text);
    }

    private static bool Feed(BracketedPasteDecoder decoder, string input, out PasteInput? pasteInput)
    {
        foreach (var c in input)
        {
            if (decoder.TryConsume(Key(c), out pasteInput))
                return true;
        }

        pasteInput = null;
        return false;
    }

    private static ConsoleKeyInfo Key(char c) => new(c, c == '\x1b' ? ConsoleKey.Escape : ConsoleKey.None, false, false, false);
}
