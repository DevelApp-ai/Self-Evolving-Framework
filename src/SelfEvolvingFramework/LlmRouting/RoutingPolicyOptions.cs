namespace SelfEvolvingFramework.LlmRouting;

public sealed record RoutingPolicyOptions(
    bool EnableLocalRouting = true,
    bool EnableCloudFallback = true,
    int CloudEscalationPromptCharacterThreshold = 32000,
    bool PreferCloudForHighComplexity = true,
    bool PreferCloudForArchitectReasoning = true,
    bool PreferDiagnosticModelForDiagnosticTasks = true,
    int TimeoutBufferMilliseconds = 500);
