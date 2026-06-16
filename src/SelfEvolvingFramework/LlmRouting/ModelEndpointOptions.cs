namespace SelfEvolvingFramework.LlmRouting;

public sealed record ModelEndpointOptions(
    string EndpointId,
    string BaseUrl,
    string ModelId,
    int TimeoutMilliseconds = 30000,
    int MaxContextCharacters = 48000,
    int MaxRetries = 1);
