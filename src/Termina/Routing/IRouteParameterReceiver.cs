namespace Termina.Routing;

/// <summary>
/// Interface implemented by source-generated code for ViewModels with <see cref="FromRouteAttribute"/> fields.
/// </summary>
/// <remarks>
/// <para>
/// This interface is implemented automatically by the source generator when a ViewModel
/// has fields marked with <see cref="FromRouteAttribute"/>.
/// </para>
/// <para>
/// The framework calls <see cref="SetRouteParameters"/> after creating the ViewModel
/// but before calling <see cref="Termina.Reactive.ReactiveViewModel.OnActivated"/>.
/// </para>
/// </remarks>
public interface IRouteParameterReceiver
{
    /// <summary>
    /// Sets the route parameter values on the ViewModel.
    /// </summary>
    /// <param name="parameters">The parameters extracted from the matched route.</param>
    void SetRouteParameters(IReadOnlyDictionary<string, object> parameters);
}
