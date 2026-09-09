namespace SelfEvolvingFramework.Security;

public sealed class JsonSecurityOptions
{
    public int MaxDepth { get; set; } = 50;
    public int MaxSizeBytes { get; set; } = 1024 * 1024;
    public HashSet<string> DisallowedPatterns { get; set; } = new();
    public HashSet<string> RequiredFields { get; set; } = new();
}
