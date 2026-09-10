using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class GeneticSharpEvolutionEngineTests
{
    [Fact]
    public async Task EvolveAsync_Uses_GeneticSharp_Lifecycle_And_Returns_Best_Candidate()
    {
        var mutator = new RecordingMutator();
        var crossover = new PassthroughCrossover();
        var fitness = new ScoreBySourceFitnessEvaluator();
        var engine = new GeneticSharpEvolutionEngine(fitness, mutator, crossover);
        var seed = CandidateProgram.FromCSharp("public static class Runner { public static int Execute() => 1; }");

        var best = await engine.EvolveAsync(
            seed,
            new GeneticSharpEvolutionEngineOptions(
                MinPopulationSize: 4,
                MaxPopulationSize: 4,
                MaxGenerations: 2,
                CrossoverProbability: 0,
                MutationProbability: 0));

        Assert.Equal(seed.SourceMaterial, best.SourceMaterial);
        Assert.Equal(0, mutator.CallCount);
        Assert.True(fitness.CallCount > 0);
    }

    [Fact]
    public async Task EvolveAsync_Throws_For_Invalid_Options()
    {
        var engine = new GeneticSharpEvolutionEngine(
            new ScoreBySourceFitnessEvaluator(),
            new RecordingMutator(),
            new PassthroughCrossover());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => engine.EvolveAsync(
            CandidateProgram.FromCSharp("public static class Runner { }"),
            new GeneticSharpEvolutionEngineOptions(MinPopulationSize: 1)));
    }

    [Fact]
    public async Task EvolveAsync_Supports_Tournament_Selection()
    {
        var mutator = new RecordingMutator();
        var fitness = new ScoreBySourceFitnessEvaluator();
        var engine = new GeneticSharpEvolutionEngine(fitness, mutator, new PassthroughCrossover());
        var seed = CandidateProgram.FromCSharp("public static class Runner { public static int Execute() => 1; }");

        var best = await engine.EvolveAsync(
            seed,
            new GeneticSharpEvolutionEngineOptions(
                MinPopulationSize: 4,
                MaxPopulationSize: 4,
                MaxGenerations: 1,
                SelectionStrategy: GeneticSharpSelectionStrategy.Tournament,
                CrossoverProbability: 0,
                MutationProbability: 0));

        Assert.Equal(seed.SourceMaterial, best.SourceMaterial);
        Assert.True(fitness.CallCount > 0);
    }

    [Fact]
    public async Task EvolveAsync_Throws_For_Invalid_Selection_Strategy()
    {
        var engine = new GeneticSharpEvolutionEngine(
            new ScoreBySourceFitnessEvaluator(),
            new RecordingMutator(),
            new PassthroughCrossover());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => engine.EvolveAsync(
            CandidateProgram.FromCSharp("public static class Runner { }"),
            new GeneticSharpEvolutionEngineOptions(SelectionStrategy: (GeneticSharpSelectionStrategy)999)));
    }

    [Fact]
    public async Task EvolveAsync_Invokes_Mutation_Rate_Strategy_Hook()
    {
        var mutationRateCalls = new List<(int Generation, int MaxGenerations, float CurrentProbability)>();
        var mutator = new RecordingMutator();
        var fitness = new ScoreBySourceFitnessEvaluator();
        var engine = new GeneticSharpEvolutionEngine(fitness, mutator, new PassthroughCrossover());
        var seed = CandidateProgram.FromCSharp("public static class Runner { public static int Execute() => 1; }");

        var best = await engine.EvolveAsync(
            seed,
            new GeneticSharpEvolutionEngineOptions(
                MinPopulationSize: 4,
                MaxPopulationSize: 4,
                MaxGenerations: 2,
                CrossoverProbability: 0,
                MutationProbability: 1,
                MutationRateStrategyHook: (generation, maxGenerations, currentProbability) =>
                {
                    mutationRateCalls.Add((generation, maxGenerations, currentProbability));
                    return generation >= 1 ? 0 : currentProbability;
                }));

        Assert.NotNull(best);
        Assert.NotEmpty(mutationRateCalls);
        Assert.All(mutationRateCalls, call => Assert.Equal(2, call.MaxGenerations));
    }

    [Fact]
    public async Task EvolveAsync_Throws_When_Mutation_Rate_Hook_Returns_Out_Of_Range()
    {
        var engine = new GeneticSharpEvolutionEngine(
            new ScoreBySourceFitnessEvaluator(),
            new RecordingMutator(),
            new PassthroughCrossover());

        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.EvolveAsync(
            CandidateProgram.FromCSharp("public static class Runner { public static int Execute() => 1; }"),
            new GeneticSharpEvolutionEngineOptions(
                MinPopulationSize: 4,
                MaxPopulationSize: 4,
                MaxGenerations: 1,
                CrossoverProbability: 0,
                MutationProbability: 1,
                MutationRateStrategyHook: (_, _, _) => -0.1f)));
    }

    private sealed class RecordingMutator : IEvolutionMutator
    {
        public CandidateFormat Format => CandidateFormat.CSharp;
        public int CallCount { get; private set; }

        public Task<CandidateProgram> MutateAsync(
            CandidateProgram candidate,
            IReadOnlyList<string> feedback,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(CandidateProgram.FromCSharp(
                candidate.SourceMaterial.Replace("=> 1", "=> 2", StringComparison.Ordinal),
                candidate.ParentId,
                candidate.Id));
        }
    }

    private sealed class PassthroughCrossover : IEvolutionCrossover
    {
        public CandidateFormat Format => CandidateFormat.CSharp;
        public Task<CandidateProgram> CrossoverAsync(
            CandidateProgram parentA,
            CandidateProgram parentB,
            CancellationToken cancellationToken = default)
            => Task.FromResult(parentA);
    }

    private sealed class ScoreBySourceFitnessEvaluator : IFitnessEvaluator
    {
        public int CallCount { get; private set; }

        public Task<double> EvaluateAsync(CandidateProgram candidate, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(candidate.SourceMaterial.Contains("=> 2", StringComparison.Ordinal) ? 10d : 1d);
        }
    }
}
