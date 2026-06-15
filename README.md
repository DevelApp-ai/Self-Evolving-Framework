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

- Pull requests targeting `main` produce prerelease packages with semantic versions like `<next-patch>-pr.<pr>.<run>` and publish to GitHub Packages.
- Pushes to `main` produce release packages with the next patch semantic version (current release: `1.3.0`; next patch release: `1.3.1`) and publish to GitHub Packages.
- Pushes to `main` and published GitHub Releases publish the same release package to NuGet.org from the release environment (`shared` by default, or `vars.RELEASE_ENVIRONMENT` when set). Configure `NUGET_API_KEY` as a repository, organization, or release-environment secret; the workflow fails if it is missing or lacks package push permission.

## Implementation roadmap

- TDS-derived implementation plan and progress tracking: `docs/Implementation TODO.md`
