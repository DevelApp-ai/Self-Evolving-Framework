using GeneticSharp;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class SourceCodeCandidateChromosomeTests
{
    [Fact]
    public void Constructor_Initializes_Candidate_And_Genes_From_Source()
    {
        var candidate = new CandidateProgram("public static class Runner { }");
        var chromosome = new SourceCodeCandidateChromosome(candidate);

        Assert.Same(candidate, chromosome.Candidate);
        Assert.Equal(candidate.SourceCode, Assert.IsType<string>(chromosome.GetGene(0).Value));
        Assert.Equal(candidate.SourceCode, Assert.IsType<string>(chromosome.GetGene(1).Value));
    }

    [Fact]
    public void SetCandidate_Updates_Candidate_And_Genes()
    {
        var original = new CandidateProgram("public static class A { }");
        var updated = new CandidateProgram("public static class B { }");
        var chromosome = new SourceCodeCandidateChromosome(original);

        chromosome.SetCandidate(updated);

        Assert.Same(updated, chromosome.Candidate);
        Assert.Equal(updated.SourceCode, Assert.IsType<string>(chromosome.GetGene(0).Value));
        Assert.Equal(updated.SourceCode, Assert.IsType<string>(chromosome.GetGene(1).Value));
    }

    [Fact]
    public void Clone_Copies_Candidate_Genes_And_Fitness()
    {
        var candidate = new CandidateProgram("public static class Runner { }");
        var chromosome = new SourceCodeCandidateChromosome(candidate)
        {
            Fitness = 7.5
        };
        chromosome.ReplaceGene(0, new Gene("g0"));
        chromosome.ReplaceGene(1, new Gene("g1"));

        var clone = Assert.IsType<SourceCodeCandidateChromosome>(chromosome.Clone());

        Assert.NotSame(chromosome, clone);
        Assert.Same(candidate, clone.Candidate);
        Assert.Equal(chromosome.Fitness, clone.Fitness);
        Assert.Equal("g0", Assert.IsType<string>(clone.GetGene(0).Value));
        Assert.Equal("g1", Assert.IsType<string>(clone.GetGene(1).Value));
    }
}
