using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SelfEvolvingFramework.Security;

public sealed class RoslynAstPolicyInputSerializer
{
    public AstPolicyInput Create(string sourceCode)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();

        var namespaces = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Select(usingDirective => usingDirective.Name?.ToString())
            .Where(ns => !string.IsNullOrWhiteSpace(ns))
            .Select(ns => Normalize(ns!))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(ns => ns, StringComparer.Ordinal)
            .ToArray();

        var methodCalls = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => Normalize(invocation.Expression.ToString()))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(methodCall => methodCall, StringComparer.Ordinal)
            .ToArray();

        var objectCreations = root.DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Select(creation => Normalize(creation.Type.ToString()))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(typeName => typeName, StringComparer.Ordinal)
            .ToArray();

        return new AstPolicyInput(namespaces, methodCalls, objectCreations);
    }

    public string Serialize(string sourceCode) => JsonSerializer.Serialize(Create(sourceCode));

    private static string Normalize(string value) => value.StartsWith("global::", StringComparison.Ordinal)
        ? value["global::".Length..]
        : value;
}

