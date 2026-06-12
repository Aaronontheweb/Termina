// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Layout;
using Termina.Reactive;

namespace Termina.Demo.Gallery.Pages;

public class GraphGalleryViewModel : ReactiveViewModel
{
    public ReactiveProperty<GraphStyle> SelectedStyle { get; } = new(GraphStyle.Blocks);

    public IReadOnlyList<GraphStyleItem> GraphStyles { get; } = new List<GraphStyleItem>
    {
        new("Blocks", GraphStyle.Blocks, "▁▂▃▄▅▆▇█ filled columns"),
        new("Outline", GraphStyle.Outline, "Only the top edge is drawn"),
        new("Braille", GraphStyle.Braille, "Double vertical resolution with braille dots"),
        new("ASCII", GraphStyle.Ascii, "_ . - ~ ^ * # @ terminal fallback")
    };

    public override void Dispose()
    {
        SelectedStyle.Dispose();
        base.Dispose();
    }
}

public record GraphStyleItem(string Name, GraphStyle Style, string Description);
