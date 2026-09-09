using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public interface IEvolutionCrossover
{
    CandidateFormat Format { get; }
    Task<CandidateProgram> CrossoverAsync(
        CandidateProgram parentA,
        CandidateProgram parentB,
        CancellationToken cancellationToken = default);
}
