using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SelfEvolvingFramework.Security;

public sealed class RoslynAstSecurityEvaluator(AstSecurityOptions? options = null) : IAstSecurityEvaluator
{
    private readonly AstSecurityOptions _options = options ?? new AstSecurityOptions();

    public SecurityEvaluationResult Evaluate(string sourceCode)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();
        var violations = new List<string>();

        foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            var ns = usingDirective.Name?.ToString();
            if (ns is null)
            {
                continue;
            }

            if (_options.RestrictedNamespaces.Any(restricted => ns.Equals(restricted, StringComparison.Ordinal) || ns.StartsWith(restricted + ".", StringComparison.Ordinal)))
            {
                violations.Add($"Restricted namespace: {ns}");
            }
        }

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var expression = invocation.Expression.ToString();
            if (_options.RestrictedInvocations.Any(restricted => expression.StartsWith(restricted, StringComparison.Ordinal)))
            {
                violations.Add($"Restricted invocation: {expression}");
            }
        }

        return violations.Count == 0
            ? SecurityEvaluationResult.Allowed()
            : SecurityEvaluationResult.Blocked(violations.Distinct(StringComparer.Ordinal));
    }
}
