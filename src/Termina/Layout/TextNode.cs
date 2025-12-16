// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Components.Streaming;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Layout;

/// <summary>
/// A layout node that renders text.
/// </summary>
public sealed class TextNode : LayoutNode
{
    private readonly string[] _lines;

    /// <summary>
    /// The text content.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Foreground color.
    /// </summary>
    public Color? Foreground { get; private set; }

    /// <summary>
    /// Background color.
    /// </summary>
    public Color? Background { get; private set; }

    /// <summary>
    /// Whether text is bold.
    /// </summary>
    public bool IsBold { get; private set; }

    /// <summary>
    /// Whether text is italic.
    /// </summary>
    public bool IsItalic { get; private set; }

    /// <summary>
    /// Whether text is underlined.
    /// </summary>
    public bool IsUnderline { get; private set; }

    /// <summary>
    /// Whether text should wrap to multiple lines when it exceeds the available width.
    /// Default is true.
    /// </summary>
    public bool WordWrap { get; private set; } = true;

    public TextNode(string content)
    {
        Content = content ?? "";
        _lines = Content.Split('\n');

        // Default to auto height based on content
        HeightConstraint = new SizeConstraint.Auto();
        WidthConstraint = new SizeConstraint.Fill();
    }

    /// <summary>
    /// Set foreground color.
    /// </summary>
    public TextNode WithForeground(Color color)
    {
        Foreground = color;
        return this;
    }

    /// <summary>
    /// Set background color.
    /// </summary>
    public TextNode WithBackground(Color color)
    {
        Background = color;
        return this;
    }

    /// <summary>
    /// Make text bold.
    /// </summary>
    public TextNode Bold()
    {
        IsBold = true;
        return this;
    }

    /// <summary>
    /// Make text italic.
    /// </summary>
    public TextNode Italic()
    {
        IsItalic = true;
        return this;
    }

    /// <summary>
    /// Make text underlined.
    /// </summary>
    public TextNode Underline()
    {
        IsUnderline = true;
        return this;
    }

    /// <summary>
    /// Disable word wrapping (text will be truncated instead of wrapped).
    /// </summary>
    public TextNode NoWrap()
    {
        WordWrap = false;
        return this;
    }

    /// <inheritdoc />
    public override Size Measure(Size available)
    {
        var maxLineWidth = _lines.Max(l => l.Length);
        var width = WidthConstraint.Compute(available.Width, maxLineWidth, available.Width);

        // Calculate height based on whether word wrap is enabled
        int height;
        if (WordWrap && width > 0)
        {
            // Calculate total wrapped line count
            height = WordWrapper.CalculateTotalWrappedLineCount(_lines, width);
        }
        else
        {
            height = _lines.Length;
        }

        var measuredHeight = HeightConstraint.Compute(available.Height, height, available.Height);

        return new Size(width, measuredHeight);
    }

    /// <inheritdoc />
    public override void Render(IRenderContext context, Rect bounds)
    {
        if (!bounds.HasArea)
            return;

        // Create a sub-context for this node's bounds so all coordinates are relative
        var textContext = context.CreateSubContext(bounds);

        // Apply colors
        if (Foreground.HasValue)
            textContext.SetForeground(Foreground.Value);
        if (Background.HasValue)
            textContext.SetBackground(Background.Value);

        // Get lines to render (wrapped or original)
        var linesToRender = WordWrap && bounds.Width > 0
            ? WordWrapper.WrapLines(_lines, bounds.Width)
            : _lines.ToList();

        // Render each line
        for (var i = 0; i < linesToRender.Count && i < bounds.Height; i++)
        {
            var line = linesToRender[i];
            // Truncate if still too long (shouldn't happen with wrapping, but safety check)
            var displayLine = line.Length > bounds.Width
                ? line[..bounds.Width]
                : line;

            textContext.WriteAt(0, i, displayLine);
        }

        // Reset colors
        if (Foreground.HasValue || Background.HasValue)
            textContext.ResetColors();
    }
}

