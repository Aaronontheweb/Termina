// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Phase of a semantic key event.
/// </summary>
internal enum KeyEventPhase
{
    Press,
    Repeat,
    Release,
}
