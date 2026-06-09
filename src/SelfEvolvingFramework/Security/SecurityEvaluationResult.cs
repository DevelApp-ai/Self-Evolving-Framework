namespace SelfEvolvingFramework.Security;

public sealed record SecurityEvaluationResult(bool IsAllowed, IReadOnlyList<string> Violations)
{
    public static SecurityEvaluationResult Allowed() => new(true, []);
    public static SecurityEvaluationResult Blocked(IEnumerable<string> violations)
        => new(false, violations.ToArray());
}
