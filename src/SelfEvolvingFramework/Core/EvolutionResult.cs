namespace SelfEvolvingFramework.Core;

public sealed record EvolutionResult(
    CandidateProgram Candidate,
    bool IsValid,
    double Fitness,
    IReadOnlyList<string> Diagnostics)
{
    public EvolutionRunTelemetry Telemetry { get; init; } = EvolutionRunTelemetry.Empty;
}
