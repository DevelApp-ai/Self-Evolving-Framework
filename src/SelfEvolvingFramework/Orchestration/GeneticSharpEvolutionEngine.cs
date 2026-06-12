using GeneticSharp;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public enum GeneticSharpSelectionStrategy
{
    Elite,
    Tournament
}

public sealed record GeneticSharpEvolutionEngineOptions(
    int MinPopulationSize = 4,
    int MaxPopulationSize = 8,
    int MaxGenerations = 3,
    GeneticSharpSelectionStrategy SelectionStrategy = GeneticSharpSelectionStrategy.Elite,
    float CrossoverProbability = GeneticAlgorithm.DefaultCrossoverProbability,
    float MutationProbability = GeneticAlgorithm.DefaultMutationProbability,
    Func<int, int, float, float>? MutationRateStrategyHook = null);

public sealed class GeneticSharpEvolutionEngine(
    IFitnessEvaluator fitnessEvaluator,
    IEvolutionMutator mutator,
    IEvolutionCrossover crossover)
{
    public async Task<CandidateProgram> EvolveAsync(
        CandidateProgram seed,
        GeneticSharpEvolutionEngineOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(seed);
        var effectiveOptions = options ?? new GeneticSharpEvolutionEngineOptions();
        ValidateOptions(effectiveOptions);

        var population = new Population(
            effectiveOptions.MinPopulationSize,
            effectiveOptions.MaxPopulationSize,
            new SourceCodeCandidateChromosome(seed));

        var engineCrossover = new EvolutionCrossoverAdapter(crossover, cancellationToken);
        var engineMutation = new EvolutionMutationAdapter(mutator, cancellationToken);
        var engineFitness = new EvolutionFitnessAdapter(fitnessEvaluator, cancellationToken);

        var algorithm = new GeneticAlgorithm(
            population,
            engineFitness,
            CreateSelection(effectiveOptions.SelectionStrategy),
            engineCrossover,
            engineMutation)
        {
            CrossoverProbability = effectiveOptions.CrossoverProbability,
            MutationProbability = effectiveOptions.MutationProbability,
            Termination = new GenerationNumberTermination(effectiveOptions.MaxGenerations)
        };

        if (effectiveOptions.MutationRateStrategyHook is { } mutationRateStrategyHook)
        {
            algorithm.GenerationRan += (_, _) =>
            {
                var nextMutationProbability = mutationRateStrategyHook(
                    algorithm.GenerationsNumber,
                    effectiveOptions.MaxGenerations,
                    algorithm.MutationProbability);

                if (nextMutationProbability is < 0 or > 1)
                {
                    throw new InvalidOperationException("Mutation rate strategy hook must return a value in the range [0, 1].");
                }

                algorithm.MutationProbability = nextMutationProbability;
            };
        }

        await Task.Run(algorithm.Start, cancellationToken);

        return algorithm.BestChromosome is SourceCodeCandidateChromosome best
            ? best.Candidate
            : seed;
    }

    private static void ValidateOptions(GeneticSharpEvolutionEngineOptions options)
    {
        if (options.MinPopulationSize < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Minimum population size must be at least 2.");
        }

        if (options.MaxPopulationSize < options.MinPopulationSize)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Maximum population size must be greater than or equal to minimum size.");
        }

        if (options.MaxGenerations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Maximum generations must be at least 1.");
        }

        if (options.CrossoverProbability is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Crossover probability must be in the range [0, 1].");
        }

        if (options.MutationProbability is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Mutation probability must be in the range [0, 1].");
        }

        if (!Enum.IsDefined(options.SelectionStrategy))
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Selection strategy is not supported.");
        }
    }

    private static ISelection CreateSelection(GeneticSharpSelectionStrategy selectionStrategy)
        => selectionStrategy switch
        {
            GeneticSharpSelectionStrategy.Elite => new EliteSelection(),
            GeneticSharpSelectionStrategy.Tournament => new TournamentSelection(),
            _ => throw new ArgumentOutOfRangeException(nameof(selectionStrategy), selectionStrategy, "Selection strategy is not supported.")
        };

    private sealed class EvolutionFitnessAdapter(
        IFitnessEvaluator fitnessEvaluator,
        CancellationToken cancellationToken) : IFitness
    {
        public double Evaluate(IChromosome chromosome)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidateChromosome = chromosome as SourceCodeCandidateChromosome
                ?? throw new ArgumentException("Chromosome must be a candidate chromosome.", nameof(chromosome));
            return fitnessEvaluator.EvaluateAsync(candidateChromosome.Candidate, cancellationToken).GetAwaiter().GetResult();
        }
    }

    private sealed class EvolutionMutationAdapter(
        IEvolutionMutator mutator,
        CancellationToken cancellationToken) : MutationBase
    {
        protected override void PerformMutate(IChromosome chromosome, float probability)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (probability <= 0)
            {
                return;
            }

            var candidateChromosome = chromosome as SourceCodeCandidateChromosome
                ?? throw new ArgumentException("Chromosome must be a candidate chromosome.", nameof(chromosome));
            var mutated = mutator.MutateAsync(candidateChromosome.Candidate, [], cancellationToken).GetAwaiter().GetResult();
            candidateChromosome.SetCandidate(mutated);
        }
    }

    private sealed class EvolutionCrossoverAdapter(
        IEvolutionCrossover crossover,
        CancellationToken cancellationToken) : CrossoverBase(2, 1, 2)
    {
        protected override IList<IChromosome> PerformCross(IList<IChromosome> parents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parentA = parents[0] as SourceCodeCandidateChromosome
                ?? throw new ArgumentException("Parent chromosome must be a candidate chromosome.", nameof(parents));
            var parentB = parents[1] as SourceCodeCandidateChromosome
                ?? throw new ArgumentException("Parent chromosome must be a candidate chromosome.", nameof(parents));
            var child = crossover.CrossoverAsync(parentA.Candidate, parentB.Candidate, cancellationToken).GetAwaiter().GetResult();
            return [new SourceCodeCandidateChromosome(child)];
        }
    }
}
