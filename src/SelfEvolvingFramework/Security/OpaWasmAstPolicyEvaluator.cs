using OpaDotNet.Wasm;

namespace SelfEvolvingFramework.Security;

public sealed class OpaWasmAstPolicyEvaluator(
    IOpaEvaluator evaluator,
    RoslynAstPolicyInputSerializer? serializer = null,
    string entrypoint = "selfevolving/allow")
{
    private readonly IOpaEvaluator _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    private readonly RoslynAstPolicyInputSerializer _serializer = serializer ?? new RoslynAstPolicyInputSerializer();
    private readonly string _entrypoint = !string.IsNullOrWhiteSpace(entrypoint)
        ? entrypoint
        : throw new ArgumentException("Entrypoint cannot be null or whitespace.", nameof(entrypoint));

    public SecurityEvaluationResult Evaluate(string sourceCode)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);

        var policyData = _serializer.Serialize(sourceCode);
        _evaluator.SetDataFromRawJson(policyData);

        var policyResult = _evaluator.EvaluatePredicate<object>(new { }, _entrypoint);
        return policyResult.Result
            ? SecurityEvaluationResult.Allowed()
            : SecurityEvaluationResult.Blocked([$"OPA policy denied code for entrypoint '{_entrypoint}'."]);
    }
}
