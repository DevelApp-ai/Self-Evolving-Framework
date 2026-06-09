using SelfEvolvingFramework.Compilation;
using SelfEvolvingFramework.Execution;

namespace SelfEvolvingFramework.Tests.Execution;

public sealed class IsolatedAssemblyExecutorTests
{
    [Fact]
    public async Task ExecuteStaticAsync_Runs_Method_Within_Timeout()
    {
        var compiler = new RoslynDynamicCompilationService();
        var executor = new IsolatedAssemblyExecutor();
        const string source = "public static class Runner { public static int Execute() => 7; }";

        var compiled = compiler.Compile(source);
        var result = await executor.ExecuteStaticAsync(compiled.AssemblyBytes!, "Runner", "Execute", TimeSpan.FromSeconds(2));

        Assert.True(result.Completed);
        Assert.Equal(7, Assert.IsType<int>(result.ReturnValue));
    }

    [Fact]
    public async Task ExecuteStaticAsync_Fails_On_Timeout()
    {
        var compiler = new RoslynDynamicCompilationService();
        var executor = new IsolatedAssemblyExecutor();
        const string source = "using System.Threading; public static class Runner { public static int Execute() { Thread.Sleep(5000); return 1; } }";

        var compiled = compiler.Compile(source);
        var result = await executor.ExecuteStaticAsync(compiled.AssemblyBytes!, "Runner", "Execute", TimeSpan.FromMilliseconds(150));

        Assert.False(result.Completed);
        Assert.Contains(result.Diagnostics, d => d.Contains("timeout", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteStaticAsync_Fails_When_Method_Is_Missing()
    {
        var compiler = new RoslynDynamicCompilationService();
        var executor = new IsolatedAssemblyExecutor();
        const string source = "public static class Runner { public static int Execute() => 7; }";

        var compiled = compiler.Compile(source);
        var result = await executor.ExecuteStaticAsync(compiled.AssemblyBytes!, "Runner", "Missing", TimeSpan.FromSeconds(2));

        Assert.False(result.Completed);
        Assert.Contains(result.Diagnostics, d => d.Contains("Missing", StringComparison.Ordinal));
    }
}
