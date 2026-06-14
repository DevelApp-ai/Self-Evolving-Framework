using SelfEvolvingFramework.Compilation;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;
using SelfEvolvingFramework.Security;

namespace SelfEvolvingFramework.Tests.Integration;

public sealed class AdversarialEvolutionIntegrationTests
{
    [Fact]
    public async Task EvolveOnceAsync_Applies_CarryForwarded_Adversarial_Outcomes_From_Review_Orchestrator()
    {
        var teams = new[]
        {
            new AdversarialTeamDefinition("team-1"),
            new AdversarialTeamDefinition("team-2"),
            new AdversarialTeamDefinition("team-3"),
            new AdversarialTeamDefinition("team-4")
        };
        var reviewOrchestrator = new MultiTeamAdversarialReviewOrchestrator(
            new RoundRobinAdversarialRotationEngine(),
            new SequencedReviewExecutor(
                [
                    [new FlawReport("F1", "needs retest", FlawSeverity.High, "trace")],
                    []
                ]),
            new SequentialAdjudicationEngine(
                [new FlawDecision("F1", FlawDisposition.Deferred, "collect more evidence")],
                [new FlawDecision("F1", FlawDisposition.Rejected, "invalid after follow-up")]),
            new AdversarialWorkflowOptions(MaxRounds: 3));

        var reviewResult = await reviewOrchestrator.RunAsync(
            new CandidateProgram("public static class Runner { public static int Execute() => 1; }"),
            teams);
        var evolutionOrchestrator = new EvolutionOrchestrator(
            new RoslynAstSecurityEvaluator(),
            new RoslynDynamicCompilationService(),
            new ConstantFitnessEvaluator(10),
            new ConstantMutator("public static class Runner { public static int Execute() => 1; }"));

        var result = await evolutionOrchestrator.EvolveOnceAsync(
            new CandidateProgram("public static class Seed{}"),
            adversarialRounds: reviewResult.Rounds);

        Assert.True(reviewResult.Converged);
        Assert.Equal(2, reviewResult.Rounds.Count);
        Assert.True(result.IsValid);
        Assert.Equal(9.75, result.Fitness);
    }

    private sealed class SequencedReviewExecutor(IReadOnlyList<IReadOnlyList<FlawReport>> reviewReportsByRound) : IAdversarialRoleExecutor
    {
        private int _reviewCalls;

        public Task<CandidateProgram> ProposeAsync(AdversarialRoleContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new CandidateProgram(context.CurrentCandidate.SourceCode, context.CurrentCandidate.Id));

        public Task<IReadOnlyList<FlawReport>> ReviewAsync(AdversarialRoleContext context, CancellationToken cancellationToken = default)
        {
            var index = Math.Min(_reviewCalls, reviewReportsByRound.Count - 1);
            _reviewCalls++;
            return Task.FromResult(reviewReportsByRound[index]);
        }

        public Task<IReadOnlyList<FlawChallenge>> OpposeAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawReport> reports,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FlawChallenge>>(
                reports.Select(report => new FlawChallenge(report.FlawId, true, "challenge")).ToArray());

        public Task<CandidateProgram> StewardAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawDecision> decisions,
            CancellationToken cancellationToken = default)
            => Task.FromResult(context.CurrentCandidate);

        public Task<CandidateProgram> FixAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawDecision> acceptedFlaws,
            CancellationToken cancellationToken = default)
            => Task.FromResult(context.CurrentCandidate);
    }

    private sealed class SequentialAdjudicationEngine(
        IReadOnlyList<FlawDecision> firstRound,
        IReadOnlyList<FlawDecision> secondRound) : IFlawAdjudicationEngine
    {
        private int _calls;

        public Task<IReadOnlyList<FlawDecision>> DecideAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawReport> reports,
            IReadOnlyList<FlawChallenge> challenges,
            CancellationToken cancellationToken = default)
        {
            _calls++;
            return Task.FromResult(_calls == 1 ? firstRound : secondRound);
        }
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
