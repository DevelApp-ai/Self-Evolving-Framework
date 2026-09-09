using System.Text.Json;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Security;

namespace SelfEvolvingFramework.Tests.Security;

public sealed class JsonSecurityEvaluatorTests
{
    [Fact]
    public void Evaluate_Allows_Valid_Json()
    {
        var evaluator = new JsonSecurityEvaluator();
        var json = "{'name': 'test', 'value': 42}";
        var result = evaluator.Evaluate(json);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Evaluate_Detects_Prototype_Pollution()
    {
        var evaluator = new JsonSecurityEvaluator();
        var json = "{'__proto__': {'malicious': true}}";
        var result = evaluator.Evaluate(json);

        Assert.False(result.IsAllowed);
        Assert.Contains("prototype pollution", result.Violations[0].ToLower());
    }

    [Fact]
    public void Evaluate_Detects_Constructor_Pollution()
    {
        var evaluator = new JsonSecurityEvaluator();
        var json = "{'constructor': {'malicious': true}}";
        var result = evaluator.Evaluate(json);

        Assert.False(result.IsAllowed);
        Assert.Contains("prototype pollution", result.Violations[0].ToLower());
    }

    [Fact]
    public void Evaluate_Detects_Nested_Prototype_Pollution()
    {
        var evaluator = new JsonSecurityEvaluator();
        var json = "{'outer': {'__proto__': {'malicious': true}}}";
        var result = evaluator.Evaluate(json);

        Assert.False(result.IsAllowed);
        Assert.Contains("prototype pollution", result.Violations[0].ToLower());
    }

    [Fact]
    public void Evaluate_Detects_Excessive_Depth()
    {
        var evaluator = new JsonSecurityEvaluator(new JsonSecurityOptions { MaxDepth = 5 });
        var json = "{'a': {'b': {'c': {'d': {'e': {'f': {}}}}}}}";
        var result = evaluator.Evaluate(json);

        Assert.False(result.IsAllowed);
        Assert.Contains("nested", result.Violations[0].ToLower());
    }

    [Fact]
    public void Evaluate_Detects_Size_Limit_Exceeded()
    {
        var evaluator = new JsonSecurityEvaluator(new JsonSecurityOptions { MaxSizeBytes = 10 });
        var json = "{'test': 'this is a very long string that exceeds the size limit'}";
        var result = evaluator.Evaluate(json);

        Assert.False(result.IsAllowed);
        Assert.Contains("size", result.Violations[0].ToLower());
    }

    [Fact]
    public void Evaluate_Detects_Disallowed_Patterns()
    {
        var evaluator = new JsonSecurityEvaluator(new JsonSecurityOptions
        {
            DisallowedPatterns = new HashSet<string> { "eval(", "script:" }
        });
        var json = "{'code': 'eval(x)'}";
        var result = evaluator.Evaluate(json);

        Assert.False(result.IsAllowed);
        Assert.Contains("Disallowed pattern", result.Violations[0]);
    }

    [Fact]
    public void Evaluate_Detects_Missing_Required_Fields()
    {
        var evaluator = new JsonSecurityEvaluator(new JsonSecurityOptions
        {
            RequiredFields = new HashSet<string> { "id", "name" }
        });
        var json = "{'name': 'test'}";
        var result = evaluator.Evaluate(json);

        Assert.False(result.IsAllowed);
        Assert.Contains("Missing required fields", result.Violations[0]);
        Assert.Contains("id", result.Violations[0]);
    }

    [Fact]
    public void Evaluate_Detects_Invalid_Json()
    {
        var evaluator = new JsonSecurityEvaluator();
        var json = "{invalid json}";
        var result = evaluator.Evaluate(json);

        Assert.False(result.IsAllowed);
        Assert.Contains("Invalid JSON", result.Violations[0]);
    }

    [Fact]
    public void Evaluate_Allows_Json_With_All_Required_Fields()
    {
        var evaluator = new JsonSecurityEvaluator(new JsonSecurityOptions
        {
            RequiredFields = new HashSet<string> { "id", "name" }
        });
        var json = "{'id': '123', 'name': 'test'}";
        var result = evaluator.Evaluate(json);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Format_Returns_Json()
    {
        var evaluator = new JsonSecurityEvaluator();
        Assert.Equal(CandidateFormat.Json, evaluator.Format);
    }

    [Fact]
    public void Evaluate_Handles_Array_Json()
    {
        var evaluator = new JsonSecurityEvaluator();
        var json = "[1, 2, 3, 4, 5]";
        var result = evaluator.Evaluate(json);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Evaluate_Handles_Nested_Arrays()
    {
        var evaluator = new JsonSecurityEvaluator();
        var json = "{'items': [{'a': 1}, {'b': 2}]}";
        var result = evaluator.Evaluate(json);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Evaluate_Detects_Prototype_Pollution_In_Array()
    {
        var evaluator = new JsonSecurityEvaluator();
        var json = "[{'__proto__': 1}, {'b': 2}]";
        var result = evaluator.Evaluate(json);

        Assert.False(result.IsAllowed);
        Assert.Contains("prototype pollution", result.Violations[0].ToLower());
    }
}
