using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Compilation;

public interface IDynamicCompilationService
{
    CompilationResult Compile(CandidateProgram candidate);
}
