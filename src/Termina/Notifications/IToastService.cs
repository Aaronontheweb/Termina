using R3;

namespace Termina.Notifications;

/// <summary>
/// Shows transient toast notifications.
/// </summary>
public interface IToastService
{
    /// <summary>
    /// Current toast, if any.
    /// </summary>
    Observable<ToastMessage?> CurrentToast { get; }

    /// <summary>
    /// Show a toast for the configured duration.
    /// </summary>
    void Show(string message, ToastOptions? options = null);
}
