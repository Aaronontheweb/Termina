using Termina.Components;
using Xunit;

namespace Termina.Tests.Components;

public class RowsTests
{
    [Fact]
    public void Rows_Renders_ChildrenVertically()
    {
        // Arrange
        var rows = new Rows()
            .Add(new Text("Line 1"))
            .Add(new Text("Line 2"))
            .Add(new Text("Line 3"));
        var context = new RenderContext(80, 24);

        // Act
        var lines = rows.Render(context);

        // Assert
        Assert.Equal(3, lines.Length);
        Assert.Contains("Line 1", lines[0]);
        Assert.Contains("Line 2", lines[1]);
        Assert.Contains("Line 3", lines[2]);
    }

    [Fact]
    public void Rows_MeasureSize_SumsChildHeights()
    {
        // Arrange
        var rows = new Rows()
            .Add(new Text("Line 1"))
            .Add(new Text("Line 2"));

        // Act
        var size = rows.MeasureSize(80, 24);

        // Assert
        Assert.Equal(2, size.Height);
    }
}
