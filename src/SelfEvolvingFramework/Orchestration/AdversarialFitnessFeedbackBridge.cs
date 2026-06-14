namespace SelfEvolvingFramework.Orchestration;

public sealed record AdversarialFitnessScoringOptions(
    double AcceptedLowPenalty = 1,
    double AcceptedMediumPenalty = 2,
    double AcceptedHighPenalty = 6,
    double AcceptedCriticalPenalty = 12,
    double DeferredPenalty = 0.75,
    double RejectedReward = 0.5,
    double SuccessfulFixReward = 2);

public sealed class AdversarialFitnessFeedbackBridge(AdversarialFitnessScoringOptions? options = null)
{
    private readonly AdversarialFitnessScoringOptions _options = ValidateOptions(options ?? new());

    public double Apply(double baseFitness, IReadOnlyList<AdversarialRoundResult> rounds)
    {
        ArgumentNullException.ThrowIfNull(rounds);
        if (double.IsNaN(baseFitness) || double.IsInfinity(baseFitness))
        {
            throw new ArgumentOutOfRangeException(nameof(baseFitness), "Base fitness must be finite.");
        }

        var adjusted = baseFitness;
        foreach (var round in rounds)
        {
            var reportsById = round.Reports.ToDictionary(report => report.FlawId, StringComparer.Ordinal);
            var acceptedCount = 0;
            foreach (var decision in round.Decisions)
            {
                if (!reportsById.TryGetValue(decision.FlawId, out var report))
                {
                    continue;
                }

                switch (decision.Disposition)
                {
                    case FlawDisposition.Accepted:
                        acceptedCount++;
                        adjusted -= GetAcceptedPenalty(report.Severity);
                        break;
                    case FlawDisposition.Deferred:
                        adjusted -= _options.DeferredPenalty;
                        break;
                    case FlawDisposition.Rejected:
                        adjusted += _options.RejectedReward;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(decision.Disposition), "Unknown flaw disposition.");
                }
            }

            if (acceptedCount > 0 && !string.Equals(round.CandidateBeforeRound.SourceCode, round.CandidateAfterRound.SourceCode, StringComparison.Ordinal))
            {
                adjusted += _options.SuccessfulFixReward;
            }
        }

        return adjusted;
    }

    private static AdversarialFitnessScoringOptions ValidateOptions(AdversarialFitnessScoringOptions options)
    {
        if (options.AcceptedLowPenalty < 0 || options.AcceptedMediumPenalty < 0 || options.AcceptedHighPenalty < 0 || options.AcceptedCriticalPenalty < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Accepted penalties must be non-negative.");
        }

        if (options.DeferredPenalty < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Deferred penalty must be non-negative.");
        }

        if (options.RejectedReward < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Rejected reward must be non-negative.");
        }

        if (options.SuccessfulFixReward < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Successful fix reward must be non-negative.");
        }

        return options;
    }

    private double GetAcceptedPenalty(FlawSeverity severity)
        => severity switch
        {
            FlawSeverity.Low => _options.AcceptedLowPenalty,
            FlawSeverity.Medium => _options.AcceptedMediumPenalty,
            FlawSeverity.High => _options.AcceptedHighPenalty,
            FlawSeverity.Critical => _options.AcceptedCriticalPenalty,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), "Unknown flaw severity.")
        };
}
