using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public interface IAdversarialRotationEngine
{
    AdversarialRoundAssignment AssignRound(IReadOnlyList<AdversarialTeamDefinition> teams, int roundNumber);
}

public interface IAdversarialRoleExecutor
{
    Task<CandidateProgram> ProposeAsync(AdversarialRoleContext context, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FlawReport>> ReviewAsync(AdversarialRoleContext context, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FlawChallenge>> OpposeAsync(
        AdversarialRoleContext context,
        IReadOnlyList<FlawReport> reports,
        CancellationToken cancellationToken = default);

    Task<CandidateProgram> StewardAsync(
        AdversarialRoleContext context,
        IReadOnlyList<FlawDecision> decisions,
        CancellationToken cancellationToken = default);

    Task<CandidateProgram> FixAsync(
        AdversarialRoleContext context,
        IReadOnlyList<FlawDecision> acceptedFlaws,
        CancellationToken cancellationToken = default);
}

public interface IFlawAdjudicationEngine
{
    Task<IReadOnlyList<FlawDecision>> DecideAsync(
        AdversarialRoleContext context,
        IReadOnlyList<FlawReport> reports,
        IReadOnlyList<FlawChallenge> challenges,
        CancellationToken cancellationToken = default);
}

public sealed class RoundRobinAdversarialRotationEngine : IAdversarialRotationEngine
{
    private static readonly AdversarialRole[] RoleOrder =
    [
        AdversarialRole.Proposer,
        AdversarialRole.Reviewer,
        AdversarialRole.Opponent,
        AdversarialRole.Steward,
        AdversarialRole.Fixer
    ];

    public AdversarialRoundAssignment AssignRound(IReadOnlyList<AdversarialTeamDefinition> teams, int roundNumber)
    {
        ArgumentNullException.ThrowIfNull(teams);
        if (teams.Count == 0)
        {
            throw new ArgumentException("At least one team is required.", nameof(teams));
        }

        if (roundNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roundNumber), "Round number must be greater than zero.");
        }

        var assignments = new Dictionary<AdversarialRole, AdversarialTeamDefinition>(RoleOrder.Length);
        var offset = (roundNumber - 1) % teams.Count;
        for (var i = 0; i < RoleOrder.Length; i++)
        {
            assignments[RoleOrder[i]] = teams[(offset + i) % teams.Count];
        }

        return new AdversarialRoundAssignment(roundNumber, assignments);
    }
}

public sealed class MultiTeamAdversarialReviewOrchestrator(
    IAdversarialRotationEngine rotationEngine,
    IAdversarialRoleExecutor roleExecutor,
    IFlawAdjudicationEngine adjudicationEngine,
    AdversarialWorkflowOptions? options = null)
{
    private readonly AdversarialWorkflowOptions _options = options ?? new();

    public async Task<AdversarialReviewResult> RunAsync(
        CandidateProgram seed,
        IReadOnlyList<AdversarialTeamDefinition> teams,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(teams);

        if (_options.MaxRounds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Max rounds must be greater than zero.");
        }

        if (teams.Count == 0)
        {
            throw new ArgumentException("At least one team is required.", nameof(teams));
        }

        var rounds = new List<AdversarialRoundResult>();
        var candidate = seed;
        var unresolvedReportsByFlawId = new Dictionary<string, FlawReport>(StringComparer.Ordinal);

        for (var roundNumber = 1; roundNumber <= _options.MaxRounds; roundNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var assignment = rotationEngine.AssignRound(teams, roundNumber);
            ValidateAssignment(assignment);

            var roundContext = new AdversarialRoleContext(roundNumber, assignment, candidate, rounds);
            var proposedCandidate = await roleExecutor.ProposeAsync(roundContext, cancellationToken);

            var reviewContext = roundContext with { CurrentCandidate = proposedCandidate };
            var roundReports = await roleExecutor.ReviewAsync(reviewContext, cancellationToken);
            var reportsByFlawId = new Dictionary<string, FlawReport>(unresolvedReportsByFlawId, StringComparer.Ordinal);
            foreach (var report in roundReports)
            {
                reportsByFlawId[report.FlawId] = report;
            }

            var reports = reportsByFlawId.Values.ToArray();
            var challenges = await roleExecutor.OpposeAsync(reviewContext, reports, cancellationToken);
            var decisions = await adjudicationEngine.DecideAsync(reviewContext, reports, challenges, cancellationToken);

            var stewardedCandidate = await roleExecutor.StewardAsync(reviewContext, decisions, cancellationToken);
            var unresolvedFlaws = decisions
                .Where(d => d.Disposition is FlawDisposition.Accepted or FlawDisposition.Deferred)
                .ToArray();
            unresolvedReportsByFlawId = unresolvedFlaws
                .Select(decision => reportsByFlawId.TryGetValue(decision.FlawId, out var report) ? report : null)
                .Where(report => report is not null)
                .ToDictionary(report => report!.FlawId, report => report!, StringComparer.Ordinal);
            var acceptedFlaws = unresolvedFlaws.Where(d => d.Disposition == FlawDisposition.Accepted).ToArray();
            var resolvedCandidate = acceptedFlaws.Length == 0
                ? stewardedCandidate
                : await roleExecutor.FixAsync(
                    reviewContext with { CurrentCandidate = stewardedCandidate },
                    acceptedFlaws,
                    cancellationToken);

            rounds.Add(new AdversarialRoundResult(
                assignment,
                candidate,
                resolvedCandidate,
                reports,
                challenges,
                decisions));

            candidate = resolvedCandidate;
            if (unresolvedReportsByFlawId.Count == 0)
            {
                return new AdversarialReviewResult(candidate, true, rounds);
            }
        }

        return new AdversarialReviewResult(candidate, false, rounds);
    }

    private void ValidateAssignment(AdversarialRoundAssignment assignment)
    {
        _ = assignment.GetTeam(AdversarialRole.Proposer);
        var reviewer = assignment.GetTeam(AdversarialRole.Reviewer);
        var opponent = assignment.GetTeam(AdversarialRole.Opponent);
        _ = assignment.GetTeam(AdversarialRole.Steward);
        _ = assignment.GetTeam(AdversarialRole.Fixer);

        if (_options.RequireDistinctReviewerAndOpponent && reviewer.TeamId == opponent.TeamId)
        {
            throw new InvalidOperationException("Reviewer and opponent teams must be distinct for each round.");
        }
    }
}
