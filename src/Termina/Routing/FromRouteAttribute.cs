namespace Termina.Routing;

/// <summary>
/// Marks a field to receive a route parameter value.
/// </summary>
/// <remarks>
/// <para>
/// Fields marked with this attribute will have their values injected by the framework
/// before <see cref="Termina.Reactive.ReactiveViewModel.OnActivated"/> is called.
/// </para>
/// <para>
/// The source generator will implement <see cref="IRouteParameterReceiver"/> for ViewModels
/// containing fields with this attribute.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public partial class TaskDetailViewModel : ReactiveViewModel
/// {
///     [FromRoute] private int _taskId;
///     // Or with explicit name:
///     [FromRoute(Name = "id")] private int _taskId;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field)]
public sealed class FromRouteAttribute : Attribute
{
    /// <summary>
    /// The route parameter name. If null, the field name (without underscore prefix) is used.
    /// </summary>
    public string? Name { get; set; }
}
