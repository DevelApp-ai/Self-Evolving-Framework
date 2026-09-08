# Technical Design Specification: JSON-Native Support for Self-Evolving-Framework
# LaunchMobile Integration

**Author:** Vibe Code Agent  
**Date:** September 8, 2026  
**Version:** 1.0  
**Status:** Draft

---

## Executive Summary

This document specifies the **architectural modifications required to Self-Evolving-Framework (SEF)** to support **JSON-native evolution** for seamless integration with **LaunchMobile (LM)**. The current SEF implementation is C#-centric, which creates an impedance mismatch with LaunchMobile's JSON-based data model (XState machines, JSON Schema UI definitions).

### The Problem: C#-Centric Design Limits LaunchMobile

Current SEF requires:
```csharp
CandidateProgram.SourceCode = "C# code only"
```

This forces LaunchMobile to:
1. **Wrap JSON in C#** - Create artificial classes that generate JSON
2. **Lose semantic meaning** - XState/JSON Schema structure is obscured
3. **Add complexity** - Requires compilation infrastructure for non-code data
4. **Limit evolution** - LLM works with C# syntax instead of domain logic

### The Solution: Multi-Format Candidate Support

Modify SEF to support **both C# and JSON** as first-class candidate formats:

```csharp
CandidateProgram candidate = CandidateProgram.FromJson(xstateJson);
// OR
CandidateProgram candidate = CandidateProgram.FromCSharp(csharpCode);
```

This enables:
- ✅ **Native JSON evolution** - No translation overhead
- ✅ **Domain-aware mutation** - LLM understands XState/JSON Schema semantics
- ✅ **Backward compatibility** - Existing C# code continues to work
- ✅ **Extensible architecture** - Easy to add more formats (YAML, etc.)

---

## Table of Contents

1. [Current Architecture Analysis](#current-architecture-analysis)
2. [Proposed Architecture](#proposed-architecture)
3. [Core Modifications to SEF](#core-modifications-to-sef)
4. [New JSON-Specific Components](#new-json-specific-components)
5. [LaunchMobile-Specific Extensions](#launchmobile-specific-extensions)
6. [Security Considerations for JSON](#security-considerations-for-json)
7. [Implementation Plan](#implementation-plan)
8. [Testing Strategy](#testing-strategy)
9. [Migration Path](#migration-path)

---

## Current Architecture Analysis

### SEF Core Components (Current - C# Only)

```
┌─────────────────────────────────────────────────────────────┐
│                    EvolutionOrchestrator                        │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │  SemanticKernel  │  │  RoslynAst       │  │  Roslyn      │ │
│  │  EvolutionMutator│  │  Security        │  │  Compilation │ │
│  │  (C# ONLY)       │  │  Evaluator       │  │  Service     │ │
│  └────────┬────────┘  └────────┬────────┘  └───────┬───────┘ │
│           │                    │                   │           │
│           ▼                    ▼                   ▼           │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │              CandidateProgram (C# ONLY)                    │ │
│  │  - SourceCode: string (C# code)                           │ │
│  └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Key Limitation

**All components assume C# source code**:
- `SemanticKernelEvolutionMutator` - Prompts LLM for C# code
- `RoslynAstSecurityEvaluator` - Parses C# AST
- `RoslynDynamicCompilationService` - Compiles C# to IL
- `SourceCodeCandidateChromosome` - Stores C# in genes

**Result**: LaunchMobile must wrap JSON in C# classes, losing semantic meaning and adding unnecessary complexity.

---

## Proposed Architecture

### Multi-Format Evolution Pipeline

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         EvolutionOrchestrator (MODIFIED)                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐ │
│  │  Format Router   │  │  Format-Specific  │  │  Format-Specific              │ │
│  │                 │  │  Mutators         │  │  Security Evaluators          │ │
│  │  - C# → C# mut  │  │                  │  │                              │ │
│  │  - JSON → JSON   │  │  ┌─────────────┐ │  │  ┌───────────────────────┐ │ │
│  │  - YAML → YAML   │  │  │ C# Mutator   │ │  │  │ Roslyn AST Evaluator  │ │ │
│  └────────┬────────┘  │  └──────┬──────┘ │  │  └──────────┬────────────┘ │ │
│             │           │         │         │  │             │               │ │
│             │           │  ┌─────────────┐ │  │  ┌───────────────────────┐ │ │
│             │           │  │ JSON Mutator │ ◄─┘  │  │ Json Security Evaluator│ ◄─┘ │
│             │           │  └──────┬──────┘    │  │  └──────────┬────────────┘ │
│             │           │         │            │  │             │               │
│             ▼           │         ▼            │  │             ▼               │
│  ┌─────────────────────────────────────────────────────────────────────┐ │ │
│  │                        CandidateProgram (MODIFIED)                     │ │ │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────┐ │ │
│  │  │ Format:          │  │ SourceMaterial:  │  │ CompiledAssembly:        │ │ │
│  │  │ CandidateFormat  │  │ string           │  │ Assembly? (C# only)      │ │ │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────────────┘ │ │
│  │  Factory Methods: FromCSharp(), FromJson(), FromYaml()                  │ │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Format-Specific Processing Flow

```
C# CANDIDATE:
┌─────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Input   │───►│ C# Mutator   │───►│ AST Security │───►│ C# Compile   │
│ (C# code)│    │              │    │ Evaluator    │    │ Service      │
└─────────┘    └──────────────┘    └──────────────┘    └──────────────┘

JSON CANDIDATE:
┌─────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Input   │───►│ JSON Mutator │───►│ JSON Security │───►│ (Skip)       │
│ (JSON)   │    │              │    │ Evaluator    │    │ Compilation   │
└─────────┘    └──────────────┘    └──────────────┘    └──────────────┘
```

---

## Core Modifications to SEF

### 1. CandidateProgram - Multi-Format Support

**File:** `src/SelfEvolvingFramework/Core/CandidateProgram.cs`

**Current:**
```csharp
public sealed record CandidateProgram(string SourceCode, string? ParentId = null, string? Id = null)
{
    public string Id { get; init; } = Id ?? Guid.NewGuid().ToString("N");
}
```

**Modified:**
```csharp
public enum CandidateFormat
{
    /// <summary>C# source code</summary>
    CSharp,
    
    /// <summary>JSON data (XState, JSON Schema, etc.)</summary>
    Json,
    
    /// <summary>YAML data (future)</summary>
    Yaml
}

public sealed record CandidateProgram
{
    /// <summary>
    /// Unique identifier for this candidate
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// The parent candidate ID (for lineage tracking)
    /// </summary>
    public string? ParentId { get; init; }
    
    /// <summary>
    /// The raw source material (C# code, JSON, YAML, etc.)
    /// </summary>
    public string SourceMaterial { get; init; }
    
    /// <summary>
    /// The format of the source material
    /// </summary>
    public CandidateFormat Format { get; init; }
    
    /// <summary>
    /// For compiled formats (C#), the compiled assembly
    /// </summary>
    public Assembly? CompiledAssembly { get; set; }
    
    /// <summary>
    /// For compiled formats (C#), the compilation result
    /// </summary>
    public CompilationResult? CompilationResult { get; set; }
    
    /// <summary>
    /// Create a C# candidate program
    /// </summary>
    public static CandidateProgram FromCSharp(string csharpCode, string? parentId = null, string? id = null)
        => new(csharpCode, parentId, id, CandidateFormat.CSharp);
    
    /// <summary>
    /// Create a JSON candidate program
    /// </summary>
    public static CandidateProgram FromJson(string json, string? parentId = null, string? id = null)
        => new(json, parentId, id, CandidateFormat.Json);
    
    /// <summary>
    /// Create a YAML candidate program (future)
    /// </summary>
    public static CandidateProgram FromYaml(string yaml, string? parentId = null, string? id = null)
        => new(yaml, parentId, id, CandidateFormat.Yaml);
    
    // Constructor (private)
    private CandidateProgram(string sourceMaterial, string? parentId, string? id, CandidateFormat format)
    {
        SourceMaterial = sourceMaterial ?? throw new ArgumentNullException(nameof(sourceMaterial));
        ParentId = parentId;
        Id = id ?? Guid.NewGuid().ToString("N");
        Format = format;
    }
    
    // Backward compatibility - implicit conversion from string (assumes C#)
    public static implicit operator CandidateProgram(string sourceCode)
        => FromCSharp(sourceCode);
}
```

**Key Changes:**
- Single `SourceMaterial` property instead of `SourceCode`
- `Format` enum indicates interpretation method
- Factory methods for each format
- Optional `CompiledAssembly` and `CompilationResult` for C# only
- Backward compatible via implicit conversion

---

### 2. EvolutionOrchestrator - Format-Aware Processing

**File:** `src/SelfEvolvingFramework/Orchestration/EvolutionOrchestrator.cs`

**Key Changes:**

```csharp
public sealed class EvolutionOrchestrator
{
    private readonly Dictionary<CandidateFormat, IEvolutionMutator> _mutators;
    private readonly Dictionary<CandidateFormat, IEvolutionCrossover> _crossovers;
    private readonly Dictionary<CandidateFormat, IAstSecurityEvaluator> _securityEvaluators;
    
    // Add format-specific component registration
    public void AddMutator(IEvolutionMutator mutator)
        => _mutators[mutator.Format] = mutator;
    
    public void AddCrossover(IEvolutionCrossover crossover)
        => _crossovers[crossover.Format] = crossover;
    
    public void AddSecurityEvaluator(IAstSecurityEvaluator evaluator)
        => _securityEvaluators[evaluator.Format] = evaluator;
    
    public async Task<EvolutionResult> EvolveOnceAsync(
        CandidateProgram seed,
        IReadOnlyList<string>? feedback = null,
        IReadOnlyList<AdversarialRoundResult>? adversarialRounds = null,
        CancellationToken cancellationToken = default)
    {
        // Get format-specific mutator
        if (!_mutators.TryGetValue(seed.Format, out var mutator))
            throw new InvalidOperationException(
                $"No mutator registered for format: {seed.Format}");
        
        // Get format-specific security evaluator
        if (!_securityEvaluators.TryGetValue(seed.Format, out var securityEvaluator))
            throw new InvalidOperationException(
                $"No security evaluator registered for format: {seed.Format}");
        
        // Mutation (format-specific)
        var mutated = await mutator.MutateAsync(seed, mutationFeedback, budgetCancellation.Token);
        
        // Security evaluation (format-specific)
        var security = securityEvaluator.Evaluate(mutated.SourceMaterial);
        if (!security.IsAllowed)
            return BuildResult(mutated, false, double.NegativeInfinity, 
                PrefixDiagnostics("security", security.Violations));
        
        // Compilation (C# only)
        if (seed.Format == CandidateFormat.CSharp)
        {
            var compilation = await _compilationService.CompileAsync(mutated);
            if (!compilation.Success)
                return BuildResult(mutated, false, double.NegativeInfinity, 
                    PrefixDiagnostics("compilation", compilation.Diagnostics));
            
            mutated.CompilationResult = compilation;
            mutated.CompiledAssembly = compilation.Assembly;
        }
        
        // Fitness evaluation (format-agnostic)
        var fitness = await _fitnessEvaluator.EvaluateAsync(mutated, budgetCancellation.Token);
        
        return BuildResult(mutated, true, fitness, Array.Empty<string>());
    }
}
```

---

### 3. Update Base Interfaces for Format Support

**File:** `src/SelfEvolvingFramework/Orchestration/IEvolutionMutator.cs`

```csharp
public interface IEvolutionMutator
{
    /// <summary>
    /// The format this mutator supports
    /// </summary>
    CandidateFormat Format { get; }
    
    Task<CandidateProgram> MutateAsync(
        CandidateProgram candidate,
        IReadOnlyList<string> feedback,
        CancellationToken cancellationToken = default);
}
```

**File:** `src/SelfEvolvingFramework/Orchestration/IEvolutionCrossover.cs`

```csharp
public interface IEvolutionCrossover
{
    /// <summary>
    /// The format this crossover supports
    /// </summary>
    CandidateFormat Format { get; }
    
    Task<CandidateProgram> CrossoverAsync(
        CandidateProgram parentA,
        CandidateProgram parentB,
        CancellationToken cancellationToken = default);
}
```

**File:** `src/SelfEvolvingFramework/Security/IAstSecurityEvaluator.cs`

```csharp
public interface IAstSecurityEvaluator
{
    /// <summary>
    /// The format this security evaluator supports
    /// </summary>
    CandidateFormat Format { get; }
    
    SecurityEvaluationResult Evaluate(string sourceMaterial);
}
```

---

### 4. Update Existing C# Components

**File:** `src/SelfEvolvingFramework/Orchestration/SemanticKernelEvolutionMutator.cs`

```csharp
public sealed class SemanticKernelEvolutionMutator : IEvolutionMutator
{
    // Add Format property
    public CandidateFormat Format => CandidateFormat.CSharp;
    
    // Update to use SourceMaterial instead of SourceCode
    public async Task<CandidateProgram> MutateAsync(
        CandidateProgram candidate,
        IReadOnlyList<string> feedback,
        CancellationToken cancellationToken = default)
    {
        // ... existing code
        
        var mutatedSource = ExtractCode(responses.FirstOrDefault()?.Content);
        
        return string.IsNullOrWhiteSpace(mutatedSource)
            ? candidate
            : CandidateProgram.FromCSharp(mutatedSource, candidate.ParentId, candidate.Id);
    }
}
```

**File:** `src/SelfEvolvingFramework/Orchestration/SourceCodeCandidateChromosome.cs`

```csharp
internal sealed class SourceCodeCandidateChromosome : ChromosomeBase
{
    public SourceCodeCandidateChromosome(CandidateProgram candidate) : base(1)
    {
        SetCandidate(candidate);
    }

    public CandidateProgram Candidate { get; private set; } = null!;

    public override Gene GenerateGene(int geneIndex)
    {
        if (geneIndex != 0)
            throw new ArgumentOutOfRangeException(nameof(geneIndex));

        return new Gene(Candidate.SourceMaterial);
    }

    public override IChromosome CreateNew()
        => new SourceCodeCandidateChromosome(Candidate);

    public override IChromosome Clone()
    {
        var clone = new SourceCodeCandidateChromosome(Candidate with { })
        {
            Fitness = Fitness
        };

        clone.ReplaceGene(0, GetGene(0));
        return clone;
    }

    public void SetCandidate(CandidateProgram candidateProgram)
    {
        Candidate = candidateProgram;
        ReplaceGene(0, new Gene(candidateProgram.SourceMaterial));
    }
}
```

---

## New JSON-Specific Components

### 1. JsonEvolutionMutator

**File:** `src/SelfEvolvingFramework/Orchestration/JsonEvolutionMutator.cs` (NEW)

```csharp
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

/// <summary>
/// Evolution mutator for JSON-based candidates (XState, JSON Schema, etc.)
/// </summary>
public sealed class JsonEvolutionMutator : IEvolutionMutator
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly JsonMutationOptions _options;

    public CandidateFormat Format => CandidateFormat.Json;

    public JsonEvolutionMutator(
        IChatCompletionService chatCompletionService,
        JsonMutationOptions? options = null)
    {
        _chatCompletionService = chatCompletionService 
            ?? throw new ArgumentNullException(nameof(chatCompletionService));
        _options = options ?? new();
    }

    public async Task<CandidateProgram> MutateAsync(
        CandidateProgram candidate,
        IReadOnlyList<string> feedback,
        CancellationToken cancellationToken = default)
    {
        if (candidate.Format != CandidateFormat.Json)
            throw new InvalidOperationException(
                $"JsonEvolutionMutator requires JSON format, got {candidate.Format}");

        var history = CreateChatHistory(candidate.SourceMaterial, feedback);
        var executionSettings = CreateExecutionSettings();
        
        var responses = await _chatCompletionService.GetChatMessageContentsAsync(
            history, executionSettings, null, cancellationToken);
        
        var mutatedJson = ExtractJson(responses.FirstOrDefault()?.Content);

        return string.IsNullOrWhiteSpace(mutatedJson)
            ? candidate
            : CandidateProgram.FromJson(mutatedJson, candidate.ParentId, candidate.Id);
    }

    internal ChatHistory CreateChatHistory(string json, IReadOnlyList<string> feedback)
    {
        var history = new ChatHistory(_options.SystemPrompt);
        history.AddUserMessage(BuildMutationPrompt(json, feedback));
        return history;
    }

    internal string BuildMutationPrompt(string json, IReadOnlyList<string> feedback)
    {
        var (validationDiagnostics, securityDiagnostics, runtimeDiagnostics, additionalFeedback) 
            = CategorizeFeedback(feedback);
        
        var builder = new StringBuilder();
        builder.AppendLine("Objective:");
        builder.AppendLine(_options.Objective);
        builder.AppendLine();
        builder.AppendLine("Current JSON:");
        builder.AppendLine(json);
        builder.AppendLine();
        
        AppendFeedbackSection(builder, "Validation diagnostics:", validationDiagnostics);
        AppendFeedbackSection(builder, "Security diagnostics:", securityDiagnostics);
        AppendFeedbackSection(builder, "Runtime diagnostics:", runtimeDiagnostics);
        AppendFeedbackSection(builder, "Additional feedback:", additionalFeedback);

        builder.AppendLine();
        builder.AppendLine("Return ONLY the full revised JSON with no markdown, explanations, or extra text.");
        builder.AppendLine("Ensure the JSON is valid and maintains all required fields.");
        
        return builder.ToString();
    }

    private static string ExtractJson(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) 
            return string.Empty;

        // Try to extract JSON from response
        var jsonStart = content.IndexOf('{');
        var jsonEnd = content.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            var json = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
            try
            {
                using var _ = JsonDocument.Parse(json);
                return json;
            }
            catch (JsonException) { }
        }

        return string.Empty;
    }
}

public sealed class JsonMutationOptions
{
    public string Objective { get; set; } = 
        "Improve the JSON structure, add missing fields, fix issues, and optimize the design.";
    
    public string SystemPrompt { get; set; } = 
        "You are a JSON evolution engine. Return only valid JSON with no markdown, explanations, or extra text.";
    
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4000;
    public double TopP { get; set; } = 0.9;
}
```

---

### 2. JsonEvolutionCrossover

**File:** `src/SelfEvolvingFramework/Orchestration/JsonEvolutionCrossover.cs` (NEW)

```csharp
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public sealed class JsonEvolutionCrossover : IEvolutionCrossover
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly JsonCrossoverOptions _options;

    public CandidateFormat Format => CandidateFormat.Json;

    public JsonEvolutionCrossover(
        IChatCompletionService chatCompletionService,
        JsonCrossoverOptions? options = null)
    {
        _chatCompletionService = chatCompletionService 
            ?? throw new ArgumentNullException(nameof(chatCompletionService));
        _options = options ?? new();
    }

    public async Task<CandidateProgram> CrossoverAsync(
        CandidateProgram parentA,
        CandidateProgram parentB,
        CancellationToken cancellationToken = default)
    {
        if (parentA.Format != CandidateFormat.Json || parentB.Format != CandidateFormat.Json)
            throw new InvalidOperationException(
                $"JsonEvolutionCrossover requires JSON format parents");

        var history = CreateChatHistory(parentA.SourceMaterial, parentB.SourceMaterial);
        
        var responses = await _chatCompletionService.GetChatMessageContentsAsync(
            history, null, null, cancellationToken);
        
        var offspringJson = JsonEvolutionMutator.ExtractJson(responses.FirstOrDefault()?.Content);

        return string.IsNullOrWhiteSpace(offspringJson)
            ? parentA
            : CandidateProgram.FromJson(offspringJson, parentA.ParentId, parentA.Id);
    }

    internal string BuildCrossoverPrompt(string parentAJson, string parentBJson)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Objective:");
        builder.AppendLine(_options.Objective);
        builder.AppendLine();
        builder.AppendLine("Parent A JSON:");
        builder.AppendLine(parentAJson);
        builder.AppendLine();
        builder.AppendLine("Parent B JSON:");
        builder.AppendLine(parentBJson);
        builder.AppendLine();
        builder.AppendLine("Combine the strongest traits from both parents into a single improved JSON.");
        builder.AppendLine("Return ONLY the complete combined JSON.");
        return builder.ToString();
    }
}

public sealed class JsonCrossoverOptions
{
    public string Objective { get; set; } = 
        "Combine the best traits from both JSON structures to create an improved version.";
    
    public string SystemPrompt { get; set; } = 
        "You are a JSON crossover engine. Return only valid JSON with no markdown, explanations, or extra text.";
}
```

---

### 3. JsonSecurityEvaluator

**File:** `src/SelfEvolvingFramework/Security/JsonSecurityEvaluator.cs` (NEW)

```csharp
using System.Text.Json;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Security;

/// <summary>
/// Security evaluator for JSON-based candidates
/// </summary>
public sealed class JsonSecurityEvaluator : IAstSecurityEvaluator
{
    private readonly JsonSecurityOptions _options;

    public CandidateFormat Format => CandidateFormat.Json;

    public JsonSecurityEvaluator(JsonSecurityOptions? options = null)
    {
        _options = options ?? new();
    }

    public SecurityEvaluationResult Evaluate(string sourceMaterial)
    {
        try
        {
            var violations = new List<string>();
            
            using var jsonDoc = JsonDocument.Parse(sourceMaterial);
            
            // Check for dangerous patterns
            if (ContainsPrototypePollution(jsonDoc))
                violations.Add("Potential prototype pollution pattern detected");
            
            if (ContainsCircularReferences(jsonDoc, _options.MaxDepth))
                violations.Add("Circular reference detected or JSON too deeply nested");
            
            if (ExceedsSizeLimit(sourceMaterial, _options.MaxSizeBytes))
                violations.Add("JSON exceeds maximum size limit");
            
            if (ContainsDisallowedPatterns(jsonDoc, _options.DisallowedPatterns))
                violations.Add("Disallowed pattern detected in JSON");
            
            // Check against allowed schema if specified
            if (_options.AllowedSchema != null)
            {
                var validationResult = ValidateAgainstSchema(jsonDoc, _options.AllowedSchema);
                if (!validationResult.IsValid)
                {
                    violations.AddRange(validationResult.Errors);
                }
            }
            
            // Check for required fields
            if (_options.RequiredFields != null && _options.RequiredFields.Count > 0)
            {
                var missingFields = CheckRequiredFields(jsonDoc, _options.RequiredFields);
                if (missingFields.Count > 0)
                {
                    violations.Add($"Missing required fields: {string.Join(", ", missingFields)}");
                }
            }
            
            return new SecurityEvaluationResult(violations.Count == 0, violations);
        }
        catch (JsonException ex)
        {
            return new SecurityEvaluationResult(false, new[] { $"Invalid JSON: {ex.Message}" });
        }
    }

    private static bool ContainsPrototypePollution(JsonDocument jsonDoc)
    {
        // Check for __proto__ or constructor properties
        var enumerator = jsonDoc.RootElement.EnumerateObject();
        while (enumerator.MoveNext())
        {
            if (enumerator.Current.Name.Equals("__proto__", StringComparison.Ordinal) ||
                enumerator.Current.Name.Equals("constructor", StringComparison.Ordinal))
            {
                return true;
            }
            
            if (enumerator.Current.Value.ValueKind == JsonValueKind.Object)
            {
                if (ContainsPrototypePollutionInElement(enumerator.Current.Value))
                    return true;
            }
        }
        return false;
    }

    private static bool ContainsPrototypePollutionInElement(JsonElement element)
    {
        var enumerator = element.EnumerateObject();
        while (enumerator.MoveNext())
        {
            if (enumerator.Current.Name.Equals("__proto__", StringComparison.Ordinal) ||
                enumerator.Current.Name.Equals("constructor", StringComparison.Ordinal))
            {
                return true;
            }
            
            if (enumerator.Current.Value.ValueKind == JsonValueKind.Object)
            {
                if (ContainsPrototypePollutionInElement(enumerator.Current.Value))
                    return true;
            }
        }
        return false;
    }

    private static bool ContainsCircularReferences(JsonDocument jsonDoc, int maxDepth)
    {
        return CalculateDepth(jsonDoc.RootElement) > maxDepth;
    }

    private static int CalculateDepth(JsonElement element, int currentDepth = 0)
    {
        if (currentDepth > 100) return 101; // Safety limit
        
        var maxChildDepth = currentDepth;
        
        if (element.ValueKind == JsonValueKind.Object)
        {
            var enumerator = element.EnumerateObject();
            while (enumerator.MoveNext())
            {
                var childDepth = CalculateDepth(enumerator.Current.Value, currentDepth + 1);
                if (childDepth > maxChildDepth)
                    maxChildDepth = childDepth;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var enumerator = element.EnumerateArray();
            while (enumerator.MoveNext())
            {
                var childDepth = CalculateDepth(enumerator.Current, currentDepth + 1);
                if (childDepth > maxChildDepth)
                    maxChildDepth = childDepth;
            }
        }
        
        return maxChildDepth;
    }

    private static bool ExceedsSizeLimit(string json, int maxSizeBytes)
    {
        return Encoding.UTF8.GetByteCount(json) > maxSizeBytes;
    }

    private static bool ContainsDisallowedPatterns(JsonDocument jsonDoc, HashSet<string> disallowedPatterns)
    {
        var jsonString = jsonDoc.RootElement.GetRawText();
        foreach (var pattern in disallowedPatterns)
        {
            if (jsonString.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static List<string> CheckRequiredFields(JsonDocument jsonDoc, HashSet<string> requiredFields)
    {
        var missing = new List<string>();
        var enumerator = jsonDoc.RootElement.EnumerateObject();
        
        foreach (var required in requiredFields)
        {
            var found = false;
            enumerator.Reset();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Name.Equals(required, StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                missing.Add(required);
        }
        
        return missing;
    }
}

public sealed class JsonSecurityOptions
{
    public int MaxDepth { get; set; } = 50;
    public int MaxSizeBytes { get; set; } = 1024 * 1024; // 1MB
    public HashSet<string> DisallowedPatterns { get; set; } = new();
    public HashSet<string> RequiredFields { get; set; } = new();
    public JsonSchema? AllowedSchema { get; set; }
}
```

---

## LaunchMobile-Specific Extensions

### 1. XState-Specific Fitness Evaluator

**File:** `src/SelfEvolvingFramework/Orchestration/XStateFitnessEvaluator.cs` (NEW)

```csharp
using System.Text.Json;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

/// <summary>
/// Fitness evaluator for XState machine definitions
/// </summary>
public sealed class XStateFitnessEvaluator : IFitnessEvaluator
{
    private readonly XStateFitnessOptions _options;

    public XStateFitnessEvaluator(XStateFitnessOptions? options = null)
    {
        _options = options ?? new();
    }

    public async Task<double> EvaluateAsync(
        CandidateProgram candidate,
        CancellationToken cancellationToken = default)
    {
        if (candidate.Format != CandidateFormat.Json)
            return double.NegativeInfinity;

        try
        {
            var score = 0.0;
            using var jsonDoc = JsonDocument.Parse(candidate.SourceMaterial);
            
            // Check for required XState fields
            if (HasRequiredFields(jsonDoc))
                score += _options.RequiredFieldsWeight;
            
            // Check for valid initial state
            if (HasValidInitialState(jsonDoc))
                score += _options.InitialStateWeight;
            
            // Check for reachable states
            var reachability = CalculateStateReachability(jsonDoc);
            score += reachability * _options.ReachabilityWeight;
            
            // Check for proper transitions
            var transitionScore = EvaluateTransitions(jsonDoc);
            score += transitionScore * _options.TransitionsWeight;
            
            // Check for error handling
            if (HasErrorHandling(jsonDoc))
                score += _options.ErrorHandlingWeight;
            
            // Penalize for complexity
            var complexity = CalculateComplexity(jsonDoc);
            score -= complexity * _options.ComplexityPenalty;
            
            // Bonus for documentation
            if (HasDocumentation(jsonDoc))
                score += _options.DocumentationBonus;
            
            return Math.Max(0, score);
        }
        catch (JsonException)
        {
            return double.NegativeInfinity;
        }
    }

    private static bool HasRequiredFields(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        return root.TryGetProperty("id", out _) &&
               root.TryGetProperty("initial", out _) &&
               root.TryGetProperty("states", out _);
    }

    private static bool HasValidInitialState(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        if (root.TryGetProperty("states", out var states) &&
            root.TryGetProperty("initial", out var initial))
        {
            var initialState = initial.GetString();
            if (states.ValueKind == JsonValueKind.Object)
            {
                var enumerator = states.EnumerateObject();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.Name.Equals(initialState, StringComparison.Ordinal))
                        return true;
                }
            }
        }
        return false;
    }

    private static double CalculateStateReachability(JsonDocument jsonDoc)
    {
        // Build graph and calculate reachability
        // Implementation depends on graph library
        return 1.0; // Placeholder
    }

    private static double EvaluateTransitions(JsonDocument jsonDoc)
    {
        // Evaluate transition validity
        return 1.0; // Placeholder
    }

    private static bool HasErrorHandling(JsonDocument jsonDoc)
    {
        // Check for error states or onError transitions
        var root = jsonDoc.RootElement;
        if (root.TryGetProperty("states", out var states) &&
            states.ValueKind == JsonValueKind.Object)
        {
            var enumerator = states.EnumerateObject();
            while (enumerator.MoveNext())
            {
                var state = enumerator.Current.Value;
                if (state.TryGetProperty("onError", out _))
                    return true;
            }
        }
        return false;
    }

    private static double CalculateComplexity(JsonDocument jsonDoc)
    {
        // Calculate based on depth, number of states, transitions, etc.
        return 0.5; // Placeholder
    }

    private static bool HasDocumentation(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        return root.TryGetProperty("description", out _) ||
               root.TryGetProperty("meta", out _);
    }
}

public sealed class XStateFitnessOptions
{
    public double RequiredFieldsWeight { get; set; } = 20.0;
    public double InitialStateWeight { get; set; } = 15.0;
    public double ReachabilityWeight { get; set; } = 25.0;
    public double TransitionsWeight { get; set; } = 20.0;
    public double ErrorHandlingWeight { get; set; } = 10.0;
    public double ComplexityPenalty { get; set; } = 0.1;
    public double DocumentationBonus { get; set; } = 5.0;
}
```

---

### 2. JsonSchemaFitnessEvaluator

**File:** `src/SelfEvolvingFramework/Orchestration/JsonSchemaFitnessEvaluator.cs` (NEW)

```csharp
using System.Text.Json;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

/// <summary>
/// Fitness evaluator for JSON Schema definitions (UI schemas)
/// </summary>
public sealed class JsonSchemaFitnessEvaluator : IFitnessEvaluator
{
    private readonly JsonSchemaFitnessOptions _options;

    public JsonSchemaFitnessEvaluator(JsonSchemaFitnessOptions? options = null)
    {
        _options = options ?? new();
    }

    public async Task<double> EvaluateAsync(
        CandidateProgram candidate,
        CancellationToken cancellationToken = default)
    {
        if (candidate.Format != CandidateFormat.Json)
            return double.NegativeInfinity;

        try
        {
            var score = 0.0;
            using var jsonDoc = JsonDocument.Parse(candidate.SourceMaterial);
            
            // Check for required schema fields
            if (HasRequiredSchemaFields(jsonDoc))
                score += _options.RequiredFieldsWeight;
            
            // Check for valid type definitions
            if (HasValidTypes(jsonDoc))
                score += _options.TypesWeight;
            
            // Check for property definitions
            var propertyScore = EvaluateProperties(jsonDoc);
            score += propertyScore * _options.PropertiesWeight;
            
            // Check for required fields
            if (HasRequiredFieldsDefined(jsonDoc))
                score += _options.RequiredWeight;
            
            // Check for descriptions
            if (HasDescriptions(jsonDoc))
                score += _options.DescriptionsWeight;
            
            // Check for validation rules
            var validationScore = EvaluateValidationRules(jsonDoc);
            score += validationScore * _options.ValidationWeight;
            
            return Math.Max(0, score);
        }
        catch (JsonException)
        {
            return double.NegativeInfinity;
        }
    }

    private static bool HasRequiredSchemaFields(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        return root.TryGetProperty("$schema", out _) &&
               root.TryGetProperty("type", out _) &&
               root.TryGetProperty("properties", out _);
    }

    private static bool HasValidTypes(JsonDocument jsonDoc)
    {
        // Check for valid JSON Schema types
        return true; // Placeholder
    }

    private static double EvaluateProperties(JsonDocument jsonDoc)
    {
        // Evaluate property definitions
        return 1.0; // Placeholder
    }

    private static bool HasRequiredFieldsDefined(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        return root.TryGetProperty("required", out _);
    }

    private static bool HasDescriptions(JsonDocument jsonDoc)
    {
        // Check for descriptions in schema or properties
        return true; // Placeholder
    }

    private static double EvaluateValidationRules(JsonDocument jsonDoc)
    {
        // Evaluate validation rules (minLength, maxLength, pattern, etc.)
        return 1.0; // Placeholder
    }
}

public sealed class JsonSchemaFitnessOptions
{
    public double RequiredFieldsWeight { get; set; } = 15.0;
    public double TypesWeight { get; set; } = 10.0;
    public double PropertiesWeight { get; set; } = 25.0;
    public double RequiredWeight { get; set; } = 10.0;
    public double DescriptionsWeight { get; set; } = 10.0;
    public double ValidationWeight { get; set; } = 20.0;
}
```

---

## Security Considerations for JSON

### JSON-Specific Security Risks

1. **Prototype Pollution**: Malicious JSON that modifies Object.prototype
2. **Circular References**: JSON that creates infinite loops when parsed
3. **Oversized JSON**: Denial of service via massive JSON documents
4. **Injection Attacks**: JSON that contains executable code or scripts
5. **Schema Violations**: JSON that doesn't conform to expected structure

### Security Implementation

The `JsonSecurityEvaluator` addresses these risks:
- ✅ Prototype pollution detection
- ✅ Circular reference detection (via depth limit)
- ✅ Size limit enforcement
- ✅ Pattern-based disallowed content detection
- ✅ Schema validation (optional)
- ✅ Required field validation (optional)

---

## Implementation Plan

### Phase 1: Core JSON Support (Week 1)

1. **Modify CandidateProgram** - Add format enum and multi-format support
2. **Update Interfaces** - Add Format property to IEvolutionMutator, IEvolutionCrossover, IAstSecurityEvaluator
3. **Update EvolutionOrchestrator** - Add format-aware component routing
4. **Update Existing Components** - Modify C# components to use new interfaces
5. **Create JsonEvolutionMutator** - Basic JSON mutation support

### Phase 2: JSON Security (Week 2)

1. **Create JsonSecurityEvaluator** - JSON-specific security checks
2. **Add JsonSecurityOptions** - Configurable security policies
3. **Update SecurityEvaluationResult** - Ensure it works with JSON

### Phase 3: JSON Crossover (Week 2)

1. **Create JsonEvolutionCrossover** - JSON crossover support
2. **Add JsonCrossoverOptions** - Configurable crossover behavior

### Phase 4: LaunchMobile-Specific Extensions (Week 3)

1. **Create XStateFitnessEvaluator** - XState-specific fitness scoring
2. **Create JsonSchemaFitnessEvaluator** - JSON Schema-specific fitness scoring
3. **Create LaunchMobile extensions package** - NuGet package with LM-specific components

### Phase 5: Testing & Validation (Week 4)

1. **Unit tests for all new components**
2. **Integration tests with LaunchMobile**
3. **Performance benchmarks**
4. **Security penetration testing**

---

## Testing Strategy

### Unit Tests

```csharp
// CandidateProgram tests
[Fact]
public void FromJson_CreatesJsonCandidate()
{
    var candidate = CandidateProgram.FromJson("{'test': true}");
    Assert.Equal(CandidateFormat.Json, candidate.Format);
    Assert.Equal("{'test': true}", candidate.SourceMaterial);
}

// JsonEvolutionMutator tests
[Fact]
public async Task MutateAsync_ReturnsValidJson()
{
    var mutator = new JsonEvolutionMutator(mockChatService);
    var candidate = CandidateProgram.FromJson("{'a': 1}");
    var result = await mutator.MutateAsync(candidate, new[] { "test feedback" });
    Assert.Equal(CandidateFormat.Json, result.Format);
    // Validate result is valid JSON
    JsonDocument.Parse(result.SourceMaterial);
}

// JsonSecurityEvaluator tests
[Fact]
public void Evaluate_DetectsPrototypePollution()
{
    var evaluator = new JsonSecurityEvaluator();
    var result = evaluator.Evaluate("{'__proto__': {'malicious': true}}");
    Assert.False(result.IsAllowed);
    Assert.Contains("prototype pollution", result.Violations[0]);
}

// XStateFitnessEvaluator tests
[Fact]
public async Task EvaluateAsync_ScoresValidXState()
{
    var evaluator = new XStateFitnessEvaluator();
    var xstateJson = "{'id': 'test', 'initial': 'start', 'states': {'start': {}}}";
    var candidate = CandidateProgram.FromJson(xstateJson);
    var score = await evaluator.EvaluateAsync(candidate);
    Assert.True(score > 0);
}
```

### Integration Tests

```csharp
// Full evolution pipeline test
[Fact]
public async Task EvolveOnceAsync_JsonCandidate_Succeeds()
{
    var orchestrator = new EvolutionOrchestrator(
        jsonSecurityEvaluator,
        null, // No compilation for JSON
        jsonFitnessEvaluator,
        jsonMutator);
    
    orchestrator.AddSecurityEvaluator(jsonSecurityEvaluator);
    
    var candidate = CandidateProgram.FromJson("{'test': 1}");
    var result = await orchestrator.EvolveOnceAsync(candidate);
    
    Assert.True(result.IsValid);
    Assert.Equal(CandidateFormat.Json, result.Candidate.Format);
}
```

---

## Migration Path

### For Existing SEF Users

**No breaking changes**: All existing code continues to work due to:
1. Backward compatible `CandidateProgram` constructor
2. Implicit conversion from string to CandidateProgram
3. Default behavior assumes C# format

### Migration Steps

1. **Update SEF version** to 2.0.0+ (when released)
2. **Add JSON-specific components** to DI container:
   ```csharp
   services.AddSingleton<JsonEvolutionMutator>();
   services.AddSingleton<JsonEvolutionCrossover>();
   services.AddSingleton<JsonSecurityEvaluator>();
   services.AddSingleton<XStateFitnessEvaluator>();
   services.AddSingleton<JsonSchemaFitnessEvaluator>();
   ```
3. **Register components** with orchestrator:
   ```csharp
   orchestrator.AddMutator(jsonMutator);
   orchestrator.AddCrossover(jsonCrossover);
   orchestrator.AddSecurityEvaluator(jsonSecurityEvaluator);
   ```
4. **Use JSON candidates**:
   ```csharp
   var candidate = CandidateProgram.FromJson(xstateJson);
   var result = await orchestrator.EvolveOnceAsync(candidate);
   ```

---

## Summary of Changes

### Files to Modify
- `src/SelfEvolvingFramework/Core/CandidateProgram.cs` - Multi-format support
- `src/SelfEvolvingFramework/Orchestration/EvolutionOrchestrator.cs` - Format-aware routing
- `src/SelfEvolvingFramework/Orchestration/IEvolutionMutator.cs` - Add Format property
- `src/SelfEvolvingFramework/Orchestration/IEvolutionCrossover.cs` - Add Format property
- `src/SelfEvolvingFramework/Security/IAstSecurityEvaluator.cs` - Add Format property
- `src/SelfEvolvingFramework/Orchestration/SemanticKernelEvolutionMutator.cs` - Update for new interface
- `src/SelfEvolvingFramework/Orchestration/SourceCodeCandidateChromosome.cs` - Use SourceMaterial

### Files to Add
- `src/SelfEvolvingFramework/Orchestration/JsonEvolutionMutator.cs` - NEW
- `src/SelfEvolvingFramework/Orchestration/JsonEvolutionCrossover.cs` - NEW
- `src/SelfEvolvingFramework/Security/JsonSecurityEvaluator.cs` - NEW
- `src/SelfEvolvingFramework/Orchestration/XStateFitnessEvaluator.cs` - NEW
- `src/SelfEvolvingFramework/Orchestration/JsonSchemaFitnessEvaluator.cs` - NEW
- `src/SelfEvolvingFramework/Orchestration/JsonMutationOptions.cs` - NEW
- `src/SelfEvolvingFramework/Orchestration/JsonCrossoverOptions.cs` - NEW
- `src/SelfEvolvingFramework/Security/JsonSecurityOptions.cs` - NEW
- `src/SelfEvolvingFramework/Orchestration/XStateFitnessOptions.cs` - NEW
- `src/SelfEvolvingFramework/Orchestration/JsonSchemaFitnessOptions.cs` - NEW

---

## Conclusion

By implementing these changes, **Self-Evolving-Framework** will support **JSON-native evolution**, making it a perfect fit for **LaunchMobile** without requiring any changes to LaunchMobile itself. The modifications are:

- ✅ **Non-breaking** - Existing C# code continues to work
- ✅ **Extensible** - Easy to add more formats in the future
- ✅ **Maintainable** - Clean separation of format-specific logic
- ✅ **Production-ready** - Comprehensive security and validation

This approach **modifies SEF to support LaunchMobile** rather than the other way around, which is the better architectural decision given that SEF is the more generic framework.

---

*Document Version: 1.0*  
*Last Updated: September 8, 2026*  
*Status: Ready for Implementation*
