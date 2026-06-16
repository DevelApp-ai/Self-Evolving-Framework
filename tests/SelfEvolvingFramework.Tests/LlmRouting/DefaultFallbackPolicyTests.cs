using SelfEvolvingFramework.LlmRouting;

namespace SelfEvolvingFramework.Tests.LlmRouting;

public sealed class DefaultFallbackPolicyTests
{
    [Fact]
    public void EvaluateLocalBypass_Returns_ContextTooLarge_When_Threshold_Exceeded()
    {
        var policy = new DefaultFallbackPolicy(new RoutingPolicyOptions(CloudEscalationPromptCharacterThreshold: 10));

        var reason = policy.EvaluateLocalBypass(new ModelInvocationContext(11));

        Assert.Equal(ModelFallbackReason.ContextTooLarge, reason);
    }

    [Fact]
    public void EvaluateLocalBypass_Returns_HighComplexityTask_When_Configured()
    {
        var policy = new DefaultFallbackPolicy(new RoutingPolicyOptions(PreferCloudForHighComplexity: true));

        var reason = policy.EvaluateLocalBypass(new ModelInvocationContext(100, RequiresHighComplexity: true));

        Assert.Equal(ModelFallbackReason.HighComplexityTask, reason);
    }

    [Fact]
    public void ShouldFallback_Returns_True_For_Timeout_And_Failure()
    {
        var policy = new DefaultFallbackPolicy();
        var invocation = new ModelInvocationContext(100);
        var timeout = new ModelEndpointAttemptTelemetry("local", ModelProviderKind.LocalPrimary, TimeSpan.FromMilliseconds(1), false, true, "timeout", ModelFallbackReason.EndpointTimedOut);
        var failure = timeout with { TimedOut = false, FailureReason = ModelFallbackReason.EndpointFailure };

        Assert.True(policy.ShouldFallback(invocation, timeout));
        Assert.True(policy.ShouldFallback(invocation, failure));
    }
}
