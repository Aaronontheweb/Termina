// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Layout;

/// <summary>
/// A multi-line text input node with word wrap and vertical scrolling.
/// Enter submits; Ctrl+Enter (configurable) or Alt+Enter inserts a newline.
/// Alt+Enter is a universal fallback that works on all terminals including tmux,
/// since terminals encode Alt as an ESC prefix.
/// Paste behavior matches <see cref="TextInputNode"/>: multi-line pastes show
/// a summary placeholder, and the full content is submitted on Enter.
/// </summary>
public sealed class TextAreaNode : TextInputBaseNode
{
    private int _scrollTopLine;
    private int _lastKnownWidth;
    private List<WrappedLine>? _wrappedLines;
    private ConsoleModifiers _newlineModifier = ConsoleModifiers.Control;
    private int _maxLines;
    private bool _wordWrap = true;

    private record struct WrappedLine(int TextStartIndex, int Length);

    public TextAreaNode(int cursorBlinkMs = 530, TimeProvider? timeProvider = null)
        : base(cursorBlinkMs, timeProvider)
    {
        HeightConstraint = new SizeConstraint.Auto { Min = 1, Max = 10 };
        WidthConstraint = new SizeConstraint.Fill();
    }

    // Text and HandlePaste use base class behavior (committed segments with summaries).

    #region Fluent API

    /// <summary>
    /// Set placeholder text.
    /// </summary>
    public TextAreaNode WithPlaceholder(string placeholder)
    {
        Placeholder = placeholder;
        return this;
    }

    /// <summary>
    /// Set foreground color.
    /// </summary>
    public TextAreaNode WithForeground(Color color)
    {
        Foreground = color;
        return this;
    }

    /// <summary>
    /// Set background color.
    /// </summary>
    public TextAreaNode WithBackground(Color color)
    {
        Background = color;
        return this;
    }

    /// <summary>
    /// Set max length (total characters, 0 = unlimited).
    /// </summary>
    public TextAreaNode WithMaxLength(int length)
    {
        MaxLength = length;
        return this;
    }

    /// <summary>
    /// Set maximum number of logical lines (0 = unlimited).
    /// When set, Enter at the limit is consumed but doesn't insert a newline.
    /// </summary>
    public TextAreaNode WithMaxLines(int lines)
    {
        _maxLines = lines;
        return this;
    }

    /// <summary>
    /// Enable or disable word wrapping (default: enabled).
    /// </summary>
    public TextAreaNode WithWordWrap(bool enabled)
    {
        _wordWrap = enabled;
        InvalidateWrappedLines();
        return this;
    }

    /// <summary>
    /// Set the modifier key required for inserting a newline (default: Control).
    /// Bare Enter submits; modifier+Enter inserts a newline.
    /// Use <see cref="ConsoleModifiers.Control"/> for Ctrl+Enter (default),
    /// <see cref="ConsoleModifiers.Alt"/> for Alt+Enter, etc.
    /// </summary>
    public TextAreaNode WithNewlineModifier(ConsoleModifiers mod)
    {
        _newlineModifier = mod;
        return this;
    }

    /// <summary>
    /// Enable built-in input history.
    /// </summary>
    /// <param name="maxEntries">Maximum history entries to keep (0 = unlimited).</param>
    public TextAreaNode WithHistory(int maxEntries = 0)
    {
        EnableHistory(maxEntries);
        return this;
    }

    /// <summary>
    /// Set maximum visible height in rows. Shorthand for HeightAuto(1, rows).
    /// </summary>
    public TextAreaNode WithMaxHeight(int rows)
    {
        HeightConstraint = new SizeConstraint.Auto { Min = 1, Max = rows };
        return this;
    }

    #endregion

    #region Overrides

    /// <inheritdoc />
    protected override bool HandleEnter(ConsoleModifiers modifiers)
    {
        // Modifier+Enter inserts a newline; bare Enter submits.
        //   Primary:  the configured _newlineModifier (default Ctrl+Enter) —
        //             requires kitty keyboard protocol support in the terminal.
        //   Fallback: Alt+Enter always works because terminals universally
        //             encode Alt as an ESC prefix, which EscapeSequenceParser detects.
        if (modifiers.HasFlag(_newlineModifier) || modifiers.HasFlag(ConsoleModifiers.Alt))
        {
            return InsertNewline();
        }

        PerformSubmit();
        return true;
    }

    private bool InsertNewline()
    {
        // Check max lines constraint (only counts newlines in _text, not committed segments)
        if (_maxLines > 0)
        {
            var currentLineCount = 1;
            foreach (var c in _text)
            {
                if (c == '\n') currentLineCount++;
            }

            if (currentLineCount >= _maxLines)
                return true; // Consume the key but don't insert
        }

        // Check max length
        if (MaxLength > 0 && _text.Length + 1 > MaxLength)
            return true;

        if (HasSelection)
            DeleteSelection();

        _text = _text.Insert(_cursorPosition, "\n");
        _cursorPosition++;
        OnTextBufferChanged();
        _textChanged.OnNext(Text);
        return true;
    }

    /// <inheritdoc />
    protected override bool HandleUpArrow()
    {
        if (_text.Length == 0 && _committedSegments.Count == 0)
            return base.HandleUpArrow();

        var lines = GetWrappedLines(EffectiveWidth);
        var displayPos = DisplayCursorPosition;
        var (row, col) = GetVisualPosition(displayPos, lines);

        if (row == 0)
            return true; // At top — consume the key

        var targetCol = Math.Min(col, lines[row - 1].Length);
        var targetDisplayPos = lines[row - 1].TextStartIndex + targetCol;
        _cursorPosition = DisplayToTextPosition(targetDisplayPos);
        _selectionStart = -1;
        return true;
    }

    /// <inheritdoc />
    protected override bool HandleDownArrow()
    {
        if (_text.Length == 0 && _committedSegments.Count == 0)
            return base.HandleDownArrow();

        var lines = GetWrappedLines(EffectiveWidth);
        var displayPos = DisplayCursorPosition;
        var (row, col) = GetVisualPosition(displayPos, lines);

        if (row >= lines.Count - 1)
            return true; // At bottom — consume the key

        var targetCol = Math.Min(col, lines[row + 1].Length);
        var targetDisplayPos = lines[row + 1].TextStartIndex + targetCol;
        _cursorPosition = DisplayToTextPosition(targetDisplayPos);
        _selectionStart = -1;
        return true;
    }

    /// <inheritdoc />
    protected override bool HandleHome(ConsoleModifiers modifiers)
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

        if (modifiers.HasFlag(ConsoleModifiers.Control))
        {
            _cursorPosition = 0;
        }
        else
        {
            var lines = GetWrappedLines(EffectiveWidth);
            var displayPos = DisplayCursorPosition;
            var (row, _) = GetVisualPosition(displayPos, lines);
            _cursorPosition = DisplayToTextPosition(lines[row].TextStartIndex);
        }

        return true;
    }

    /// <inheritdoc />
    protected override bool HandleEnd(ConsoleModifiers modifiers)
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

        if (modifiers.HasFlag(ConsoleModifiers.Control))
        {
            _cursorPosition = _text.Length;
        }
        else
        {
            var lines = GetWrappedLines(EffectiveWidth);
            var displayPos = DisplayCursorPosition;
            var (row, _) = GetVisualPosition(displayPos, lines);
            var line = lines[row];
            _cursorPosition = DisplayToTextPosition(line.TextStartIndex + line.Length);
        }

        return true;
    }

    /// <inheritdoc />
    public override void Clear()
    {
        _scrollTopLine = 0;
        InvalidateWrappedLines();
        base.Clear();
    }

    /// <inheritdoc />
    protected override void OnTextBufferChanged()
    {
        InvalidateWrappedLines();
    }

    /// <inheritdoc />
    public override Size Measure(Size available)
    {
        var prefixWidth = CommittedDisplayPrefix.Length;
        var width = WidthConstraint.Compute(available.Width, prefixWidth + _text.Length + 1, available.Width);
        _lastKnownWidth = width;

        var lines = GetWrappedLines(width);
        var lineCount = Math.Max(1, lines.Count);

        var height = HeightConstraint.Compute(available.Height, lineCount, available.Height);
        return new Size(width, height);
    }

    /// <inheritdoc />
    public override void Render(IRenderContext context, Rect bounds)
    {
        if (!bounds.HasArea)
            return;

        var inputContext = context.CreateSubContext(bounds);
        _lastKnownWidth = bounds.Width;

        var prefix = CommittedDisplayPrefix;
        var prefixLen = prefix.Length;
        var activeText = _text;

        if (IsPassword && activeText.Length > 0)
            activeText = new string(PasswordChar, activeText.Length);

        var fullDisplayText = prefix + activeText;
        var lines = GetWrappedLines(bounds.Width);

        // Show placeholder if completely empty
        if (fullDisplayText.Length == 0 && !string.IsNullOrEmpty(Placeholder))
        {
            inputContext.SetForeground(PlaceholderColor);
            var placeholder = Placeholder.Length > bounds.Width
                ? Placeholder[..bounds.Width]
                : Placeholder;
            inputContext.WriteAt(0, 0, placeholder);
            inputContext.ResetColors();

            if (_cursorVisible)
            {
                inputContext.SetBackground(CursorColor);
                inputContext.WriteAt(0, 0, ' ');
                inputContext.ResetColors();
            }
            return;
        }

        // Cursor position in display coordinates
        var displayCursor = prefixLen + _cursorPosition;
        var (cursorRow, cursorCol) = GetVisualPosition(displayCursor, lines);
        EnsureCursorVisible(cursorRow, bounds.Height);

        // Selection range in display coordinates
        var selDisplayStart = -1;
        var selDisplayEnd = -1;
        if (HasSelection)
        {
            selDisplayStart = prefixLen + Math.Min(_selectionStart, _cursorPosition);
            selDisplayEnd = prefixLen + Math.Max(_selectionStart, _cursorPosition);
        }

        // Pre-compute committed segment boundaries for coloring
        var segBoundaries = new List<(int Start, int End, SegmentKind Kind)>();
        var segOff = 0;
        foreach (var seg in _committedSegments)
        {
            segBoundaries.Add((segOff, segOff + seg.DisplayText.Length, seg.Kind));
            segOff += seg.DisplayText.Length;
        }

        // Render visible lines
        for (var row = 0; row < bounds.Height; row++)
        {
            var lineIndex = _scrollTopLine + row;
            if (lineIndex >= lines.Count)
                break;

            var line = lines[lineIndex];

            for (var col = 0; col < line.Length && col < bounds.Width; col++)
            {
                var displayIndex = line.TextStartIndex + col;
                if (displayIndex >= fullDisplayText.Length)
                    break;

                var ch = fullDisplayText[displayIndex];

                inputContext.ResetColors();

                // Selection highlighting
                if (selDisplayStart >= 0 && displayIndex >= selDisplayStart && displayIndex < selDisplayEnd)
                {
                    inputContext.SetBackground(SelectionColor);
                }
                else if (Background.HasValue)
                {
                    inputContext.SetBackground(Background.Value);
                }

                // Foreground: committed segment coloring vs active text
                if (displayIndex < prefixLen)
                {
                    // Find which committed segment this character belongs to
                    var segColor = Foreground;
                    foreach (var (start, end, kind) in segBoundaries)
                    {
                        if (displayIndex >= start && displayIndex < end)
                        {
                            if (kind == SegmentKind.Pasted)
                                segColor = Color.BrightBlack;
                            break;
                        }
                    }

                    if (segColor.HasValue)
                        inputContext.SetForeground(segColor.Value);
                }
                else
                {
                    if (Foreground.HasValue)
                        inputContext.SetForeground(Foreground.Value);
                }

                inputContext.WriteAt(col, row, ch);
            }
        }

        inputContext.ResetColors();

        // Draw cursor
        if (_cursorVisible)
        {
            var visibleCursorRow = cursorRow - _scrollTopLine;
            if (visibleCursorRow >= 0 && visibleCursorRow < bounds.Height
                && cursorCol >= 0 && cursorCol < bounds.Width)
            {
                inputContext.SetBackground(CursorColor);
                inputContext.SetForeground(Background ?? Color.Black);

                char cursorChar;
                if (cursorRow < lines.Count)
                {
                    var cursorDisplayIndex = lines[cursorRow].TextStartIndex + cursorCol;
                    cursorChar = cursorDisplayIndex < fullDisplayText.Length
                        ? fullDisplayText[cursorDisplayIndex]
                        : ' ';
                }
                else
                {
                    cursorChar = ' ';
                }

                inputContext.WriteAt(cursorCol, visibleCursorRow, cursorChar);
                inputContext.ResetColors();
            }
        }
    }

    #endregion

    #region Word wrap and position mapping

    /// <summary>
    /// The effective width used for wrapping calculations.
    /// </summary>
    private int EffectiveWidth => _lastKnownWidth > 0 ? _lastKnownWidth : 80;

    /// <summary>
    /// Cursor position in the full display text coordinate space.
    /// </summary>
    private int DisplayCursorPosition => CommittedDisplayPrefix.Length + _cursorPosition;

    /// <summary>
    /// Converts a position in the full display text back to an active-text position,
    /// clamping so the cursor never lands inside the committed prefix.
    /// </summary>
    private int DisplayToTextPosition(int displayPos)
    {
        var prefixLen = CommittedDisplayPrefix.Length;
        return Math.Clamp(displayPos - prefixLen, 0, _text.Length);
    }

    /// <summary>
    /// Builds the full display text used for wrapping and rendering:
    /// committed segment display prefix + active text (with password masking).
    /// </summary>
    private string GetFullDisplayText()
    {
        var activeText = IsPassword && _text.Length > 0
            ? new string(PasswordChar, _text.Length)
            : _text;
        return CommittedDisplayPrefix + activeText;
    }

    private void InvalidateWrappedLines()
    {
        _wrappedLines = null;
    }

    private List<WrappedLine> GetWrappedLines(int width)
    {
        if (width <= 0)
            width = 1;

        if (_wrappedLines is not null && _lastKnownWidth == width)
            return _wrappedLines;

        _lastKnownWidth = width;
        _wrappedLines = ComputeWrappedLines(width);
        return _wrappedLines;
    }

    private List<WrappedLine> ComputeWrappedLines(int width)
    {
        var result = new List<WrappedLine>();
        var fullText = GetFullDisplayText();

        if (fullText.Length == 0)
        {
            result.Add(new WrappedLine(0, 0));
            return result;
        }

        // Split by logical lines (newlines), then word-wrap each
        var lineStart = 0;
        for (var i = 0; i <= fullText.Length; i++)
        {
            if (i == fullText.Length || fullText[i] == '\n')
            {
                var logicalLineLength = i - lineStart;

                if (!_wordWrap || logicalLineLength <= width)
                {
                    result.Add(new WrappedLine(lineStart, logicalLineLength));
                }
                else
                {
                    WrapLogicalLine(result, fullText, lineStart, logicalLineLength, width);
                }

                lineStart = i + 1;
            }
        }

        // If text ends with \n, add an empty line
        if (fullText.Length > 0 && fullText[^1] == '\n')
        {
            result.Add(new WrappedLine(fullText.Length, 0));
        }

        return result;
    }

    private static void WrapLogicalLine(
        List<WrappedLine> result, string text, int lineStart, int lineLength, int width)
    {
        var pos = lineStart;
        var end = lineStart + lineLength;

        while (pos < end)
        {
            var remaining = end - pos;
            if (remaining <= width)
            {
                result.Add(new WrappedLine(pos, remaining));
                break;
            }

            // Try to break at a word boundary
            var breakPos = pos + width;
            var wordBreak = breakPos;

            while (wordBreak > pos && !char.IsWhiteSpace(text[wordBreak - 1]))
                wordBreak--;

            if (wordBreak == pos)
                wordBreak = breakPos;

            result.Add(new WrappedLine(pos, wordBreak - pos));
            pos = wordBreak;

            // Skip leading whitespace on the new line
            while (pos < end && text[pos] == ' ')
                pos++;
        }
    }

    /// <summary>
    /// Maps a display-text index to a visual (row, col) position within the wrapped lines.
    /// </summary>
    private (int Row, int Col) GetVisualPosition(int displayIndex, List<WrappedLine> lines)
    {
        if (lines.Count == 0)
            return (0, 0);

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineEnd = line.TextStartIndex + line.Length;

            if (displayIndex >= line.TextStartIndex && displayIndex <= lineEnd)
            {
                // If exactly at the start of the next line, prefer the next line
                if (displayIndex == lineEnd && i + 1 < lines.Count
                    && displayIndex == lines[i + 1].TextStartIndex)
                    continue;

                return (i, displayIndex - line.TextStartIndex);
            }
        }

        // Past the end — cursor at end of last line
        var lastLine = lines[^1];
        return (lines.Count - 1, lastLine.Length);
    }

    private void EnsureCursorVisible(int cursorRow, int viewportHeight)
    {
        if (viewportHeight <= 0)
            return;

        if (cursorRow < _scrollTopLine)
        {
            _scrollTopLine = cursorRow;
        }
        else if (cursorRow >= _scrollTopLine + viewportHeight)
        {
            _scrollTopLine = cursorRow - viewportHeight + 1;
        }
    }

    #endregion
}
