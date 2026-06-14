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

    private static AdversarialRoleContext BuildContext()
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
            []);
    }
}
