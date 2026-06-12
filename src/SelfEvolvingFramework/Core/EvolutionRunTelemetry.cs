namespace SelfEvolvingFramework.Core;

public sealed record EvolutionRunTelemetry(
    TimeSpan TotalDuration,
    TimeSpan MutationDuration,
    TimeSpan SecurityEvaluationDuration,
    TimeSpan CompilationDuration,
    TimeSpan FitnessEvaluationDuration,
    int DiagnosticCount,
    bool CanceledByCaller,
    bool TimedOut,
    int ExecutionBudgetMilliseconds)
{
    public static EvolutionRunTelemetry Empty { get; } = new(
        TimeSpan.Zero,
        TimeSpan.Zero,
        TimeSpan.Zero,
        TimeSpan.Zero,
        TimeSpan.Zero,
        0,
        false,
        false,
        0);
}
