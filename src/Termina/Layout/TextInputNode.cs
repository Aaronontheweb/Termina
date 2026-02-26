// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Diagnostics;
using Termina.Input;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Layout;

/// <summary>
/// A stateful layout node that handles text input with cursor and selection.
/// Supports optional built-in input history via <see cref="WithHistory"/>.
/// </summary>
public sealed class TextInputNode : LayoutNode, IAnimatedNode, IInvalidatingNode, IFocusable, IPasteReceiver
{
    private readonly TimeProvider _timeProvider;
    private readonly int _cursorBlinkMs;
    private IDisposable? _cursorTimerSubscription;
    private readonly Subject<Unit> _invalidated = new();
    private readonly Subject<string> _textChanged = new();
    private readonly Subject<string> _submitted = new();
    private string _text = "";
    private string? _pasteContent;  // full paste content (with newlines preserved)
    private int _cursorPosition;
    private int _selectionStart = -1;
    private int _scrollOffset;
    private bool _cursorVisible = true;
    private bool _hasFocus;
    private bool _disposed;

    // Input history (null = disabled, opt-in via WithHistory)
    private List<string>? _history;
    private int _historyIndex = -1;
    private string? _savedInput;
    private int _maxHistoryEntries;

    /// <inheritdoc />
    public Observable<Unit> Invalidated => _invalidated;

    /// <summary>
    /// Observable that emits when the text value changes.
    /// </summary>
    public Observable<string> TextChanged => _textChanged;

    /// <summary>
    /// Observable that emits when Enter is pressed.
    /// </summary>
    public Observable<string> Submitted => _submitted;

    /// <inheritdoc />
    public bool IsAnimating { get; private set; }

    /// <inheritdoc />
    public bool CanFocus => true;

    /// <inheritdoc />
    public bool HasFocus => _hasFocus;

    /// <summary>
    /// Text input has low-medium priority (lower than modal and selection list).
    /// </summary>
    public int FocusPriority => 5;

    /// <inheritdoc />
    public void OnFocused()
    {
        TerminaTrace.Focus.Debug(this, "OnFocused");
        _hasFocus = true;
        _cursorVisible = true;
        Start(); // Ensure cursor is blinking
        _invalidated.OnNext(Unit.Default);
    }

    /// <inheritdoc />
    public void OnBlurred()
    {
        TerminaTrace.Focus.Debug(this, "OnBlurred");
        _hasFocus = false;
        Stop(); // Stop cursor blinking when not focused
        _invalidated.OnNext(Unit.Default);
    }

    /// <summary>
    /// Gets or sets the current text value.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            var newValue = value ?? "";
            if (_text != newValue || _pasteContent != null)
            {
                // Multi-line content should display condensed, same as HandlePaste
                if (newValue.Contains('\n'))
                {
                    _pasteContent = newValue;
                    var lineCount = 1;
                    foreach (var c in newValue)
                    {
                        if (c == '\n') lineCount++;
                    }

                    _text = lineCount > 1
                        ? $"[Pasted {lineCount} lines, {newValue.Length} chars]"
                        : $"[Pasted {newValue.Length} chars]";
                }
                else
                {
                    _pasteContent = null;
                    _text = newValue;
                }

                _cursorPosition = Math.Min(_cursorPosition, _text.Length);
                _selectionStart = -1;
                _textChanged.OnNext(_text);
                _invalidated.OnNext(Unit.Default);
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

    public TextInputNode(int cursorBlinkMs = 530, TimeProvider? timeProvider = null)
    {
        _cursorBlinkMs = cursorBlinkMs;
        _timeProvider = timeProvider ?? TimeProvider.System;

        HeightConstraint = new SizeConstraint.Fixed(1);
        WidthConstraint = new SizeConstraint.Fill();

        // Don't auto-start - animation should only start when focused
        // Start() will be called by OnFocused() when the node gains focus
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

    /// <summary>
    /// Enable built-in input history. When enabled, Up/Down arrow keys navigate
    /// through previous submissions, and Enter auto-records non-empty text.
    /// </summary>
    /// <param name="maxEntries">Maximum history entries to keep (0 = unlimited).</param>
    public TextInputNode WithHistory(int maxEntries = 0)
    {
        _history = new List<string>();
        _maxHistoryEntries = maxEntries;
        return this;
    }

    /// <summary>
    /// Programmatically add an entry to the input history.
    /// Use this for submissions that bypass <see cref="HandleInput"/> (e.g., custom prompts).
    /// No-op when history is not enabled.
    /// </summary>
    public void AddHistory(string entry)
    {
        if (_history is null || string.IsNullOrEmpty(entry))
            return;

        _history.Add(entry);

        if (_maxHistoryEntries > 0 && _history.Count > _maxHistoryEntries)
            _history.RemoveAt(0);
    }

    /// <inheritdoc />
    public void Start()
    {
        if (!IsAnimating)
        {
            IsAnimating = true;
            _cursorVisible = true;
            _cursorTimerSubscription ??= Observable
                .Interval(TimeSpan.FromMilliseconds(_cursorBlinkMs), _timeProvider)
                .Subscribe(_ =>
                {
                    _cursorVisible = !_cursorVisible;
                    _invalidated.OnNext(Unit.Default);
                });
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (IsAnimating)
        {
            _cursorTimerSubscription?.Dispose();
            _cursorTimerSubscription = null;
            IsAnimating = false;
            _cursorVisible = false;
            _invalidated.OnNext(Unit.Default);
        }
    }

    /// <summary>
    /// Handle a key input event. Returns true if the event was handled.
    /// </summary>
    public bool HandleInput(ConsoleKeyInfo key)
    {
        // Don't process input if disposed
        if (_disposed)
        {
            TerminaTrace.Input.Debug(this, "HandleInput rejected: disposed");
            return false;
        }

        TerminaTrace.Input.Trace(this, "HandleInput: key={0}, char='{1}'", key.Key, key.KeyChar);

        // Reset cursor to visible on any input - restart the blink cycle
        _cursorVisible = true;
        _cursorTimerSubscription?.Dispose();
        _cursorTimerSubscription = null;
        IsAnimating = false;
        Start();

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

        TerminaTrace.Input.Trace(this, "HandleInput result: handled={0}", handled);

        if (handled)
        {
            _invalidated.OnNext(Unit.Default);
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

        // Any character input clears paste mode — return to normal editing
        ClearPasteContent();

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
        _textChanged.OnNext(_text);
        return true;
    }

    private bool HandleBackspace(ConsoleModifiers modifiers)
    {
        // Backspace clears paste mode — return to normal editing with empty input
        if (_pasteContent != null)
        {
            ClearPasteContent();
            return true;
        }

        if (HasSelection)
        {
            DeleteSelection();
            _textChanged.OnNext(_text);
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

        _textChanged.OnNext(_text);
        return true;
    }

    private bool HandleDelete(ConsoleModifiers modifiers)
    {
        // Delete clears paste mode — return to normal editing with empty input
        if (_pasteContent != null)
        {
            ClearPasteContent();
            return true;
        }

        if (HasSelection)
        {
            DeleteSelection();
            _textChanged.OnNext(_text);
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

        _textChanged.OnNext(_text);
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
        if (_history is null)
            return false; // History disabled — let caller handle

        if (_history.Count == 0)
            return true; // History enabled but empty — consume the key

        if (_historyIndex < 0)
        {
            // First press: save current input and jump to most recent
            _savedInput = _pasteContent ?? _text;
            _historyIndex = _history.Count - 1;
        }
        else if (_historyIndex > 0)
        {
            _historyIndex--;
        }

        ApplyHistoryEntry(_history[_historyIndex]);
        return true;
    }

    private bool HandleDownArrow()
    {
        if (_history is null)
            return false; // History disabled — let caller handle

        if (_historyIndex < 0)
            return true; // Not browsing history — consume the key

        if (_historyIndex < _history.Count - 1)
        {
            _historyIndex++;
            ApplyHistoryEntry(_history[_historyIndex]);
        }
        else
        {
            // Past the end — restore saved input
            _historyIndex = -1;
            var saved = _savedInput ?? "";
            _savedInput = null;
            ApplyHistoryEntry(saved);
        }

        return true;
    }

    private void ApplyHistoryEntry(string entry)
    {
        // Use Text setter which handles multi-line condensing
        Text = entry;
        _cursorPosition = _text.Length;
    }

    private bool HandleEnter()
    {
        // When paste content is stored, submit the full paste (with newlines) instead of the summary
        var content = _pasteContent ?? _text;
        TerminaTrace.Input.Debug(this, "HandleEnter: submitting text length={0} (paste={1})",
            content.Length, _pasteContent != null);
        _pasteContent = null;

        // Auto-record to history when enabled
        if (_history is not null && !string.IsNullOrWhiteSpace(content))
        {
            _history.Add(content);
            if (_maxHistoryEntries > 0 && _history.Count > _maxHistoryEntries)
                _history.RemoveAt(0);
        }

        _historyIndex = -1;
        _savedInput = null;

        _submitted.OnNext(content);
        return true;
    }

    /// <summary>
    /// Clears the text and resets cursor position.
    /// Call this from ViewModel after handling submission.
    /// </summary>
    public void Clear()
    {
        _pasteContent = null;
        _text = "";
        _cursorPosition = 0;
        _selectionStart = -1;
        _scrollOffset = 0;
        _textChanged.OnNext(_text);
        _invalidated.OnNext(Unit.Default);
    }

    private bool HandleEscape()
    {
        // Escape clears paste mode
        if (_pasteContent != null)
        {
            ClearPasteContent();
            return true;
        }

        if (HasSelection)
        {
            _selectionStart = -1;
            return true;
        }

        if (!string.IsNullOrEmpty(_text))
        {
            _text = "";
            _cursorPosition = 0;
            _textChanged.OnNext(_text);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles pasted text from the terminal's bracketed paste mode.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The full paste content (including newlines) is preserved and will be submitted
    /// verbatim when the user presses Enter. The input displays a summary placeholder
    /// (e.g., <c>[Pasted 500 lines, 12345 chars]</c>) instead of the raw content.
    /// </para>
    /// <para>
    /// Any subsequent character input, backspace, or delete clears the paste and returns
    /// the input to normal editing mode. This matches the behavior of CLI tools like
    /// Claude Code and OpenCode.
    /// </para>
    /// </remarks>
    /// <param name="paste">The paste event containing the text to insert.</param>
    /// <returns><see langword="true"/> if the paste was handled; <see langword="false"/> if empty.</returns>
    public bool HandlePaste(PasteEvent paste)
    {
        if (string.IsNullOrEmpty(paste.Content))
            return false;

        // Store the full paste content (newlines preserved) for submission
        _pasteContent = paste.Content;

        // Count lines for the summary display
        var lineCount = 1;
        foreach (var c in paste.Content)
        {
            if (c == '\n') lineCount++;
        }

        // Show a summary in the input instead of the raw content
        var summary = lineCount > 1
            ? $"[Pasted {lineCount} lines, {paste.Content.Length} chars]"
            : $"[Pasted {paste.Content.Length} chars]";

        _text = summary;
        _cursorPosition = _text.Length;
        _selectionStart = -1;
        _scrollOffset = 0;

        _textChanged.OnNext(_text);
        _invalidated.OnNext(Unit.Default);
        return true;
    }

    /// <summary>
    /// Clears paste content and resets the input to an empty state.
    /// Called when the user starts editing (typing, backspace, delete, escape) after a paste.
    /// </summary>
    private void ClearPasteContent()
    {
        if (_pasteContent == null) return;
        _pasteContent = null;
        _text = "";
        _cursorPosition = 0;
        _selectionStart = -1;
        _scrollOffset = 0;
        _textChanged.OnNext(_text);
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
    public override void OnActivate()
    {
        // Resume cursor blinking if this node has focus
        if (_hasFocus)
        {
            Start();
        }
        base.OnActivate();
    }

    /// <inheritdoc />
    public override void OnDeactivate()
    {
        // Stop cursor blinking to conserve resources
        Stop();
        base.OnDeactivate();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Stop();

        _invalidated.OnCompleted();
        _invalidated.Dispose();
        _textChanged.OnCompleted();
        _textChanged.Dispose();
        _submitted.OnCompleted();
        _submitted.Dispose();

        base.Dispose();
    }
}
