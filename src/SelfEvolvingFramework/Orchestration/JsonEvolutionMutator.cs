using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SelfEvolvingFramework.LlmRouting;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public sealed class JsonEvolutionMutator : IEvolutionMutator
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly JsonMutationOptions _options;

    public CandidateFormat Format => CandidateFormat.Json;

    public JsonEvolutionMutator(
        IChatCompletionService chatCompletionService,
        JsonMutationOptions? options = null)
    {
        _chatCompletionService = chatCompletionService 
            ?? throw new ArgumentNullException(nameof(chatCompletionService));
        _options = options ?? new();
    }

    public async Task<CandidateProgram> MutateAsync(
        CandidateProgram candidate,
        IReadOnlyList<string> feedback,
        CancellationToken cancellationToken = default)
    {
        if (candidate.Format != CandidateFormat.Json)
            throw new InvalidOperationException(
                $"JsonEvolutionMutator requires JSON format, got {candidate.Format}");

        var history = CreateChatHistory(candidate.SourceMaterial, feedback);
        var executionSettings = CreateExecutionSettings();
        
        var responses = await _chatCompletionService.GetChatMessageContentsAsync(
            history, executionSettings, null, cancellationToken);
        
        var mutatedJson = ExtractJson(responses.FirstOrDefault()?.Content);

        return string.IsNullOrWhiteSpace(mutatedJson)
            ? candidate
            : CandidateProgram.FromJson(mutatedJson, candidate.ParentId, candidate.Id);
    }

    internal ChatHistory CreateChatHistory(string json, IReadOnlyList<string> feedback)
    {
        var history = new ChatHistory(_options.SystemPrompt);
        history.AddUserMessage(BuildMutationPrompt(json, feedback));
        return history;
    }

    internal string BuildMutationPrompt(string json, IReadOnlyList<string> feedback)
    {
        var (validationDiagnostics, securityDiagnostics, runtimeDiagnostics, additionalFeedback) 
            = CategorizeFeedback(feedback);
        
        var builder = new StringBuilder();
        builder.AppendLine("Objective:");
        builder.AppendLine(_options.Objective);
        builder.AppendLine();
        builder.AppendLine("Current JSON:");
        builder.AppendLine(json);
        builder.AppendLine();
        
        AppendFeedbackSection(builder, "Validation diagnostics:", validationDiagnostics);
        AppendFeedbackSection(builder, "Security diagnostics:", securityDiagnostics);
        AppendFeedbackSection(builder, "Runtime diagnostics:", runtimeDiagnostics);
        AppendFeedbackSection(builder, "Additional feedback:", additionalFeedback);

        builder.AppendLine();
        builder.AppendLine("Return ONLY the full revised JSON with no markdown, explanations, or extra text.");
        builder.AppendLine("Ensure the JSON is valid and maintains all required fields.");
        
        return builder.ToString();
    }

    private static void AppendFeedbackSection(StringBuilder builder, string title, IReadOnlyList<string> items)
    {
        builder.AppendLine(title);
        if (items.Count == 0)
        {
            builder.AppendLine("- None");
            return;
        }

        foreach (var item in items)
        {
            builder.Append("- ").AppendLine(item);
        }
    }

    private static (IReadOnlyList<string> Validation, IReadOnlyList<string> Security, IReadOnlyList<string> Runtime, IReadOnlyList<string> Additional)
        CategorizeFeedback(IReadOnlyList<string> feedback)
    {
        var validation = new List<string>();
        var security = new List<string>();
        var runtime = new List<string>();
        var additional = new List<string>();

        foreach (var item in feedback)
        {
            if (TryStripPrefix(item, "validation", out var validationDiagnostic))
            {
                validation.Add(validationDiagnostic);
                continue;
            }

            if (TryStripPrefix(item, "security", out var securityDiagnostic))
            {
                security.Add(securityDiagnostic);
                continue;
            }

            if (TryStripPrefix(item, "runtime", out var runtimeDiagnostic))
            {
                runtime.Add(runtimeDiagnostic);
                continue;
            }

            additional.Add(item);
        }

        return (validation, security, runtime, additional);
    }

    private static bool TryStripPrefix(string value, string prefix, out string strippedValue)
    {
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            value.Length > prefix.Length &&
            value[prefix.Length] == ':')
        {
            strippedValue = value[(prefix.Length + 1)..].Trim();
            return true;
        }

        strippedValue = value;
        return false;
    }

    private PromptExecutionSettings? CreateExecutionSettings()
    {
        var executionBudgetMilliseconds = ExecutionBudgetContext.CurrentExecutionBudgetMilliseconds;
        if (executionBudgetMilliseconds is null or <= 0)
        {
            return null;
        }

        return new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["temperature"] = _options.Temperature,
                ["max_tokens"] = _options.MaxTokens,
                ["top_p"] = _options.TopP,
                [RoutingExecutionSettingsKeys.ExecutionBudgetMilliseconds] = executionBudgetMilliseconds.Value
            }
        };
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
                using var _ = LenientJson.Parse(json);
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
                using var _ = LenientJson.Parse(json);
                return json;
            }
            catch (JsonException) { }
        }

        return string.Empty;
    }
}
