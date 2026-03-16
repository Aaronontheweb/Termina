// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Reactive;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// ViewModel for the clipboard gallery page.
/// </summary>
public sealed class ClipboardGalleryViewModel : ReactiveViewModel
{
    private readonly TraceFileInfo _traceFileInfo;

    public ClipboardGalleryViewModel(TraceFileInfo traceFileInfo)
    {
        _traceFileInfo = traceFileInfo;
    }

    public ReactiveProperty<string> StatusMessage { get; } = new("Try copying one of the values or paste into the input field.");

    public string TraceFilePath => _traceFileInfo.FilePath;

    public void SetStatus(string status)
    {
        StatusMessage.Value = status;
    }

    public override void Dispose()
    {
        StatusMessage.Dispose();
        base.Dispose();
    }
}
