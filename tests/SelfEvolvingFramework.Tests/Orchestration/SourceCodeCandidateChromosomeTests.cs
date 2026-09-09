using GeneticSharp;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class SourceCodeCandidateChromosomeTests
{
    [Fact]
    public void Constructor_Initializes_Candidate_And_Genes_From_Source()
    {
        var candidate = CandidateProgram.FromCSharp("public static class Runner { }");
        var chromosome = new SourceCodeCandidateChromosome(candidate);

        Assert.Same(candidate, chromosome.Candidate);
        Assert.Equal(candidate.SourceMaterial, Assert.IsType<string>(chromosome.GetGene(0).Value));
    }

    [Fact]
    public void SetCandidate_Updates_Candidate_And_Genes()
    {
        var original = CandidateProgram.FromCSharp("public static class A { }");
        var updated = CandidateProgram.FromCSharp("public static class B { }");
        var chromosome = new SourceCodeCandidateChromosome(original);

        chromosome.SetCandidate(updated);

        Assert.Same(updated, chromosome.Candidate);
        Assert.Equal(updated.SourceMaterial, Assert.IsType<string>(chromosome.GetGene(0).Value));
    }

    [Fact]
    public void Clone_Copies_Candidate_Genes_And_Fitness()
    {
        var candidate = CandidateProgram.FromCSharp("public static class Runner { }");
        var chromosome = new SourceCodeCandidateChromosome(candidate)
        {
            Fitness = 7.5
        };
        chromosome.ReplaceGene(0, new Gene("g0"));

        var clone = Assert.IsType<SourceCodeCandidateChromosome>(chromosome.Clone());

        Assert.NotSame(chromosome, clone);
        Assert.NotSame(candidate, clone.Candidate);
        Assert.Equal(chromosome.Fitness, clone.Fitness);
        Assert.Equal("g0", Assert.IsType<string>(clone.GetGene(0).Value));
    }
}
