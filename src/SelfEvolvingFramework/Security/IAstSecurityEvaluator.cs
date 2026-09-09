using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Security;

public interface IAstSecurityEvaluator
{
    CandidateFormat Format { get; }
    SecurityEvaluationResult Evaluate(string sourceMaterial);
}
