using GeneticSharp;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public sealed record GeneticSharpEvolutionEngineOptions(
    int MinPopulationSize = 4,
    int MaxPopulationSize = 8,
    int MaxGenerations = 3,
    float CrossoverProbability = GeneticAlgorithm.DefaultCrossoverProbability,
    float MutationProbability = GeneticAlgorithm.DefaultMutationProbability);

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
            new CandidateChromosome(seed));

        var engineCrossover = new EvolutionCrossoverAdapter(crossover, cancellationToken);
        var engineMutation = new EvolutionMutationAdapter(mutator, cancellationToken);
        var engineFitness = new EvolutionFitnessAdapter(fitnessEvaluator, cancellationToken);

        var algorithm = new GeneticAlgorithm(
            population,
            engineFitness,
            new EliteSelection(),
            engineCrossover,
            engineMutation)
        {
            CrossoverProbability = effectiveOptions.CrossoverProbability,
            MutationProbability = effectiveOptions.MutationProbability,
            Termination = new GenerationNumberTermination(effectiveOptions.MaxGenerations)
        };

        await Task.Run(algorithm.Start, cancellationToken);

        return algorithm.BestChromosome is CandidateChromosome best
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
    }

    private sealed class CandidateChromosome : ChromosomeBase
    {
        public CandidateChromosome(CandidateProgram candidate) : base(2)
        {
            SetCandidate(candidate);
        }

        public CandidateProgram Candidate { get; private set; } = null!;

        public override Gene GenerateGene(int geneIndex)
        {
            if (geneIndex is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(geneIndex));
            }

            return new Gene(Candidate.SourceCode);
        }

        public override IChromosome CreateNew()
            => new CandidateChromosome(Candidate);

        public override IChromosome Clone()
        {
            var clone = new CandidateChromosome(Candidate)
            {
                Fitness = Fitness
            };

            clone.ReplaceGene(0, GetGene(0));
            clone.ReplaceGene(1, GetGene(1));
            return clone;
        }

        public void SetCandidate(CandidateProgram candidateProgram)
        {
            Candidate = candidateProgram;
            ReplaceGene(0, new Gene(candidateProgram.SourceCode));
            ReplaceGene(1, new Gene(candidateProgram.SourceCode));
        }
    }

    private sealed class EvolutionFitnessAdapter(
        IFitnessEvaluator fitnessEvaluator,
        CancellationToken cancellationToken) : IFitness
    {
        public double Evaluate(IChromosome chromosome)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidateChromosome = chromosome as CandidateChromosome
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

            var candidateChromosome = chromosome as CandidateChromosome
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
            var parentA = parents[0] as CandidateChromosome
                ?? throw new ArgumentException("Parent chromosome must be a candidate chromosome.", nameof(parents));
            var parentB = parents[1] as CandidateChromosome
                ?? throw new ArgumentException("Parent chromosome must be a candidate chromosome.", nameof(parents));
            var child = crossover.CrossoverAsync(parentA.Candidate, parentB.Candidate, cancellationToken).GetAwaiter().GetResult();
            return [new CandidateChromosome(child)];
        }
    }
}
