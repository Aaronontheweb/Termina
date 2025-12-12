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
    public void Create_ReturnsPersistedComponent()
    {
        var component = StreamingText.Create();

        Assert.IsType<PersistedStreamBuffer>(component.Buffer);
    }

    [Fact]
    public void CreateWindowed_ReturnsWindowedComponent()
    {
        var component = StreamingText.CreateWindowed(windowSize: 5);

        Assert.IsType<WindowedStreamBuffer>(component.Buffer);
    }

    [Fact]
    public void MaxWidth_DefaultValue()
    {
        var component = new StreamingText();

        Assert.Equal(80, component.MaxWidth);
    }

    [Fact]
    public void MaxWidth_SetValue()
    {
        var component = new StreamingText();

        component.MaxWidth = 120;

        Assert.Equal(120, component.MaxWidth);
    }

    [Fact]
    public void MaxWidth_ZeroDisablesWrapping()
    {
        var component = new StreamingText();

        component.MaxWidth = 0;

        Assert.Equal(0, component.MaxWidth);
    }

    [Fact]
    public void MaxWidth_NegativeBecomesZero()
    {
        var component = new StreamingText();

        component.MaxWidth = -5;

        Assert.Equal(0, component.MaxWidth);
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
        component.MaxWidth = 20;
        component.AppendLine("This is some text that would wrap");

        // The prefix takes 4 chars, so effective width is 16
        // Word wrapping should account for this
        var result = component.Render();
        Assert.NotNull(result);
    }

    [Fact]
    public void Render_RendersAllContent()
    {
        var component = new StreamingText();

        // Add many lines
        for (int i = 0; i < 100; i++)
        {
            component.AppendLine($"Line {i}");
        }

        // Render should produce all lines (no viewport truncation)
        var result = component.Render();
        Assert.NotNull(result);

        // Verify buffer has all lines
        Assert.Equal(100, component.Buffer.LineCount);
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

    [Fact]
    public void WindowedMode_OnlyRetainsWindowSize()
    {
        var component = StreamingText.CreateWindowed(windowSize: 3);

        // Add more lines than window size
        component.AppendLine("Line 1");
        component.AppendLine("Line 2");
        component.AppendLine("Line 3");
        component.AppendLine("Line 4");
        component.AppendLine("Line 5");

        // Should only have last 3 lines
        var lines = component.Buffer.GetAllLines();
        Assert.Equal(3, lines.Count);
        Assert.Equal("Line 3", lines[0]);
        Assert.Equal("Line 4", lines[1]);
        Assert.Equal("Line 5", lines[2]);
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
