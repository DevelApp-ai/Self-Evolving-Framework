# Self-Evolving-Framework

A .NET framework scaffold for building secure, LLM-driven self-evolving C# systems.

## What is included

- `DevelApp.SelfEvolvingFramework` NuGet-packable class library (`net8.0`)
- Core abstractions for candidate programs and evolution results
- Roslyn-based AST security evaluation for restricted namespaces/invocations
- Roslyn dynamic in-memory compilation service
- Collectible `AssemblyLoadContext` execution helper for isolated runtime invocation
- Evolution orchestration interfaces for mutation and fitness evaluation
- Unit and integration tests for security, compilation, orchestration, and execution

## Usage

Provide your own mutation and fitness implementations so you can use any LLM/model API:

```csharp
var orchestrator = new EvolutionOrchestrator(
    new RoslynAstSecurityEvaluator(),
    new RoslynDynamicCompilationService(),
    fitnessEvaluator,
    mutator);

var result = await orchestrator.EvolveOnceAsync(
    new CandidateProgram("public static class Seed { }"),
    cancellationToken);
```

For an end-to-end wiring example that combines mutation, fitness, and the multi-team adversarial review loop, see:

- `tests/SelfEvolvingFramework.Tests/Integration/AdversarialLoopWiringIntegrationTests.cs`

## Build and test

```bash
dotnet build SelfEvolvingFramework.slnx
dotnet test SelfEvolvingFramework.slnx
```

## Package

The library is configured as a NuGet package with `GeneratePackageOnBuild=true`.

## Versioning and publishing

- Pull requests targeting `main` produce prerelease packages with semantic versions like `<next-minor>-pr.<pr>.<run>` and publish to GitHub Packages.
- Pushes to `main` produce release packages with the next minor semantic version (for example `1.1.0`, then `1.2.0`) and publish to GitHub Packages.
- Pushes to `main` also publish the same release package to NuGet.org. The workflow fails if `NUGET_API_KEY` is missing or does not have permission to push the package.

## Implementation roadmap

- TDS-derived implementation plan and progress tracking: `docs/Implementation TODO.md`
