// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Timers;
using Termina.Rendering;
using Termina.Terminal;
using Timer = System.Timers.Timer;

namespace Termina.Layout;

/// <summary>
/// A stateful layout node that handles text input with cursor, selection, and history.
/// </summary>
public sealed class TextInputNode : LayoutNode, IAnimatedNode, IInvalidatingNode
{
    private readonly Timer _cursorTimer;
    private readonly List<string> _history = new();
    private int _historyIndex = -1;
    private string _text = "";
    private int _cursorPosition;
    private int _selectionStart = -1;
    private int _scrollOffset;
    private bool _cursorVisible = true;
    private bool _disposed;

    /// <inheritdoc />
    public event Action? Invalidated;

    /// <summary>
    /// Event raised when the text value changes.
    /// </summary>
    public event Action<string>? TextChanged;

    /// <summary>
    /// Event raised when Enter is pressed.
    /// </summary>
    public event Action<string>? Submitted;

    /// <inheritdoc />
    public bool IsAnimating { get; private set; }

    /// <summary>
    /// Gets or sets the current text value.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value ?? "";
                _cursorPosition = Math.Min(_cursorPosition, _text.Length);
                _selectionStart = -1;
                TextChanged?.Invoke(_text);
                Invalidated?.Invoke();
            }
        }
    }

    /// <summary>
    /// Gets or sets the placeholder text shown when empty.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets the foreground color.
    /// </summary>
    public Color? Foreground { get; private set; }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public Color? Background { get; private set; }

    /// <summary>
    /// Gets or sets the placeholder color.
    /// </summary>
    public Color PlaceholderColor { get; set; } = Color.BrightBlack;

    /// <summary>
    /// Gets or sets the cursor color.
    /// </summary>
    public Color CursorColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the selection background color.
    /// </summary>
    public Color SelectionColor { get; set; } = Color.Blue;

    /// <summary>
    /// Gets or sets the maximum length (0 = unlimited).
    /// </summary>
    public int MaxLength { get; set; }

    /// <summary>
    /// Gets or sets whether to mask input (password mode).
    /// </summary>
    public bool IsPassword { get; set; }

    /// <summary>
    /// Gets or sets the mask character for password mode.
    /// </summary>
    public char PasswordChar { get; set; } = '•';

    /// <summary>
    /// Gets whether there is selected text.
    /// </summary>
    public bool HasSelection => _selectionStart >= 0 && _selectionStart != _cursorPosition;

    /// <summary>
    /// Gets the selected text.
    /// </summary>
    public string SelectedText
    {
        get
        {
            if (!HasSelection)
                return "";
            var start = Math.Min(_selectionStart, _cursorPosition);
            var end = Math.Max(_selectionStart, _cursorPosition);
            return _text[start..end];
        }
    }

    public TextInputNode(int cursorBlinkMs = 530)
    {
        _cursorTimer = new Timer(cursorBlinkMs);
        _cursorTimer.Elapsed += OnCursorBlink;
        _cursorTimer.AutoReset = true;

        HeightConstraint = new SizeConstraint.Fixed(1);
        WidthConstraint = new SizeConstraint.Fill();

        Start();
    }

    /// <summary>
    /// Set placeholder text.
    /// </summary>
    public TextInputNode WithPlaceholder(string placeholder)
    {
        Placeholder = placeholder;
        return this;
    }

    /// <summary>
    /// Set foreground color.
    /// </summary>
    public TextInputNode WithForeground(Color color)
    {
        Foreground = color;
        return this;
    }

    /// <summary>
    /// Set background color.
    /// </summary>
    public TextInputNode WithBackground(Color color)
    {
        Background = color;
        return this;
    }

    /// <summary>
    /// Set max length.
    /// </summary>
    public TextInputNode WithMaxLength(int length)
    {
        MaxLength = length;
        return this;
    }

    /// <summary>
    /// Enable password mode.
    /// </summary>
    public TextInputNode AsPassword(char maskChar = '•')
    {
        IsPassword = true;
        PasswordChar = maskChar;
        return this;
    }

    /// <inheritdoc />
    public void Start()
    {
        if (!IsAnimating)
        {
            IsAnimating = true;
            _cursorVisible = true;
            _cursorTimer.Start();
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (IsAnimating)
        {
            _cursorTimer.Stop();
            IsAnimating = false;
            _cursorVisible = false;
            Invalidated?.Invoke();
        }
    }

    private void OnCursorBlink(object? sender, ElapsedEventArgs e)
    {
        _cursorVisible = !_cursorVisible;
        Invalidated?.Invoke();
    }

    /// <summary>
    /// Handle a key input event. Returns true if the event was handled.
    /// </summary>
    public bool HandleInput(ConsoleKeyInfo key)
    {
        // Don't process input if disposed
        if (_disposed)
            return false;

        // Reset cursor to visible on any input
        _cursorVisible = true;
        _cursorTimer.Stop();
        _cursorTimer.Start();

        var handled = key.Key switch
        {
            ConsoleKey.Backspace => HandleBackspace(key.Modifiers),
            ConsoleKey.Delete => HandleDelete(key.Modifiers),
            ConsoleKey.LeftArrow => HandleLeftArrow(key.Modifiers),
            ConsoleKey.RightArrow => HandleRightArrow(key.Modifiers),
            ConsoleKey.Home => HandleHome(key.Modifiers),
            ConsoleKey.End => HandleEnd(key.Modifiers),
            ConsoleKey.UpArrow => HandleUpArrow(),
            ConsoleKey.DownArrow => HandleDownArrow(),
            ConsoleKey.Enter => HandleEnter(),
            ConsoleKey.Escape => HandleEscape(),
            _ when key.KeyChar != '\0' && !char.IsControl(key.KeyChar) => HandleCharacter(key.KeyChar, key.Modifiers),
            _ => false
        };

        if (handled)
        {
            Invalidated?.Invoke();
        }

        return handled;
    }

    private bool HandleCharacter(char c, ConsoleModifiers modifiers)
    {
        // Handle Ctrl+A (select all)
        if (modifiers.HasFlag(ConsoleModifiers.Control) && (c == 'a' || c == 'A'))
        {
            SelectAll();
            return true;
        }

        // Handle Ctrl+C (copy - just clear selection for now)
        if (modifiers.HasFlag(ConsoleModifiers.Control) && (c == 'c' || c == 'C'))
        {
            // Would need clipboard integration
            return true;
        }

        // Handle Ctrl+V (paste - would need clipboard integration)
        if (modifiers.HasFlag(ConsoleModifiers.Control) && (c == 'v' || c == 'V'))
        {
            // Would need clipboard integration
            return true;
        }

        // Check max length
        var addLength = HasSelection ? 1 - SelectedText.Length : 1;
        if (MaxLength > 0 && _text.Length + addLength > MaxLength)
            return false;

        // Delete selection if any
        if (HasSelection)
        {
            DeleteSelection();
        }

        // Insert character
        _text = _text.Insert(_cursorPosition, c.ToString());
        _cursorPosition++;
        TextChanged?.Invoke(_text);
        return true;
    }

    private bool HandleBackspace(ConsoleModifiers modifiers)
    {
        if (HasSelection)
        {
            DeleteSelection();
            TextChanged?.Invoke(_text);
            return true;
        }

        if (_cursorPosition == 0)
            return false;

        if (modifiers.HasFlag(ConsoleModifiers.Control))
        {
            // Delete word
            var wordStart = FindWordBoundary(_cursorPosition, -1);
            _text = _text.Remove(wordStart, _cursorPosition - wordStart);
            _cursorPosition = wordStart;
        }
        else
        {
            // Delete single character
            _text = _text.Remove(_cursorPosition - 1, 1);
            _cursorPosition--;
        }

        TextChanged?.Invoke(_text);
        return true;
    }

    private bool HandleDelete(ConsoleModifiers modifiers)
    {
        if (HasSelection)
        {
            DeleteSelection();
            TextChanged?.Invoke(_text);
            return true;
        }

        if (_cursorPosition >= _text.Length)
            return false;

        if (modifiers.HasFlag(ConsoleModifiers.Control))
        {
            // Delete word
            var wordEnd = FindWordBoundary(_cursorPosition, 1);
            _text = _text.Remove(_cursorPosition, wordEnd - _cursorPosition);
        }
        else
        {
            // Delete single character
            _text = _text.Remove(_cursorPosition, 1);
        }

        TextChanged?.Invoke(_text);
        return true;
    }

    private bool HandleLeftArrow(ConsoleModifiers modifiers)
    {
        var newPos = modifiers.HasFlag(ConsoleModifiers.Control)
            ? FindWordBoundary(_cursorPosition, -1)
            : Math.Max(0, _cursorPosition - 1);

        if (modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            if (_selectionStart < 0)
                _selectionStart = _cursorPosition;
        }
        else
        {
            _selectionStart = -1;
        }

        _cursorPosition = newPos;
        return true;
    }

    private bool HandleRightArrow(ConsoleModifiers modifiers)
    {
        var newPos = modifiers.HasFlag(ConsoleModifiers.Control)
            ? FindWordBoundary(_cursorPosition, 1)
            : Math.Min(_text.Length, _cursorPosition + 1);

        if (modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            if (_selectionStart < 0)
                _selectionStart = _cursorPosition;
        }
        else
        {
            _selectionStart = -1;
        }

        _cursorPosition = newPos;
        return true;
    }

    private bool HandleHome(ConsoleModifiers modifiers)
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

        _cursorPosition = 0;
        return true;
    }

    private bool HandleEnd(ConsoleModifiers modifiers)
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

        _cursorPosition = _text.Length;
        return true;
    }

    private bool HandleUpArrow()
    {
        // Navigate history
        if (_history.Count == 0)
            return false;

        if (_historyIndex < 0)
        {
            _historyIndex = _history.Count - 1;
        }
        else if (_historyIndex > 0)
        {
            _historyIndex--;
        }

        _text = _history[_historyIndex];
        _cursorPosition = _text.Length;
        _selectionStart = -1;
        return true;
    }

    private bool HandleDownArrow()
    {
        if (_historyIndex < 0)
            return false;

        if (_historyIndex < _history.Count - 1)
        {
            _historyIndex++;
            _text = _history[_historyIndex];
        }
        else
        {
            _historyIndex = -1;
            _text = "";
        }

        _cursorPosition = _text.Length;
        _selectionStart = -1;
        return true;
    }

    private bool HandleEnter()
    {
        if (!string.IsNullOrWhiteSpace(_text))
        {
            _history.Add(_text);
            _historyIndex = -1;
        }

        Submitted?.Invoke(_text);
        _text = "";
        _cursorPosition = 0;
        _selectionStart = -1;
        TextChanged?.Invoke(_text);
        return true;
    }

    private bool HandleEscape()
    {
        if (HasSelection)
        {
            _selectionStart = -1;
            return true;
        }

        if (!string.IsNullOrEmpty(_text))
        {
            _text = "";
            _cursorPosition = 0;
            TextChanged?.Invoke(_text);
            return true;
        }

        return false;
    }

    private void SelectAll()
    {
        _selectionStart = 0;
        _cursorPosition = _text.Length;
    }

    private void DeleteSelection()
    {
        if (!HasSelection)
            return;

        var start = Math.Min(_selectionStart, _cursorPosition);
        var end = Math.Max(_selectionStart, _cursorPosition);
        _text = _text.Remove(start, end - start);
        _cursorPosition = start;
        _selectionStart = -1;
    }

    private int FindWordBoundary(int from, int direction)
    {
        if (direction < 0)
        {
            if (from <= 0)
                return 0;

            var pos = from - 1;
            // Skip whitespace
            while (pos > 0 && char.IsWhiteSpace(_text[pos]))
                pos--;
            // Skip word characters
            while (pos > 0 && !char.IsWhiteSpace(_text[pos - 1]))
                pos--;
            return pos;
        }
        else
        {
            if (from >= _text.Length)
                return _text.Length;

            var pos = from;
            // Skip word characters
            while (pos < _text.Length && !char.IsWhiteSpace(_text[pos]))
                pos++;
            // Skip whitespace
            while (pos < _text.Length && char.IsWhiteSpace(_text[pos]))
                pos++;
            return pos;
        }
    }

    /// <inheritdoc />
    public override Size Measure(Size available)
    {
        var width = WidthConstraint.Compute(available.Width, _text.Length + 1, available.Width);
        return new Size(width, 1);
    }

    /// <inheritdoc />
    public override void Render(IRenderContext context, Rect bounds)
    {
        if (!bounds.HasArea)
            return;

        var displayText = _text;
        var displayCursor = _cursorPosition;

        // Apply password masking
        if (IsPassword && displayText.Length > 0)
        {
            displayText = new string(PasswordChar, displayText.Length);
        }

        // Show placeholder if empty
        if (string.IsNullOrEmpty(displayText) && !string.IsNullOrEmpty(Placeholder))
        {
            context.SetForeground(PlaceholderColor);
            var placeholder = Placeholder.Length > bounds.Width
                ? Placeholder[..bounds.Width]
                : Placeholder;
            context.WriteAt(0, 0, placeholder);
            context.ResetColors();

            // Still show cursor at position 0
            if (_cursorVisible)
            {
                context.SetBackground(CursorColor);
                context.WriteAt(0, 0, ' ');
                context.ResetColors();
            }
            return;
        }

        // Calculate scroll offset to keep cursor visible
        if (displayCursor < _scrollOffset)
        {
            _scrollOffset = displayCursor;
        }
        else if (displayCursor >= _scrollOffset + bounds.Width)
        {
            _scrollOffset = displayCursor - bounds.Width + 1;
        }

        // Set colors
        if (Foreground.HasValue)
            context.SetForeground(Foreground.Value);
        if (Background.HasValue)
            context.SetBackground(Background.Value);

        // Get visible portion
        var visibleText = displayText.Length > _scrollOffset
            ? displayText[_scrollOffset..]
            : "";
        if (visibleText.Length > bounds.Width)
            visibleText = visibleText[..bounds.Width];

        // Draw selection background
        if (HasSelection)
        {
            var selStart = Math.Min(_selectionStart, _cursorPosition);
            var selEnd = Math.Max(_selectionStart, _cursorPosition);
            var visSelStart = Math.Max(0, selStart - _scrollOffset);
            var visSelEnd = Math.Min(bounds.Width, selEnd - _scrollOffset);

            if (visSelEnd > visSelStart)
            {
                context.SetBackground(SelectionColor);
                for (var x = visSelStart; x < visSelEnd && x < visibleText.Length; x++)
                {
                    context.WriteAt(x, 0, visibleText[x]);
                }

                // Draw non-selected portions
                context.ResetColors();
                if (Foreground.HasValue)
                    context.SetForeground(Foreground.Value);
                if (Background.HasValue)
                    context.SetBackground(Background.Value);

                if (visSelStart > 0)
                {
                    context.WriteAt(0, 0, visibleText[..visSelStart]);
                }
                if (visSelEnd < visibleText.Length)
                {
                    context.WriteAt(visSelEnd, 0, visibleText[visSelEnd..]);
                }
            }
            else
            {
                context.WriteAt(0, 0, visibleText);
            }
        }
        else
        {
            context.WriteAt(0, 0, visibleText);
        }

        context.ResetColors();

        // Draw cursor
        if (_cursorVisible)
        {
            var cursorX = displayCursor - _scrollOffset;
            if (cursorX >= 0 && cursorX < bounds.Width)
            {
                context.SetBackground(CursorColor);
                context.SetForeground(Background ?? Color.Black);
                var cursorChar = cursorX < visibleText.Length ? visibleText[cursorX] : ' ';
                context.WriteAt(cursorX, 0, cursorChar);
                context.ResetColors();
            }
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cursorTimer.Stop();
        _cursorTimer.Dispose();
        base.Dispose();
    }
}
