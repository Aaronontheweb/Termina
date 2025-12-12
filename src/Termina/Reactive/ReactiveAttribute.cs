using System.Diagnostics;

namespace Termina.Reactive;

/// <summary>
/// <b>Source Generator Attribute:</b> Marks a field to generate a reactive property with BehaviorSubject backing.
/// </summary>
/// <remarks>
/// <para>
/// <b>This attribute is processed by the Termina.Generators source generator at compile time.</b>
/// The containing class MUST be declared as <c>partial</c> for code generation to work.
/// </para>
/// <para>
/// The source generator will create:
/// </para>
/// <list type="bullet">
///   <item>A <see cref="System.Reactive.Subjects.BehaviorSubject{T}"/> backing field (<c>_fieldNameSubject</c>)</item>
///   <item>A public property with get/set that reads/writes to the subject</item>
///   <item>A public <c>IObservable{T}</c> property (<c>PropertyNameChanged</c>) for subscriptions</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// </para>
/// <code>
/// public partial class MyViewModel : ReactiveViewModel
/// {
///     [Reactive] private string _name = string.Empty;
///     [Reactive] private int _count;
/// }
/// </code>
/// <para>
/// <b>Generated code:</b>
/// </para>
/// <code>
/// private readonly BehaviorSubject&lt;string&gt; _nameSubject = new(string.Empty);
///
/// public string Name
/// {
///     get => _nameSubject.Value;
///     set => _nameSubject.OnNext(value);
/// }
///
/// public IObservable&lt;string&gt; NameChanged => _nameSubject.AsObservable();
/// </code>
/// <para>
/// <b>Note:</b> The original field marked with [Reactive] is intentionally unused at runtime.
/// It serves as a marker for the source generator and provides the field name and initial value.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Field)]
[Conditional("SOURCE_GENERATOR_HINT")] // Indicates this attribute is consumed by source generators
public sealed class ReactiveAttribute : Attribute
{
}
