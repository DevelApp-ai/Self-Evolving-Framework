using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SelfEvolving.Framework.Compilation;

public sealed class RoslynDynamicCompilationService : IDynamicCompilationService
{
    public CompilationResult Compile(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = GetDefaultReferences();
        var compilation = CSharpCompilation.Create(
            assemblyName: $"Dynamic_{Guid.NewGuid():N}",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var assemblyStream = new MemoryStream();
        var emitResult = compilation.Emit(assemblyStream);
        if (!emitResult.Success)
        {
            return CompilationResult.Failed(emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString()));
        }

        return new CompilationResult(true, assemblyStream.ToArray(), []);
    }

    private static IEnumerable<MetadataReference> GetDefaultReferences()
    {
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Console).Assembly,
            typeof(Enumerable).Assembly,
            Assembly.Load("System.Runtime")
        };

        return assemblies
            .Distinct()
            .Where(a => !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));
    }
}
