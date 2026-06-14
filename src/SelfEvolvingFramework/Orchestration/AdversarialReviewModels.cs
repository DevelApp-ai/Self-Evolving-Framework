using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public enum AdversarialRole
{
    Proposer,
    Reviewer,
    Opponent,
    Steward,
    Fixer
}

public enum FlawSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum FlawDisposition
{
    Accepted,
    Rejected,
    Deferred
}

public sealed record AdversarialTeamDefinition(string TeamId, string? Description = null)
{
    public string TeamId { get; init; } = string.IsNullOrWhiteSpace(TeamId)
        ? throw new ArgumentException("Team id must be provided.", nameof(TeamId))
        : TeamId;
}

public sealed record AdversarialWorkflowOptions(
    int MaxRounds = 8,
    bool RequireDistinctReviewerAndOpponent = true);

public sealed record AdversarialRoundAssignment(
    int RoundNumber,
    IReadOnlyDictionary<AdversarialRole, AdversarialTeamDefinition> RoleAssignments)
{
    public AdversarialTeamDefinition GetTeam(AdversarialRole role)
        => RoleAssignments.TryGetValue(role, out var team)
            ? team
            : throw new InvalidOperationException($"Role '{role}' is missing from round assignment.");
}

public sealed record FlawReport(
    string FlawId,
    string Summary,
    FlawSeverity Severity,
    string? Evidence = null)
{
    public string FlawId { get; init; } = string.IsNullOrWhiteSpace(FlawId)
        ? throw new ArgumentException("Flaw id must be provided.", nameof(FlawId))
        : FlawId;

    public string Summary { get; init; } = string.IsNullOrWhiteSpace(Summary)
        ? throw new ArgumentException("Flaw summary must be provided.", nameof(Summary))
        : Summary;
}

public sealed record FlawChallenge(
    string FlawId,
    bool Disputed,
    string Rationale)
{
    public string FlawId { get; init; } = string.IsNullOrWhiteSpace(FlawId)
        ? throw new ArgumentException("Flaw id must be provided.", nameof(FlawId))
        : FlawId;

    public string Rationale { get; init; } = string.IsNullOrWhiteSpace(Rationale)
        ? throw new ArgumentException("Challenge rationale must be provided.", nameof(Rationale))
        : Rationale;
}

public sealed record FlawDecision(
    string FlawId,
    FlawDisposition Disposition,
    string Rationale)
{
    public string FlawId { get; init; } = string.IsNullOrWhiteSpace(FlawId)
        ? throw new ArgumentException("Flaw id must be provided.", nameof(FlawId))
        : FlawId;

    public string Rationale { get; init; } = string.IsNullOrWhiteSpace(Rationale)
        ? throw new ArgumentException("Decision rationale must be provided.", nameof(Rationale))
        : Rationale;
}

public sealed record AdversarialRoundResult(
    AdversarialRoundAssignment Assignment,
    CandidateProgram CandidateBeforeRound,
    CandidateProgram CandidateAfterRound,
    IReadOnlyList<FlawReport> Reports,
    IReadOnlyList<FlawChallenge> Challenges,
    IReadOnlyList<FlawDecision> Decisions);

public sealed record AdversarialReviewResult(
    CandidateProgram FinalCandidate,
    bool Converged,
    IReadOnlyList<AdversarialRoundResult> Rounds);

public sealed record AdversarialRoleContext(
    int RoundNumber,
    AdversarialRoundAssignment Assignment,
    CandidateProgram CurrentCandidate,
    IReadOnlyList<AdversarialRoundResult> PriorRounds);
