# Source Generators

Termina uses C# source generators to eliminate boilerplate code while maintaining AOT compatibility. This page explains what gets generated and how it works.

## Why Source Generators?

Source generators provide:

1. **No Reflection** - Code is generated at compile time, not runtime
2. **AOT Compatible** - Works with Native AOT publishing
3. **IDE Support** - Generated code appears in IntelliSense
4. **Debuggable** - You can step into generated code

## The [Reactive] Generator

### What You Write

```csharp
public partial class CounterViewModel : ReactiveViewModel
{
    [Reactive] private int _count;
    [Reactive] private string _status = "Ready";
}
```

### What Gets Generated

```csharp
// CounterViewModel.g.cs (generated)
partial class CounterViewModel
{
    private readonly BehaviorSubject<int> _countSubject = new(default);

    public int Count
    {
        get => _countSubject.Value;
        set => _countSubject.OnNext(value);
    }

    public IObservable<int> CountChanged => _countSubject.AsObservable();

    private readonly BehaviorSubject<string> _statusSubject = new("Ready");

    public string Status
    {
        get => _statusSubject.Value;
        set => _statusSubject.OnNext(value);
    }

    public IObservable<string> StatusChanged => _statusSubject.AsObservable();
}
```

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

## Viewing Generated Code

To see the generated code in your IDE, add to your project file:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated files appear in `obj/Generated/`.

## Generator Implementation

The generators are in the `Termina.Generators` project:

::: details View ReactivePropertyGenerator
<<< @/../src/Termina.Generators/ReactivePropertyGenerator.cs{csharp}
:::

## Key Requirements

### Partial Classes

Your ViewModel class **must** be `partial`:

```csharp
// ✓ Correct
public partial class MyViewModel : ReactiveViewModel { }

// ✗ Won't work - missing partial
public class MyViewModel : ReactiveViewModel { }
```

### Field Naming

Fields must start with `_` and use camelCase:

```csharp
// ✓ Correct
[Reactive] private int _count;        // → Count, CountChanged
[Reactive] private string _userName;  // → UserName, UserNameChanged

// ✗ Won't work correctly
[Reactive] private int count;         // No underscore
[Reactive] private int Count;         // PascalCase
```

### Default Values

Default values are preserved:

```csharp
[Reactive] private int _count = 10;              // BehaviorSubject initialized with 10
[Reactive] private List<string> _items = new();  // Initialized with empty list
```

## Compile-Time Errors

The generator produces compile errors for common mistakes:

- Non-partial class with `[Reactive]` attributes
- Invalid field naming conventions
- Missing required dependencies

## Benefits for AOT

Traditional reactive libraries use reflection to:
- Find properties at runtime
- Create bindings dynamically
- Invoke property changed notifications

Termina's source generator approach:
- Generates all code at compile time
- No runtime reflection
- Full support for `PublishAot=true`

```bash
# Works because no reflection is needed
dotnet publish -c Release -r win-x64 --self-contained /p:PublishAot=true
```
