using Xunit;

namespace Termina.Tests;

public class AnsiHelperTests
{
    [Fact]
    public void StripAnsiCodes_RemovesAllAnsiSequences()
    {
        // Arrange
        var input = "\x1b[32mGreen\x1b[0m Text \x1b[1mBold\x1b[0m";

        // Act
        var result = AnsiHelper.StripAnsiCodes(input);

        // Assert
        Assert.Equal("Green Text Bold", result);
    }

    [Fact]
    public void StripAnsiCodes_HandlesTextWithoutAnsi()
    {
        // Arrange
        var input = "Plain text";

        // Act
        var result = AnsiHelper.StripAnsiCodes(input);

        // Assert
        Assert.Equal("Plain text", result);
    }

    [Fact]
    public void GetVisualWidth_ReturnsCorrectWidth()
    {
        // Arrange - "Status: Active" with cyan color codes
        var input = "\x1b[36mStatus: Active\x1b[0m";

        // Act
        var width = AnsiHelper.GetVisualWidth(input);

        // Assert
        Assert.Equal(14, width); // "Status: Active" is 14 characters
    }

    [Fact]
    public void GetVisualWidth_HandlesPlainText()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var width = AnsiHelper.GetVisualWidth(input);

        // Assert
        Assert.Equal(11, width);
    }

    [Fact]
    public void PadRightVisual_AddsCorrectPadding()
    {
        // Arrange
        var input = "\x1b[32mTest\x1b[0m"; // "Test" in green

        // Act
        var result = AnsiHelper.PadRightVisual(input, 10);

        // Assert
        Assert.Equal(10, AnsiHelper.GetVisualWidth(result));
        Assert.Contains("\x1b[32m", result); // ANSI codes preserved
        Assert.EndsWith("      ", result); // 6 spaces added (10 - 4)
    }

    [Fact]
    public void PadRightVisual_DoesNotPadIfAlreadyWide()
    {
        // Arrange
        var input = "\x1b[32mLong Text\x1b[0m";

        // Act
        var result = AnsiHelper.PadRightVisual(input, 5);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void TruncateVisual_TruncatesLongText()
    {
        // Arrange
        var input = "\x1b[32mThis is a very long text\x1b[0m";

        // Act
        var result = AnsiHelper.TruncateVisual(input, 10);

        // Assert
        Assert.Equal(10, AnsiHelper.GetVisualWidth(result));
        Assert.EndsWith("...", result);
    }

    [Fact]
    public void TruncateVisual_DoesNotTruncateShortText()
    {
        // Arrange
        var input = "\x1b[32mShort\x1b[0m";

        // Act
        var result = AnsiHelper.TruncateVisual(input, 10);

        // Assert - Original text preserved when no truncation needed
        Assert.Equal(input, result);
        Assert.Equal(5, AnsiHelper.GetVisualWidth(result));
    }

    [Fact]
    public void TruncateVisual_HandlesVerySmallWidth()
    {
        // Arrange
        var input = "Test";

        // Act
        var result = AnsiHelper.TruncateVisual(input, 2);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Te", result);
    }
}
