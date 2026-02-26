// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Visual style for the wizard progress indicator.
/// </summary>
public enum WizardProgressStyle
{
    /// <summary>
    /// Block bar: [====>    ] Step 2 of 4
    /// </summary>
    BlockBar,

    /// <summary>
    /// Arrow chain: Step1 > Step2 > Step3
    /// </summary>
    Arrow,

    /// <summary>
    /// Dot indicators: [*] [*] [ ] [ ]
    /// </summary>
    Dots,

    /// <summary>
    /// No progress indicator shown.
    /// </summary>
    None
}
