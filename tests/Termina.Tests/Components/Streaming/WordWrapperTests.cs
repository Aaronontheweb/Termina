using Termina.Components.Streaming;

namespace Termina.Tests.Components.Streaming;

public class WordWrapperTests
{
    [Fact]
    public void WrapLine_EmptyString_ReturnsSingleEmptyLine()
    {
        var result = WordWrapper.WrapLine("", 80);

        Assert.Single(result);
        Assert.Equal("", result[0]);
    }

    [Fact]
    public void WrapLine_ShortText_ReturnsUnchanged()
    {
        var result = WordWrapper.WrapLine("Hello world", 80);

        Assert.Single(result);
        Assert.Equal("Hello world", result[0]);
    }

    [Fact]
    public void WrapLine_ExactWidth_ReturnsUnchanged()
    {
        var result = WordWrapper.WrapLine("Hello", 5);

        Assert.Single(result);
        Assert.Equal("Hello", result[0]);
    }

    [Fact]
    public void WrapLine_TwoWords_WrapsAtWordBoundary()
    {
        var result = WordWrapper.WrapLine("Hello world", 8);

        Assert.Equal(2, result.Count);
        Assert.Equal("Hello", result[0]);
        Assert.Equal("world", result[1]);
    }

    [Fact]
    public void WrapLine_LongWord_BreaksWord()
    {
        var result = WordWrapper.WrapLine("Supercalifragilisticexpialidocious", 10);

        Assert.True(result.Count > 1);
        Assert.All(result.Take(result.Count - 1), line => Assert.Equal(10, line.Length));
    }

    [Fact]
    public void WrapLine_MultipleWords_WrapsCorrectly()
    {
        var result = WordWrapper.WrapLine("The quick brown fox jumps over the lazy dog", 15);

        Assert.True(result.Count >= 3);
        Assert.All(result, line => Assert.True(line.Length <= 15));
    }

    [Fact]
    public void WrapLine_MultipleSpaces_CollapsedWhenWrapping()
    {
        // WordWrapper collapses multiple spaces when wrapping is needed
        var result = WordWrapper.WrapLine("Hello    world", 8);

        // When wrapping occurs, words are rejoined with single space
        Assert.Equal(2, result.Count);
        Assert.Equal("Hello", result[0]);
        Assert.Equal("world", result[1]);
    }

    [Fact]
    public void WrapLine_ShortTextWithSpaces_PreservedWhenNoWrappingNeeded()
    {
        // When text fits without wrapping, it's returned as-is
        var result = WordWrapper.WrapLine("Hello    world", 80);

        Assert.Single(result);
        Assert.Equal("Hello    world", result[0]); // Preserved because no wrapping needed
    }

    [Fact]
    public void WrapLine_LeadingWhitespace_TrimmedWhenWrapping()
    {
        // Leading whitespace is trimmed when word-splitting occurs
        var result = WordWrapper.WrapLine("   Hello world test", 8);

        // Words extracted without leading spaces
        Assert.True(result.Count >= 2);
        Assert.Equal("Hello", result[0]); // No leading spaces
    }

    [Fact]
    public void WrapLine_ZeroWidth_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WordWrapper.WrapLine("test", 0));
    }

    [Fact]
    public void WrapLine_NegativeWidth_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WordWrapper.WrapLine("test", -5));
    }

    [Fact]
    public void WrapLines_MultipleLines_WrapsEach()
    {
        var lines = new[] { "Hello world", "Goodbye world" };
        var result = WordWrapper.WrapLines(lines, 8);

        Assert.Equal(4, result.Count);
        Assert.Equal("Hello", result[0]);
        Assert.Equal("world", result[1]);
        Assert.Equal("Goodbye", result[2]);
        Assert.Equal("world", result[3]);
    }

    [Fact]
    public void CalculateWrappedLineCount_ReturnsCorrectCount()
    {
        var count = WordWrapper.CalculateWrappedLineCount("Hello world", 8);

        Assert.Equal(2, count);
    }

    [Fact]
    public void CalculateTotalWrappedLineCount_ReturnsCorrectTotal()
    {
        var lines = new[] { "Hello world", "Goodbye world" };
        var count = WordWrapper.CalculateTotalWrappedLineCount(lines, 8);

        Assert.Equal(4, count);
    }
}
