using System.Reflection;

namespace SelfEvolvingFramework.Core;

public enum CandidateFormat
{
    CSharp,
    Json,
    Yaml
}

public sealed record CandidateProgram
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string? ParentId { get; init; }
    public string SourceMaterial { get; init; }
    public CandidateFormat Format { get; init; }
    public Assembly? CompiledAssembly { get; set; }
    public CompilationResult? CompilationResult { get; set; }

    private CandidateProgram(string sourceMaterial, string? parentId, string? id, CandidateFormat format)
    {
        SourceMaterial = sourceMaterial ?? throw new ArgumentNullException(nameof(sourceMaterial));
        ParentId = parentId;
        Id = id ?? Guid.NewGuid().ToString("N");
        Format = format;
    }

    public static CandidateProgram FromCSharp(string csharpCode, string? parentId = null, string? id = null)
        => new(csharpCode, parentId, id, CandidateFormat.CSharp);

    public static CandidateProgram FromJson(string json, string? parentId = null, string? id = null)
        => new(json, parentId, id, CandidateFormat.Json);

    public static CandidateProgram FromYaml(string yaml, string? parentId = null, string? id = null)
        => new(yaml, parentId, id, CandidateFormat.Yaml);

    public static implicit operator CandidateProgram(string sourceCode)
        => FromCSharp(sourceCode);
}
