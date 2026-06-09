namespace SelfEvolving.Framework.Execution;

public interface IIsolatedAssemblyExecutor
{
    Task<ExecutionResult> ExecuteStaticAsync(byte[] assemblyBytes, string typeName, string methodName, TimeSpan timeout, CancellationToken cancellationToken = default);
}
