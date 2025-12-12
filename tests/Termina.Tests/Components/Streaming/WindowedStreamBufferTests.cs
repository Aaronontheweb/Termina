using Termina.Components.Streaming;

namespace Termina.Tests.Components.Streaming;

public class WindowedStreamBufferTests
{
    [Fact]
    public void Mode_ReturnsWindowed()
    {
        var buffer = new WindowedStreamBuffer();

        Assert.Equal(StreamMode.Windowed, buffer.Mode);
    }

    [Fact]
    public void NewBuffer_HasNoContent()
    {
        var buffer = new WindowedStreamBuffer();

        Assert.False(buffer.HasContent);
        Assert.Equal(0, buffer.LineCount);
        Assert.Equal(0, buffer.CharacterCount);
    }

    [Fact]
    public void DefaultWindowSize_IsThree()
    {
        var buffer = new WindowedStreamBuffer();

        Assert.Equal(3, buffer.WindowSize);
    }

    [Fact]
    public void CustomWindowSize_IsRespected()
    {
        var buffer = new WindowedStreamBuffer(windowSize: 5);

        Assert.Equal(5, buffer.WindowSize);
    }

    [Fact]
    public void Constructor_InvalidWindowSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WindowedStreamBuffer(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WindowedStreamBuffer(-1));
    }

    [Fact]
    public void Append_SingleLine_AddsContent()
    {
        var buffer = new WindowedStreamBuffer();

        buffer.Append("Hello world");

        Assert.True(buffer.HasContent);
        Assert.Equal(1, buffer.LineCount);
    }

    [Fact]
    public void Append_WithNewline_CreatesTwoLines()
    {
        var buffer = new WindowedStreamBuffer();

        buffer.Append("Hello\nworld");

        Assert.Equal(2, buffer.LineCount);
        var lines = buffer.GetAllLines();
        Assert.Equal("Hello", lines[0]);
        Assert.Equal("world", lines[1]);
    }

    [Fact]
    public void AppendLine_AddsLineWithNewline()
    {
        var buffer = new WindowedStreamBuffer();

        buffer.AppendLine("Hello");
        buffer.AppendLine("World");

        Assert.Equal(2, buffer.LineCount);
    }

    [Fact]
    public void Append_IgnoresCarriageReturns()
    {
        var buffer = new WindowedStreamBuffer();

        buffer.Append("Hello\r\nWorld");

        var lines = buffer.GetAllLines();
        Assert.Equal("Hello", lines[0]);
        Assert.Equal("World", lines[1]);
    }

    [Fact]
    public void Append_ExceedsWindowSize_DiscardsOldest()
    {
        var buffer = new WindowedStreamBuffer(windowSize: 3);

        buffer.AppendLine("Line 1");
        buffer.AppendLine("Line 2");
        buffer.AppendLine("Line 3");
        buffer.AppendLine("Line 4"); // This should discard "Line 1"

        Assert.Equal(3, buffer.LineCount);
        var lines = buffer.GetAllLines();
        Assert.Equal("Line 2", lines[0]);
        Assert.Equal("Line 3", lines[1]);
        Assert.Equal("Line 4", lines[2]);
    }

    [Fact]
    public void Append_MultipleExceedsWindowSize_DiscardsMultiple()
    {
        var buffer = new WindowedStreamBuffer(windowSize: 2);

        buffer.AppendLine("Line 1");
        buffer.AppendLine("Line 2");
        buffer.AppendLine("Line 3");
        buffer.AppendLine("Line 4");
        buffer.AppendLine("Line 5");

        var lines = buffer.GetAllLines();
        Assert.Equal(2, lines.Count);
        Assert.Equal("Line 4", lines[0]);
        Assert.Equal("Line 5", lines[1]);
    }

    [Fact]
    public void DiscardedLineCount_TracksDiscardedLines()
    {
        var buffer = new WindowedStreamBuffer(windowSize: 2);

        buffer.AppendLine("Line 1");
        buffer.AppendLine("Line 2");
        Assert.Equal(0, buffer.DiscardedLineCount);

        buffer.AppendLine("Line 3"); // Discards Line 1
        Assert.Equal(1, buffer.DiscardedLineCount);

        buffer.AppendLine("Line 4"); // Discards Line 2
        Assert.Equal(2, buffer.DiscardedLineCount);
    }

    [Fact]
    public void Clear_RemovesAllContent()
    {
        var buffer = new WindowedStreamBuffer();
        buffer.AppendLine("Line 1");
        buffer.AppendLine("Line 2");

        buffer.Clear();

        Assert.False(buffer.HasContent);
        Assert.Equal(0, buffer.LineCount);
    }

    [Fact]
    public void Clear_DoesNotResetDiscardedCount()
    {
        var buffer = new WindowedStreamBuffer(windowSize: 1);
        buffer.AppendLine("Line 1");
        buffer.AppendLine("Line 2"); // Discards Line 1

        buffer.Clear();

        Assert.Equal(1, buffer.DiscardedLineCount); // Still tracked
    }

    [Fact]
    public void ResetDiscardedCount_ResetsCounter()
    {
        var buffer = new WindowedStreamBuffer(windowSize: 1);
        buffer.AppendLine("Line 1");
        buffer.AppendLine("Line 2");

        buffer.ResetDiscardedCount();

        Assert.Equal(0, buffer.DiscardedLineCount);
    }

    [Fact]
    public void GetVisibleLines_ReturnsAllLines_WhenBelowViewport()
    {
        var buffer = new WindowedStreamBuffer(windowSize: 5);
        buffer.AppendLine("Line 1");
        buffer.AppendLine("Line 2");

        var visible = buffer.GetVisibleLines(viewportHeight: 10, viewportWidth: 80);

        Assert.Equal(2, visible.Count);
    }

    [Fact]
    public void GetVisibleLines_ReturnsLastNLines_WhenExceedsViewport()
    {
        var buffer = new WindowedStreamBuffer(windowSize: 10);
        for (int i = 1; i <= 10; i++)
            buffer.AppendLine($"Line {i}");

        var visible = buffer.GetVisibleLines(viewportHeight: 3, viewportWidth: 80);

        Assert.Equal(3, visible.Count);
        Assert.Equal("Line 8", visible[0]);
        Assert.Equal("Line 9", visible[1]);
        Assert.Equal("Line 10", visible[2]);
    }

    [Fact]
    public void GetVisibleLines_ZeroViewport_ReturnsEmpty()
    {
        var buffer = new WindowedStreamBuffer();
        buffer.AppendLine("Hello");

        var visible = buffer.GetVisibleLines(viewportHeight: 0, viewportWidth: 80);

        Assert.Empty(visible);
    }

    [Fact]
    public void GetVisibleLines_WrapsLongLines()
    {
        var buffer = new WindowedStreamBuffer(windowSize: 5);
        buffer.AppendLine("Hello world");

        var visible = buffer.GetVisibleLines(viewportHeight: 5, viewportWidth: 8);

        Assert.Equal(2, visible.Count);
        Assert.Equal("Hello", visible[0]);
        Assert.Equal("world", visible[1]);
    }

    [Fact]
    public void Append_EmptyString_DoesNothing()
    {
        var buffer = new WindowedStreamBuffer();

        buffer.Append("");
        buffer.Append(null!);

        Assert.False(buffer.HasContent);
    }

    [Fact]
    public void GetAllLines_ReturnsAllContent()
    {
        var buffer = new WindowedStreamBuffer(windowSize: 5);
        buffer.AppendLine("Line 1");
        buffer.AppendLine("Line 2");
        buffer.Append("Partial");

        var lines = buffer.GetAllLines();

        Assert.Equal(3, lines.Count);
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Line 2", lines[1]);
        Assert.Equal("Partial", lines[2]);
    }

    [Fact]
    public void CharacterCount_IncludesNewlines()
    {
        var buffer = new WindowedStreamBuffer();
        buffer.AppendLine("Hi"); // "Hi" + newline = 3 chars
        buffer.Append("AB"); // 2 chars

        // "Hi\n" = 3, "AB" = 2, but stored as "Hi" (2) + newline marker (1) + "AB" (2)
        Assert.Equal(5, buffer.CharacterCount);
    }

    [Fact]
    public void PartialLine_IncludedInLineCount()
    {
        var buffer = new WindowedStreamBuffer();
        buffer.Append("Hello"); // Partial line, no newline

        Assert.Equal(1, buffer.LineCount);
        Assert.Equal("Hello", buffer.GetAllLines()[0]);
    }

    [Fact]
    public void WindowSize_AppliesOnlyToCompletedLines()
    {
        var buffer = new WindowedStreamBuffer(windowSize: 2);

        buffer.AppendLine("Line 1");
        buffer.AppendLine("Line 2");
        buffer.Append("Partial"); // This is partial, doesn't count against window

        var lines = buffer.GetAllLines();
        Assert.Equal(3, lines.Count); // 2 complete + 1 partial
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Line 2", lines[1]);
        Assert.Equal("Partial", lines[2]);
    }

    [Fact]
    public void RollingWindow_TickerTapeEffect()
    {
        // Simulates a "thinking" indicator with rolling content
        var buffer = new WindowedStreamBuffer(windowSize: 2);

        buffer.AppendLine("Thinking...");
        var lines1 = buffer.GetAllLines();
        Assert.Single(lines1);

        buffer.AppendLine("Processing step 1...");
        var lines2 = buffer.GetAllLines();
        Assert.Equal(2, lines2.Count);

        buffer.AppendLine("Processing step 2...");
        var lines3 = buffer.GetAllLines();
        Assert.Equal(2, lines3.Count);
        Assert.Equal("Processing step 1...", lines3[0]);
        Assert.Equal("Processing step 2...", lines3[1]);
    }
}
