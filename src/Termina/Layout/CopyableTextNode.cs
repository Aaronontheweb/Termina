using R3;
using Termina.Clipboard;
using Termina.Components.Streaming;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Layout;

/// <summary>
/// A focusable read-only text node that copies its full content when Enter is pressed.
/// </summary>
public sealed class CopyableTextNode : LayoutNode, IFocusable, IInvalidatingNode
{
    private readonly IClipboardService _clipboardService;
    private readonly Subject<Unit> _invalidated = new();
    private bool _hasFocus;
    private bool _disposed;

    public CopyableTextNode(IClipboardService clipboardService, string content)
    {
        _clipboardService = clipboardService;
        Content = content ?? string.Empty;
        WidthConstraint = new SizeConstraint.Fill();
        HeightConstraint = SizeConstraint.AutoSize();
    }

    public string Content { get; private set; }

    public string? Hint { get; private set; } = "Press Enter to copy";

    public Color Foreground { get; private set; } = Color.White;

    public Color FocusedForeground { get; private set; } = Color.Black;

    public Color FocusedBackground { get; private set; } = Color.Cyan;

    public bool CanFocus => true;

    public bool HasFocus => _hasFocus;

    public int FocusPriority => 5;

    public Observable<Unit> Invalidated => _invalidated;

    public CopyableTextNode WithContent(string content)
    {
        Content = content ?? string.Empty;
        Invalidate();
        return this;
    }

    public CopyableTextNode WithHint(string? hint)
    {
        Hint = hint;
        Invalidate();
        return this;
    }

    public CopyableTextNode WithForeground(Color color)
    {
        Foreground = color;
        return this;
    }

    public CopyableTextNode WithFocusedColors(Color foreground, Color background)
    {
        FocusedForeground = foreground;
        FocusedBackground = background;
        return this;
    }

    public void OnFocused()
    {
        _hasFocus = true;
        Invalidate();
    }

    public void OnBlurred()
    {
        _hasFocus = false;
        Invalidate();
    }

    public bool HandleInput(ConsoleKeyInfo key)
    {
        if (key.Key != ConsoleKey.Enter)
            return false;

        _clipboardService.Copy(Content);
        return true;
    }

    public override Size Measure(Size available)
    {
        var lines = GetLines(available.Width > 0 ? available.Width : Content.Length);
        var hintHeight = string.IsNullOrWhiteSpace(Hint) ? 0 : 1;
        return new Size(available.Width, lines.Count + hintHeight);
    }

    public override void Render(IRenderContext context, Rect bounds)
    {
        if (!bounds.HasArea)
            return;

        var nodeContext = context.CreateSubContext(bounds);
        var lines = GetLines(bounds.Width);
        var hasHint = !string.IsNullOrWhiteSpace(Hint);
        var lineCount = hasHint
            ? Math.Min(lines.Count, Math.Max(0, bounds.Height - 1))
            : Math.Min(lines.Count, bounds.Height);

        if (_hasFocus)
        {
            nodeContext.SetForeground(FocusedForeground);
            nodeContext.SetBackground(FocusedBackground);
            nodeContext.Fill(0, 0, bounds.Width, lineCount, ' ');
        }
        else
        {
            nodeContext.SetForeground(Foreground);
        }

        for (var i = 0; i < lineCount; i++)
        {
            nodeContext.WriteAt(0, i, lines[i]);
        }

        nodeContext.ResetColors();

        if (hasHint && bounds.Height > lineCount)
        {
            nodeContext.SetForeground(_hasFocus ? Color.BrightBlack : Color.Gray);
            nodeContext.WriteAt(0, lineCount, Hint!.Length > bounds.Width ? Hint[..bounds.Width] : Hint);
            nodeContext.ResetColors();
        }
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _invalidated.OnCompleted();
        _invalidated.Dispose();
        base.Dispose();
    }

    private List<string> GetLines(int width)
    {
        if (width <= 0)
            return [string.Empty];

        return WordWrapper.WrapLines(Content.Split('\n'), width);
    }

    private void Invalidate()
    {
        if (!_disposed)
            _invalidated.OnNext(Unit.Default);
    }
}
