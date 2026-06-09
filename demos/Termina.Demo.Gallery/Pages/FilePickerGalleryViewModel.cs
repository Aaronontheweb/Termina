// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Reactive;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// ViewModel for the File Picker gallery page.
/// </summary>
public class FilePickerGalleryViewModel : ReactiveViewModel
{
    public ReactiveProperty<string> StatusMessage { get; } = new("Navigate directories, select files or folders");

    public void OnFileSelected(IReadOnlyList<string> paths)
    {
        StatusMessage.Value = paths.Count == 1
            ? $"File selected: {paths[0]}"
            : $"Selected {paths.Count} files: {string.Join(", ", paths.Select(Path.GetFileName))}";
    }

    public void OnFolderSelected(IReadOnlyList<string> paths)
    {
        StatusMessage.Value = paths.Count == 1
            ? $"Folder selected: {paths[0]}"
            : $"Selected {paths.Count} folders: {string.Join(", ", paths.Select(Path.GetFileName))}";
    }

    public void OnFocusChanged(int pickerIndex)
    {
        var name = pickerIndex == 0 ? "File Picker" : "Folder Picker";
        StatusMessage.Value = $"Focus: {name} — Use Tab to switch";
    }

    public override void Dispose()
    {
        StatusMessage.Dispose();
        base.Dispose();
    }
}
