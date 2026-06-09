namespace SelfEvolvingFramework.Security;

public interface IAstSecurityEvaluator
{
    SecurityEvaluationResult Evaluate(string sourceCode);
}
