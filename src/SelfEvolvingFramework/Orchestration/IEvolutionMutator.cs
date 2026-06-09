using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public interface IEvolutionMutator
{
    Task<CandidateProgram> MutateAsync(CandidateProgram candidate, IReadOnlyList<string> feedback, CancellationToken cancellationToken = default);
}
