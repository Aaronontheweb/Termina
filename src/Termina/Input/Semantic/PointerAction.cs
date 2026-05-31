// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Canonical pointer action used by the internal semantic input pipeline.
/// </summary>
internal enum PointerAction
{
    Press,
    Release,
    Drag,
    Move,
    Wheel,
}
