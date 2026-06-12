using SelfEvolvingFramework.Compilation;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Execution;

namespace SelfEvolvingFramework.Behavioral;

public sealed record PlaywrightPostCompilationBehavioralEvaluatorOptions(
    string EntryPointTypeName = "Runner",
    string EntryPointMethodName = "Execute",
    int EntryPointTimeoutMilliseconds = 5000);

public sealed class PlaywrightPostCompilationBehavioralEvaluator(
    IDynamicCompilationService compilationService,
    IIsolatedAssemblyExecutor assemblyExecutor,
    IPlaywrightBehavioralFlowRunner flowRunner,
    PlaywrightPostCompilationBehavioralEvaluatorOptions? options = null) : IPostCompilationBehavioralEvaluator
{
    private readonly IDynamicCompilationService _compilationService = compilationService ?? throw new ArgumentNullException(nameof(compilationService));
    private readonly IIsolatedAssemblyExecutor _assemblyExecutor = assemblyExecutor ?? throw new ArgumentNullException(nameof(assemblyExecutor));
    private readonly IPlaywrightBehavioralFlowRunner _flowRunner = flowRunner ?? throw new ArgumentNullException(nameof(flowRunner));
    private readonly PlaywrightPostCompilationBehavioralEvaluatorOptions _options = options ?? new();

    public async Task<PostCompilationBehavioralEvaluationResult> EvaluateAsync(CandidateProgram candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ValidateOptions(_options);

        var compilation = _compilationService.Compile(candidate.SourceCode);
        if (!compilation.Success)
        {
            return PostCompilationBehavioralEvaluationResult.Failed(compilation.Diagnostics.Select(diagnostic => $"compiler: {diagnostic}"));
        }

        if (compilation.AssemblyBytes is null)
        {
            return PostCompilationBehavioralEvaluationResult.Failed(["compiler: Assembly bytes were not produced."]);
        }

        var execution = await _assemblyExecutor.ExecuteStaticAsync(
            compilation.AssemblyBytes,
            _options.EntryPointTypeName,
            _options.EntryPointMethodName,
            TimeSpan.FromMilliseconds(_options.EntryPointTimeoutMilliseconds),
            cancellationToken);

        if (!execution.Completed)
        {
            return PostCompilationBehavioralEvaluationResult.Failed(execution.Diagnostics.Select(diagnostic => $"runtime: {diagnostic}"));
        }

        if (execution.ReturnValue is not string endpoint ||
            !Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            return PostCompilationBehavioralEvaluationResult.Failed(["runtime: Entry point did not return a valid absolute endpoint URL."]);
        }

        var diagnostics = await _flowRunner.RunAsync(endpointUri, cancellationToken);
        return new PostCompilationBehavioralEvaluationResult(diagnostics.Count == 0, diagnostics);
    }

    private static void ValidateOptions(PlaywrightPostCompilationBehavioralEvaluatorOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.EntryPointTypeName))
        {
            throw new ArgumentException("Entry point type name must be provided.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.EntryPointMethodName))
        {
            throw new ArgumentException("Entry point method name must be provided.", nameof(options));
        }

        if (options.EntryPointTimeoutMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Entry point timeout must be greater than zero.");
        }
    }
}
