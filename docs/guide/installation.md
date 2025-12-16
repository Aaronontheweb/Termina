# Installation

## NuGet Package

Install Termina via NuGet:

::: code-group
```bash [.NET CLI]
dotnet add package Termina
```

```xml [PackageReference]
<PackageReference Include="Termina" Version="0.1.0-beta1" />
```

```powershell [Package Manager]
Install-Package Termina
```
:::

## Requirements

- **.NET 10.0** or later
- A terminal with ANSI escape sequence support (most modern terminals)

## Included Dependencies

Termina automatically includes:

- **System.Reactive** - For reactive programming with `IObservable<T>`
- **Microsoft.Extensions.Hosting** - For application lifecycle management
- **Microsoft.Extensions.DependencyInjection** - For dependency injection

## Project Setup

### Minimal Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Termina" Version="0.1.0-beta1" />
  </ItemGroup>
</Project>
```

### AOT Publishing

Termina fully supports Native AOT compilation. To publish as a native executable:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

Then publish:

```bash
dotnet publish -c Release
```

::: tip Why AOT?
Termina uses source generators instead of reflection, making it fully compatible with Native AOT. This means:
- Faster startup time
- Smaller binary size
- Single-file deployment
:::

## Optional: Akka.NET Integration

For applications using Akka.NET actors:

```bash
dotnet add package Akka.Hosting
```

See the [Akka.NET Integration](/advanced/akka-integration) guide for details.

## Verifying Installation

Create a minimal test to verify everything is working:

```csharp
using Microsoft.Extensions.Hosting;
using Termina.Hosting;
using Termina.Layout;
using Termina.Pages;
using Termina.Reactive;

// Simple ViewModel
public partial class TestViewModel : ReactiveViewModel { }

// Simple Page
public class TestPage : ReactivePage<TestViewModel>
{
    protected override ILayoutNode BuildLayout()
    {
        return new TextNode("Termina is working!");
    }
}

// Program.cs
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTermina("/", termina =>
{
    termina.RegisterRoute<TestPage, TestViewModel>("/");
});
await builder.Build().RunAsync();
```

Run with `dotnet run`. If you see "Termina is working!" in your terminal, you're all set!

## IDE Support

Termina's source generators work with:

- **Visual Studio 2022** 17.8+
- **Visual Studio Code** with C# Dev Kit
- **JetBrains Rider** 2024.1+

To see generated code in your IDE, add to your project file:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated files will appear in `obj/Generated/`.
