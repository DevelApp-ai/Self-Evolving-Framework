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
        var (compilerDiagnostics, securityDiagnostics, runtimeDiagnostics, additionalFeedback) = CategorizeFeedback(feedback);
        var builder = new StringBuilder();
        builder.AppendLine("Objective:");
        builder.AppendLine(_objective);
        builder.AppendLine();
        builder.AppendLine("Current C# source:");
        builder.AppendLine(sourceCode);
        builder.AppendLine();
        AppendFeedbackSection(builder, "Compiler diagnostics:", compilerDiagnostics);
        AppendFeedbackSection(builder, "Security diagnostics:", securityDiagnostics);
        AppendFeedbackSection(builder, "Runtime diagnostics:", runtimeDiagnostics);
        AppendFeedbackSection(builder, "Additional feedback:", additionalFeedback);

        builder.AppendLine();
        builder.AppendLine("Return only the full revised C# source code.");
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

    private static (IReadOnlyList<string> Compiler, IReadOnlyList<string> Security, IReadOnlyList<string> Runtime, IReadOnlyList<string> Additional)
        CategorizeFeedback(IReadOnlyList<string> feedback)
    {
        var compiler = new List<string>();
        var security = new List<string>();
        var runtime = new List<string>();
        var additional = new List<string>();

        foreach (var item in feedback)
        {
            if (TryStripPrefix(item, "compiler", out var compilerDiagnostic))
            {
                compiler.Add(compilerDiagnostic);
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

        return (compiler, security, runtime, additional);
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

    internal static string? ExtractCode(string? modelResponse)
    {
        if (string.IsNullOrWhiteSpace(modelResponse))
        {
            return null;
        }

        var response = modelResponse.Trim();
        var firstFence = response.IndexOf("```", StringComparison.Ordinal);
        if (firstFence < 0)
        {
            return response;
        }

        var firstLineEnd = response.IndexOf('\n', firstFence);
        if (firstLineEnd < 0)
        {
            return null;
        }

        var lastFenceStart = response.LastIndexOf("```", StringComparison.Ordinal);
        if (lastFenceStart <= firstLineEnd || lastFenceStart == firstFence)
        {
            return null;
        }

        return response[(firstLineEnd + 1)..lastFenceStart].Trim();
    }
}
