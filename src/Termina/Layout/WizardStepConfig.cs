// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Configuration for a single wizard step.
/// </summary>
public sealed class WizardStepConfig<TStep> where TStep : struct, Enum
{
    /// <summary>
    /// The enum value identifying this step.
    /// </summary>
    public TStep Step { get; }

    /// <summary>
    /// Display name for the step (shown in progress bar).
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Factory that builds the content layout for this step.
    /// </summary>
    public Func<ILayoutNode> ContentFactory { get; }

    /// <summary>
    /// Optional help text displayed below the content area.
    /// </summary>
    public string? HelpText { get; }

    /// <summary>
    /// Number of sub-steps within this step (1 = no sub-steps).
    /// </summary>
    public int SubStepCount { get; }

    public WizardStepConfig(TStep step, string displayName, Func<ILayoutNode> contentFactory,
        string? helpText = null, int subStepCount = 1)
    {
        Step = step;
        DisplayName = displayName;
        ContentFactory = contentFactory;
        HelpText = helpText;
        SubStepCount = Math.Max(1, subStepCount);
    }
}
