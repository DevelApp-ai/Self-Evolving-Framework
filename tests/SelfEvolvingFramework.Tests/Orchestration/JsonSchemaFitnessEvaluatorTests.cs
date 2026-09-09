using System.Text.Json;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class JsonSchemaFitnessEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_Returns_Negative_Infinity_For_Non_Json()
    {
        var evaluator = new JsonSchemaFitnessEvaluator();
        var candidate = CandidateProgram.FromCSharp("public class Test { }");
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.Equal(double.NegativeInfinity, score);
    }

    [Fact]
    public async Task EvaluateAsync_Returns_Negative_Infinity_For_Invalid_Json()
    {
        var evaluator = new JsonSchemaFitnessEvaluator();
        var candidate = CandidateProgram.FromJson("{invalid json}");
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.Equal(double.NegativeInfinity, score);
    }

    [Fact]
    public async Task EvaluateAsync_Scores_Valid_Schema()
    {
        var evaluator = new JsonSchemaFitnessEvaluator();
        var schemaJson = "{'$schema': 'http://json-schema.org/draft-07/schema#', 'type': 'object', 'properties': {'name': {'type': 'string'}}}";
        var candidate = CandidateProgram.FromJson(schemaJson);
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.True(score > 0);
    }

    [Fact]
    public async Task EvaluateAsync_Scores_Higher_With_Descriptions()
    {
        var evaluator = new JsonSchemaFitnessEvaluator();
        var schemaJson = "{'$schema': 'http://json-schema.org/draft-07/schema#', 'type': 'object', 'properties': {'name': {'type': 'string', 'description': 'User name'}}, 'description': 'User schema'}";
        var candidate = CandidateProgram.FromJson(schemaJson);
        var score = await evaluator.EvaluateAsync(candidate);

        var schemaJsonWithoutDesc = "{'$schema': 'http://json-schema.org/draft-07/schema#', 'type': 'object', 'properties': {'name': {'type': 'string'}}}";
        var candidateWithoutDesc = CandidateProgram.FromJson(schemaJsonWithoutDesc);
        var scoreWithoutDesc = await evaluator.EvaluateAsync(candidateWithoutDesc);

        Assert.True(score > scoreWithoutDesc);
    }

    [Fact]
    public async Task EvaluateAsync_Scores_Higher_With_Required_Fields()
    {
        var evaluator = new JsonSchemaFitnessEvaluator();
        var schemaJson = "{'$schema': 'http://json-schema.org/draft-07/schema#', 'type': 'object', 'properties': {'name': {'type': 'string'}}, 'required': ['name']}";
        var candidate = CandidateProgram.FromJson(schemaJson);
        var score = await evaluator.EvaluateAsync(candidate);

        var schemaJsonWithoutRequired = "{'$schema': 'http://json-schema.org/draft-07/schema#', 'type': 'object', 'properties': {'name': {'type': 'string'}}}";
        var candidateWithoutRequired = CandidateProgram.FromJson(schemaJsonWithoutRequired);
        var scoreWithoutRequired = await evaluator.EvaluateAsync(candidateWithoutRequired);

        Assert.True(score > scoreWithoutRequired);
    }

    [Fact]
    public async Task EvaluateAsync_Scores_Higher_With_Validation_Rules()
    {
        var evaluator = new JsonSchemaFitnessEvaluator();
        var schemaJson = "{'$schema': 'http://json-schema.org/draft-07/schema#', 'type': 'object', 'properties': {'name': {'type': 'string', 'minLength': 1, 'maxLength': 100}}}";
        var candidate = CandidateProgram.FromJson(schemaJson);
        var score = await evaluator.EvaluateAsync(candidate);

        var schemaJsonWithoutValidation = "{'$schema': 'http://json-schema.org/draft-07/schema#', 'type': 'object', 'properties': {'name': {'type': 'string'}}}";
        var candidateWithoutValidation = CandidateProgram.FromJson(schemaJsonWithoutValidation);
        var scoreWithoutValidation = await evaluator.EvaluateAsync(candidateWithoutValidation);

        Assert.True(score > scoreWithoutValidation);
    }

    [Fact]
    public async Task EvaluateAsync_Returns_Zero_Minimum()
    {
        var evaluator = new JsonSchemaFitnessEvaluator();
        var candidate = CandidateProgram.FromJson("{}");
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.Equal(0, score);
    }

    [Fact]
    public async Task EvaluateAsync_Handles_Missing_Required_Fields()
    {
        var evaluator = new JsonSchemaFitnessEvaluator();
        var candidate = CandidateProgram.FromJson("{'type': 'object'}");
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.Equal(0, score);
    }

    [Fact]
    public async Task EvaluateAsync_Handles_Array_Type()
    {
        var evaluator = new JsonSchemaFitnessEvaluator();
        var schemaJson = "{'$schema': 'http://json-schema.org/draft-07/schema#', 'type': 'array', 'items': {'type': 'string'}}";
        var candidate = CandidateProgram.FromJson(schemaJson);
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.True(score > 0);
    }

    [Fact]
    public async Task EvaluateAsync_Handles_Complex_Schema()
    {
        var evaluator = new JsonSchemaFitnessEvaluator();
        var schemaJson = "{'$schema': 'http://json-schema.org/draft-07/schema#', 'type': 'object', 'properties': {'name': {'type': 'string', 'minLength': 1}, 'age': {'type': 'integer', 'minimum': 0}}, 'required': ['name'], 'description': 'User profile'}";
        var candidate = CandidateProgram.FromJson(schemaJson);
        var score = await evaluator.EvaluateAsync(candidate);

        Assert.True(score > 50);
    }
}
