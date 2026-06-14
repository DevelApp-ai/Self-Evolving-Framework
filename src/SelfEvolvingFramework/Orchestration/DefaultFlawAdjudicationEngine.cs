namespace SelfEvolvingFramework.Orchestration;

public sealed class DefaultFlawAdjudicationEngine : IFlawAdjudicationEngine
{
    public Task<IReadOnlyList<FlawDecision>> DecideAsync(
        AdversarialRoleContext context,
        IReadOnlyList<FlawReport> reports,
        IReadOnlyList<FlawChallenge> challenges,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentNullException.ThrowIfNull(challenges);
        cancellationToken.ThrowIfCancellationRequested();

        var challengesByFlawId = challenges
            .GroupBy(challenge => challenge.FlawId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
        var priorDeferredFlaws = context.PriorRounds
            .SelectMany(round => round.Decisions)
            .Where(decision => decision.Disposition == FlawDisposition.Deferred)
            .Select(decision => decision.FlawId)
            .ToHashSet(StringComparer.Ordinal);
        var priorDisputedCountsByFlawId = context.PriorRounds
            .SelectMany(round => round.Challenges)
            .Where(challenge => challenge.Disputed)
            .GroupBy(challenge => challenge.FlawId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var disputedCount = reports.Count(report =>
            challengesByFlawId.TryGetValue(report.FlawId, out var challenge) && challenge.Disputed);
        var isConflictHeavyRound = reports.Count > 0 && disputedCount * 2 >= reports.Count;

        var decisions = new List<FlawDecision>(reports.Count);
        foreach (var report in reports)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!challengesByFlawId.TryGetValue(report.FlawId, out var challenge))
            {
                decisions.Add(new FlawDecision(
                    report.FlawId,
                    FlawDisposition.Accepted,
                    "No opposition was provided for this flaw."));
                continue;
            }

            if (!challenge.Disputed)
            {
                decisions.Add(new FlawDecision(
                    report.FlawId,
                    FlawDisposition.Accepted,
                    "Opponent did not dispute this flaw."));
                continue;
            }

            var hasEvidence = !string.IsNullOrWhiteSpace(report.Evidence);
            var priorDisputedCount = priorDisputedCountsByFlawId.TryGetValue(report.FlawId, out var count)
                ? count
                : 0;
            if (hasEvidence
                && priorDeferredFlaws.Contains(report.FlawId)
                && report.Severity is FlawSeverity.Critical or FlawSeverity.High)
            {
                decisions.Add(new FlawDecision(
                    report.FlawId,
                    FlawDisposition.Accepted,
                    "Repeated deferred severe flaw promoted to accepted for remediation."));
                continue;
            }

            if (hasEvidence
                && report.Severity == FlawSeverity.Medium
                && (isConflictHeavyRound || priorDisputedCount > 0))
            {
                var mediumConflictRationale = isConflictHeavyRound
                    ? "Conflict-heavy round deferred this disputed medium flaw with evidence for tie-break follow-up."
                    : "Repeatedly disputed medium flaw with evidence deferred for additional verification.";
                decisions.Add(new FlawDecision(
                    report.FlawId,
                    FlawDisposition.Deferred,
                    mediumConflictRationale));
                continue;
            }

            var disposition = report.Severity switch
            {
                FlawSeverity.Critical or FlawSeverity.High when hasEvidence => FlawDisposition.Deferred,
                _ => FlawDisposition.Rejected
            };

            var rationale = disposition == FlawDisposition.Deferred
                ? "Disputed severe flaw with evidence requires another review round."
                : "Disputed flaw rejected due to insufficient severity/evidence confidence.";

            decisions.Add(new FlawDecision(report.FlawId, disposition, rationale));
        }

        return Task.FromResult<IReadOnlyList<FlawDecision>>(decisions);
    }
}
