// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Layout;

/// <summary>
/// A modal overlay component that displays content over the rest of the UI.
/// </summary>
/// <remarks>
/// <para>
/// ModalNode captures all keyboard input when focused (FocusPriority = 100).
/// It supports configurable backdrops, borders, and positioning.
/// </para>
/// <para>
/// Use the focus manager to show/hide modals:
/// <code>
/// Focus.PushFocus(modal);  // Show modal
/// Focus.PopFocus();        // Hide modal
/// </code>
/// </para>
/// </remarks>
public sealed class ModalNode : IFocusable, IInvalidatingNode
{
    private readonly Subject<Unit> _invalidated = new();
    private readonly Subject<Unit> _dismissed = new();
    private ILayoutNode? _content;
    private string? _title;
    private Color? _titleColor;
    private BorderStyle _borderStyle = BorderStyle.Rounded;
    private Color? _borderColor;
    private BackdropStyle _backdrop = BackdropStyle.Dim;
    private Color _backdropColor = Color.BrightBlack;
    private char _backdropChar = '░';
    private ModalPosition _position = ModalPosition.Center;
    private int _padding = 1;
    private bool _dismissOnEscape = true;
    private bool _hasFocus;
    private bool _disposed;

    private readonly record struct BorderCharsData(
        char TopLeft, char TopRight,
        char BottomLeft, char BottomRight,
        char Horizontal, char Vertical);

    private static BorderCharsData GetBorderChars(BorderStyle style) => style switch
    {
        BorderStyle.Single => new BorderCharsData('┌', '┐', '└', '┘', '─', '│'),
        BorderStyle.Double => new BorderCharsData('╔', '╗', '╚', '╝', '═', '║'),
        BorderStyle.Rounded => new BorderCharsData('╭', '╮', '╰', '╯', '─', '│'),
        BorderStyle.Ascii => new BorderCharsData('+', '+', '+', '+', '-', '|'),
        _ => new BorderCharsData(' ', ' ', ' ', ' ', ' ', ' ')
    };

    /// <inheritdoc />
    public IObservable<Unit> Invalidated => _invalidated.AsObservable();

    /// <summary>
    /// Observable that emits when the modal is dismissed (e.g., Escape pressed).
    /// </summary>
    public IObservable<Unit> Dismissed => _dismissed.AsObservable();

    /// <inheritdoc />
    public SizeConstraint WidthConstraint => SizeConstraint.FillRemaining();

    /// <inheritdoc />
    public SizeConstraint HeightConstraint => SizeConstraint.FillRemaining();

    /// <inheritdoc />
    public bool CanFocus => true;

    /// <inheritdoc />
    public bool HasFocus => _hasFocus;

    /// <summary>
    /// Modal has high priority to capture all input.
    /// </summary>
    public int FocusPriority => 100;

    /// <summary>
    /// Gets or sets the modal content.
    /// </summary>
    public ILayoutNode? Content
    {
        get => _content;
        set
        {
            _content = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Sets the modal content.
    /// </summary>
    public ModalNode WithContent(ILayoutNode content)
    {
        _content = content;
        return this;
    }

    /// <summary>
    /// Sets the modal title.
    /// </summary>
    public ModalNode WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    /// Sets the title color.
    /// </summary>
    public ModalNode WithTitleColor(Color color)
    {
        _titleColor = color;
        return this;
    }

    /// <summary>
    /// Sets the border style.
    /// </summary>
    public ModalNode WithBorder(BorderStyle style)
    {
        _borderStyle = style;
        return this;
    }

    /// <summary>
    /// Sets the border color.
    /// </summary>
    public ModalNode WithBorderColor(Color color)
    {
        _borderColor = color;
        return this;
    }

    /// <summary>
    /// Sets the backdrop style.
    /// </summary>
    public ModalNode WithBackdrop(BackdropStyle style)
    {
        _backdrop = style;
        return this;
    }

    /// <summary>
    /// Sets the backdrop color.
    /// </summary>
    public ModalNode WithBackdropColor(Color color)
    {
        _backdropColor = color;
        return this;
    }

    /// <summary>
    /// Sets the backdrop character (for Dim style).
    /// </summary>
    public ModalNode WithBackdropChar(char c)
    {
        _backdropChar = c;
        return this;
    }

    /// <summary>
    /// Sets the modal position.
    /// </summary>
    public ModalNode WithPosition(ModalPosition position)
    {
        _position = position;
        return this;
    }

    /// <summary>
    /// Sets the padding between border and content.
    /// </summary>
    public ModalNode WithPadding(int padding)
    {
        _padding = Math.Max(0, padding);
        return this;
    }

    /// <summary>
    /// Sets whether Escape key dismisses the modal.
    /// </summary>
    public ModalNode WithDismissOnEscape(bool dismiss = true)
    {
        _dismissOnEscape = dismiss;
        return this;
    }

    /// <inheritdoc />
    public void OnFocused()
    {
        _hasFocus = true;
        Invalidate();
    }

    /// <inheritdoc />
    public void OnBlurred()
    {
        _hasFocus = false;
        Invalidate();
    }

    /// <inheritdoc />
    public bool HandleInput(ConsoleKeyInfo key)
    {
        // Handle Escape to dismiss
        if (_dismissOnEscape && key.Key == ConsoleKey.Escape)
        {
            _dismissed.OnNext(Unit.Default);
            return true;
        }

        // Forward input to content if it's focusable
        if (_content is IFocusable focusable && focusable.CanFocus)
        {
            return focusable.HandleInput(key);
        }

        // Modal consumes all input to prevent it reaching underlying UI
        return true;
    }

    /// <inheritdoc />
    public Size Measure(Size available)
    {
        // Modal takes full available space (for backdrop)
        return available;
    }

    /// <inheritdoc />
    public void Render(IRenderContext context, Rect bounds)
    {
        // Draw backdrop
        RenderBackdrop(context, bounds);

        // Calculate modal size based on content
        var contentSize = _content?.Measure(new Size(
            bounds.Width - 4 - (_padding * 2),  // Account for border and padding
            bounds.Height - 4 - (_padding * 2)
        )) ?? new Size(20, 5);

        var modalWidth = Math.Min(
            contentSize.Width + 2 + (_padding * 2),  // +2 for border
            bounds.Width - 4  // Leave some margin
        );
        var modalHeight = Math.Min(
            contentSize.Height + 2 + (_padding * 2),  // +2 for border
            bounds.Height - 2  // Leave some margin
        );

        // Calculate position
        var x = (bounds.Width - modalWidth) / 2;
        var y = _position switch
        {
            ModalPosition.Top => 1,
            ModalPosition.Bottom => bounds.Height - modalHeight - 1,
            _ => (bounds.Height - modalHeight) / 2
        };

        // Draw modal panel
        RenderPanel(context, new Rect(x, y, modalWidth, modalHeight));
    }

    private void RenderBackdrop(IRenderContext context, Rect bounds)
    {
        switch (_backdrop)
        {
            case BackdropStyle.Dim:
                context.SetForeground(_backdropColor);
                context.Fill(0, 0, bounds.Width, bounds.Height, _backdropChar);
                break;

            case BackdropStyle.Solid:
                context.SetBackground(_backdropColor);
                context.Fill(0, 0, bounds.Width, bounds.Height, ' ');
                context.ResetColors();
                break;

            // Transparent - don't render anything
        }
    }

    private void RenderPanel(IRenderContext context, Rect bounds)
    {
        var border = GetBorderChars(_borderStyle);

        // Create a sub-context for this panel's bounds so all coordinates are relative to the panel
        var panelContext = context.CreateSubContext(bounds);

        // Set border color
        if (_borderColor.HasValue)
            panelContext.SetForeground(_borderColor.Value);

        // Draw border
        // Top
        panelContext.WriteAt(0, 0, border.TopLeft.ToString());
        panelContext.WriteAt(1, 0, new string(border.Horizontal, bounds.Width - 2));
        panelContext.WriteAt(bounds.Width - 1, 0, border.TopRight.ToString());

        // Sides and fill interior
        for (var row = 1; row < bounds.Height - 1; row++)
        {
            panelContext.WriteAt(0, row, border.Vertical.ToString());
            // Clear interior
            panelContext.ResetColors();
            panelContext.WriteAt(1, row, new string(' ', bounds.Width - 2));
            if (_borderColor.HasValue)
                panelContext.SetForeground(_borderColor.Value);
            panelContext.WriteAt(bounds.Width - 1, row, border.Vertical.ToString());
        }

        // Bottom
        panelContext.WriteAt(0, bounds.Height - 1, border.BottomLeft.ToString());
        panelContext.WriteAt(1, bounds.Height - 1, new string(border.Horizontal, bounds.Width - 2));
        panelContext.WriteAt(bounds.Width - 1, bounds.Height - 1, border.BottomRight.ToString());

        // Draw title if present
        if (!string.IsNullOrEmpty(_title))
        {
            var maxTitleLen = bounds.Width - 4;
            var displayTitle = _title.Length > maxTitleLen ? _title[..maxTitleLen] : _title;
            var titleText = $" {displayTitle} ";
            var titleX = 2;

            if (_titleColor.HasValue)
                panelContext.SetForeground(_titleColor.Value);
            else
                panelContext.ResetColors();

            panelContext.WriteAt(titleX, 0, titleText);
        }

        panelContext.ResetColors();

        // Render content
        if (_content != null)
        {
            var contentBounds = new Rect(
                1 + _padding,
                1 + _padding,
                bounds.Width - 2 - (_padding * 2),
                bounds.Height - 2 - (_padding * 2)
            );

            if (contentBounds.HasArea)
            {
                var contentContext = panelContext.CreateSubContext(contentBounds);
                var innerBounds = new Rect(0, 0, contentBounds.Width, contentBounds.Height);
                _content.Render(contentContext, innerBounds);
            }
        }
    }

    private void Invalidate()
    {
        if (!_disposed)
            _invalidated.OnNext(Unit.Default);
    }

    /// <summary>
    /// Called when the node becomes active (page navigated to).
    /// Propagates activation to the content if it's a LayoutNode.
    /// </summary>
    public void OnActivate()
    {
        // Activate content if it's a LayoutNode
        if (_content is LayoutNode contentNode)
        {
            contentNode.OnActivate();
        }
    }

    /// <summary>
    /// Called when the node becomes inactive (navigating away from page).
    /// Propagates deactivation to the content if it's a LayoutNode.
    /// </summary>
    public void OnDeactivate()
    {
        // Deactivate content if it's a LayoutNode
        if (_content is LayoutNode contentNode)
        {
            contentNode.OnDeactivate();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _invalidated.OnCompleted();
        _invalidated.Dispose();
        _dismissed.OnCompleted();
        _dismissed.Dispose();
        _content?.Dispose();
    }
}
