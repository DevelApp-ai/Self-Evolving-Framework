using SelfEvolving.Framework.Core;

namespace SelfEvolving.Framework.Orchestration;

public interface IFitnessEvaluator
{
    Task<double> EvaluateAsync(CandidateProgram candidate, CancellationToken cancellationToken = default);
}
