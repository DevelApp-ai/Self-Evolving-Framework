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
        Assert.All(result.Diagnostics, diagnostic => Assert.StartsWith("security: ", diagnostic, StringComparison.Ordinal));
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
        Assert.All(result.Diagnostics, diagnostic => Assert.StartsWith("compiler: ", diagnostic, StringComparison.Ordinal));
        Assert.Equal(0, fitnessEvaluator.CallCount);
    }

    [Fact]
    public async Task EvolveOnceAsync_Forwards_Feedback_To_Mutator()
    {
        var fitnessEvaluator = new ConstantFitnessEvaluator(1);
        var mutator = new CapturingMutator("public static class Runner { public static int Execute() => 1; }");
        var orchestrator = new EvolutionOrchestrator(
            new RoslynAstSecurityEvaluator(),
            new RoslynDynamicCompilationService(),
            fitnessEvaluator,
            mutator);

        var feedback = new[] { "compiler error", "security warning" };
        _ = await orchestrator.EvolveOnceAsync(new CandidateProgram("public static class Seed{}"), feedback);

        Assert.Equal(feedback, mutator.LastFeedback);
    }

    [Fact]
    public async Task EvolveOnceAsync_Forwards_Empty_Feedback_When_Null()
    {
        var fitnessEvaluator = new ConstantFitnessEvaluator(1);
        var mutator = new CapturingMutator("public static class Runner { public static int Execute() => 1; }");
        var orchestrator = new EvolutionOrchestrator(
            new RoslynAstSecurityEvaluator(),
            new RoslynDynamicCompilationService(),
            fitnessEvaluator,
            mutator);

        _ = await orchestrator.EvolveOnceAsync(new CandidateProgram("public static class Seed{}"), null);

        Assert.Empty(mutator.LastFeedback);
    }

    [Fact]
    public async Task EvolveOnceAsync_Forwards_Feedback_Snapshot_To_Mutator()
    {
        var fitnessEvaluator = new ConstantFitnessEvaluator(1);
        var mutator = new CapturingMutator("public static class Runner { public static int Execute() => 1; }");
        var orchestrator = new EvolutionOrchestrator(
            new RoslynAstSecurityEvaluator(),
            new RoslynDynamicCompilationService(),
            fitnessEvaluator,
            mutator);

        var feedback = new List<string> { "compiler error", "security warning" };
        _ = await orchestrator.EvolveOnceAsync(new CandidateProgram("public static class Seed{}"), feedback);

        Assert.Equal(feedback, mutator.LastFeedback);
        Assert.False(ReferenceEquals(feedback, mutator.LastFeedback));
    }

    [Fact]
    public async Task EvolveOnceAsync_Forwards_Budget_CancellationToken()
    {
        var fitnessEvaluator = new ConstantFitnessEvaluator(1);
        var mutator = new CapturingMutator("public static class Runner { public static int Execute() => 1; }");
        var orchestrator = new EvolutionOrchestrator(
            new RoslynAstSecurityEvaluator(),
            new RoslynDynamicCompilationService(),
            fitnessEvaluator,
            mutator,
            new EvolutionOrchestratorOptions(ExecutionBudgetMilliseconds: 1000));

        _ = await orchestrator.EvolveOnceAsync(new CandidateProgram("public static class Seed{}"));

        Assert.True(mutator.LastCancellationToken.CanBeCanceled);
    }

    [Fact]
    public async Task EvolveOnceAsync_Throws_When_Execution_Budget_Exceeded()
    {
        var orchestrator = new EvolutionOrchestrator(
            new RoslynAstSecurityEvaluator(),
            new RoslynDynamicCompilationService(),
            new ConstantFitnessEvaluator(1),
            new DelayingMutator(),
            new EvolutionOrchestratorOptions(ExecutionBudgetMilliseconds: 25));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            orchestrator.EvolveOnceAsync(new CandidateProgram("public static class Seed{}")));
    }

    [Fact]
    public async Task EvolveOnceAsync_Throws_For_Invalid_Execution_Budget()
    {
        var orchestrator = new EvolutionOrchestrator(
            new RoslynAstSecurityEvaluator(),
            new RoslynDynamicCompilationService(),
            new ConstantFitnessEvaluator(1),
            new ConstantMutator("public static class Runner { public static int Execute() => 1; }"),
            new EvolutionOrchestratorOptions(ExecutionBudgetMilliseconds: 0));

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            orchestrator.EvolveOnceAsync(new CandidateProgram("public static class Seed{}")));

        Assert.Equal("options", exception.ParamName);
    }

    private sealed class ConstantMutator(string sourceCode) : IEvolutionMutator
    {
        public Task<CandidateProgram> MutateAsync(CandidateProgram candidate, IReadOnlyList<string> feedback, CancellationToken cancellationToken = default)
            => Task.FromResult(new CandidateProgram(sourceCode, candidate.Id));
    }

    private sealed class CapturingMutator(string sourceCode) : IEvolutionMutator
    {
        public IReadOnlyList<string> LastFeedback { get; private set; } = [];
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<CandidateProgram> MutateAsync(CandidateProgram candidate, IReadOnlyList<string> feedback, CancellationToken cancellationToken = default)
        {
            LastFeedback = feedback;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(new CandidateProgram(sourceCode, candidate.Id));
        }
    }

    private sealed class DelayingMutator : IEvolutionMutator
    {
        public async Task<CandidateProgram> MutateAsync(CandidateProgram candidate, IReadOnlyList<string> feedback, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return candidate;
        }
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
