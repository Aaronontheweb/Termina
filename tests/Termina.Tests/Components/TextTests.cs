using Termina.Components;
using Xunit;

namespace Termina.Tests.Components;

public class TextTests
{
    [Fact]
    public void Text_Renders_SimpleContent()
    {
        // Arrange
        var text = new Text("Hello World");
        var context = new RenderContext(80, 24);

        // Act
        var lines = text.Render(context);

        // Assert
        Assert.Single(lines);
        Assert.Equal("Hello World", lines[0]);
    }

    [Fact]
    public void Text_Renders_WithColor()
    {
        // Arrange
        var text = new Text("Success").Color(TextColor.Green);
        var context = new RenderContext(80, 24);

        // Act
        var lines = text.Render(context);

        // Assert
        Assert.Single(lines);
        Assert.Contains("\x1b[32m", lines[0]); // Green color code
        Assert.Contains("Success", lines[0]);
        Assert.Contains("\x1b[0m", lines[0]); // Reset code
    }

    [Fact]
    public void Text_Renders_WithBold()
    {
        // Arrange
        var text = new Text("Important").Bold();
        var context = new RenderContext(80, 24);

        // Act
        var lines = text.Render(context);

        // Assert
        Assert.Single(lines);
        Assert.Contains("\x1b[1m", lines[0]); // Bold code
        Assert.Contains("Important", lines[0]);
    }

    [Fact]
    public void Text_Truncates_WhenTooWide()
    {
        // Arrange
        var text = new Text("This is a very long line that should be truncated");
        var context = new RenderContext(20, 24);

        // Act
        var lines = text.Render(context);

        // Assert
        Assert.Single(lines);
        Assert.True(lines[0].Length <= 20);
        Assert.Contains("...", lines[0]);
    }

    [Fact]
    public void Text_MeasureSize_ReturnsCorrectDimensions()
    {
        // Arrange
        var text = new Text("Hello");

        // Act
        var size = text.MeasureSize(80, 24);

        // Assert
        Assert.Equal(5, size.Width);
        Assert.Equal(1, size.Height);
    }
}
