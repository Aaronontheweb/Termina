// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Layout;

/// <summary>
/// Configuration options for the visual scrollbar drawn alongside scrollable content.
/// </summary>
/// <param name="TrackChar">
/// Character used for the scrollbar track (the non-thumb area).
/// Defaults to '░' (light shade).
/// </param>
/// <param name="ThumbChar">
/// Character used for the scrollbar thumb (the draggable indicator).
/// Defaults to '█' (full block).
/// </param>
/// <param name="TrackColor">
/// Foreground color for the track character.
/// Defaults to <see cref="Color.BrightBlack"/> when <see langword="null"/>.
/// </param>
/// <param name="ThumbColor">
/// Foreground color for the thumb character.
/// Defaults to <see cref="Color.White"/> when <see langword="null"/>.
/// </param>
/// <param name="AutoHide">
/// When <see langword="true"/> (default), the scrollbar is hidden if all content fits
/// within the viewport. When <see langword="false"/>, the scrollbar is always drawn.
/// </param>
public sealed record ScrollbarOptions(
    char TrackChar = '░',
    char ThumbChar = '█',
    Color? TrackColor = null,
    Color? ThumbColor = null,
    bool AutoHide = true);
