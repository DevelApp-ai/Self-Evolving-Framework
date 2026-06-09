using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public interface IFitnessEvaluator
{
    Task<double> EvaluateAsync(CandidateProgram candidate, CancellationToken cancellationToken = default);
}
