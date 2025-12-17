// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Unique identifier for a tracked segment in a streaming text buffer.
/// Uses lightweight integer instead of GUID for performance during rapid segment insertion.
/// </summary>
public readonly record struct SegmentId(int Value)
{
    /// <summary>
    /// Represents an invalid or unassigned segment ID.
    /// </summary>
    public static readonly SegmentId None = new(0);
}
