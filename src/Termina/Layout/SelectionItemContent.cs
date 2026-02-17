// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Components.Streaming;
using Termina.Terminal;

namespace Termina.Layout;

/// <summary>
/// Represents the display content for a selection list item, supporting multiple lines
/// and multiple styled/animated segments per line.
/// </summary>
/// <remarks>
/// Example usage:
/// <code>
/// var content = new SelectionItemContent()
///     .AddLine(new StaticTextSegment("Server Name", Color.White, decoration: TextDecoration.Bold))
///     .AddLine(
///         new StaticTextSegment("   "),
///         new SpinnerSegment(SpinnerStyle.Dots, Color.Blue),
///         new StaticTextSegment(" Connecting...")
///     );
/// </code>
/// </remarks>
public sealed class SelectionItemContent : IDisposable
{
    private readonly List<IReadOnlyList<ITextSegment>> _lines = new();
    private readonly Subject<Unit> _invalidated = new();
    private readonly CompositeDisposable _subscriptions = new();
    private bool _disposed;

    /// <summary>
    /// Gets all lines of content. Each line is a list of segments.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<ITextSegment>> Lines => _lines.AsReadOnly();

    /// <summary>
    /// Gets the number of lines in this content.
    /// </summary>
    public int LineCount => _lines.Count;

    /// <summary>
    /// Observable that fires when any animated segment in this content changes.
    /// </summary>
    public Observable<Unit> Invalidated => _invalidated.AsObservable();

    /// <summary>
    /// Gets whether this content contains any animated segments.
    /// </summary>
    public bool HasAnimations => _subscriptions.Count > 0;

    /// <summary>
    /// Adds a line with the specified segments.
    /// </summary>
    /// <param name="segments">The segments that make up this line.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public SelectionItemContent AddLine(params ITextSegment[] segments)
    {
        return AddLine((IEnumerable<ITextSegment>)segments);
    }

    /// <summary>
    /// Adds a line with the specified segments.
    /// </summary>
    /// <param name="segments">The segments that make up this line.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public SelectionItemContent AddLine(IEnumerable<ITextSegment> segments)
    {
        var lineSegments = segments.ToList();
        _lines.Add(lineSegments.AsReadOnly());

        // Subscribe to any animated segments
        foreach (var segment in lineSegments)
        {
            SubscribeToAnimations(segment);
        }

        return this;
    }

    /// <summary>
    /// Adds a simple text line with optional styling.
    /// </summary>
    /// <param name="text">The text for this line.</param>
    /// <param name="foreground">Optional foreground color.</param>
    /// <param name="background">Optional background color.</param>
    /// <param name="decoration">Optional text decoration.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public SelectionItemContent AddLine(string text, Color? foreground = null, Color? background = null,
        TextDecoration decoration = TextDecoration.None)
    {
        return AddLine(new StaticTextSegment(text, foreground, background, decoration));
    }

    /// <summary>
    /// Gets the plain text representation of this content (all lines joined with newlines).
    /// </summary>
    public string ToPlainText()
    {
        return string.Join("\n", _lines.Select(line =>
            string.Concat(line.Select(s => s.GetCurrentSegment().Text))));
    }

    /// <summary>
    /// Gets the plain text of the first line (for display in single-line contexts).
    /// </summary>
    public string GetFirstLineText()
    {
        if (_lines.Count == 0) return string.Empty;
        return string.Concat(_lines[0].Select(s => s.GetCurrentSegment().Text));
    }

    private void SubscribeToAnimations(ITextSegment segment)
    {
        if (segment is IAnimatedTextSegment animated)
        {
            var subscription = animated.Invalidated.Subscribe(_ =>
            {
                if (!_disposed)
                {
                    _invalidated.OnNext(Unit.Default);
                }
            });
            _subscriptions.Add(subscription);
        }

        // Also check composite segments for nested animations
        if (segment is ICompositeTextSegment composite)
        {
            foreach (var child in composite.Children)
            {
                SubscribeToAnimations(child);
            }
        }
    }

    /// <summary>
    /// Creates a simple single-line content from a plain string.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <returns>A new SelectionItemContent with one line.</returns>
    public static SelectionItemContent FromString(string text)
    {
        return new SelectionItemContent().AddLine(text);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _subscriptions.Dispose();
        _invalidated.OnCompleted();
        _invalidated.Dispose();

        // Dispose all segments
        foreach (var line in _lines)
        {
            foreach (var segment in line)
            {
                segment.Dispose();
            }
        }
    }
}
