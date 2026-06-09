using SelfEvolvingFramework.Compilation;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;
using SelfEvolvingFramework.Security;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class EvolutionOrchestratorTests
{
    [Fact]
    public async Task EvolveOnceAsync_Returns_Invalid_For_Security_Violation()
    {
        var orchestrator = new EvolutionOrchestrator(
            new RoslynAstSecurityEvaluator(),
            new RoslynDynamicCompilationService(),
            new ConstantFitnessEvaluator(1),
            new ConstantMutator("using System.IO; public static class Runner { public static int Execute() => 1; }"));

        var result = await orchestrator.EvolveOnceAsync(new CandidateProgram("public static class Seed{}"));

        Assert.False(result.IsValid);
        Assert.Equal(double.NegativeInfinity, result.Fitness);
    }

    [Fact]
    public async Task EvolveOnceAsync_Returns_Fitness_For_Valid_Candidate()
    {
        var fitnessEvaluator = new ConstantFitnessEvaluator(9.5);
        var orchestrator = new EvolutionOrchestrator(
            new RoslynAstSecurityEvaluator(),
            new RoslynDynamicCompilationService(),
            fitnessEvaluator,
            new ConstantMutator("public static class Runner { public static int Execute() => 1; }"));

        var result = await orchestrator.EvolveOnceAsync(new CandidateProgram("public static class Seed{}"));

        Assert.True(result.IsValid);
        Assert.Equal(9.5, result.Fitness);
        Assert.Equal(1, fitnessEvaluator.CallCount);
    }

    [Fact]
    public async Task EvolveOnceAsync_Returns_Invalid_For_Compilation_Failure_And_Skips_Fitness()
    {
        var fitnessEvaluator = new ConstantFitnessEvaluator(5);
        var orchestrator = new EvolutionOrchestrator(
            new RoslynAstSecurityEvaluator(),
            new RoslynDynamicCompilationService(),
            fitnessEvaluator,
            new ConstantMutator("public static class Runner { public static int Execute( => 1; }"));

        var result = await orchestrator.EvolveOnceAsync(new CandidateProgram("public static class Seed{}"));

        Assert.False(result.IsValid);
        Assert.Equal(0, result.Fitness);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Equal(0, fitnessEvaluator.CallCount);
    }

    private sealed class ConstantMutator(string sourceCode) : IEvolutionMutator
    {
        public Task<CandidateProgram> MutateAsync(CandidateProgram candidate, IReadOnlyList<string> feedback, CancellationToken cancellationToken = default)
            => Task.FromResult(new CandidateProgram(sourceCode, candidate.Id));
    }

    private sealed class ConstantFitnessEvaluator(double fitness) : IFitnessEvaluator
    {
        public int CallCount { get; private set; }

        public Task<double> EvaluateAsync(CandidateProgram candidate, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(fitness);
        }
    }
}
