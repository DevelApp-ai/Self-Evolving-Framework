namespace SelfEvolvingFramework.LlmRouting;

public sealed record ModelRoutingTelemetry(
    string SelectedEndpointId,
    ModelProviderKind SelectedProviderKind,
    ModelFallbackReason FinalReason,
    int PromptCharacterCount,
    int EstimatedInputTokens,
    bool PromptCacheKeyApplied,
    int TimeoutCount,
    int ErrorCount,
    IReadOnlyList<ModelEndpointAttemptTelemetry> Attempts);
