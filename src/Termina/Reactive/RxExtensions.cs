using System.Reactive.Disposables;

namespace Termina.Reactive;

/// <summary>
/// Extension methods for working with System.Reactive in Termina.
/// </summary>
public static class RxExtensions
{
    /// <summary>
    /// Adds a disposable to a CompositeDisposable and returns the disposable.
    /// Enables fluent subscription patterns.
    /// </summary>
    /// <typeparam name="T">The disposable type.</typeparam>
    /// <param name="disposable">The disposable to add.</param>
    /// <param name="composite">The composite to add it to.</param>
    /// <returns>The original disposable for chaining.</returns>
    /// <example>
    /// <code>
    /// observable
    ///     .Subscribe(x => HandleValue(x))
    ///     .DisposeWith(Subscriptions);
    /// </code>
    /// </example>
    public static T DisposeWith<T>(this T disposable, CompositeDisposable composite)
        where T : IDisposable
    {
        composite.Add(disposable);
        return disposable;
    }
}
