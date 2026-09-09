using System.Text.Json;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class XStateFitnessEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_Returns_Negative_Infinity_For_Non_Json()
    {
        var evaluator = new XStateFitnessEvaluator();
        var candidate = CandidateProgram.FromCSharp("public class Test { }");
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.Equal(double.NegativeInfinity, score);
    }

    [Fact]
    public async Task EvaluateAsync_Returns_Negative_Infinity_For_Invalid_Json()
    {
        var evaluator = new XStateFitnessEvaluator();
        var candidate = CandidateProgram.FromJson("{invalid json}");
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.Equal(double.NegativeInfinity, score);
    }

    [Fact]
    public async Task EvaluateAsync_Scores_Valid_XState()
    {
        var evaluator = new XStateFitnessEvaluator();
        var xstateJson = "{'id': 'test', 'initial': 'start', 'states': {'start': {}}}";
        var candidate = CandidateProgram.FromJson(xstateJson);
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.True(score > 0);
    }

    [Fact]
    public async Task EvaluateAsync_Scores_Higher_With_Documentation()
    {
        var evaluator = new XStateFitnessEvaluator();
        var xstateJson = "{'id': 'test', 'initial': 'start', 'states': {'start': {}}, 'description': 'Test machine'}";
        var candidate = CandidateProgram.FromJson(xstateJson);
        var score = await evaluator.EvaluateAsync(candidate);

        var xstateJsonWithoutDoc = "{'id': 'test', 'initial': 'start', 'states': {'start': {}}}";
        var candidateWithoutDoc = CandidateProgram.FromJson(xstateJsonWithoutDoc);
        var scoreWithoutDoc = await evaluator.EvaluateAsync(candidateWithoutDoc);

        Assert.True(score > scoreWithoutDoc);
    }

    [Fact]
    public async Task EvaluateAsync_Scores_Higher_With_Error_Handling()
    {
        var evaluator = new XStateFitnessEvaluator();
        var xstateJson = "{'id': 'test', 'initial': 'start', 'states': {'start': {'onError': 'errorState'}}}";
        var candidate = CandidateProgram.FromJson(xstateJson);
        var score = await evaluator.EvaluateAsync(candidate);

        var xstateJsonWithoutError = "{'id': 'test', 'initial': 'start', 'states': {'start': {}}}";
        var candidateWithoutError = CandidateProgram.FromJson(xstateJsonWithoutError);
        var scoreWithoutError = await evaluator.EvaluateAsync(candidateWithoutError);

        Assert.True(score > scoreWithoutError);
    }

    [Fact]
    public async Task EvaluateAsync_Returns_Zero_Minimum()
    {
        var evaluator = new XStateFitnessEvaluator();
        var candidate = CandidateProgram.FromJson("{}");
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.Equal(0, score);
    }

    [Fact]
    public async Task EvaluateAsync_Handles_Missing_Required_Fields()
    {
        var evaluator = new XStateFitnessEvaluator();
        var candidate = CandidateProgram.FromJson("{'id': 'test'}");
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.Equal(0, score);
    }

    [Fact]
    public async Task EvaluateAsync_Handles_Transitions()
    {
        var evaluator = new XStateFitnessEvaluator();
        var xstateJson = "{'id': 'test', 'initial': 'start', 'states': {'start': {'on': {'EVENT': 'next'}}, 'next': {}}}";
        var candidate = CandidateProgram.FromJson(xstateJson);
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.True(score > 0);
    }

    [Fact]
    public async Task EvaluateAsync_Handles_Complex_Machine()
    {
        var evaluator = new XStateFitnessEvaluator();
        var xstateJson = "{'id': 'complex', 'initial': 'idle', 'states': {'idle': {'on': {'START': 'running'}}, 'running': {'on': {'STOP': 'idle'}, 'onError': 'error'}, 'error': {}}, 'description': 'Complex machine'}";
        var candidate = CandidateProgram.FromJson(xstateJson);
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.True(score > 50);
    }
}
