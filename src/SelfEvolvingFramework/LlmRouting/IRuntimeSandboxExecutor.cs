namespace SelfEvolvingFramework.LlmRouting;

public interface IRuntimeSandboxExecutor
{
    Task<int> ExecuteShellAsync(string command, SandboxOptions options, CancellationToken cancellationToken = default);
}
