using Microsoft.Extensions.Time.Testing;
using R3;
using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.Layout;

public class GraphNodeTests
{
    [Fact]
    public void WithStyle_ReturnsSelf()
    {
        var graph = new GraphNode();
        var result = graph.WithStyle(GraphStyle.Braille);
        Assert.Same(graph, result);
    }

    [Fact]
    public void WithColor_ReturnsSelf()
    {
        var graph = new GraphNode();
        var result = graph.WithColor(Color.Red);
        Assert.Same(graph, result);
    }

    [Fact]
    public void WithGradient_ReturnsSelf()
    {
        var graph = new GraphNode();
        var gradient = Gradient.Create(Color.Red, Color.Green);
        var result = graph.WithGradient(gradient);
        Assert.Same(graph, result);
    }

    [Fact]
    public void WithRange_ReturnsSelf()
    {
        var graph = new GraphNode();
        var result = graph.WithRange(0, 200);
        Assert.Same(graph, result);
    }

    [Fact]
    public void Render_EmptyBounds_DoesNotThrow()
    {
        var graph = new GraphNode();
        var ctx = new TestRenderContext(0, 0);
        graph.Render(ctx, new Rect(0, 0, 0, 0));
        // No exception means success
    }

    [Fact]
    public void Render_WithData_WritesCells()
    {
        var graph = new GraphNode()
            .WithColor(Color.Cyan)
            .WithRange(0, 100);
        graph.SetData([50, 100]);

        var ctx = new TestRenderContext(2, 4);
        graph.Render(ctx, new Rect(0, 0, 2, 4));

        // With data [50, 100], col 0 = 50% filled (2 rows), col 1 = 100% filled (4 rows)
        // Should have written non-space characters
        Assert.True(ctx.WrittenCells.Count > 0);
    }

    [Fact]
    public void Render_WithGradient_AppliesDifferentColorsPerRow()
    {
        var gradient = Gradient.Create(Color.Red, Color.Green);
        var graph = new GraphNode()
            .WithGradient(gradient)
            .WithRange(0, 100);
        graph.SetData([100]); // fully filled column

        var ctx = new TestRenderContext(1, 4);
        graph.Render(ctx, new Rect(0, 0, 1, 4));

        // All 4 rows should be filled, each with a different gradient sample
        var colors = ctx.ForegroundColors.Select(c => c.Color).Distinct().ToList();
        Assert.True(colors.Count > 1, $"Expected multiple gradient colors, got {colors.Count}");
    }

    [Fact]
    public void OnDeactivate_StopsAnimation()
    {
        var timeProvider = new FakeTimeProvider();
        var graph = new GraphNode(intervalMs: 100, timeProvider: timeProvider);

        Assert.True(graph.IsAnimating);

        graph.OnDeactivate();

        Assert.False(graph.IsAnimating);
    }

    [Fact]
    public void OnActivate_ResumesAnimation()
    {
        var timeProvider = new FakeTimeProvider();
        var graph = new GraphNode(intervalMs: 100, timeProvider: timeProvider);

        graph.OnDeactivate();
        Assert.False(graph.IsAnimating);

        graph.OnActivate();
        Assert.True(graph.IsAnimating);
    }

    [Fact]
    public void Dispose_CompletesInvalidated()
    {
        var timeProvider = new FakeTimeProvider();
        var graph = new GraphNode(intervalMs: 100, timeProvider: timeProvider);

        var completed = false;
        graph.Invalidated.Subscribe(
            onNext: _ => { },
            onErrorResume: _ => { },
            onCompleted: _ => completed = true);

        graph.Dispose();

        Assert.True(completed);
    }

    [Fact]
    public void Render_Braille_WithData_WritesBrailleChars()
    {
        var graph = new GraphNode()
            .WithStyle(GraphStyle.Braille)
            .WithColor(Color.Cyan)
            .WithRange(0, 100);
        graph.SetData([100, 100]);

        var ctx = new TestRenderContext(1, 2);
        graph.Render(ctx, new Rect(0, 0, 1, 2));

        Assert.True(ctx.WrittenCells.Count > 0);
    }

    [Fact]
    public void Render_Outline_WithData_WritesOutlineChars()
    {
        var graph = new GraphNode()
            .WithStyle(GraphStyle.Outline)
            .WithColor(Color.Cyan)
            .WithRange(0, 100);
        graph.SetData([50]);

        var ctx = new TestRenderContext(1, 4);
        graph.Render(ctx, new Rect(0, 0, 1, 4));

        Assert.True(ctx.WrittenCells.Count > 0);
    }

    [Fact]
    public void Render_Ascii_WithData_WritesAsciiChars()
    {
        var graph = new GraphNode()
            .WithStyle(GraphStyle.Ascii)
            .WithColor(Color.Cyan)
            .WithRange(0, 100);
        graph.SetData([100]);

        var ctx = new TestRenderContext(1, 2);
        graph.Render(ctx, new Rect(0, 0, 1, 2));

        Assert.True(ctx.WrittenCells.Count > 0);
    }

    [Fact]
    public void SetData_FiresInvalidated()
    {
        var graph = new GraphNode(intervalMs: 0);
        var invalidated = false;
        graph.Invalidated.Subscribe(_ => invalidated = true);

        graph.SetData([1, 2, 3]);

        Assert.True(invalidated);
    }

    [Fact]
    public void IntervalZero_DisablesInternalTimer()
    {
        var time = new FakeTimeProvider();
        var graph = new GraphNode(intervalMs: 0, timeProvider: time);
        var fired = 0;
        graph.Invalidated.Subscribe(_ => fired++);

        time.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(0, fired);
        Assert.False(graph.IsAnimating);
    }

    /// <summary>
    /// Minimal render context for testing that captures written content and colors.
    /// </summary>
    private sealed class TestRenderContext : IRenderContext
    {
        private readonly int _width;
        private readonly int _height;
        private Color? _currentForeground;

        public List<(int X, int Y, string Text)> WrittenCells { get; } = [];
        public List<(int X, int Y, Color Color)> ForegroundColors { get; } = [];

        public TestRenderContext(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public int Width => _width;
        public int Height => _height;

        public void WriteAt(int x, int y, char c)
        {
            WrittenCells.Add((x, y, c.ToString()));
            if (_currentForeground.HasValue)
            {
                ForegroundColors.Add((x, y, _currentForeground.Value));
            }
        }

        public void WriteAt(int x, int y, string text)
        {
            WrittenCells.Add((x, y, text));
            if (_currentForeground.HasValue)
            {
                ForegroundColors.Add((x, y, _currentForeground.Value));
            }
        }

        public void SetForeground(Color color)
        {
            _currentForeground = color;
        }

        public void SetBackground(Color color) { }

        public void ResetColors()
        {
            _currentForeground = null;
        }

        public void SetDecoration(TextDecoration decoration) { }
        public void ApplyStyle(TextStyle style) { }
        public void Fill(int x, int y, int width, int height, char c = ' ') { }
        public void Clear() { }

        public IRenderContext CreateSubContext(Rect bounds)
        {
            return new SubContext(this, bounds);
        }

        private sealed class SubContext : IRenderContext
        {
            private readonly TestRenderContext _parent;
            private readonly Rect _bounds;

            public SubContext(TestRenderContext parent, Rect bounds)
            {
                _parent = parent;
                _bounds = bounds;
            }

            public int Width => _bounds.Width;
            public int Height => _bounds.Height;

            public void WriteAt(int x, int y, char c) => _parent.WriteAt(_bounds.X + x, _bounds.Y + y, c);
            public void WriteAt(int x, int y, string text) => _parent.WriteAt(_bounds.X + x, _bounds.Y + y, text);
            public void SetForeground(Color color) => _parent.SetForeground(color);
            public void SetBackground(Color color) => _parent.SetBackground(color);
            public void ResetColors() => _parent.ResetColors();
            public void SetDecoration(TextDecoration decoration) { }
            public void ApplyStyle(TextStyle style) { }
            public void Fill(int x, int y, int width, int height, char c = ' ') { }
            public void Clear() { }

            public IRenderContext CreateSubContext(Rect bounds) =>
                new SubContext(_parent, new Rect(_bounds.X + bounds.X, _bounds.Y + bounds.Y, bounds.Width, bounds.Height));
        }
    }
}
