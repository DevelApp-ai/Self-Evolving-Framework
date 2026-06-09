# Self-Evolving-Framework

A .NET framework scaffold for building secure, LLM-driven self-evolving C# systems.

## What is included

- `SelfEvolving.Framework` NuGet-packable class library (`net8.0`)
- Core abstractions for candidate programs and evolution results
- Roslyn-based AST security evaluation for restricted namespaces/invocations
- Roslyn dynamic in-memory compilation service
- Collectible `AssemblyLoadContext` execution helper for isolated runtime invocation
- Evolution orchestration interfaces for mutation and fitness evaluation
- Unit and integration tests for security, compilation, orchestration, and execution

## Build and test

```bash
dotnet build SelfEvolving.Framework.slnx
dotnet test SelfEvolving.Framework.slnx
```

## Package

The library is configured as a NuGet package with `GeneratePackageOnBuild=true`.
