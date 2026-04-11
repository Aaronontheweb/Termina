// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Represents the current text selection state in a layout node.
/// </summary>
/// <param name="StartRow">The starting row (0-based) of the selection.</param>
/// <param name="StartCol">The starting column (0-based) of the selection.</param>
/// <param name="EndRow">The ending row (0-based) of the selection.</param>
/// <param name="EndCol">The ending column (0-based) of the selection.</param>
public readonly record struct TextSelectionState(
    int StartRow,
    int StartCol,
    int EndRow,
    int EndCol)
{
    /// <summary>
    /// Empty selection state indicating no active selection.
    /// </summary>
    public static TextSelectionState None => new(-1, -1, -1, -1);

    /// <summary>
    /// Returns true if this selection is active (has valid start and end positions).
    /// </summary>
    public bool IsActive => StartRow >= 0 && EndRow >= 0;

    /// <summary>
    /// Returns true if this selection contains any characters.
    /// </summary>
    public bool HasContent => IsActive && (StartRow != EndRow || StartCol != EndCol);

    /// <summary>
    /// Normalizes the selection so that Start is always before End.
    /// </summary>
    public TextSelectionState Normalized
    {
        get
        {
            if (!IsActive) return None;

            if (StartRow < EndRow || (StartRow == EndRow && StartCol <= EndCol))
                return this;

            return new TextSelectionState(EndRow, EndCol, StartRow, StartCol);
        }
    }
}
