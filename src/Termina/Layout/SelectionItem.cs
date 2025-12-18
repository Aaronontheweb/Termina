// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive;
using Termina.Components.Streaming;

namespace Termina.Layout;

/// <summary>
/// Represents an item in a SelectionListNode.
/// </summary>
/// <typeparam name="T">The type of the underlying value.</typeparam>
public sealed class SelectionItem<T> : IDisposable
{
    private readonly SelectionItemContent _content;
    private IDisposable? _animationSubscription;
    private bool _disposed;

    /// <summary>
    /// Creates a new selection item with plain text display.
    /// </summary>
    /// <param name="value">The underlying value.</param>
    /// <param name="displayText">The text to display for this item.</param>
    /// <param name="isOther">Whether this is the "Other..." option.</param>
    public SelectionItem(T value, string displayText, bool isOther = false)
    {
        Value = value;
        IsOther = isOther;
        _content = SelectionItemContent.FromString(displayText);
    }

    /// <summary>
    /// Creates a new selection item with rich content display.
    /// </summary>
    /// <param name="value">The underlying value.</param>
    /// <param name="content">The rich content to display for this item.</param>
    /// <param name="isOther">Whether this is the "Other..." option.</param>
    public SelectionItem(T value, SelectionItemContent content, bool isOther = false)
    {
        Value = value;
        IsOther = isOther;
        _content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <summary>
    /// The underlying value for this item.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// The text displayed for this item (first line, plain text).
    /// For full content with multiple lines and styling, use <see cref="Content"/>.
    /// </summary>
    public string DisplayText => _content.GetFirstLineText();

    /// <summary>
    /// The rich content for this item, supporting multiple lines and styled segments.
    /// </summary>
    public SelectionItemContent Content => _content;

    /// <summary>
    /// Gets the number of lines this item occupies.
    /// </summary>
    public int LineCount => _content.LineCount;

    /// <summary>
    /// Whether this item is selected.
    /// </summary>
    public bool IsSelected { get; internal set; }

    /// <summary>
    /// Whether this item represents the "Other..." option for custom input.
    /// </summary>
    public bool IsOther { get; }

    /// <summary>
    /// Subscribes to animation invalidation events from this item's content.
    /// </summary>
    /// <param name="onInvalidated">The action to invoke when content changes.</param>
    internal void SubscribeToAnimations(Action<Unit> onInvalidated)
    {
        if (_content.HasAnimations && _animationSubscription == null)
        {
            _animationSubscription = _content.Invalidated.Subscribe(onInvalidated);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _animationSubscription?.Dispose();
        _content.Dispose();
    }
}
