using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public interface IEvolutionCrossover
{
    Task<CandidateProgram> CrossoverAsync(
        CandidateProgram parentA,
        CandidateProgram parentB,
        CancellationToken cancellationToken = default);
}
