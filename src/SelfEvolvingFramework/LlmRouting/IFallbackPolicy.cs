namespace SelfEvolvingFramework.LlmRouting;

public interface IFallbackPolicy
{
    ModelFallbackReason EvaluateLocalBypass(ModelInvocationContext invocationContext);

    bool ShouldFallback(ModelInvocationContext invocationContext, ModelEndpointAttemptTelemetry failedAttempt);
}
