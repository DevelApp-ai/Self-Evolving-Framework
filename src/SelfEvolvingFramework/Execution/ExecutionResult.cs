namespace SelfEvolvingFramework.Execution;

public sealed record ExecutionResult(bool Completed, object? ReturnValue, IReadOnlyList<string> Diagnostics)
{
    public static ExecutionResult Failed(IEnumerable<string> diagnostics)
        => new(false, null, diagnostics.ToArray());
}
