using R3;
using Termina.Clipboard;
using Termina.Diagnostics;
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
    private int _cursorPosition;
    private int _selectionStart = -1;
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

    public Color SelectionForeground { get; private set; } = Color.Black;

    public Color SelectionBackground { get; private set; } = Color.BrightYellow;

    public bool CanFocus => true;

    public bool HasFocus => _hasFocus;

    public int FocusPriority => 5;

    public Observable<Unit> Invalidated => _invalidated;

    public bool HasSelection => _selectionStart >= 0 && _selectionStart != _cursorPosition;

    public string SelectedText
    {
        get
        {
            if (!HasSelection)
                return string.Empty;

            var start = Math.Min(_selectionStart, _cursorPosition);
            var end = Math.Max(_selectionStart, _cursorPosition);
            return Content[start..end];
        }
    }

    public CopyableTextNode WithContent(string content)
    {
        Content = content ?? string.Empty;
        _cursorPosition = Math.Min(_cursorPosition, Content.Length);
        if (_selectionStart > Content.Length)
            _selectionStart = -1;
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

    public CopyableTextNode WithSelectionColors(Color foreground, Color background)
    {
        SelectionForeground = foreground;
        SelectionBackground = background;
        return this;
    }

    public void OnFocused()
    {
        _hasFocus = true;
        _cursorPosition = Math.Min(_cursorPosition, Content.Length);
        TerminaTrace.Focus.Debug(this, "CopyableTextNode focused: contentLength={0}", Content.Length);
        Invalidate();
    }

    public void OnBlurred()
    {
        _hasFocus = false;
        TerminaTrace.Focus.Debug(this, "CopyableTextNode blurred");
        Invalidate();
    }

    public bool HandleInput(ConsoleKeyInfo key)
    {
        TerminaTrace.Input.Trace(this, "CopyableTextNode.HandleInput: key={0}, hasFocus={1}", key.Key, _hasFocus);

        bool handled;

        if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && (key.KeyChar == 'a' || key.KeyChar == 'A'))
        {
            SelectAll();
            handled = true;
        }
        else if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.C)
        {
            CopySelectionOrContent();
            handled = true;
        }
        else
        {
            handled = key.Key switch
            {
                ConsoleKey.Enter => CopySelectionOrContent(),
                ConsoleKey.LeftArrow => MoveCursor(-1, key.Modifiers),
                ConsoleKey.RightArrow => MoveCursor(1, key.Modifiers),
                ConsoleKey.Home => MoveToBoundary(0, key.Modifiers),
                ConsoleKey.End => MoveToBoundary(Content.Length, key.Modifiers),
                ConsoleKey.Escape => ClearSelection(),
                _ => false
            };
        }

        if (handled)
        {
            Invalidate();
        }

        return handled;
    }

    public override Size Measure(Size available)
    {
        var lines = BuildRenderedLines(available.Width > 0 ? available.Width : Math.Max(1, Content.Length));
        var hintHeight = string.IsNullOrWhiteSpace(Hint) ? 0 : 1;
        return new Size(available.Width, lines.Count + hintHeight);
    }

    public override void Render(IRenderContext context, Rect bounds)
    {
        if (!bounds.HasArea)
            return;

        var nodeContext = context.CreateSubContext(bounds);
        var lines = BuildRenderedLines(bounds.Width);
        var hasHint = !string.IsNullOrWhiteSpace(Hint);
        var lineCount = hasHint
            ? Math.Min(lines.Count, Math.Max(0, bounds.Height - 1))
            : Math.Min(lines.Count, bounds.Height);

        nodeContext.SetForeground(Foreground);
        nodeContext.Fill(0, 0, bounds.Width, lineCount, ' ');

        for (var i = 0; i < lineCount; i++)
        {
            RenderLine(nodeContext, i, lines[i], bounds.Width);
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

    private bool CopySelectionOrContent()
    {
        var textToCopy = HasSelection ? SelectedText : Content;
        TerminaTrace.Input.Info(this, "CopyableTextNode copy requested: contentLength={0}, selectionLength={1}", Content.Length, textToCopy.Length);
        _clipboardService.Copy(textToCopy);
        return true;
    }

    private bool MoveCursor(int delta, ConsoleModifiers modifiers)
    {
        if (Content.Length == 0)
            return false;

        var newPosition = Math.Clamp(_cursorPosition + delta, 0, Content.Length);
        UpdateSelectionForMove(modifiers, newPosition);
        return true;
    }

    private bool MoveToBoundary(int position, ConsoleModifiers modifiers)
    {
        var newPosition = Math.Clamp(position, 0, Content.Length);
        UpdateSelectionForMove(modifiers, newPosition);
        return true;
    }

    private bool ClearSelection()
    {
        if (!HasSelection)
            return false;

        _selectionStart = -1;
        return true;
    }

    private void SelectAll()
    {
        _selectionStart = 0;
        _cursorPosition = Content.Length;
    }

    private void UpdateSelectionForMove(ConsoleModifiers modifiers, int newPosition)
    {
        if (modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            if (_selectionStart < 0)
                _selectionStart = _cursorPosition;
        }
        else
        {
            _selectionStart = -1;
        }

        _cursorPosition = newPosition;
    }

    private void RenderLine(IRenderContext context, int row, RenderedLine line, int width)
    {
        for (var column = 0; column < line.Text.Length && column < width; column++)
        {
            var sourceIndex = line.StartIndex + column;
            var isSelected = IsSelected(sourceIndex);
            var isCursor = _hasFocus && !HasSelection && sourceIndex == _cursorPosition;

            if (isSelected)
            {
                context.SetForeground(SelectionForeground);
                context.SetBackground(SelectionBackground);
            }
            else if (isCursor)
            {
                context.SetForeground(FocusedForeground);
                context.SetBackground(FocusedBackground);
            }
            else
            {
                context.SetForeground(Foreground);
                context.SetBackground(Color.Default);
            }

            context.WriteAt(column, row, line.Text[column]);
        }

        if (_hasFocus && !HasSelection && line.Text.Length < width && _cursorPosition == line.StartIndex + line.Text.Length)
        {
            context.SetForeground(FocusedForeground);
            context.SetBackground(FocusedBackground);
            context.WriteAt(line.Text.Length, row, ' ');
        }

        context.ResetColors();
    }

    private bool IsSelected(int sourceIndex)
    {
        if (!HasSelection)
            return false;

        var start = Math.Min(_selectionStart, _cursorPosition);
        var end = Math.Max(_selectionStart, _cursorPosition);
        return sourceIndex >= start && sourceIndex < end;
    }

    private List<RenderedLine> BuildRenderedLines(int width)
    {
        if (width <= 0)
            return [new RenderedLine(string.Empty, 0)];

        var rendered = new List<RenderedLine>();
        var sourceLines = Content.Split('\n');
        var sourceOffset = 0;

        for (var i = 0; i < sourceLines.Length; i++)
        {
            var line = sourceLines[i];
            if (line.Length == 0)
            {
                rendered.Add(new RenderedLine(string.Empty, sourceOffset));
            }
            else
            {
                for (var chunkStart = 0; chunkStart < line.Length; chunkStart += width)
                {
                    var chunkLength = Math.Min(width, line.Length - chunkStart);
                    rendered.Add(new RenderedLine(line.Substring(chunkStart, chunkLength), sourceOffset + chunkStart));
                }
            }

            sourceOffset += line.Length;
            if (i < sourceLines.Length - 1)
                sourceOffset += 1;
        }

        return rendered;
    }

    private void Invalidate()
    {
        if (!_disposed)
            _invalidated.OnNext(Unit.Default);
    }

    private sealed record RenderedLine(string Text, int StartIndex);
}
