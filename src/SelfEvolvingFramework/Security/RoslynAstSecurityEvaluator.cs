using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace SelfEvolvingFramework.Security;

public sealed class RoslynAstSecurityEvaluator(AstSecurityOptions? options = null) : IAstSecurityEvaluator
{
    private readonly AstSecurityOptions _options = options ?? new AstSecurityOptions();

    public SecurityEvaluationResult Evaluate(string sourceCode)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();
        var violations = new List<string>();
        var semanticModel = TryCreateSemanticModel(tree);

        foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            var ns = usingDirective.Name?.ToString();
            if (ns is null)
            {
                continue;
            }

            if (IsRestrictedNamespace(ns))
            {
                violations.Add($"Restricted namespace: {ns}");
            }
        }

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var expression = invocation.Expression.ToString();
            if (IsRestrictedInvocation(expression))
            {
                violations.Add($"Restricted invocation: {expression}");
            }

            if (semanticModel is not null)
            {
                var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                var containingType = symbol?.ContainingType?.ToDisplayString();
                if (containingType is not null && IsRestrictedInvocation(containingType))
                {
                    violations.Add($"Restricted invocation: {containingType}.{symbol!.Name}");
                }
            }
        }

        foreach (var objectCreation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            var typeName = objectCreation.Type.ToString();
            if (IsRestrictedNamespace(typeName) || IsRestrictedInvocation(typeName))
            {
                violations.Add($"Restricted type: {typeName}");
                continue;
            }

            if (semanticModel is not null)
            {
                var symbol = semanticModel.GetSymbolInfo(objectCreation).Symbol as IMethodSymbol;
                var constructedType = symbol?.ContainingType?.ToDisplayString();
                if (constructedType is not null && (IsRestrictedNamespace(constructedType) || IsRestrictedInvocation(constructedType)))
                {
                    violations.Add($"Restricted type: {constructedType}");
                }
            }
        }

        return violations.Count == 0
            ? SecurityEvaluationResult.Allowed()
            : SecurityEvaluationResult.Blocked(violations.Distinct(StringComparer.Ordinal));
    }

    private bool IsRestrictedNamespace(string candidate)
    {
        var normalized = Normalize(candidate);
        return _options.RestrictedNamespaces.Any(restricted =>
            normalized.Equals(restricted, StringComparison.Ordinal) ||
            normalized.StartsWith(restricted + ".", StringComparison.Ordinal));
    }

    private bool IsRestrictedInvocation(string candidate)
    {
        var normalized = Normalize(candidate);
        return _options.RestrictedInvocations.Any(restricted =>
            normalized.StartsWith(restricted, StringComparison.Ordinal));
    }

    private static string Normalize(string value) => value.StartsWith("global::", StringComparison.Ordinal)
        ? value["global::".Length..]
        : value;

    private static SemanticModel? TryCreateSemanticModel(SyntaxTree tree)
    {
        try
        {
            var references = new[]
            {
                typeof(object).Assembly,
                typeof(Console).Assembly,
                typeof(Enumerable).Assembly,
                typeof(System.IO.File).Assembly,
                typeof(System.Runtime.InteropServices.Marshal).Assembly,
                Assembly.Load("System.Runtime")
            }
            .Distinct()
            .Where(a => !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));

            var compilation = CSharpCompilation.Create(
                assemblyName: "SecurityAnalysis",
                syntaxTrees: [tree],
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return compilation.GetSemanticModel(tree, ignoreAccessibility: true);
        }
        catch
        {
            return null;
        }
    }
}
