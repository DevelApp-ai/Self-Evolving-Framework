namespace SelfEvolvingFramework.LlmRouting;

public sealed record SandboxOptions(
    string ExecutorType = "docker",
    string ImageProfile = "default",
    int TimeoutMilliseconds = 30000,
    int MemoryLimitMegabytes = 2048);
