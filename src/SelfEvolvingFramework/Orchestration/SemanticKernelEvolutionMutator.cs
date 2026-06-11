using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public sealed class SemanticKernelEvolutionMutator(
    IChatCompletionService chatCompletionService,
    string objective,
    string? systemPrompt = null) : IEvolutionMutator
{
    private const string DefaultSystemPrompt =
        "You are a C# mutation engine. Return only syntactically valid C# code with no markdown, no explanations, and no extra text.";

    private readonly IChatCompletionService _chatCompletionService = chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
    private readonly string _objective = !string.IsNullOrWhiteSpace(objective)
        ? objective
        : throw new ArgumentException("Objective cannot be null or whitespace.", nameof(objective));

    private readonly string _systemPrompt = string.IsNullOrWhiteSpace(systemPrompt) ? DefaultSystemPrompt : systemPrompt;

    public async Task<CandidateProgram> MutateAsync(CandidateProgram candidate, IReadOnlyList<string> feedback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(feedback);

        var history = CreateChatHistory(candidate, feedback);
        var responses = await _chatCompletionService.GetChatMessageContentsAsync(history, null, null, cancellationToken);
        var mutatedSource = ExtractCode(responses.FirstOrDefault()?.Content);

        return string.IsNullOrWhiteSpace(mutatedSource)
            ? candidate
            : new CandidateProgram(mutatedSource, candidate.Id);
    }

    internal ChatHistory CreateChatHistory(CandidateProgram candidate, IReadOnlyList<string> feedback)
    {
        var history = new ChatHistory(_systemPrompt);
        history.AddUserMessage(BuildMutationPrompt(candidate.SourceCode, feedback));
        return history;
    }

    internal string BuildMutationPrompt(string sourceCode, IReadOnlyList<string> feedback)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Objective:");
        builder.AppendLine(_objective);
        builder.AppendLine();
        builder.AppendLine("Current C# source:");
        builder.AppendLine(sourceCode);
        builder.AppendLine();
        builder.AppendLine("Feedback from previous evaluation:");

        if (feedback.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var item in feedback)
            {
                builder.Append("- ").AppendLine(item);
            }
        }

        builder.AppendLine();
        builder.AppendLine("Return only the full revised C# source code.");
        return builder.ToString();
    }

    internal static string? ExtractCode(string? modelResponse)
    {
        if (string.IsNullOrWhiteSpace(modelResponse))
        {
            return null;
        }

        var response = modelResponse.Trim();
        if (!response.StartsWith("```", StringComparison.Ordinal))
        {
            return response;
        }

        var firstLineEnd = response.IndexOf('\n');
        if (firstLineEnd < 0)
        {
            return null;
        }

        var lastFenceStart = response.LastIndexOf("```", StringComparison.Ordinal);
        if (lastFenceStart <= firstLineEnd)
        {
            return null;
        }

        return response[(firstLineEnd + 1)..lastFenceStart].Trim();
    }
}
