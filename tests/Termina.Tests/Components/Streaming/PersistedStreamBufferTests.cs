using Termina.Components.Streaming;

namespace Termina.Tests.Components.Streaming;

public class PersistedStreamBufferTests
{
    [Fact]
    public void Mode_ReturnsPersisted()
    {
        var buffer = new PersistedStreamBuffer();

        Assert.Equal(StreamMode.Persisted, buffer.Mode);
    }

    [Fact]
    public void NewBuffer_HasNoContent()
    {
        var buffer = new PersistedStreamBuffer();

        Assert.False(buffer.HasContent);
        Assert.Equal(0, buffer.LineCount);
        Assert.Equal(0, buffer.CharacterCount);
    }

    [Fact]
    public void Append_SingleLine_AddsContent()
    {
        var buffer = new PersistedStreamBuffer();

        buffer.Append("Hello world");

        Assert.True(buffer.HasContent);
        Assert.Equal(1, buffer.LineCount); // Partial line counts as 1
        Assert.Equal(11, buffer.CharacterCount);
    }

    [Fact]
    public void Append_WithNewline_CreatesTwoLines()
    {
        var buffer = new PersistedStreamBuffer();

        buffer.Append("Hello\nworld");

        Assert.Equal(2, buffer.LineCount);
        var lines = buffer.GetAllLines();
        Assert.Equal("Hello", lines[0]);
        Assert.Equal("world", lines[1]);
    }

    [Fact]
    public void AppendLine_AddsLineWithNewline()
    {
        var buffer = new PersistedStreamBuffer();

        buffer.AppendLine("Hello");
        buffer.AppendLine("World");

        Assert.Equal(2, buffer.LineCount);
        var lines = buffer.GetAllLines();
        Assert.Equal("Hello", lines[0]);
        Assert.Equal("World", lines[1]);
    }

    [Fact]
    public void Append_IgnoresCarriageReturns()
    {
        var buffer = new PersistedStreamBuffer();

        buffer.Append("Hello\r\nWorld");

        var lines = buffer.GetAllLines();
        Assert.Equal("Hello", lines[0]);
        Assert.Equal("World", lines[1]);
    }

    [Fact]
    public void Clear_RemovesAllContent()
    {
        var buffer = new PersistedStreamBuffer();
        buffer.Append("Hello\nWorld");

        buffer.Clear();

        Assert.False(buffer.HasContent);
        Assert.Equal(0, buffer.LineCount);
    }

    [Fact]
    public void GetVisibleLines_ReturnsLastNLines()
    {
        var buffer = new PersistedStreamBuffer();
        buffer.AppendLine("Line 1");
        buffer.AppendLine("Line 2");
        buffer.AppendLine("Line 3");
        buffer.AppendLine("Line 4");
        buffer.AppendLine("Line 5");

        var visible = buffer.GetVisibleLines(viewportHeight: 3, viewportWidth: 80);

        Assert.Equal(3, visible.Count);
        Assert.Equal("Line 3", visible[0]);
        Assert.Equal("Line 4", visible[1]);
        Assert.Equal("Line 5", visible[2]);
    }

    [Fact]
    public void GetVisibleLines_FewerLinesThanViewport_ReturnsAll()
    {
        var buffer = new PersistedStreamBuffer();
        buffer.AppendLine("Line 1");
        buffer.AppendLine("Line 2");

        var visible = buffer.GetVisibleLines(viewportHeight: 5, viewportWidth: 80);

        Assert.Equal(2, visible.Count);
    }

    [Fact]
    public void GetVisibleLines_ZeroViewport_ReturnsEmpty()
    {
        var buffer = new PersistedStreamBuffer();
        buffer.AppendLine("Hello");

        var visible = buffer.GetVisibleLines(viewportHeight: 0, viewportWidth: 80);

        Assert.Empty(visible);
    }

    [Fact]
    public void ScrollUp_MovesViewportUp()
    {
        var buffer = new PersistedStreamBuffer();
        for (int i = 1; i <= 10; i++)
            buffer.AppendLine($"Line {i}");

        buffer.ScrollUp(3, viewportWidth: 80);

        var visible = buffer.GetVisibleLines(viewportHeight: 3, viewportWidth: 80);
        // Scrolled up 3 from bottom, so we should see lines 5, 6, 7
        Assert.Equal("Line 5", visible[0]);
        Assert.Equal("Line 6", visible[1]);
        Assert.Equal("Line 7", visible[2]);
    }

    [Fact]
    public void ScrollDown_MovesViewportDown()
    {
        var buffer = new PersistedStreamBuffer();
        for (int i = 1; i <= 10; i++)
            buffer.AppendLine($"Line {i}");

        buffer.ScrollUp(5, viewportWidth: 80);
        buffer.ScrollDown(2);

        // Scrolled up 5 then down 2, net scroll = 3 up
        var visible = buffer.GetVisibleLines(viewportHeight: 3, viewportWidth: 80);
        Assert.Equal("Line 5", visible[0]);
    }

    [Fact]
    public void ScrollToBottom_ResetsScrollOffset()
    {
        var buffer = new PersistedStreamBuffer();
        for (int i = 1; i <= 10; i++)
            buffer.AppendLine($"Line {i}");

        buffer.ScrollUp(5, viewportWidth: 80);
        buffer.ScrollToBottom();

        Assert.False(buffer.IsScrolledUp);
        Assert.Equal(0, buffer.ScrollOffset);
    }

    [Fact]
    public void ScrollToTop_ScrollsToOldestContent()
    {
        var buffer = new PersistedStreamBuffer();
        for (int i = 1; i <= 10; i++)
            buffer.AppendLine($"Line {i}");

        buffer.ScrollToTop(viewportWidth: 80);

        var visible = buffer.GetVisibleLines(viewportHeight: 3, viewportWidth: 80);
        Assert.Equal("Line 1", visible[0]);
    }

    [Fact]
    public void IsScrolledUp_FalseWhenAtBottom()
    {
        var buffer = new PersistedStreamBuffer();
        buffer.AppendLine("Hello");

        Assert.False(buffer.IsScrolledUp);
    }

    [Fact]
    public void IsScrolledUp_TrueWhenScrolledUp()
    {
        var buffer = new PersistedStreamBuffer();
        for (int i = 1; i <= 10; i++)
            buffer.AppendLine($"Line {i}");

        buffer.ScrollUp(3, viewportWidth: 80);

        Assert.True(buffer.IsScrolledUp);
    }

    [Fact]
    public void AutoScroll_WhenEnabled_KeepsViewAtBottom()
    {
        var buffer = new PersistedStreamBuffer { AutoScroll = true };
        buffer.AppendLine("Line 1");
        buffer.AppendLine("Line 2");

        // New content should keep us at the bottom
        buffer.AppendLine("Line 3");

        Assert.Equal(0, buffer.ScrollOffset);
    }

    [Fact]
    public void AutoScroll_WhenScrolledUp_DoesNotJumpToBottom()
    {
        var buffer = new PersistedStreamBuffer { AutoScroll = true };
        for (int i = 1; i <= 10; i++)
            buffer.AppendLine($"Line {i}");

        buffer.ScrollUp(3, viewportWidth: 80);
        var offsetAfterScroll = buffer.ScrollOffset;

        // Add new content - should NOT change scroll position
        buffer.AppendLine("Line 11");

        Assert.Equal(offsetAfterScroll, buffer.ScrollOffset);
    }

    [Fact]
    public void ScrollDown_ToBottom_ReenablesAutoScroll()
    {
        var buffer = new PersistedStreamBuffer { AutoScroll = true };
        for (int i = 1; i <= 10; i++)
            buffer.AppendLine($"Line {i}");

        buffer.ScrollUp(3, viewportWidth: 80);
        Assert.True(buffer.IsScrolledUp);

        // Scroll back down to bottom
        buffer.ScrollDown(10);

        Assert.False(buffer.IsScrolledUp);
    }

    [Fact]
    public void GetWrappedLineCount_ReturnsCorrectCount()
    {
        var buffer = new PersistedStreamBuffer();
        buffer.AppendLine("Hello world this is a test");

        // With width 10, "Hello world this is a test" wraps to multiple lines
        var count = buffer.GetWrappedLineCount(viewportWidth: 10);

        Assert.True(count > 1);
    }

    [Fact]
    public void GetVisibleLines_WrapsLongLines()
    {
        var buffer = new PersistedStreamBuffer();
        buffer.AppendLine("Hello world");

        var visible = buffer.GetVisibleLines(viewportHeight: 5, viewportWidth: 8);

        Assert.Equal(2, visible.Count);
        Assert.Equal("Hello", visible[0]);
        Assert.Equal("world", visible[1]);
    }

    [Fact]
    public void Clear_ResetsScrollState()
    {
        var buffer = new PersistedStreamBuffer();
        for (int i = 1; i <= 10; i++)
            buffer.AppendLine($"Line {i}");

        buffer.ScrollUp(5, viewportWidth: 80);
        buffer.Clear();

        Assert.False(buffer.IsScrolledUp);
        Assert.Equal(0, buffer.ScrollOffset);
    }

    [Fact]
    public void Append_EmptyString_DoesNothing()
    {
        var buffer = new PersistedStreamBuffer();

        buffer.Append("");
        buffer.Append(null!);

        Assert.False(buffer.HasContent);
    }

    [Fact]
    public void GetAllLines_ReturnsAllContent()
    {
        var buffer = new PersistedStreamBuffer();
        buffer.AppendLine("Line 1");
        buffer.AppendLine("Line 2");
        buffer.Append("Partial");

        var lines = buffer.GetAllLines();

        Assert.Equal(3, lines.Count);
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Line 2", lines[1]);
        Assert.Equal("Partial", lines[2]);
    }
}
