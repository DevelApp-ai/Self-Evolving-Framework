using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public sealed class JsonEvolutionCrossover : IEvolutionCrossover
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly JsonCrossoverOptions _options;

    public CandidateFormat Format => CandidateFormat.Json;

    public JsonEvolutionCrossover(
        IChatCompletionService chatCompletionService,
        JsonCrossoverOptions? options = null)
    {
        _chatCompletionService = chatCompletionService 
            ?? throw new ArgumentNullException(nameof(chatCompletionService));
        _options = options ?? new();
    }

    public async Task<CandidateProgram> CrossoverAsync(
        CandidateProgram parentA,
        CandidateProgram parentB,
        CancellationToken cancellationToken = default)
    {
        if (parentA.Format != CandidateFormat.Json || parentB.Format != CandidateFormat.Json)
            throw new InvalidOperationException(
                $"JsonEvolutionCrossover requires JSON format parents");

        var history = CreateChatHistory(parentA.SourceMaterial, parentB.SourceMaterial);
        
        var responses = await _chatCompletionService.GetChatMessageContentsAsync(
            history, null, null, cancellationToken);
        
        var offspringJson = ExtractJson(responses.FirstOrDefault()?.Content);

        return string.IsNullOrWhiteSpace(offspringJson)
            ? parentA
            : CandidateProgram.FromJson(offspringJson, parentA.ParentId, parentA.Id);
    }

    internal ChatHistory CreateChatHistory(string parentASourceMaterial, string parentBSourceMaterial)
    {
        var history = new ChatHistory(_options.SystemPrompt);
        history.AddUserMessage(BuildCrossoverPrompt(parentASourceMaterial, parentBSourceMaterial));
        return history;
    }

    internal string BuildCrossoverPrompt(string parentASourceMaterial, string parentBSourceMaterial)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Objective:");
        builder.AppendLine(_options.Objective);
        builder.AppendLine();
        builder.AppendLine("Parent A JSON:");
        builder.AppendLine(parentASourceMaterial);
        builder.AppendLine();
        builder.AppendLine("Parent B JSON:");
        builder.AppendLine(parentBSourceMaterial);
        builder.AppendLine();
        builder.AppendLine("Combine the strongest traits from both parents into a single improved JSON.");
        builder.AppendLine("Return ONLY the complete combined JSON.");
        return builder.ToString();
    }

    internal static string ExtractJson(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) 
            return string.Empty;

        var jsonStart = content.IndexOf('{');
        var jsonEnd = content.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            var json = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
            try
            {
                using var _ = JsonDocument.Parse(json);
                return json;
            }
            catch (JsonException) { }
        }

        jsonStart = content.IndexOf('[');
        jsonEnd = content.LastIndexOf(']');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            var json = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
            try
            {
                using var _ = JsonDocument.Parse(json);
                return json;
            }
            catch (JsonException) { }
        }

        return string.Empty;
    }
}
