using System.Text.Json;
using OpaDotNet.Wasm;
using SelfEvolvingFramework.Security;

namespace SelfEvolvingFramework.Tests.Security;

public sealed class OpaWasmAstPolicyEvaluatorTests
{
    [Fact]
    public void Evaluate_Allows_When_Opa_Predicate_Is_True()
    {
        var evaluator = new FakeOpaEvaluator(allow: true);
        var adapter = new OpaWasmAstPolicyEvaluator(evaluator);
        const string source = "using System.Text; public static class Sample { public static object Run() => new System.Text.StringBuilder(); }";

        var result = adapter.Evaluate(source);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
        Assert.Equal("selfevolving/allow", evaluator.LastEntrypoint);
        Assert.NotNull(evaluator.LastDataJson);

        using var json = JsonDocument.Parse(evaluator.LastDataJson!);
        Assert.True(json.RootElement.TryGetProperty("Namespaces", out _));
        Assert.True(json.RootElement.TryGetProperty("MethodCalls", out _));
        Assert.True(json.RootElement.TryGetProperty("ObjectCreations", out _));
    }

    [Fact]
    public void Evaluate_Blocks_When_Opa_Predicate_Is_False()
    {
        var evaluator = new FakeOpaEvaluator(allow: false);
        var adapter = new OpaWasmAstPolicyEvaluator(evaluator, entrypoint: "security/allow");
        const string source = "public static class Sample { public static int Run() => 1; }";

        var result = adapter.Evaluate(source);

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Violations, v => v.Contains("security/allow", StringComparison.Ordinal));
    }

    [Fact]
    public void Constructor_Throws_For_Whitespace_Entrypoint()
    {
        var evaluator = new FakeOpaEvaluator(allow: true);

        Assert.Throws<ArgumentException>(() => new OpaWasmAstPolicyEvaluator(evaluator, entrypoint: " "));
    }

    private sealed class FakeOpaEvaluator(bool allow) : IOpaEvaluator
    {
        public Version AbiVersion => new(1, 0);

        public string? LastDataJson { get; private set; }

        public string? LastEntrypoint { get; private set; }

        public PolicyEvaluationResult<bool> EvaluatePredicate<TInput>(TInput input, string? entrypoint = null)
        {
            LastEntrypoint = entrypoint;
            return new PolicyEvaluationResult<bool> { Result = allow };
        }

        public PolicyEvaluationResult<TOutput> Evaluate<TInput, TOutput>(TInput input, string? entrypoint = null)
            where TOutput : notnull
            => throw new NotSupportedException();

        public string EvaluateRaw(ReadOnlySpan<char> inputJson, string? entrypoint = null)
            => throw new NotSupportedException();

        public void SetDataFromRawJson(ReadOnlySpan<char> dataJson)
            => LastDataJson = dataJson.ToString();

        public void SetDataFromStream(Stream? utf8Json)
            => throw new NotSupportedException();

        public void SetData<T>(T? data) where T : class
            => throw new NotSupportedException();

        public void Reset()
        {
        }

        public bool TryGetFeature<TFeature>(out TFeature feature) where TFeature : class, OpaDotNet.Wasm.Features.IOpaEvaluatorFeature
        {
            feature = default!;
            return false;
        }

        public PolicyEvaluationResult<TOutput?> EvaluateOrDefault<TInput, TOutput>(TInput input, string? entrypoint = null)
            => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
