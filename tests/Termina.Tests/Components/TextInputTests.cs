using Termina.Components;
using Xunit;

namespace Termina.Tests.Components;

public class TextInputTests
{
    [Fact]
    public void TextInput_CanFocus_ReturnsTrue()
    {
        // Arrange
        var input = new TextInput();

        // Act & Assert
        Assert.True(input.CanFocus);
    }

    [Fact]
    public void TextInput_Renders_Placeholder_WhenEmpty()
    {
        // Arrange
        var input = new TextInput().Placeholder("Enter name");
        var context = new RenderContext(80, 24);

        // Act
        var lines = input.Render(context);

        // Assert
        Assert.Single(lines);
        Assert.Contains("Enter name", lines[0]);
        Assert.Contains("\x1b[2m", lines[0]); // Dim formatting
    }

    [Fact]
    public void TextInput_Renders_FocusIndicator_WhenFocused()
    {
        // Arrange
        var input = new TextInput();
        var context = new RenderContext(80, 24, FocusedComponent: input);

        // Act
        var lines = input.Render(context);

        // Assert
        Assert.Single(lines);
        Assert.Contains("_", lines[0]); // Cursor
        Assert.Contains("\x1b[32m", lines[0]); // Green > indicator
    }

    [Fact]
    public void TextInput_HandlesCharacterInput()
    {
        // Arrange
        var input = new TextInput();
        var key = new ConsoleKeyInfo('H', ConsoleKey.H, false, false, false);

        // Act
        input.OnKeyPress(key);

        // Assert
        Assert.Equal("H", input.GetValue());
    }

    [Fact]
    public void TextInput_HandlesBackspace()
    {
        // Arrange
        var input = new TextInput();
        input.SetValue("Hello");
        var backspace = new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false);

        // Act
        input.OnKeyPress(backspace);

        // Assert
        Assert.Equal("Hell", input.GetValue());
    }

    [Fact]
    public void TextInput_HandlesEscape()
    {
        // Arrange
        var input = new TextInput();
        input.SetValue("Hello");
        var escape = new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false);

        // Act
        input.OnKeyPress(escape);

        // Assert
        Assert.Equal("", input.GetValue());
    }

    [Fact]
    public void TextInput_CallsOnSubmit_WhenEnterPressed()
    {
        // Arrange
        string? submittedValue = null;
        var input = new TextInput()
            .OnSubmit(value => submittedValue = value);
        input.SetValue("Test");
        var enter = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);

        // Act
        input.OnKeyPress(enter);

        // Assert
        Assert.Equal("Test", submittedValue);
    }

    [Fact]
    public void TextInput_CallsOnChange_WhenTextChanges()
    {
        // Arrange
        string? changedValue = null;
        var input = new TextInput()
            .OnChange(value => changedValue = value);
        var key = new ConsoleKeyInfo('X', ConsoleKey.X, false, false, false);

        // Act
        input.OnKeyPress(key);

        // Assert
        Assert.Equal("X", changedValue);
    }
}
