// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Represents a file or directory entry in a <see cref="FilePickerNode"/>.
/// </summary>
public sealed record FileSystemEntry(
    string Name,
    string FullPath,
    bool IsDirectory,
    long? Size = null,
    DateTimeOffset? LastModified = null);
