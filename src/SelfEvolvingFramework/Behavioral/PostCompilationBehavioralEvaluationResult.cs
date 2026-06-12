namespace SelfEvolvingFramework.Behavioral;

public sealed record PostCompilationBehavioralEvaluationResult(bool Passed, IReadOnlyList<string> Diagnostics)
{
    public static PostCompilationBehavioralEvaluationResult Failed(IEnumerable<string> diagnostics)
        => new(false, diagnostics.ToArray());
}
