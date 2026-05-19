# Contributing to Termina

Thank you for your interest in contributing to Termina! This guide will help you get set up for local development.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) (for documentation)
- Git

## Getting Started

```bash
# Clone the repository
git clone https://github.com/Aaronontheweb/Termina.git
cd Termina

# Restore .NET tools
dotnet tool restore

# Build the solution
dotnet build
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test -v normal

# Run tests for a specific project
dotnet test tests/Termina.Tests
```

## Building NuGet Packages

```bash
# Build packages
dotnet pack -c Release -o ./bin/nuget

# The packages will be in ./bin/nuget/
ls ./bin/nuget/*.nupkg
```

## Running the Demos

```bash
# Run the counter demo
dotnet run --project demos/Termina.Demo.RegionBased

# Run the streaming chat demo (requires more setup)
dotnet run --project demos/Termina.Demo.Streaming

# Run the original demo
dotnet run --project demos/Termina.Demo
```

## Demo Screenshots

The animated GIFs shown throughout the documentation are recorded from the demo
apps with [VHS](https://github.com/charmbracelet/vhs). The recording pipeline
lives in [`screenshots/`](screenshots/README.md): each `.tape` file scripts a
demo, and `screenshots/capture.sh` builds the demos and runs the tapes.

Generated assets are committed under `docs/public/gallery/` and deployed
automatically by the docs workflow. Regenerate them locally when a demo's UI
changes:

```bash
# Requires vhs, ttyd, and ffmpeg in addition to the .NET SDK
./screenshots/capture.sh            # regenerate every GIF
./screenshots/capture.sh counter    # regenerate a single GIF
```

See [`screenshots/README.md`](screenshots/README.md) for setup and details.

## Documentation

The documentation website is built with [VitePress](https://vitepress.dev/).

### Building Docs Locally

```bash
# Navigate to docs folder
cd docs

# Install dependencies (first time only)
npm install

# Start development server with hot reload
npm run dev

# The site will be available at http://localhost:5173/Termina/
```

### Building for Production

```bash
cd docs

# Build the static site
npm run build

# Preview the production build
npm run preview
```

### Documentation Structure

```
docs/
├── .vitepress/
│   └── config.ts          # VitePress configuration
├── public/
│   └── termina-icon.png   # Site logo
├── index.md               # Homepage
├── guide/                 # Getting started guides
├── tutorials/             # Step-by-step tutorials
├── layout/                # Layout system docs
├── components/            # Component reference
├── styling/               # Styling guide
├── concepts/              # Core concepts
├── advanced/              # Advanced topics
└── comparison/            # Comparisons with alternatives
```

### Embedding Code Snippets

VitePress can include code directly from source files:

```markdown
<!-- Include entire file -->
<<< @/../demos/Termina.Demo.RegionBased/CounterViewModel.cs{csharp}

<!-- Include specific lines -->
<<< @/../src/Termina/Layout/TextNode.cs{15-45 csharp}

<!-- Include with line highlighting -->
<<< @/../demos/Termina.Demo.RegionBased/CounterPage.cs{12,15-20 csharp:line-numbers}
```

This keeps documentation in sync with actual code.

## AOT Validation

Termina supports Native AOT compilation. To test AOT publishing:

```bash
# Publish with AOT
dotnet publish demos/Termina.Demo -c Release -r linux-x64 --self-contained

# Test the AOT binary
./demos/Termina.Demo/bin/Release/net10.0/linux-x64/publish/Termina.Demo --test
```

## Code Style

- Use `dotnet format` to format code
- Follow existing patterns in the codebase
- Add XML documentation comments to public APIs
- Ensure all tests pass before submitting PRs

## Pull Request Process

1. Create a feature branch from `dev`
2. Make your changes
3. Ensure tests pass: `dotnet test`
4. Ensure docs build: `cd docs && npm run build`
5. Submit a PR to `dev` branch

## Project Structure

```
Termina/
├── src/
│   ├── Termina/                    # Core framework
│   │   ├── Layout/                 # Layout nodes (TextNode, PanelNode, etc.)
│   │   ├── Reactive/               # ReactiveViewModel, [Reactive] attribute
│   │   ├── Routing/                # Route templates and matching
│   │   ├── Terminal/               # ANSI terminal abstraction
│   │   └── ...
│   └── Termina.Generators/         # Source generators
├── demos/
│   ├── Termina.Demo.RegionBased/   # Counter demo
│   ├── Termina.Demo.Streaming/     # Chat demo with Akka.NET
│   └── Termina.Demo/               # Original demo
├── tests/
│   └── Termina.Tests/              # Unit tests
└── docs/                           # VitePress documentation
```

## Questions?

- Open an issue for bugs or feature requests
- Check existing issues before creating new ones

## License

By contributing, you agree that your contributions will be licensed under the Apache 2.0 License.
