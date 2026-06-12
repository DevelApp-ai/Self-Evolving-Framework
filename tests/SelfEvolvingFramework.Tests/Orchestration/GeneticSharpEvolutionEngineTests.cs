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
        var seed = new CandidateProgram("public static class Runner { public static int Execute() => 1; }");

        var best = await engine.EvolveAsync(
            seed,
            new GeneticSharpEvolutionEngineOptions(
                MinPopulationSize: 4,
                MaxPopulationSize: 4,
                MaxGenerations: 2,
                CrossoverProbability: 0,
                MutationProbability: 0));

        Assert.Equal(seed.SourceCode, best.SourceCode);
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
            new CandidateProgram("public static class Runner { }"),
            new GeneticSharpEvolutionEngineOptions(MinPopulationSize: 1)));
    }

    private sealed class RecordingMutator : IEvolutionMutator
    {
        public int CallCount { get; private set; }

        public Task<CandidateProgram> MutateAsync(
            CandidateProgram candidate,
            IReadOnlyList<string> feedback,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new CandidateProgram(
                candidate.SourceCode.Replace("=> 1", "=> 2", StringComparison.Ordinal),
                candidate.Id));
        }
    }

    private sealed class PassthroughCrossover : IEvolutionCrossover
    {
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
            return Task.FromResult(candidate.SourceCode.Contains("=> 2", StringComparison.Ordinal) ? 10d : 1d);
        }
    }
}
