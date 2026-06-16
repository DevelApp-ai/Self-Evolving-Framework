namespace SelfEvolvingFramework.LlmRouting;

public sealed record LocalEndpointOptions(
    ModelEndpointOptions Primary,
    ModelEndpointOptions? Diagnostic = null,
    int ConsecutiveFailureCircuitThreshold = 3,
    int CircuitOpenSeconds = 60);
