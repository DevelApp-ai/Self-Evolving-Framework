using SelfEvolvingFramework.Compilation;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.LlmRouting;
using SelfEvolvingFramework.Orchestration;
using SelfEvolvingFramework.Security;

namespace SelfEvolvingFramework.Tests.Integration;

public sealed class AdversarialLoopWiringIntegrationTests
{
    [Fact]
    public async Task EvolveOnceAsync_Wires_Mutator_Fitness_And_Adversarial_Review_Loop()
    {
        var teams = new[]
        {
            new AdversarialTeamDefinition("team-1"),
            new AdversarialTeamDefinition("team-2"),
            new AdversarialTeamDefinition("team-3"),
            new AdversarialTeamDefinition("team-4")
        };

        var sandboxExecutor = new RecordingRuntimeSandboxExecutor();
        var reviewOrchestrator = new MultiTeamAdversarialReviewOrchestrator(
            new RoundRobinAdversarialRotationEngine(),
            new SandboxGuardedAdversarialRoleExecutor(
                new SampleRoleExecutor(),
                sandboxExecutor,
                new SandboxOptions(ExecutorType: "host", TimeoutMilliseconds: 1000)),
            new SequenceAdjudicationEngine(),
            new AdversarialWorkflowOptions(MaxRounds: 2));
        var reviewResult = await reviewOrchestrator.RunAsync(
            new CandidateProgram("public static class Runner { public static int Execute() => 2; }"),
            teams);

        var mutator = new RecordingMutator();
        var fitness = new RecordingFitnessEvaluator();
        var evolutionOrchestrator = new EvolutionOrchestrator(
            new RoslynAstSecurityEvaluator(),
            new RoslynDynamicCompilationService(),
            fitness,
            mutator);

        var result = await evolutionOrchestrator.EvolveOnceAsync(
            new CandidateProgram("public static class Runner { public static int Execute() => 1; }"),
            feedback: ["prefer return value 2"],
            adversarialRounds: reviewResult.Rounds);

        Assert.True(reviewResult.Converged);
        Assert.Equal(2, reviewResult.Rounds.Count);
        Assert.True(result.IsValid);
        Assert.Equal(100.5, result.Fitness);
        Assert.Equal(["prefer return value 2"], mutator.LastFeedback);
        Assert.NotNull(fitness.LastCandidate);
        Assert.Contains("=> 3;", fitness.LastCandidate!.SourceCode, StringComparison.Ordinal);
        Assert.True(sandboxExecutor.Calls > 0);
    }

    private sealed class RecordingMutator : IEvolutionMutator
    {
        public IReadOnlyList<string> LastFeedback { get; private set; } = Array.Empty<string>();

        public Task<CandidateProgram> MutateAsync(CandidateProgram candidate, IReadOnlyList<string> feedback, CancellationToken cancellationToken = default)
        {
            LastFeedback = feedback.ToArray();
            return Task.FromResult(new CandidateProgram("public static class Runner { public static int Execute() => 2; }", candidate.Id));
        }
    }

    private sealed class RecordingFitnessEvaluator : IFitnessEvaluator
    {
        public CandidateProgram? LastCandidate { get; private set; }

        public Task<double> EvaluateAsync(CandidateProgram candidate, CancellationToken cancellationToken = default)
        {
            LastCandidate = candidate;
            return Task.FromResult(100d);
        }
    }

    private sealed class SampleRoleExecutor : IAdversarialRoleExecutor
    {
        private int _reviewCalls;

        public Task<CandidateProgram> ProposeAsync(AdversarialRoleContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(context.CurrentCandidate);

        public Task<IReadOnlyList<FlawReport>> ReviewAsync(AdversarialRoleContext context, CancellationToken cancellationToken = default)
        {
            _reviewCalls++;
            return Task.FromResult<IReadOnlyList<FlawReport>>(
                _reviewCalls == 1
                    ? [new FlawReport("F1", "return value should be 3", FlawSeverity.Medium)]
                    : []);
        }

        public Task<IReadOnlyList<FlawChallenge>> OpposeAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawReport> reports,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FlawChallenge>>(
                reports.Select(report => new FlawChallenge(report.FlawId, true, "challenge")).ToArray());

        public Task<CandidateProgram> StewardAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawDecision> decisions,
            CancellationToken cancellationToken = default)
            => Task.FromResult(context.CurrentCandidate);

        public Task<CandidateProgram> FixAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawDecision> acceptedFlaws,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new CandidateProgram("public static class Runner { public static int Execute() => 3; }", context.CurrentCandidate.Id));
    }

    private sealed class SequenceAdjudicationEngine : IFlawAdjudicationEngine
    {
        private int _calls;

        public Task<IReadOnlyList<FlawDecision>> DecideAsync(
            AdversarialRoleContext context,
            IReadOnlyList<FlawReport> reports,
            IReadOnlyList<FlawChallenge> challenges,
            CancellationToken cancellationToken = default)
        {
            _calls++;
            return Task.FromResult<IReadOnlyList<FlawDecision>>(
                _calls == 1
                    ? [new FlawDecision("F1", FlawDisposition.Accepted, "must fix")]
                    : [new FlawDecision("F1", FlawDisposition.Rejected, "fixed")]);
        }
    }

    private sealed class RecordingRuntimeSandboxExecutor : IRuntimeSandboxExecutor
    {
        public int Calls { get; private set; }

        public Task<int> ExecuteShellAsync(string command, SandboxOptions options, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(0);
        }
    }
}
