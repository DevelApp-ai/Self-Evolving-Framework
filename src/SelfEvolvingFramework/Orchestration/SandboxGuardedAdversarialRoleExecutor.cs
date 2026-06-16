using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.LlmRouting;

namespace SelfEvolvingFramework.Orchestration;

public sealed class SandboxGuardedAdversarialRoleExecutor(
    IAdversarialRoleExecutor innerExecutor,
    IRuntimeSandboxExecutor runtimeSandboxExecutor,
    SandboxOptions sandboxOptions,
    string preflightCommand = "exit 0") : IAdversarialRoleExecutor
{
    private readonly IAdversarialRoleExecutor _innerExecutor = innerExecutor ?? throw new ArgumentNullException(nameof(innerExecutor));
    private readonly IRuntimeSandboxExecutor _runtimeSandboxExecutor = runtimeSandboxExecutor ?? throw new ArgumentNullException(nameof(runtimeSandboxExecutor));
    private readonly SandboxOptions _sandboxOptions = sandboxOptions ?? throw new ArgumentNullException(nameof(sandboxOptions));
    private readonly string _preflightCommand = !string.IsNullOrWhiteSpace(preflightCommand)
        ? preflightCommand
        : throw new ArgumentException("Preflight command must be provided.", nameof(preflightCommand));

    public async Task<CandidateProgram> ProposeAsync(AdversarialRoleContext context, CancellationToken cancellationToken = default)
    {
        await EnsureSandboxAsync(cancellationToken);
        return await _innerExecutor.ProposeAsync(context, cancellationToken);
    }

    public async Task<IReadOnlyList<FlawReport>> ReviewAsync(AdversarialRoleContext context, CancellationToken cancellationToken = default)
    {
        await EnsureSandboxAsync(cancellationToken);
        return await _innerExecutor.ReviewAsync(context, cancellationToken);
    }

    public async Task<IReadOnlyList<FlawChallenge>> OpposeAsync(
        AdversarialRoleContext context,
        IReadOnlyList<FlawReport> reports,
        CancellationToken cancellationToken = default)
    {
        await EnsureSandboxAsync(cancellationToken);
        return await _innerExecutor.OpposeAsync(context, reports, cancellationToken);
    }

    public async Task<CandidateProgram> StewardAsync(
        AdversarialRoleContext context,
        IReadOnlyList<FlawDecision> decisions,
        CancellationToken cancellationToken = default)
    {
        await EnsureSandboxAsync(cancellationToken);
        return await _innerExecutor.StewardAsync(context, decisions, cancellationToken);
    }

    public async Task<CandidateProgram> FixAsync(
        AdversarialRoleContext context,
        IReadOnlyList<FlawDecision> acceptedFlaws,
        CancellationToken cancellationToken = default)
    {
        await EnsureSandboxAsync(cancellationToken);
        return await _innerExecutor.FixAsync(context, acceptedFlaws, cancellationToken);
    }

    private async Task EnsureSandboxAsync(CancellationToken cancellationToken)
    {
        await _runtimeSandboxExecutor.ExecuteShellAsync(_preflightCommand, _sandboxOptions, cancellationToken);
    }
}
