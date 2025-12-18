// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Reactive;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// ViewModel for the TextInput gallery page.
/// </summary>
public partial class TextInputGalleryViewModel : ReactiveViewModel
{
    [Reactive] private string _statusMessage = "Type in the input fields and press Enter to submit";

    public void OnBasicInputSubmitted(string text)
    {
        StatusMessage = $"Basic input submitted: \"{text}\"";
    }

    public void OnPlaceholderInputSubmitted(string text)
    {
        StatusMessage = $"Placeholder input submitted: \"{text}\"";
    }
}
