namespace SelfEvolvingFramework.LlmRouting;

public sealed class DefaultFallbackPolicy(RoutingPolicyOptions? options = null) : IFallbackPolicy
{
    private readonly RoutingPolicyOptions _options = options ?? new();

    public ModelFallbackReason EvaluateLocalBypass(ModelInvocationContext invocationContext)
    {
        if (invocationContext.PromptCharacterCount >= _options.CloudEscalationPromptCharacterThreshold)
        {
            return ModelFallbackReason.ContextTooLarge;
        }

        if (invocationContext.RequiresHighComplexity && _options.PreferCloudForHighComplexity)
        {
            return ModelFallbackReason.HighComplexityTask;
        }

        if (invocationContext.RequiresArchitectReasoning && _options.PreferCloudForArchitectReasoning)
        {
            return ModelFallbackReason.ArchitectReasoningTask;
        }

        return ModelFallbackReason.None;
    }

    public bool ShouldFallback(ModelInvocationContext invocationContext, ModelEndpointAttemptTelemetry failedAttempt)
    {
        _ = invocationContext;
        return failedAttempt.FailureReason is ModelFallbackReason.EndpointTimedOut or ModelFallbackReason.EndpointFailure;
    }
}
