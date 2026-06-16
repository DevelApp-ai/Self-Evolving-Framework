namespace SelfEvolvingFramework.LlmRouting;

public sealed record CloudEndpointOptions(
    string ApiKeyEnvironmentVariable,
    string BaseUrl,
    string PromptCacheKey,
    ModelEndpointOptions Small,
    ModelEndpointOptions? Devstral2 = null,
    ModelEndpointOptions? Large3 = null,
    ModelEndpointOptions? Codestral = null);
