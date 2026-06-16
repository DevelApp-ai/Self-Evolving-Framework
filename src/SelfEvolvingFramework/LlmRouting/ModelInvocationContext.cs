namespace SelfEvolvingFramework.LlmRouting;

public sealed record ModelInvocationContext(
    int PromptCharacterCount,
    bool RequiresHighComplexity = false,
    bool RequiresArchitectReasoning = false,
    bool IsDiagnosticTask = false);
