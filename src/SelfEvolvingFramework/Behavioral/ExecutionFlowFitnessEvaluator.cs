using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Behavioral;

public sealed record ExecutionFlowFitnessScoringOptions(
    double AssertionFailurePenalty = 1000,
    double ConsoleErrorPenalty = 100,
    double PageErrorPenalty = 100,
    double NetworkFailurePenalty = 10,
    double RuntimeFailurePenalty = 250,
    double CompilerFailurePenalty = 500,
    double UnknownFailurePenalty = 50);

public sealed class ExecutionFlowFitnessEvaluator(
    IPostCompilationBehavioralEvaluator behavioralEvaluator,
    ExecutionFlowFitnessScoringOptions? options = null) : IFitnessEvaluator
{
    private readonly IPostCompilationBehavioralEvaluator _behavioralEvaluator = behavioralEvaluator ?? throw new ArgumentNullException(nameof(behavioralEvaluator));
    private readonly ExecutionFlowFitnessScoringOptions _options = ValidateOptions(options ?? new());

    public async Task<double> EvaluateAsync(CandidateProgram candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var evaluation = await _behavioralEvaluator.EvaluateAsync(candidate, cancellationToken);
        if (evaluation.Passed)
        {
            return 0;
        }

        var penalty = evaluation.Diagnostics.Sum(GetPenaltyForDiagnostic);
        if (penalty <= 0)
        {
            penalty = _options.UnknownFailurePenalty;
        }

        return -penalty;
    }

    private double GetPenaltyForDiagnostic(string diagnostic)
    {
        if (diagnostic.StartsWith("flow-failed:", StringComparison.OrdinalIgnoreCase))
        {
            return _options.AssertionFailurePenalty;
        }

        if (diagnostic.StartsWith("console:", StringComparison.OrdinalIgnoreCase))
        {
            return _options.ConsoleErrorPenalty;
        }

        if (diagnostic.StartsWith("page-error:", StringComparison.OrdinalIgnoreCase))
        {
            return _options.PageErrorPenalty;
        }

        if (diagnostic.StartsWith("request-failed:", StringComparison.OrdinalIgnoreCase))
        {
            return _options.NetworkFailurePenalty;
        }

        if (diagnostic.StartsWith("runtime:", StringComparison.OrdinalIgnoreCase))
        {
            return _options.RuntimeFailurePenalty;
        }

        if (diagnostic.StartsWith("compiler:", StringComparison.OrdinalIgnoreCase))
        {
            return _options.CompilerFailurePenalty;
        }

        return _options.UnknownFailurePenalty;
    }

    private static ExecutionFlowFitnessScoringOptions ValidateOptions(ExecutionFlowFitnessScoringOptions options)
    {
        if (options.AssertionFailurePenalty < 0 ||
            options.ConsoleErrorPenalty < 0 ||
            options.PageErrorPenalty < 0 ||
            options.NetworkFailurePenalty < 0 ||
            options.RuntimeFailurePenalty < 0 ||
            options.CompilerFailurePenalty < 0 ||
            options.UnknownFailurePenalty < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Penalty values must be greater than or equal to zero.");
        }

        return options;
    }
}
