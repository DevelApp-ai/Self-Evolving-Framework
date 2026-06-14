using SelfEvolvingFramework.Compilation;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;
using SelfEvolvingFramework.Security;

namespace SelfEvolvingFramework.Tests.Integration;

public sealed class MalformedAdjudicationIntegrationTests
{
    [Fact]
    public async Task EvolveOnceAsync_Does_Not_Adjust_Fitness_For_Unknown_Flaw_Decision()
    {
        var reviewOrchestrator = new MultiTeamAdversarialReviewOrchestrator(
            new RoundRobinAdversarialRotationEngine(),
            new SingleFlawRoleExecutor(),
            new UnknownFlawAdjudicationEngine(),
            new AdversarialWorkflowOptions(MaxRounds: 1));

        var reviewResult = await reviewOrchestrator.RunAsync(
            new CandidateProgram("public static class Runner { public static int Execute() => 1; }"),
            BuildTeams());
        var evolutionOrchestrator = new EvolutionOrchestrator(
            new RoslynAstSecurityEvaluator(),
            new RoslynDynamicCompilationService(),
            new ConstantFitnessEvaluator(10),
            new ConstantMutator("public static class Runner { public static int Execute() => 1; }"));

        var result = await evolutionOrchestrator.EvolveOnceAsync(
            new CandidateProgram("public static class Seed { }"),
            adversarialRounds: reviewResult.Rounds);

        Assert.True(reviewResult.Converged);
        Assert.Single(reviewResult.Rounds);
        Assert.True(result.IsValid);
        Assert.Equal(10, result.Fitness);
    }

    [Fact]
    public async Task EvolveOnceAsync_Handles_Conflicting_Decisions_For_Same_Flaw()
    {
        var reviewOrchestrator = new MultiTeamAdversarialReviewOrchestrator(
            new RoundRobinAdversarialRotationEngine(),
            new SingleFlawRoleExecutor(),
            new ConflictingDecisionAdjudicationEngine(),
            new AdversarialWorkflowOptions(MaxRounds: 1));

        var reviewResult = await reviewOrchestrator.RunAsync(
            new CandidateProgram("public static class Runner { public static int Execute() => 1; }"),
            BuildTeams());
        var evolutionOrchestrator = new EvolutionOrchestrator(
            new RoslynAstSecurityEvaluator(),
            new RoslynDynamicCompilationService(),
            new ConstantFitnessEvaluator(10),
            new ConstantMutator("public static class Seed { }"));

        var result = await evolutionOrchestrator.EvolveOnceAsync(
            new CandidateProgram("public static class Seed { }"),
            adversarialRounds: reviewResult.Rounds);

        Assert.False(reviewResult.Converged);
        Assert.Single(reviewResult.Rounds);
        Assert.True(result.IsValid);
        Assert.Equal(10.5, result.Fitness);
        Assert.Contains("=> 2;", reviewResult.FinalCandidate.SourceCode, StringComparison.Ordinal);
    }

    private static IReadOnlyList<AdversarialTeamDefinition> BuildTeams()
        =>
        [
            new AdversarialTeamDefinition("team-1"),
            new AdversarialTeamDefinition("team-2"),
            new AdversarialTeamDefinition("team-3"),
            new AdversarialTeamDefinition("team-4")
        ];

    private sealed class SingleFlawRoleExecutor : IAdversarialRoleExecutor
    {
        public Task<CandidateProgram> ProposeAsync(AdversarialRoleContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(context.CurrentCandidate);

        public Task<IReadOnlyList<FlawReport>> ReviewAsync(AdversarialRoleContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FlawReport>>([new FlawReport("F1", "issue", FlawSeverity.Medium, "trace")]);

        public Task<IReadOnlyList<FlawChallenge>> OpposeAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawReport> reports,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FlawChallenge>>([new FlawChallenge("F1", true, "disputed")]);

        public Task<CandidateProgram> StewardAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawDecision> decisions,
            CancellationToken cancellationToken = default)
            => Task.FromResult(context.CurrentCandidate);

        public Task<CandidateProgram> FixAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawDecision> acceptedFlaws,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new CandidateProgram("public static class Runner { public static int Execute() => 2; }", context.CurrentCandidate.Id));
    }

    private sealed class UnknownFlawAdjudicationEngine : IFlawAdjudicationEngine
    {
        public Task<IReadOnlyList<FlawDecision>> DecideAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawReport> reports,
            IReadOnlyList<FlawChallenge> challenges,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FlawDecision>>([new FlawDecision("UNKNOWN", FlawDisposition.Accepted, "malformed output")]);
    }

    private sealed class ConflictingDecisionAdjudicationEngine : IFlawAdjudicationEngine
    {
        public Task<IReadOnlyList<FlawDecision>> DecideAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawReport> reports,
            IReadOnlyList<FlawChallenge> challenges,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FlawDecision>>(
            [
                new FlawDecision("F1", FlawDisposition.Accepted, "first malformed decision"),
                new FlawDecision("F1", FlawDisposition.Rejected, "conflicting malformed decision")
            ]);
    }

    private sealed class ConstantMutator(string sourceCode) : IEvolutionMutator
    {
        public Task<CandidateProgram> MutateAsync(CandidateProgram candidate, IReadOnlyList<string> feedback, CancellationToken cancellationToken = default)
            => Task.FromResult(new CandidateProgram(sourceCode, candidate.Id));
    }

    private sealed class ConstantFitnessEvaluator(double fitness) : IFitnessEvaluator
    {
        public Task<double> EvaluateAsync(CandidateProgram candidate, CancellationToken cancellationToken = default)
            => Task.FromResult(fitness);
    }
}
