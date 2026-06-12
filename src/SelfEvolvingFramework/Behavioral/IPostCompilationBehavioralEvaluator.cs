using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Behavioral;

public interface IPostCompilationBehavioralEvaluator
{
    Task<PostCompilationBehavioralEvaluationResult> EvaluateAsync(CandidateProgram candidate, CancellationToken cancellationToken = default);
}
