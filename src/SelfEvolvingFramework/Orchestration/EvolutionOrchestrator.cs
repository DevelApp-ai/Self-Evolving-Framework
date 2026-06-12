using SelfEvolvingFramework.Compilation;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Security;

namespace SelfEvolvingFramework.Orchestration;

public sealed class EvolutionOrchestrator(
    IAstSecurityEvaluator securityEvaluator,
    IDynamicCompilationService compilationService,
    IFitnessEvaluator fitnessEvaluator,
    IEvolutionMutator mutator)
{
    public async Task<EvolutionResult> EvolveOnceAsync(
        CandidateProgram seed,
        IReadOnlyList<string>? feedback = null,
        CancellationToken cancellationToken = default)
    {
        var mutationFeedback = feedback is null or { Count: 0 } ? Array.Empty<string>() : feedback.ToArray();
        var mutated = await mutator.MutateAsync(seed, mutationFeedback, cancellationToken);

        var security = securityEvaluator.Evaluate(mutated.SourceCode);
        if (!security.IsAllowed)
        {
            return new EvolutionResult(mutated, false, double.NegativeInfinity, PrefixDiagnostics("security", security.Violations));
        }

        var compilation = compilationService.Compile(mutated.SourceCode);
        if (!compilation.Success)
        {
            return new EvolutionResult(mutated, false, 0, PrefixDiagnostics("compiler", compilation.Diagnostics));
        }

        var fitness = await fitnessEvaluator.EvaluateAsync(mutated, cancellationToken);
        return new EvolutionResult(mutated, true, fitness, []);
    }

    private static IReadOnlyList<string> PrefixDiagnostics(string category, IReadOnlyList<string> diagnostics)
        => diagnostics.Select(diagnostic => $"{category}: {diagnostic}").ToArray();
}
