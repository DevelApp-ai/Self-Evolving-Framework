namespace SelfEvolvingFramework.Core;

public sealed record CandidateProgram(string SourceCode, string? ParentId = null, string? Id = null)
{
    public string Id { get; init; } = Id ?? Guid.NewGuid().ToString("N");
}
