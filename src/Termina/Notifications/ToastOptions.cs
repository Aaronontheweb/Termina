using Termina.Terminal;

namespace Termina.Notifications;

/// <summary>
/// Optional display settings for a toast notification.
/// </summary>
public sealed record ToastOptions(
    TimeSpan? Duration = null,
    ToastPosition Position = ToastPosition.BottomRight,
    Color? Color = null,
    string? Icon = null);
