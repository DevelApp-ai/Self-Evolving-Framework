namespace SelfEvolving.Framework.Security;

public interface IAstSecurityEvaluator
{
    SecurityEvaluationResult Evaluate(string sourceCode);
}
