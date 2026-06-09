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

## Build and test

```bash
dotnet build SelfEvolving.Framework.slnx
dotnet test SelfEvolving.Framework.slnx
```

## Package

The library is configured as a NuGet package with `GeneratePackageOnBuild=true`.

## Versioning and publishing

- Pull requests targeting `main` produce prerelease packages with semantic versions like `1.0.0-pr.<pr>.<run>` and publish to GitHub Packages.
- Pushes to `main` produce release packages with semantic version `1.0.0` and publish to GitHub Packages.
- If `NUGET_API_KEY` is configured in repository secrets, the same `main` release package is also published to NuGet.org.
