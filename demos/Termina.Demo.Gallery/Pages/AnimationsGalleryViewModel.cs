// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Input;
using Termina.Reactive;
using Termina.Terminal;
using LayoutSpinnerStyle = Termina.Layout.SpinnerStyle;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// ViewModel for the Animations gallery page.
/// </summary>
public partial class AnimationsGalleryViewModel : ReactiveViewModel
{
    [Reactive] private Termina.Layout.SpinnerStyle _selectedStyle = Termina.Layout.SpinnerStyle.Dots;

    public IReadOnlyList<SpinnerStyleItem> SpinnerStyles { get; } = new List<SpinnerStyleItem>
    {
        new("Dots", LayoutSpinnerStyle.Dots, Color.Blue, "Braille dots pattern"),
        new("Line", LayoutSpinnerStyle.Line, Color.Green, "Classic line spinner"),
        new("Arrow", LayoutSpinnerStyle.Arrow, Color.Yellow, "Rotating arrow"),
        new("Bounce", LayoutSpinnerStyle.Bounce, Color.Magenta, "Bouncing dot"),
        new("Box", LayoutSpinnerStyle.Box, Color.Cyan, "Rotating box corners"),
        new("Circle", LayoutSpinnerStyle.Circle, Color.Red, "Rotating circle")
    };

    public override void OnActivated()
    {
        Input.OfType<KeyPressed>()
            .Where(k => k.KeyInfo.Key == ConsoleKey.Escape)
            .Subscribe(_ => Navigate("/menu"))
            .DisposeWith(Subscriptions);
    }
}
