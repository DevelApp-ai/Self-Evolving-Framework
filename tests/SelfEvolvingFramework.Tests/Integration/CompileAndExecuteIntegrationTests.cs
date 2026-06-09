using SelfEvolvingFramework.Compilation;
using SelfEvolvingFramework.Execution;
using SelfEvolvingFramework.Security;

namespace SelfEvolvingFramework.Tests.Integration;

public sealed class CompileAndExecuteIntegrationTests
{
    [Fact]
    public async Task Compile_And_Execute_Works_For_Allowed_Code()
    {
        const string source = "public static class Runner { public static int Execute() => 99; }";
        var security = new RoslynAstSecurityEvaluator();
        var compiler = new RoslynDynamicCompilationService();
        var executor = new IsolatedAssemblyExecutor();

        var securityResult = security.Evaluate(source);
        var compilation = compiler.Compile(source);
        var execution = await executor.ExecuteStaticAsync(compilation.AssemblyBytes!, "Runner", "Execute", TimeSpan.FromSeconds(2));

        Assert.True(securityResult.IsAllowed);
        Assert.True(compilation.Success);
        Assert.True(execution.Completed);
        Assert.Equal(99, Assert.IsType<int>(execution.ReturnValue));
    }
}
