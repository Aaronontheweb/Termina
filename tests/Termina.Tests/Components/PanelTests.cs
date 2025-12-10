using Termina.Components;
using Xunit;

namespace Termina.Tests.Components;

public class PanelTests
{
    [Fact]
    public void Panel_Renders_WithHeader()
    {
        // Arrange
        var panel = new Panel("Test Panel");
        var context = new RenderContext(40, 10);

        // Act
        var lines = panel.Render(context);

        // Assert
        Assert.True(lines.Length >= 2); // At least top and bottom border
        Assert.Contains("Test Panel", lines[0]);
        Assert.Contains("╭", lines[0]); // Rounded border top-left
        Assert.Contains("╯", lines[^1]); // Rounded border bottom-right
    }

    [Fact]
    public void Panel_Renders_WithChildren()
    {
        // Arrange
        var panel = new Panel("Container")
            .Add(new Text("Line 1"))
            .Add(new Text("Line 2"));
        var context = new RenderContext(40, 10);

        // Act
        var lines = panel.Render(context);

        // Assert
        Assert.Contains(lines, l => l.Contains("Line 1"));
        Assert.Contains(lines, l => l.Contains("Line 2"));
    }

    [Fact]
    public void Panel_FluentAPI_ChainsCorrectly()
    {
        // Arrange & Act
        var panel = new Panel("Test")
            .Border(BoxBorder.Double)
            .Add(new Text("Content"));

        // Assert
        Assert.Single(panel.Children);
    }
}
