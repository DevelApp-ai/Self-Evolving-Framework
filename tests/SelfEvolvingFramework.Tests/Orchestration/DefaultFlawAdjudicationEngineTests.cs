using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class DefaultFlawAdjudicationEngineTests
{
    [Fact]
    public async Task DecideAsync_Accepts_When_No_Challenge_Exists()
    {
        var engine = new DefaultFlawAdjudicationEngine();
        var decisions = await engine.DecideAsync(
            BuildContext(),
            [new FlawReport("F1", "issue", FlawSeverity.Medium)],
            []);

        Assert.Single(decisions);
        Assert.Equal(FlawDisposition.Accepted, decisions[0].Disposition);
    }

    [Fact]
    public async Task DecideAsync_Rejects_Disputed_Flaw_With_Weak_Evidence()
    {
        var engine = new DefaultFlawAdjudicationEngine();
        var decisions = await engine.DecideAsync(
            BuildContext(),
            [new FlawReport("F1", "issue", FlawSeverity.Low)],
            [new FlawChallenge("F1", true, "false positive")]);

        Assert.Single(decisions);
        Assert.Equal(FlawDisposition.Rejected, decisions[0].Disposition);
    }

    [Fact]
    public async Task DecideAsync_Defers_Disputed_Severe_Flaw_With_Evidence()
    {
        var engine = new DefaultFlawAdjudicationEngine();
        var decisions = await engine.DecideAsync(
            BuildContext(),
            [new FlawReport("F1", "issue", FlawSeverity.High, "repro trace")],
            [new FlawChallenge("F1", true, "needs retest")]);

        Assert.Single(decisions);
        Assert.Equal(FlawDisposition.Deferred, decisions[0].Disposition);
    }

    [Fact]
    public async Task DecideAsync_Accepts_Repeated_Deferred_Severe_Flaw_With_Evidence()
    {
        var engine = new DefaultFlawAdjudicationEngine();
        var decisions = await engine.DecideAsync(
            BuildContext(
                [
                    BuildRoundResult(
                        "public static class Seed { }",
                        "public static class Seed { }",
                        [new FlawReport("F1", "issue", FlawSeverity.High, "prior trace")],
                        [new FlawDecision("F1", FlawDisposition.Deferred, "needs more evidence")])
                ]),
            [new FlawReport("F1", "issue", FlawSeverity.High, "current trace")],
            [new FlawChallenge("F1", true, "still disputed")]);

        Assert.Single(decisions);
        Assert.Equal(FlawDisposition.Accepted, decisions[0].Disposition);
    }

    [Fact]
    public async Task DecideAsync_Defers_Disputed_Medium_Flaw_With_Evidence_In_Conflict_Heavy_Round()
    {
        var engine = new DefaultFlawAdjudicationEngine();
        var decisions = await engine.DecideAsync(
            BuildContext(),
            [
                new FlawReport("F1", "issue-1", FlawSeverity.Medium, "trace"),
                new FlawReport("F2", "issue-2", FlawSeverity.Low)
            ],
            [
                new FlawChallenge("F1", true, "disputed"),
                new FlawChallenge("F2", true, "disputed")
            ]);

        Assert.Equal(2, decisions.Count);
        var f1Decision = Assert.Single(decisions.Where(decision => decision.FlawId == "F1"));
        Assert.Equal(FlawDisposition.Deferred, f1Decision.Disposition);
    }

    [Fact]
    public async Task DecideAsync_Defers_Repeatedly_Disputed_Medium_Flaw_With_Evidence()
    {
        var engine = new DefaultFlawAdjudicationEngine();
        var decisions = await engine.DecideAsync(
            BuildContext(
                [
                    BuildRoundResult(
                        "public static class Seed { }",
                        "public static class Seed { }",
                        [new FlawReport("F1", "issue", FlawSeverity.Medium, "prior trace")],
                        [new FlawDecision("F1", FlawDisposition.Deferred, "needs follow-up")],
                        [new FlawChallenge("F1", true, "still disputed")])
                ]),
            [new FlawReport("F1", "issue", FlawSeverity.Medium, "current trace")],
            [new FlawChallenge("F1", true, "still disputed")]);

        Assert.Single(decisions);
        Assert.Equal(FlawDisposition.Deferred, decisions[0].Disposition);
    }

    private static AdversarialRoleContext BuildContext(IReadOnlyList<AdversarialRoundResult>? priorRounds = null)
    {
        var team = new AdversarialTeamDefinition("team-1");
        var assignment = new AdversarialRoundAssignment(
            1,
            new Dictionary<AdversarialRole, AdversarialTeamDefinition>
            {
                [AdversarialRole.Proposer] = team,
                [AdversarialRole.Reviewer] = team,
                [AdversarialRole.Opponent] = team,
                [AdversarialRole.Steward] = team,
                [AdversarialRole.Fixer] = team
            });

        return new AdversarialRoleContext(
            1,
            assignment,
            new CandidateProgram("public static class Seed { }"),
            priorRounds ?? []);
    }

    private static AdversarialRoundResult BuildRoundResult(
        string before,
        string after,
        IReadOnlyList<FlawReport> reports,
        IReadOnlyList<FlawDecision> decisions,
        IReadOnlyList<FlawChallenge>? challenges = null)
    {
        var team = new AdversarialTeamDefinition("team-1");
        var assignment = new AdversarialRoundAssignment(
            1,
            new Dictionary<AdversarialRole, AdversarialTeamDefinition>
            {
                [AdversarialRole.Proposer] = team,
                [AdversarialRole.Reviewer] = team,
                [AdversarialRole.Opponent] = team,
                [AdversarialRole.Steward] = team,
                [AdversarialRole.Fixer] = team
            });

        return new AdversarialRoundResult(
            assignment,
            new CandidateProgram(before),
            new CandidateProgram(after),
            reports,
            challenges ?? [],
            decisions);
    }
}
