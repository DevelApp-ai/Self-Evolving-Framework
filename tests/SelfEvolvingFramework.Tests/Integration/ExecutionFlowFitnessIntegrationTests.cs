using SelfEvolvingFramework.Behavioral;
using SelfEvolvingFramework.Compilation;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Execution;

namespace SelfEvolvingFramework.Tests.Integration;

public sealed class ExecutionFlowFitnessIntegrationTests
{
    [Fact]
    public async Task EvaluateAsync_Returns_Zero_When_Browser_Flow_Has_No_Diagnostics()
    {
        var flowRunner = new StubFlowRunner([]);
        var behavioralEvaluator = new PlaywrightPostCompilationBehavioralEvaluator(
            new RoslynDynamicCompilationService(),
            new IsolatedAssemblyExecutor(),
            flowRunner);
        var fitnessEvaluator = new ExecutionFlowFitnessEvaluator(behavioralEvaluator);

        var fitness = await fitnessEvaluator.EvaluateAsync(new CandidateProgram(
            "public static class Runner { public static string Execute() => \"https://localhost:5001\"; }"));

        Assert.Equal(0, fitness);
        Assert.Equal("https://localhost:5001/", flowRunner.LastEndpoint?.ToString());
    }

    [Fact]
    public async Task EvaluateAsync_Applies_Browser_Flow_Failure_Penalties()
    {
        var flowRunner = new StubFlowRunner([
            "flow-failed: assertion did not pass",
            "console: unexpected browser error",
            "request-failed: https://localhost:5001/api/health"
        ]);
        var behavioralEvaluator = new PlaywrightPostCompilationBehavioralEvaluator(
            new RoslynDynamicCompilationService(),
            new IsolatedAssemblyExecutor(),
            flowRunner);
        var fitnessEvaluator = new ExecutionFlowFitnessEvaluator(behavioralEvaluator);

        var fitness = await fitnessEvaluator.EvaluateAsync(new CandidateProgram(
            "public static class Runner { public static string Execute() => \"https://localhost:5001\"; }"));

        Assert.Equal(-(1000 + 100 + 10), fitness);
    }

    private sealed class StubFlowRunner(IReadOnlyList<string> diagnostics) : IPlaywrightBehavioralFlowRunner
    {
        public Uri? LastEndpoint { get; private set; }

        public Task<IReadOnlyList<string>> RunAsync(Uri endpoint, CancellationToken cancellationToken = default)
        {
            LastEndpoint = endpoint;
            return Task.FromResult(diagnostics);
        }
    }
}
