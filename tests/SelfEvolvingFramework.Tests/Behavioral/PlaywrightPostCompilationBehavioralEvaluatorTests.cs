using SelfEvolvingFramework.Behavioral;
using SelfEvolvingFramework.Compilation;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Execution;

namespace SelfEvolvingFramework.Tests.Behavioral;

public sealed class PlaywrightPostCompilationBehavioralEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_Returns_Compiler_Diagnostics_When_Compilation_Fails()
    {
        var evaluator = new PlaywrightPostCompilationBehavioralEvaluator(
            new StubCompilationService(CompilationResult.Failed(["CS1002"])),
            new StubAssemblyExecutor(),
            new StubFlowRunner());

        var result = await evaluator.EvaluateAsync(new CandidateProgram("public static class Runner{}"));

        Assert.False(result.Passed);
        Assert.Contains("compiler: CS1002", result.Diagnostics);
    }

    [Fact]
    public async Task EvaluateAsync_Returns_Runtime_Diagnostics_When_Execution_Fails()
    {
        var evaluator = new PlaywrightPostCompilationBehavioralEvaluator(
            new StubCompilationService(new CompilationResult(true, [1], [])),
            new StubAssemblyExecutor(ExecutionResult.Failed(["Execution exceeded timeout"])),
            new StubFlowRunner());

        var result = await evaluator.EvaluateAsync(new CandidateProgram("public static class Runner{}"));

        Assert.False(result.Passed);
        Assert.Contains("runtime: Execution exceeded timeout", result.Diagnostics);
    }

    [Fact]
    public async Task EvaluateAsync_Returns_Failure_When_Entry_Point_Returns_Invalid_Endpoint()
    {
        var evaluator = new PlaywrightPostCompilationBehavioralEvaluator(
            new StubCompilationService(new CompilationResult(true, [1], [])),
            new StubAssemblyExecutor(new ExecutionResult(true, "not-a-url", [])),
            new StubFlowRunner());

        var result = await evaluator.EvaluateAsync(new CandidateProgram("public static class Runner{}"));

        Assert.False(result.Passed);
        Assert.Contains("runtime: Entry point did not return a valid absolute endpoint URL.", result.Diagnostics);
    }

    [Fact]
    public async Task EvaluateAsync_Uses_Playwright_Runner_For_Valid_Endpoint()
    {
        var flowRunner = new StubFlowRunner(["console: unexpected error"]);
        var evaluator = new PlaywrightPostCompilationBehavioralEvaluator(
            new StubCompilationService(new CompilationResult(true, [1], [])),
            new StubAssemblyExecutor(new ExecutionResult(true, "https://localhost:5001", [])),
            flowRunner);

        var result = await evaluator.EvaluateAsync(new CandidateProgram("public static class Runner{}"));

        Assert.False(result.Passed);
        Assert.Equal("https://localhost:5001/", flowRunner.LastEndpoint?.ToString());
        Assert.Contains("console: unexpected error", result.Diagnostics);
    }

    private sealed class StubCompilationService(CompilationResult result) : IDynamicCompilationService
    {
        public CompilationResult Compile(string sourceCode) => result;
    }

    private sealed class StubAssemblyExecutor(ExecutionResult? result = null) : IIsolatedAssemblyExecutor
    {
        public Task<ExecutionResult> ExecuteStaticAsync(byte[] assemblyBytes, string typeName, string methodName, TimeSpan timeout, CancellationToken cancellationToken = default)
            => Task.FromResult(result ?? new ExecutionResult(true, "https://localhost:5001", []));
    }

    private sealed class StubFlowRunner(IReadOnlyList<string>? diagnostics = null) : IPlaywrightBehavioralFlowRunner
    {
        public Uri? LastEndpoint { get; private set; }

        public Task<IReadOnlyList<string>> RunAsync(Uri endpoint, CancellationToken cancellationToken = default)
        {
            LastEndpoint = endpoint;
            return Task.FromResult(diagnostics ?? (IReadOnlyList<string>)[]);
        }
    }
}
