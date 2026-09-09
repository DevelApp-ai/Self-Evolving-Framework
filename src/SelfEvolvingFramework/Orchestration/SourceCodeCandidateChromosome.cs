using GeneticSharp;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

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
