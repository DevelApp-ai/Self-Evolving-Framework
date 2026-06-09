namespace SelfEvolvingFramework.Compilation;

public sealed record CompilationResult(bool Success, byte[]? AssemblyBytes, IReadOnlyList<string> Diagnostics)
{
    public static CompilationResult Failed(IEnumerable<string> diagnostics)
        => new(false, null, diagnostics.ToArray());
}
