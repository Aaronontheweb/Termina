// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Represents an item in a SelectionListNode.
/// </summary>
/// <typeparam name="T">The type of the underlying value.</typeparam>
public sealed class SelectionItem<T>
{
    /// <summary>
    /// Creates a new selection item.
    /// </summary>
    /// <param name="value">The underlying value.</param>
    /// <param name="displayText">The text to display for this item.</param>
    /// <param name="isOther">Whether this is the "Other..." option.</param>
    public SelectionItem(T value, string displayText, bool isOther = false)
    {
        Value = value;
        DisplayText = displayText;
        IsOther = isOther;
    }

    /// <summary>
    /// The underlying value for this item.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// The text displayed for this item.
    /// </summary>
    public string DisplayText { get; }

    /// <summary>
    /// Whether this item is selected.
    /// </summary>
    public bool IsSelected { get; internal set; }

    /// <summary>
    /// Whether this item represents the "Other..." option for custom input.
    /// </summary>
    public bool IsOther { get; }
}
