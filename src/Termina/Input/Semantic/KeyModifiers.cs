// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Canonical keyboard modifier flags used by the internal semantic input pipeline.
/// </summary>
[Flags]
internal enum KeyModifiers
{
    None = 0,
    Shift = 1 << 0,
    Alt = 1 << 1,
    Control = 1 << 2,
    Super = 1 << 3,
    Hyper = 1 << 4,
    Meta = 1 << 5,
}

internal static class KeyModifiersExtensions
{
    public static KeyModifiers ToTerminaKeyModifiers(this ConsoleModifiers modifiers)
    {
        var result = KeyModifiers.None;
        if (modifiers.HasFlag(ConsoleModifiers.Shift)) result |= KeyModifiers.Shift;
        if (modifiers.HasFlag(ConsoleModifiers.Alt)) result |= KeyModifiers.Alt;
        if (modifiers.HasFlag(ConsoleModifiers.Control)) result |= KeyModifiers.Control;
        return result;
    }

    public static ConsoleModifiers ToConsoleModifiers(this KeyModifiers modifiers)
    {
        var result = (ConsoleModifiers)0;
        if (modifiers.HasFlag(KeyModifiers.Shift)) result |= ConsoleModifiers.Shift;
        if (modifiers.HasFlag(KeyModifiers.Alt)) result |= ConsoleModifiers.Alt;
        if (modifiers.HasFlag(KeyModifiers.Control)) result |= ConsoleModifiers.Control;
        return result;
    }
}
