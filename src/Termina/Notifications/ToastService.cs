using R3;

namespace Termina.Notifications;

public sealed class ToastService : IToastService, IDisposable
{
    private readonly TimeProvider _timeProvider;
    private readonly ReactiveProperty<ToastMessage?> _currentToast = new(null);
    private IDisposable? _dismissSubscription;
    private bool _disposed;

    public ToastService(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Observable<ToastMessage?> CurrentToast => _currentToast;

    public void Show(string message, TimeSpan? duration = null)
    {
        if (_disposed)
            return;

        _currentToast.Value = new ToastMessage(message);

        _dismissSubscription?.Dispose();
        _dismissSubscription = Observable
            .Interval(duration ?? TimeSpan.FromSeconds(2), _timeProvider)
            .Take(1)
            .Subscribe(_ => _currentToast.Value = null);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _dismissSubscription?.Dispose();
        _currentToast.Dispose();
    }
}
