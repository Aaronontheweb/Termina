// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;

namespace Termina.Tests.Input;

public class TerminalModeContextTests
{
    [Fact]
    public void Default_HasNoModeFlagsEnabled()
    {
        Assert.False(TerminalModeContext.Default.KittyReportAllKeysVisible);
    }
}
