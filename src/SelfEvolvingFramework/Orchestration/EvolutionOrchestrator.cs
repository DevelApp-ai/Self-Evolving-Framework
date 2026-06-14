using System.Diagnostics;
using SelfEvolvingFramework.Compilation;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Security;

namespace SelfEvolvingFramework.Orchestration;

public sealed class EvolutionOrchestrator(
    IAstSecurityEvaluator securityEvaluator,
    IDynamicCompilationService compilationService,
    IFitnessEvaluator fitnessEvaluator,
    IEvolutionMutator mutator,
    EvolutionOrchestratorOptions? options = null,
    AdversarialFitnessFeedbackBridge? adversarialFitnessFeedbackBridge = null)
{
    private readonly EvolutionOrchestratorOptions _options = options ?? new();
    private readonly AdversarialFitnessFeedbackBridge _adversarialFitnessFeedbackBridge = adversarialFitnessFeedbackBridge ?? new();

    public async Task<EvolutionResult> EvolveOnceAsync(
        CandidateProgram seed,
        IReadOnlyList<string>? feedback = null,
        IReadOnlyList<AdversarialRoundResult>? adversarialRounds = null,
        CancellationToken cancellationToken = default)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var mutationDuration = TimeSpan.Zero;
        var securityEvaluationDuration = TimeSpan.Zero;
        var compilationDuration = TimeSpan.Zero;
        var fitnessEvaluationDuration = TimeSpan.Zero;

        EvolutionResult BuildResult(
            CandidateProgram candidate,
            bool isValid,
            double fitness,
            IReadOnlyList<string> diagnostics,
            bool canceledByCaller = false,
            bool timedOut = false)
        {
            totalStopwatch.Stop();
            return new EvolutionResult(candidate, isValid, fitness, diagnostics)
            {
                Telemetry = new EvolutionRunTelemetry(
                    totalStopwatch.Elapsed,
                    mutationDuration,
                    securityEvaluationDuration,
                    compilationDuration,
                    fitnessEvaluationDuration,
                    diagnostics.Count,
                    canceledByCaller,
                    timedOut,
                    _options.ExecutionBudgetMilliseconds)
            };
        }

        if (_options.ExecutionBudgetMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Execution budget must be greater than zero.");
        }

        using var budgetCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budgetCancellation.CancelAfter(TimeSpan.FromMilliseconds(_options.ExecutionBudgetMilliseconds));

        var mutationFeedback = feedback is null or { Count: 0 } ? Array.Empty<string>() : feedback.ToArray();
        var roundsForFitness = adversarialRounds is null or { Count: 0 } ? Array.Empty<AdversarialRoundResult>() : adversarialRounds.ToArray();
        CandidateProgram mutated;
        var mutationStopwatch = Stopwatch.StartNew();
        try
        {
            mutated = await mutator.MutateAsync(seed, mutationFeedback, budgetCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            mutationDuration = mutationStopwatch.Elapsed;
            return BuildResult(
                seed,
                false,
                double.NegativeInfinity,
                GetCancellationDiagnostics(cancellationToken),
                canceledByCaller: cancellationToken.IsCancellationRequested,
                timedOut: !cancellationToken.IsCancellationRequested);
        }
        catch (Exception ex)
        {
            mutationDuration = mutationStopwatch.Elapsed;
            return BuildResult(seed, false, double.NegativeInfinity, PrefixDiagnostics("mutation", [ex.Message]));
        }
        mutationDuration = mutationStopwatch.Elapsed;

        var securityStopwatch = Stopwatch.StartNew();
        var security = securityEvaluator.Evaluate(mutated.SourceCode);
        securityEvaluationDuration = securityStopwatch.Elapsed;
        if (!security.IsAllowed)
        {
            return BuildResult(mutated, false, double.NegativeInfinity, PrefixDiagnostics("security", security.Violations));
        }

        var compilationStopwatch = Stopwatch.StartNew();
        var compilation = compilationService.Compile(mutated.SourceCode);
        compilationDuration = compilationStopwatch.Elapsed;
        if (!compilation.Success)
        {
            return BuildResult(mutated, false, 0, PrefixDiagnostics("compiler", compilation.Diagnostics));
        }

        var fitnessStopwatch = Stopwatch.StartNew();
        try
        {
            var baseFitness = await fitnessEvaluator.EvaluateAsync(mutated, budgetCancellation.Token);
            var fitness = roundsForFitness.Length == 0
                ? baseFitness
                : _adversarialFitnessFeedbackBridge.Apply(baseFitness, roundsForFitness);
            fitnessEvaluationDuration = fitnessStopwatch.Elapsed;
            return BuildResult(mutated, true, fitness, []);
        }
        catch (OperationCanceledException)
        {
            fitnessEvaluationDuration = fitnessStopwatch.Elapsed;
            return BuildResult(
                mutated,
                false,
                double.NegativeInfinity,
                GetCancellationDiagnostics(cancellationToken),
                canceledByCaller: cancellationToken.IsCancellationRequested,
                timedOut: !cancellationToken.IsCancellationRequested);
        }
        catch (Exception ex)
        {
            fitnessEvaluationDuration = fitnessStopwatch.Elapsed;
            return BuildResult(mutated, false, double.NegativeInfinity, PrefixDiagnostics("fitness", [ex.Message]));
        }
    }

    private static IReadOnlyList<string> PrefixDiagnostics(string category, IReadOnlyList<string> diagnostics)
        => diagnostics.Select(diagnostic => $"{category}: {diagnostic}").ToArray();

    private IReadOnlyList<string> GetCancellationDiagnostics(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ["cancellation: Operation canceled by caller."];
        }

        return [$"cancellation: Operation exceeded execution budget of {_options.ExecutionBudgetMilliseconds}ms."];
    }
}
