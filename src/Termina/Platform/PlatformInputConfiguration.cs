// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Platform;

/// <summary>
/// Snapshot of platform input transport properties that influence parser configuration.
/// </summary>
public readonly record struct PlatformInputConfiguration(bool RawInputActive, bool KittyReportAllKeysVisible);
