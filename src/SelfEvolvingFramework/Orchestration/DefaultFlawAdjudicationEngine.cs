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
