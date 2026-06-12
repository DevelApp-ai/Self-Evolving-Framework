using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public sealed class SemanticKernelEvolutionCrossover(
    IChatCompletionService chatCompletionService,
    string objective,
    string? systemPrompt = null) : IEvolutionCrossover
{
    private const string DefaultSystemPrompt =
        "You are a C# crossover engine. Return only syntactically valid C# code with no markdown, no explanations, and no extra text.";

    private readonly IChatCompletionService _chatCompletionService = chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
    private readonly string _objective = !string.IsNullOrWhiteSpace(objective)
        ? objective
        : throw new ArgumentException("Objective cannot be null or whitespace.", nameof(objective));

    private readonly string _systemPrompt = string.IsNullOrWhiteSpace(systemPrompt) ? DefaultSystemPrompt : systemPrompt;

    public async Task<CandidateProgram> CrossoverAsync(
        CandidateProgram parentA,
        CandidateProgram parentB,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parentA);
        ArgumentNullException.ThrowIfNull(parentB);

        var history = CreateChatHistory(parentA.SourceCode, parentB.SourceCode);
        var responses = await _chatCompletionService.GetChatMessageContentsAsync(history, null, null, cancellationToken);
        var offspringSource = SemanticKernelEvolutionMutator.ExtractCode(responses.FirstOrDefault()?.Content);

        return string.IsNullOrWhiteSpace(offspringSource)
            ? parentA
            : new CandidateProgram(offspringSource, parentA.Id);
    }

    internal ChatHistory CreateChatHistory(string parentASourceCode, string parentBSourceCode)
    {
        var history = new ChatHistory(_systemPrompt);
        history.AddUserMessage(BuildCrossoverPrompt(parentASourceCode, parentBSourceCode));
        return history;
    }

    internal string BuildCrossoverPrompt(string parentASourceCode, string parentBSourceCode)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Objective:");
        builder.AppendLine(_objective);
        builder.AppendLine();
        builder.AppendLine("Parent A C# source:");
        builder.AppendLine(parentASourceCode);
        builder.AppendLine();
        builder.AppendLine("Parent B C# source:");
        builder.AppendLine(parentBSourceCode);
        builder.AppendLine();
        builder.AppendLine("Combine the strongest traits from both parents into one valid C# source file.");
        builder.AppendLine("Return only the full revised C# source code.");
        return builder.ToString();
    }
}
