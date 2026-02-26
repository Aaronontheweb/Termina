// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Cancellable event args fired before a wizard advances to the next step.
/// Set <see cref="Cancel"/> to true to prevent navigation.
/// </summary>
public sealed class WizardBeforeAdvanceArgs<TStep> where TStep : struct, Enum
{
    /// <summary>
    /// The step being advanced from.
    /// </summary>
    public TStep CurrentStep { get; }

    /// <summary>
    /// The step that will be navigated to.
    /// </summary>
    public TStep NextStep { get; }

    /// <summary>
    /// Set to true to cancel the advance.
    /// </summary>
    public bool Cancel { get; set; }

    public WizardBeforeAdvanceArgs(TStep currentStep, TStep nextStep)
    {
        CurrentStep = currentStep;
        NextStep = nextStep;
    }
}
