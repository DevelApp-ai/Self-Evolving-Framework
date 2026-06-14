using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class AdversarialFitnessFeedbackBridgeTests
{
    [Fact]
    public void Apply_Adjusts_Fitness_From_Decisions_And_Fixes()
    {
        var bridge = new AdversarialFitnessFeedbackBridge();
        var rounds = new[]
        {
            BuildRound(
                before: "public static class Seed { }",
                after: "public static class Seed { public static int X => 1; }",
                reports:
                [
                    new FlawReport("F1", "critical issue", FlawSeverity.Critical, "trace"),
                    new FlawReport("F2", "minor issue", FlawSeverity.Low)
                ],
                decisions:
                [
                    new FlawDecision("F1", FlawDisposition.Accepted, "valid"),
                    new FlawDecision("F2", FlawDisposition.Rejected, "false positive")
                ])
        };

        var adjusted = bridge.Apply(100, rounds);

        Assert.Equal(90.5, adjusted);
    }

    [Fact]
    public void Apply_Penalizes_Deferred_Flaws()
    {
        var bridge = new AdversarialFitnessFeedbackBridge();
        var rounds = new[]
        {
            BuildRound(
                before: "public static class Seed { }",
                after: "public static class Seed { }",
                reports: [new FlawReport("F1", "needs more review", FlawSeverity.High, "trace")],
                decisions: [new FlawDecision("F1", FlawDisposition.Deferred, "defer")])
        };

        var adjusted = bridge.Apply(10, rounds);

        Assert.Equal(9.25, adjusted);
    }

    private static AdversarialRoundResult BuildRound(
        string before,
        string after,
        IReadOnlyList<FlawReport> reports,
        IReadOnlyList<FlawDecision> decisions)
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
            [],
            decisions);
    }
}
