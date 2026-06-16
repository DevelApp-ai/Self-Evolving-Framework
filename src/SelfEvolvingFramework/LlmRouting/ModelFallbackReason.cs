namespace SelfEvolvingFramework.LlmRouting;

public enum ModelFallbackReason
{
    None,
    PolicyBypassedLocal,
    LocalEndpointUnavailable,
    EndpointTimedOut,
    EndpointFailure,
    ContextTooLarge,
    HighComplexityTask,
    ArchitectReasoningTask,
    DiagnosticTask,
    ExhaustedAllEndpoints
}
