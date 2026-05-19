// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Platform;

/// <summary>
/// Snapshot of negotiated terminal capabilities the platform console pushed during initialization.
/// </summary>
/// <remarks>
/// Surfaced via <see cref="IPlatformConsole.Capabilities"/> and consumed by input plumbing
/// (e.g. <see cref="Input.PlatformInputSource"/> uses it to wire
/// <see cref="Input.EscapeSequenceParser.KittyKeyboardActive"/>). New capability bits should
/// be additive — a zero-value <see cref="TerminalCapabilities"/> represents a console that
/// performed no negotiation.
/// </remarks>
/// <param name="KittyKeyboardActive">
/// True when the platform console has pushed the kitty keyboard protocol enhancement
/// (<c>CSI &gt; N u</c>). Causes real keyboard events to arrive as canonical kitty
/// CSI-u / second-form sequences, making them byte-level distinct from <c>?1007h</c>
/// wheel events that continue to arrive as legacy <c>CSI A/B</c> or <c>SS3 OA/OB</c>.
/// </param>
public readonly record struct TerminalCapabilities(bool KittyKeyboardActive);
