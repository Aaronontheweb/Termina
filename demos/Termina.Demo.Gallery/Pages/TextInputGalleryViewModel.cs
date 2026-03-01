// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Reactive;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// ViewModel for the TextInput gallery page.
/// </summary>
public class TextInputGalleryViewModel : ReactiveViewModel
{
    public ReactiveProperty<string> StatusMessage { get; } = new("Type in the input fields and press Enter to submit");

    public void OnBasicInputSubmitted(string text)
    {
        StatusMessage.Value = $"Basic input submitted: \"{text}\"";
    }

    public void OnPlaceholderInputSubmitted(string text)
    {
        StatusMessage.Value = $"Placeholder input submitted: \"{text}\"";
    }

    public void OnTextAreaSubmitted(string text)
    {
        var lineCount = text.Split('\n').Length;
        StatusMessage.Value = $"TextArea submitted: {lineCount} line(s), {text.Length} chars";
    }

    public override void Dispose()
    {
        StatusMessage.Dispose();
        base.Dispose();
    }
}
