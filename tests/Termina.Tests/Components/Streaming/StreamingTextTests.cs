using Termina.Components.Streaming;

namespace Termina.Tests.Components.Streaming;

public class StreamingTextTests
{
    [Fact]
    public void Constructor_DefaultMode_UsesPersisted()
    {
        var component = new StreamingText();

        Assert.IsType<PersistedStreamBuffer>(component.Buffer);
        Assert.Equal(StreamMode.Persisted, component.Buffer.Mode);
    }

    [Fact]
    public void Constructor_WindowedMode_UsesWindowed()
    {
        var component = new StreamingText(StreamMode.Windowed, windowSize: 5);

        Assert.IsType<WindowedStreamBuffer>(component.Buffer);
        Assert.Equal(StreamMode.Windowed, component.Buffer.Mode);
    }

    [Fact]
    public void Constructor_CustomBuffer_UsesProvidedBuffer()
    {
        var customBuffer = new WindowedStreamBuffer(10);
        var component = new StreamingText(customBuffer);

        Assert.Same(customBuffer, component.Buffer);
    }

    [Fact]
    public void Constructor_NullBuffer_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new StreamingText(null!));
    }

    [Fact]
    public void CreatePersisted_ReturnsPersistedComponent()
    {
        var component = StreamingText.CreatePersisted();

        Assert.IsType<PersistedStreamBuffer>(component.Buffer);
    }

    [Fact]
    public void CreateWindowed_ReturnsWindowedComponent()
    {
        var component = StreamingText.CreateWindowed(windowSize: 5);

        Assert.IsType<WindowedStreamBuffer>(component.Buffer);
    }

    [Fact]
    public void ViewportHeight_DefaultValue()
    {
        var component = new StreamingText();

        Assert.Equal(10, component.ViewportHeight);
    }

    [Fact]
    public void ViewportHeight_SetValue()
    {
        var component = new StreamingText();

        component.ViewportHeight = 20;

        Assert.Equal(20, component.ViewportHeight);
    }

    [Fact]
    public void ViewportHeight_MinimumIsOne()
    {
        var component = new StreamingText();

        component.ViewportHeight = 0;
        Assert.Equal(1, component.ViewportHeight);

        component.ViewportHeight = -5;
        Assert.Equal(1, component.ViewportHeight);
    }

    [Fact]
    public void ViewportWidth_DefaultValue()
    {
        var component = new StreamingText();

        Assert.Equal(80, component.ViewportWidth);
    }

    [Fact]
    public void ViewportWidth_SetValue()
    {
        var component = new StreamingText();

        component.ViewportWidth = 120;

        Assert.Equal(120, component.ViewportWidth);
    }

    [Fact]
    public void ViewportWidth_MinimumIsTen()
    {
        var component = new StreamingText();

        component.ViewportWidth = 5;
        Assert.Equal(10, component.ViewportWidth);

        component.ViewportWidth = -5;
        Assert.Equal(10, component.ViewportWidth);
    }

    [Fact]
    public void Append_DelegatesToBuffer()
    {
        var component = new StreamingText();

        component.Append("Hello");

        Assert.True(component.Buffer.HasContent);
        Assert.Contains("Hello", component.Buffer.GetAllLines());
    }

    [Fact]
    public void AppendLine_DelegatesToBuffer()
    {
        var component = new StreamingText();

        component.AppendLine("Hello");
        component.AppendLine("World");

        var lines = component.Buffer.GetAllLines();
        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public void Clear_DelegatesToBuffer()
    {
        var component = new StreamingText();
        component.Append("Hello");

        component.Clear();

        Assert.False(component.Buffer.HasContent);
    }

    [Fact]
    public void ScrollUp_WorksForPersistedMode()
    {
        var component = new StreamingText(StreamMode.Persisted);
        for (int i = 0; i < 20; i++)
            component.AppendLine($"Line {i}");

        component.ScrollUp(5);

        Assert.True(component.IsScrolledUp);
    }

    [Fact]
    public void ScrollUp_NoOpForWindowedMode()
    {
        var component = new StreamingText(StreamMode.Windowed);
        for (int i = 0; i < 10; i++)
            component.AppendLine($"Line {i}");

        component.ScrollUp(5);

        // Should not throw, just no-op
        Assert.False(component.IsScrolledUp); // Windowed mode never reports scrolled up
    }

    [Fact]
    public void ScrollDown_WorksForPersistedMode()
    {
        var component = new StreamingText(StreamMode.Persisted);
        for (int i = 0; i < 20; i++)
            component.AppendLine($"Line {i}");

        component.ScrollUp(5);
        component.ScrollDown(3);

        // Still scrolled up (5 - 3 = 2)
        Assert.True(component.IsScrolledUp);
    }

    [Fact]
    public void ScrollToBottom_WorksForPersistedMode()
    {
        var component = new StreamingText(StreamMode.Persisted);
        for (int i = 0; i < 20; i++)
            component.AppendLine($"Line {i}");

        component.ScrollUp(5);
        component.ScrollToBottom();

        Assert.False(component.IsScrolledUp);
    }

    [Fact]
    public void IsScrolledUp_FalseForWindowedMode()
    {
        var component = new StreamingText(StreamMode.Windowed);

        Assert.False(component.IsScrolledUp);
    }

    [Fact]
    public void Prefix_CanBeSet()
    {
        var component = new StreamingText();

        component.Prefix = "> ";

        Assert.Equal("> ", component.Prefix);
    }

    [Fact]
    public void Render_EmptyBuffer_ReturnsEmptyText()
    {
        var component = new StreamingText();

        var result = component.Render();

        Assert.NotNull(result);
    }

    [Fact]
    public void Render_WithContent_ReturnsRenderable()
    {
        var component = new StreamingText();
        component.AppendLine("Hello");
        component.AppendLine("World");

        var result = component.Render();

        Assert.NotNull(result);
    }

    [Fact]
    public void Render_WithPrefix_ReducesEffectiveWidth()
    {
        var component = new StreamingText();
        component.Prefix = ">>> ";
        component.ViewportWidth = 20;
        component.AppendLine("This is some text that would wrap");

        // The prefix takes 4 chars, so effective width is 16
        // Word wrapping should account for this
        var result = component.Render();
        Assert.NotNull(result);
    }

    [Fact]
    public void TextStyle_CanBeSet()
    {
        var component = new StreamingText();
        var style = new Spectre.Console.Style(Spectre.Console.Color.Red);

        component.TextStyle = style;

        Assert.Equal(style, component.TextStyle);
    }

    [Fact]
    public void PrefixStyle_CanBeSet()
    {
        var component = new StreamingText();
        var style = new Spectre.Console.Style(Spectre.Console.Color.Blue);

        component.PrefixStyle = style;

        Assert.Equal(style, component.PrefixStyle);
    }

    [Fact]
    public async Task ConsumeAsync_AppendsAllChunks()
    {
        var component = new StreamingText();

        await component.ConsumeAsync(GenerateChunks("Hello", " ", "World"));

        var content = string.Join("", component.Buffer.GetAllLines());
        Assert.Contains("Hello World", content);
    }

    [Fact]
    public async Task ConsumeAsync_StopsOnCancellation()
    {
        var component = new StreamingText();
        var cts = new CancellationTokenSource();

        // Start consuming with a stream that has delays
        var consumeTask = component.ConsumeAsync(GenerateChunksWithDelay(), cts.Token);

        // Let it start consuming
        await Task.Delay(50);

        // Cancel mid-stream
        await cts.CancelAsync();

        // Should throw OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumeTask);
    }

    [Fact]
    public async Task ConsumeLinesAsync_AppendsAllLines()
    {
        var component = new StreamingText();

        await component.ConsumeLinesAsync(GenerateChunks("Line 1", "Line 2", "Line 3"));

        var lines = component.Buffer.GetAllLines();
        Assert.Equal(3, lines.Count);
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Line 2", lines[1]);
        Assert.Equal("Line 3", lines[2]);
    }

    [Fact]
    public async Task ConsumeAsync_EmptyStream_DoesNothing()
    {
        var component = new StreamingText();

        await component.ConsumeAsync(GenerateChunks());

        Assert.False(component.Buffer.HasContent);
    }

    private static async IAsyncEnumerable<string> GenerateChunks(params string[] chunks)
    {
        foreach (var chunk in chunks)
        {
            yield return chunk;
        }
        await Task.CompletedTask; // Satisfy async requirement
    }

    private static async IAsyncEnumerable<string> GenerateChunksWithDelay(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return "First";
        await Task.Delay(200, ct);
        yield return "Second";
        await Task.Delay(200, ct);
        yield return "Third";
    }
}
