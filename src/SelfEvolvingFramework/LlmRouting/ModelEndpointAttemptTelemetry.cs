namespace SelfEvolvingFramework.LlmRouting;

public sealed record ModelEndpointAttemptTelemetry(
    string EndpointId,
    ModelProviderKind ProviderKind,
    TimeSpan Latency,
    bool Success,
    bool TimedOut,
    string? ErrorMessage,
    ModelFallbackReason FailureReason);
