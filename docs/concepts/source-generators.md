# Source Generators

Termina uses C# source generators to eliminate boilerplate code while maintaining AOT compatibility.

## Why Source Generators?

Source generators provide:

1. **No Reflection** - Code is generated at compile time, not runtime
2. **AOT Compatible** - Works with Native AOT publishing
3. **IDE Support** - Generated code appears in IntelliSense
4. **Debuggable** - You can step into generated code

Termina uses source generators for route parameter injection via the `[FromRoute]` attribute.

## The [FromRoute] Generator

### What You Write

```csharp
public partial class DetailViewModel : ReactiveViewModel
{
    [FromRoute] private int _id;
}
```

### What Gets Generated

```csharp
// DetailViewModel.g.cs (generated)
partial class DetailViewModel
{
    public int Id { get; private set; }

    internal void ApplyRouteParameters(IDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("id", out var idValue))
        {
            Id = Convert.ToInt32(idValue);
        }
    }
}
```

::: warning
Your ViewModel class **must** be `partial` when using `[FromRoute]` for the source generator to work.
:::

## Viewing Generated Code

To see the generated code in your IDE, add to your project file:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated files appear in `obj/Generated/`.

## Key Requirements

### Partial Classes (for [FromRoute])

Your ViewModel class **must** be `partial` when using `[FromRoute]`:

```csharp
// ✓ Correct
public partial class MyViewModel : ReactiveViewModel
{
    [FromRoute] private int _id;
}

// ✗ Won't work - missing partial
public class MyViewModel : ReactiveViewModel
{
    [FromRoute] private int _id;
}
```

::: tip
If your ViewModel doesn't use `[FromRoute]`, it does **not** need to be `partial`. Reactive state is managed with `ReactiveProperty<T>` which doesn't require source generation.
:::

### Field Naming

`[FromRoute]` fields must start with `_` and use camelCase:

```csharp
// ✓ Correct
[FromRoute] private int _id;          // → Id
[FromRoute] private string _userName; // → UserName

// ✗ Won't work correctly
[FromRoute] private int id;           // No underscore
[FromRoute] private int Id;           // PascalCase
```

## Benefits for AOT

Traditional frameworks use reflection to:
- Find properties at runtime
- Create bindings dynamically
- Invoke property changed notifications

Termina's approach:
- `[FromRoute]` generates route parameter binding at compile time
- `ReactiveProperty<T>` provides observable state without reflection
- Full support for `PublishAot=true`

```bash
# Works because no reflection is needed
dotnet publish -c Release -r win-x64 --self-contained /p:PublishAot=true
```
