using SelfEvolving.Framework.Compilation;
using SelfEvolving.Framework.Core;
using SelfEvolving.Framework.Security;

namespace SelfEvolving.Framework.Orchestration;

public sealed class EvolutionOrchestrator(
    IAstSecurityEvaluator securityEvaluator,
    IDynamicCompilationService compilationService,
    IFitnessEvaluator fitnessEvaluator,
    IEvolutionMutator mutator)
{
    public async Task<EvolutionResult> EvolveOnceAsync(CandidateProgram seed, CancellationToken cancellationToken = default)
    {
        var mutated = await mutator.MutateAsync(seed, [], cancellationToken);

        var security = securityEvaluator.Evaluate(mutated.SourceCode);
        if (!security.IsAllowed)
        {
            return new EvolutionResult(mutated, false, double.NegativeInfinity, security.Violations);
        }

        var compilation = compilationService.Compile(mutated.SourceCode);
        if (!compilation.Success)
        {
            return new EvolutionResult(mutated, false, 0, compilation.Diagnostics);
        }

        var fitness = await fitnessEvaluator.EvaluateAsync(mutated, cancellationToken);
        return new EvolutionResult(mutated, true, fitness, []);
    }
}
