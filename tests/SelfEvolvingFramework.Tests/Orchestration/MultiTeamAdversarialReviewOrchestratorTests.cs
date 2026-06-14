using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class MultiTeamAdversarialReviewOrchestratorTests
{
    [Fact]
    public void AssignRound_Rotates_Teams_Across_Roles()
    {
        var teams = new[]
        {
            new AdversarialTeamDefinition("team-1"),
            new AdversarialTeamDefinition("team-2"),
            new AdversarialTeamDefinition("team-3"),
            new AdversarialTeamDefinition("team-4")
        };

        var engine = new RoundRobinAdversarialRotationEngine();
        var roundOne = engine.AssignRound(teams, 1);
        var roundTwo = engine.AssignRound(teams, 2);

        Assert.Equal("team-1", roundOne.GetTeam(AdversarialRole.Proposer).TeamId);
        Assert.Equal("team-2", roundOne.GetTeam(AdversarialRole.Reviewer).TeamId);
        Assert.Equal("team-3", roundOne.GetTeam(AdversarialRole.Opponent).TeamId);
        Assert.Equal("team-4", roundOne.GetTeam(AdversarialRole.Steward).TeamId);
        Assert.Equal("team-1", roundOne.GetTeam(AdversarialRole.Fixer).TeamId);

        Assert.Equal("team-2", roundTwo.GetTeam(AdversarialRole.Proposer).TeamId);
        Assert.Equal("team-3", roundTwo.GetTeam(AdversarialRole.Reviewer).TeamId);
        Assert.Equal("team-4", roundTwo.GetTeam(AdversarialRole.Opponent).TeamId);
        Assert.Equal("team-1", roundTwo.GetTeam(AdversarialRole.Steward).TeamId);
        Assert.Equal("team-2", roundTwo.GetTeam(AdversarialRole.Fixer).TeamId);
    }

    [Fact]
    public async Task RunAsync_Stops_When_No_Accepted_Flaws_Remain()
    {
        var teams = new[]
        {
            new AdversarialTeamDefinition("team-1"),
            new AdversarialTeamDefinition("team-2"),
            new AdversarialTeamDefinition("team-3"),
            new AdversarialTeamDefinition("team-4")
        };

        var executor = new RecordingRoleExecutor();
        var adjudicator = new SequentialAdjudicationEngine(
            [new FlawDecision("F1", FlawDisposition.Accepted, "valid issue")],
            []);

        var orchestrator = new MultiTeamAdversarialReviewOrchestrator(
            new RoundRobinAdversarialRotationEngine(),
            executor,
            adjudicator,
            new AdversarialWorkflowOptions(MaxRounds: 3));

        var result = await orchestrator.RunAsync(
            new CandidateProgram("public static class Seed { }"),
            teams);

        Assert.True(result.Converged);
        Assert.Equal(2, result.Rounds.Count);
        Assert.Equal(2, executor.ProposeCalls);
        Assert.Equal(1, executor.FixCalls);
        Assert.Equal(1, result.Rounds[0].AcceptedDecisionCount);
        Assert.Equal(0, result.Rounds[0].DeferredDecisionCount);
        Assert.Equal(1, result.Rounds[0].UnresolvedDecisionCount);
        Assert.True(result.Rounds[0].HasUnresolvedDecisions);
        Assert.Equal(0, result.Rounds[1].UnresolvedDecisionCount);
        Assert.False(result.Rounds[1].HasUnresolvedDecisions);
    }

    [Fact]
    public async Task RunAsync_Continues_When_Deferred_Flaws_Remain()
    {
        var teams = new[]
        {
            new AdversarialTeamDefinition("team-1"),
            new AdversarialTeamDefinition("team-2"),
            new AdversarialTeamDefinition("team-3"),
            new AdversarialTeamDefinition("team-4")
        };

        var executor = new RecordingRoleExecutor();
        var adjudicator = new SequentialAdjudicationEngine(
            [new FlawDecision("F1", FlawDisposition.Deferred, "needs another round")],
            [new FlawDecision("F1", FlawDisposition.Rejected, "resolved")]);

        var orchestrator = new MultiTeamAdversarialReviewOrchestrator(
            new RoundRobinAdversarialRotationEngine(),
            executor,
            adjudicator,
            new AdversarialWorkflowOptions(MaxRounds: 3));

        var result = await orchestrator.RunAsync(
            new CandidateProgram("public static class Seed { }"),
            teams);

        Assert.True(result.Converged);
        Assert.Equal(2, result.Rounds.Count);
        Assert.Equal(2, executor.ProposeCalls);
        Assert.Equal(0, executor.FixCalls);
        Assert.Equal(1, result.Rounds[0].DeferredDecisionCount);
        Assert.Equal(0, result.Rounds[0].RejectedDecisionCount);
        Assert.Equal(1, result.Rounds[0].UnresolvedDecisionCount);
        Assert.True(result.Rounds[0].HasUnresolvedDecisions);
        Assert.Equal(1, result.Rounds[1].RejectedDecisionCount);
        Assert.Equal(0, result.Rounds[1].UnresolvedDecisionCount);
        Assert.False(result.Rounds[1].HasUnresolvedDecisions);
    }

    [Fact]
    public async Task RunAsync_Throws_When_Rotation_Assigns_Same_Reviewer_And_Opponent()
    {
        var orchestrator = new MultiTeamAdversarialReviewOrchestrator(
            new InvalidRotationEngine(),
            new RecordingRoleExecutor(),
            new SequentialAdjudicationEngine([], []));

        await Assert.ThrowsAsync<InvalidOperationException>(() => orchestrator.RunAsync(
            new CandidateProgram("public static class Seed { }"),
            [new AdversarialTeamDefinition("team-1")]));
    }

    [Fact]
    public async Task RunAsync_Carries_Deferred_Flaw_When_Not_ReReported_Next_Round()
    {
        var teams = new[]
        {
            new AdversarialTeamDefinition("team-1"),
            new AdversarialTeamDefinition("team-2"),
            new AdversarialTeamDefinition("team-3"),
            new AdversarialTeamDefinition("team-4")
        };

        var executor = new SequencedRoleExecutor(
            [
                [new FlawReport("F1", "needs deeper investigation", FlawSeverity.High, "trace")],
                []
            ]);
        var adjudicator = new SequentialAdjudicationEngine(
            [new FlawDecision("F1", FlawDisposition.Deferred, "collect more evidence")],
            [new FlawDecision("F1", FlawDisposition.Rejected, "invalid after follow-up")]);
        var orchestrator = new MultiTeamAdversarialReviewOrchestrator(
            new RoundRobinAdversarialRotationEngine(),
            executor,
            adjudicator,
            new AdversarialWorkflowOptions(MaxRounds: 3));

        var result = await orchestrator.RunAsync(
            new CandidateProgram("public static class Seed { }"),
            teams);

        Assert.True(result.Converged);
        Assert.Equal(2, result.Rounds.Count);
        Assert.Equal(["F1"], executor.OpposeFlawIdsByRound[0]);
        Assert.Equal(["F1"], executor.OpposeFlawIdsByRound[1]);
        Assert.Equal(["F1"], result.Rounds[1].Reports.Select(report => report.FlawId).ToArray());
    }

    private sealed class RecordingRoleExecutor : IAdversarialRoleExecutor
    {
        public int ProposeCalls { get; private set; }
        public int FixCalls { get; private set; }

        public Task<CandidateProgram> ProposeAsync(AdversarialRoleContext context, CancellationToken cancellationToken = default)
        {
            ProposeCalls++;
            return Task.FromResult(new CandidateProgram(context.CurrentCandidate.SourceCode, context.CurrentCandidate.Id));
        }

        public Task<IReadOnlyList<FlawReport>> ReviewAsync(AdversarialRoleContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FlawReport>>([new FlawReport("F1", "sample flaw", FlawSeverity.Medium)]);

        public Task<IReadOnlyList<FlawChallenge>> OpposeAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawReport> reports,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FlawChallenge>>([new FlawChallenge("F1", true, "challenge")]);

        public Task<CandidateProgram> StewardAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawDecision> decisions,
            CancellationToken cancellationToken = default)
            => Task.FromResult(context.CurrentCandidate);

        public Task<CandidateProgram> FixAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawDecision> acceptedFlaws,
            CancellationToken cancellationToken = default)
        {
            FixCalls++;
            return Task.FromResult(new CandidateProgram(context.CurrentCandidate.SourceCode, context.CurrentCandidate.Id));
        }
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

    private sealed class InvalidRotationEngine : IAdversarialRotationEngine
    {
        public AdversarialRoundAssignment AssignRound(IReadOnlyList<AdversarialTeamDefinition> teams, int roundNumber)
        {
            var team = new AdversarialTeamDefinition("team-1");
            return new AdversarialRoundAssignment(
                roundNumber,
                new Dictionary<AdversarialRole, AdversarialTeamDefinition>
                {
                    [AdversarialRole.Proposer] = team,
                    [AdversarialRole.Reviewer] = team,
                    [AdversarialRole.Opponent] = team,
                    [AdversarialRole.Steward] = team,
                    [AdversarialRole.Fixer] = team
                });
        }
    }

    private sealed class SequencedRoleExecutor(IReadOnlyList<IReadOnlyList<FlawReport>> reviewReportsByRound) : IAdversarialRoleExecutor
    {
        private int _reviewCalls;

        public List<string[]> OpposeFlawIdsByRound { get; } = [];

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
        {
            OpposeFlawIdsByRound.Add(reports.Select(report => report.FlawId).ToArray());
            return Task.FromResult<IReadOnlyList<FlawChallenge>>(
                reports.Select(report => new FlawChallenge(report.FlawId, true, "challenge")).ToArray());
        }

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
}
