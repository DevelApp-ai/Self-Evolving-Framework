namespace SelfEvolvingFramework.Compilation;

public interface IDynamicCompilationService
{
    CompilationResult Compile(string sourceCode);
}
