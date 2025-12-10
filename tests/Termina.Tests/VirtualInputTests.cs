using Termina;
using Termina.Components;
using Xunit;

namespace Termina.Tests;

public class VirtualInputTests
{
    [Fact]
    public void VirtualInputSource_QueuesAndReadsKeys()
    {
        // Arrange
        var input = new VirtualInputSource();
        input.QueueKey(ConsoleKey.A, 'A');
        input.QueueKey(ConsoleKey.B, 'B');

        // Act
        var key1 = input.ReadKey();
        var key2 = input.ReadKey();

        // Assert
        Assert.Equal(ConsoleKey.A, key1.Key);
        Assert.Equal('A', key1.KeyChar);
        Assert.Equal(ConsoleKey.B, key2.Key);
        Assert.Equal('B', key2.KeyChar);
    }

    [Fact]
    public void VirtualInputSource_QueueString_QueuesAllCharacters()
    {
        // Arrange
        var input = new VirtualInputSource();

        // Act
        input.QueueString("Test");

        // Assert
        Assert.True(input.IsKeyAvailable);
        Assert.Equal('T', input.ReadKey().KeyChar);
        Assert.Equal('e', input.ReadKey().KeyChar);
        Assert.Equal('s', input.ReadKey().KeyChar);
        Assert.Equal('t', input.ReadKey().KeyChar);
        Assert.False(input.IsKeyAvailable);
    }

    [Fact]
    public void VirtualInputSource_ThrowsWhenEmpty()
    {
        // Arrange
        var input = new VirtualInputSource();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => input.ReadKey());
    }
}

public class IntegrationTests
{
    [Fact]
    public void TextInput_WithVirtualInput_ProcessesStringInput()
    {
        // Arrange
        var virtualInput = new VirtualInputSource();
        virtualInput.QueueString("Hello");
        virtualInput.QueueKey(ConsoleKey.Enter);

        var input = new TextInput();
        string? submittedValue = null;
        input.OnSubmit(value => submittedValue = value);

        // Act - Simulate input processing loop
        while (virtualInput.IsKeyAvailable)
        {
            var key = virtualInput.ReadKey();
            input.OnKeyPress(key);
        }

        // Assert
        Assert.Equal("Hello", submittedValue);
    }

    [Fact]
    public void ComplexUI_RendersCorrectly()
    {
        // Arrange
        var ui = UI.Panel("Registration")
            .Add(UI.Rows()
                .Add(UI.Text("Name:"))
                .Add(UI.TextInput().Placeholder("John Doe"))
                .Add(UI.Text("Status: Ready").Color(TextColor.Green)));

        var context = new RenderContext(60, 20);

        // Act
        var lines = ui.Render(context);

        // Assert
        Assert.True(lines.Length > 0);
        Assert.Contains(lines, l => l.Contains("Registration"));
        Assert.Contains(lines, l => l.Contains("Name:"));
        Assert.Contains(lines, l => l.Contains("John Doe"));
        Assert.Contains(lines, l => l.Contains("Status: Ready"));
    }
}
