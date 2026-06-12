using GeneticSharp;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

internal sealed class SourceCodeCandidateChromosome : ChromosomeBase
{
    public SourceCodeCandidateChromosome(CandidateProgram candidate) : base(2)
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
        => new SourceCodeCandidateChromosome(Candidate);

    public override IChromosome Clone()
    {
        var clone = new SourceCodeCandidateChromosome(Candidate)
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
