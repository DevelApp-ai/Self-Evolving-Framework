using SelfEvolvingFramework.Behavioral;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Tests.Behavioral;

public sealed class ExecutionFlowFitnessEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_Returns_Zero_When_Behavioral_Evaluation_Passes()
    {
        var evaluator = new ExecutionFlowFitnessEvaluator(
            new StubBehavioralEvaluator(new PostCompilationBehavioralEvaluationResult(true, [])));

        var fitness = await evaluator.EvaluateAsync(new CandidateProgram("public static class Runner{}"));

        Assert.Equal(0, fitness);
    }

    [Fact]
    public async Task EvaluateAsync_Applies_Configured_Penalties_From_Diagnostics()
    {
        var options = new ExecutionFlowFitnessScoringOptions(
            AssertionFailurePenalty: 1000,
            ConsoleErrorPenalty: 100,
            PageErrorPenalty: 80,
            NetworkFailurePenalty: 10,
            RuntimeFailurePenalty: 250,
            CompilerFailurePenalty: 500,
            UnknownFailurePenalty: 5);

        var evaluator = new ExecutionFlowFitnessEvaluator(
            new StubBehavioralEvaluator(new PostCompilationBehavioralEvaluationResult(false, [
                "console: js exception",
                "request-failed: https://localhost/api",
                "flow-failed: expected selector missing",
                "misc: some other failure"
            ])),
            options);

        var fitness = await evaluator.EvaluateAsync(new CandidateProgram("public static class Runner{}"));

        Assert.Equal(-(100 + 10 + 1000 + 5), fitness);
    }

    [Fact]
    public async Task EvaluateAsync_Uses_Unknown_Failure_Penalty_When_Failed_Without_Diagnostics()
    {
        var evaluator = new ExecutionFlowFitnessEvaluator(
            new StubBehavioralEvaluator(new PostCompilationBehavioralEvaluationResult(false, [])),
            new ExecutionFlowFitnessScoringOptions(UnknownFailurePenalty: 12));

        var fitness = await evaluator.EvaluateAsync(new CandidateProgram("public static class Runner{}"));

        Assert.Equal(-12, fitness);
    }

    [Fact]
    public void Ctor_Throws_For_Negative_Penalty_Options()
    {
        var options = new ExecutionFlowFitnessScoringOptions(ConsoleErrorPenalty: -1);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExecutionFlowFitnessEvaluator(new StubBehavioralEvaluator(PostCompilationBehavioralEvaluationResult.Failed([])), options));

        Assert.Contains("Penalty values", exception.Message, StringComparison.Ordinal);
    }

    private sealed class StubBehavioralEvaluator(PostCompilationBehavioralEvaluationResult result) : IPostCompilationBehavioralEvaluator
    {
        public Task<PostCompilationBehavioralEvaluationResult> EvaluateAsync(CandidateProgram candidate, CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }
}
