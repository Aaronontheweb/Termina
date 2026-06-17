// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Notifications;
using Termina.Reactive;
using Termina.Terminal;

namespace Termina.Demo.Gallery.Pages;

public class ToastGalleryViewModel : ReactiveViewModel
{
    public ReactiveProperty<string> StatusMessage { get; } = new("Press a key to trigger a toast");

    public IReadOnlyList<ToastPreset> Presets { get; } = new List<ToastPreset>
    {
        new("Success", "Operation completed", Color.BrightGreen, "✓", ToastPosition.TopRight),
        new("Error", "Something went wrong", Color.BrightRed, "✗", ToastPosition.TopRight),
        new("Warning", "Check your input", Color.BrightYellow, "⚠", ToastPosition.TopCenter),
        new("Info", "3 items updated", Color.BrightCyan, "ℹ", ToastPosition.BottomRight),
        new("Custom", "Deployed to production", Color.Magenta, "🚀", ToastPosition.BottomCenter),
        new("Default", "Plain toast (no color/icon override)", null, null, ToastPosition.BottomRight)
    };

    public override void Dispose()
    {
        StatusMessage.Dispose();
        base.Dispose();
    }
}

public record ToastPreset(string Name, string Message, Color? Color, string? Icon, ToastPosition Position);
