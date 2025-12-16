# AOT Compilation

Termina is designed for Native AOT compatibility, enabling single-file executables with instant startup.

## Why AOT?

Native AOT publishing provides:

- **Fast Startup** - No JIT compilation delay
- **Single File** - One executable, no runtime dependencies
- **Small Size** - Trimmed, optimized binary
- **Predictable Performance** - No JIT warmup

## Enabling AOT

Add to your project file:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

## Publishing

```bash
# Windows
dotnet publish -c Release -r win-x64

# Linux
dotnet publish -c Release -r linux-x64

# macOS
dotnet publish -c Release -r osx-x64

# macOS ARM
dotnet publish -c Release -r osx-arm64
```

## How Termina Supports AOT

### Source Generators

Termina uses source generators instead of reflection:

```csharp
// Traditional (requires reflection)
public int Count
{
    get => _count;
    set
    {
        _count = value;
        OnPropertyChanged();  // Reflection-based notification
    }
}

// Termina (generated at compile time)
[Reactive] private int _count;

// Generated code:
private readonly BehaviorSubject<int> _countSubject = new(default);
public int Count
{
    get => _countSubject.Value;
    set => _countSubject.OnNext(value);
}
public IObservable<int> CountChanged => _countSubject.AsObservable();
```

### No Dynamic Code

Termina avoids:
- `Reflection.Emit`
- `Expression.Compile()`
- Dynamic proxies
- Runtime code generation

### Trimming-Safe

All types are statically referenced, ensuring the trimmer doesn't remove needed code.

## AOT Considerations

### JSON Serialization

If using JSON, use source generators:

```csharp
[JsonSerializable(typeof(TodoItem))]
[JsonSerializable(typeof(List<TodoItem>))]
internal partial class AppJsonContext : JsonSerializerContext { }

// Usage
var json = JsonSerializer.Serialize(item, AppJsonContext.Default.TodoItem);
```

### Dependency Injection

Ensure all registered types are AOT-compatible:

```csharp
builder.Services.AddTermina("/", termina =>
{
    // These types must be statically analyzable
    termina.RegisterRoute<HomePage, HomeViewModel>("/");
});
```

### Third-Party Libraries

Verify library AOT compatibility. Termina's dependencies (System.Reactive, Microsoft.Extensions.*) support AOT.

## Trimming Configuration

Fine-tune trimming with:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <TrimMode>link</TrimMode>
  <IlcOptimizationPreference>Size</IlcOptimizationPreference>
</PropertyGroup>
```

Or preserve assemblies:

```xml
<ItemGroup>
  <TrimmerRootAssembly Include="SomeLibrary" />
</ItemGroup>
```

## Testing AOT Builds

Always test AOT builds separately:

```bash
# Build AOT
dotnet publish -c Release -r linux-x64

# Run the published binary
./bin/Release/net10.0/linux-x64/publish/MyApp
```

AOT builds may behave differently than JIT builds due to trimming.

## Size Optimization

Reduce binary size:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <StripSymbols>true</StripSymbols>
  <IlcOptimizationPreference>Size</IlcOptimizationPreference>
</PropertyGroup>
```

Typical Termina app sizes:
- Windows: ~15-25 MB
- Linux: ~12-20 MB
- macOS: ~15-25 MB

## Debugging AOT Issues

### Trimming Warnings

Enable warnings:

```xml
<PropertyGroup>
  <SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
</PropertyGroup>
```

### Common Issues

**Missing Types**: Ensure all types used with DI are registered explicitly.

**Reflection Usage**: Avoid or mark with `[DynamicallyAccessedMembers]`:

```csharp
public void Process<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
```

**JSON Errors**: Use source-generated serializers.

## CI/CD Integration

Test AOT in your pipeline:

```yaml
- name: Publish AOT
  run: dotnet publish -c Release -r linux-x64

- name: Test AOT Binary
  run: ./bin/Release/net10.0/linux-x64/publish/MyApp --test
```
