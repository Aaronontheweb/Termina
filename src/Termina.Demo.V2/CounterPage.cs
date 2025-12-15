// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Extensions;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;

namespace Termina.Demo.V2;

/// <summary>
/// Page for the counter demo.
/// Demonstrates reactive page binding with v2 rendering infrastructure.
/// </summary>
public class CounterPage : ReactivePageBase<CounterViewModel>
{
    public override IEnumerable<Region> GetRegions()
    {
        // Header panel at the top
        yield return Region.TopRow("header", 3);

        // Counter display in the middle
        yield return new Region("counter",
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(3),
            new LayoutConstraint.Remaining(),
            new LayoutConstraint.Fixed(3));

        // Input region
        yield return new Region("input",
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(6),
            new LayoutConstraint.Remaining(),
            new LayoutConstraint.Fixed(3));

        // Messages area
        yield return new Region("messages",
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(9),
            new LayoutConstraint.Remaining(),
            new LayoutConstraint.Fixed(10));

        // Status bar at the bottom
        yield return Region.BottomRow("status", 1);
    }

    protected override void OnBound(RenderCoordinator coordinator)
    {
        // Header - static content
        coordinator.RenderRegion("header", new Panel
        {
            Title = "Termina v2 Demo",
            Border = BorderStyle.Double,
            Content = new Text("Reactive Region-Based Rendering")
        });

        // Counter display - bound to CountChanged observable
        ViewModel.CountChanged
            .Select(count => new Panel
            {
                Title = "Counter",
                Border = BorderStyle.Single,
                Content = new Text($"Count: {count}")
            })
            .RenderTo(coordinator, "counter")
            .DisposeWith(Subscriptions);

        // Input field - bound to InputTextChanged observable
        ViewModel.InputTextChanged
            .Select(text => new Panel
            {
                Title = "Input",
                Border = BorderStyle.Single,
                Content = new Text($"> {text}_")
            })
            .RenderTo(coordinator, "input")
            .DisposeWith(Subscriptions);

        // Messages list - bound to MessagesChanged observable
        ViewModel.MessagesChanged
            .Select(messages => new Panel
            {
                Title = "Messages",
                Border = BorderStyle.Single,
                Content = new Text(messages.Count > 0
                    ? string.Join("\n", messages)
                    : "(no messages yet)")
            })
            .RenderTo(coordinator, "messages")
            .DisposeWith(Subscriptions);

        // Status bar - bound to StatusMessageChanged observable
        ViewModel.StatusMessageChanged
            .Select(status => new Text(status))
            .RenderTo(coordinator, "status")
            .DisposeWith(Subscriptions);
    }
}
