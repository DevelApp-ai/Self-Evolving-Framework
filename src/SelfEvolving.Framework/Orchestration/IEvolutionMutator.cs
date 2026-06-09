using SelfEvolving.Framework.Core;

namespace SelfEvolving.Framework.Orchestration;

public interface IEvolutionMutator
{
    Task<CandidateProgram> MutateAsync(CandidateProgram candidate, IReadOnlyList<string> feedback, CancellationToken cancellationToken = default);
}
