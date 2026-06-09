namespace SelfEvolving.Framework.Core;

public sealed record EvolutionResult(
    CandidateProgram Candidate,
    bool IsValid,
    double Fitness,
    IReadOnlyList<string> Diagnostics);
