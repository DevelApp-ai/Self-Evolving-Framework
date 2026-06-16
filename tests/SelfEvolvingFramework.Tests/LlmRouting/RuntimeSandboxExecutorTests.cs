using SelfEvolvingFramework.LlmRouting;

namespace SelfEvolvingFramework.Tests.LlmRouting;

public sealed class RuntimeSandboxExecutorTests
{
    [Fact]
    public async Task ExecuteShellAsync_HostExecutor_Throws_In_Production_Mode()
    {
        var original = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Production");
        try
        {
            var executor = new RuntimeSandboxExecutor();
            var options = new SandboxOptions(ExecutorType: "host", TimeoutMilliseconds: 1000);

            await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ExecuteShellAsync("echo ok", options));
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", original);
        }
    }

    [Fact]
    public async Task ExecuteShellAsync_HostExecutor_Succeeds_Outside_Production_Mode()
    {
        var original = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
        try
        {
            var executor = new RuntimeSandboxExecutor();
            var options = new SandboxOptions(ExecutorType: "host", TimeoutMilliseconds: 1000);

            var exitCode = await executor.ExecuteShellAsync("exit 0", options);

            Assert.Equal(0, exitCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", original);
        }
    }
}
