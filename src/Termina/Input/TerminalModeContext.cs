// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Captures terminal mode state needed to disambiguate input sequences.
/// </summary>
internal readonly record struct TerminalModeContext(bool KittyReportAllKeysVisible, bool DeckmConfirmed = false)
{
    public static TerminalModeContext Default { get; } = new(false);
}
