// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// ViewModel for the Animations gallery page.
/// </summary>
public partial class AnimationsGalleryViewModel : ReactiveViewModel
{
    public override void OnActivated()
    {
        Input.OfType<KeyPressed>()
            .Where(k => k.KeyInfo.Key == ConsoleKey.Escape)
            .Subscribe(_ => Navigate("/menu"))
            .DisposeWith(Subscriptions);
    }
}
