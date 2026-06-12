using System.Text.Json;
using OpaDotNet.Wasm;
using SelfEvolvingFramework.Security;

namespace SelfEvolvingFramework.Tests.Integration;

public sealed class PolicyDrivenAstSecurityIntegrationTests
{
    [Fact]
    public void Policy_Driven_Evaluator_Allows_Code_Without_Restricted_Usage()
    {
        const string source = "using System.Text; public static class Runner { public static object Execute() => new StringBuilder(); }";
        using var evaluator = new DefaultDenyListPolicyEvaluator();
        var adapter = new OpaWasmAstPolicyEvaluator(evaluator);

        var result = adapter.Evaluate(source);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Policy_Driven_Evaluator_Denies_Code_With_Restricted_Usage()
    {
        const string source = "using System.IO; public static class Runner { public static string Execute() => System.IO.File.ReadAllText(\"x.txt\"); }";
        using var evaluator = new DefaultDenyListPolicyEvaluator();
        var adapter = new OpaWasmAstPolicyEvaluator(evaluator);

        var result = adapter.Evaluate(source);

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Violations, v => v.Contains("selfevolving/allow", StringComparison.Ordinal));
    }

    private sealed class DefaultDenyListPolicyEvaluator : IOpaEvaluator
    {
        private static readonly string[] RestrictedNamespaces =
        [
            "System.IO",
            "System.Net",
            "System.Reflection",
            "System.Runtime.InteropServices"
        ];

        private static readonly string[] RestrictedInvocations =
        [
            "System.IO.File",
            "System.IO.Directory",
            "System.Reflection.Assembly",
            "System.Runtime.InteropServices.Marshal"
        ];

        private AstPolicyInput _input = new([], [], []);

        public Version AbiVersion => new(1, 0);

        public PolicyEvaluationResult<bool> EvaluatePredicate<TInput>(TInput input, string? entrypoint = null)
        {
            var hasRestrictedNamespace = _input.Namespaces.Any(ns =>
                RestrictedNamespaces.Any(restricted =>
                    ns.Equals(restricted, StringComparison.Ordinal) ||
                    ns.StartsWith(restricted + ".", StringComparison.Ordinal)));

            var hasRestrictedInvocation = _input.MethodCalls.Any(call =>
                RestrictedInvocations.Any(restricted =>
                    call.StartsWith(restricted, StringComparison.Ordinal)));

            return new PolicyEvaluationResult<bool>
            {
                Result = !hasRestrictedNamespace && !hasRestrictedInvocation
            };
        }

        public PolicyEvaluationResult<TOutput> Evaluate<TInput, TOutput>(TInput input, string? entrypoint = null)
            where TOutput : notnull
            => throw new NotSupportedException();

        public string EvaluateRaw(ReadOnlySpan<char> inputJson, string? entrypoint = null)
            => throw new NotSupportedException();

        public void SetDataFromRawJson(ReadOnlySpan<char> dataJson)
        {
            _input = JsonSerializer.Deserialize<AstPolicyInput>(dataJson.ToString())
                ?? new AstPolicyInput([], [], []);
        }

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
