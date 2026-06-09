// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Reactive;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// ViewModel for the gallery menu.
/// </summary>
public partial class GalleryMenuViewModel : ReactiveViewModel
{
    public IReadOnlyList<GalleryMenuItem> MenuItems { get; } = new List<GalleryMenuItem>
    {
        new("Selection Lists", "Single/multi-select, numbered, Other option, rich content", "/selection"),
        new("Text Input", "Text fields, placeholder text, submission handling", "/textinput"),
        new("Clipboard", "OSC 52 copy, toasts, and paste validation", "/clipboard"),
        new("Layouts", "Vertical, horizontal, grid, panels, borders", "/layouts"),
        new("Animations", "Spinners, streaming text, progress indicators", "/animations"),
        new("File Picker", "File/folder selection, directory navigation, fuzzy filtering", "/filepicker")
    };

    public void NavigateToGallery(string route)
    {
        Navigate(route);
    }
}
