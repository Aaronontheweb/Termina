namespace Termina.Notifications;

/// <summary>
/// A transient toast message.
/// </summary>
public sealed record ToastMessage(string Message, ToastPosition Position = ToastPosition.BottomRight);
