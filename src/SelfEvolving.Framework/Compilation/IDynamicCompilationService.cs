namespace SelfEvolving.Framework.Compilation;

public interface IDynamicCompilationService
{
    CompilationResult Compile(string sourceCode);
}
