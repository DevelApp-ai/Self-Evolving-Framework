namespace SelfEvolvingFramework.Security;

public sealed record AstPolicyInput(
    IReadOnlyList<string> Namespaces,
    IReadOnlyList<string> MethodCalls,
    IReadOnlyList<string> ObjectCreations);

