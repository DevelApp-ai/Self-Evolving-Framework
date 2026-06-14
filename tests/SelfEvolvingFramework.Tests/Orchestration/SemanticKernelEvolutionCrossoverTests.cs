using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class SemanticKernelEvolutionCrossoverTests
{
    [Fact]
    public async Task CrossoverAsync_Builds_Expected_Prompt_And_Returns_Model_Code()
    {
        var chat = new CapturingChatCompletionService("public static class Runner { public static int Execute() => 3; }");
        var crossover = new SemanticKernelEvolutionCrossover(chat, "Optimize for throughput and correctness.");
        var parentA = new CandidateProgram("public static class Runner { public static int Execute() => 1; }");
        var parentB = new CandidateProgram("public static class Runner { public static int Execute() => 2; }");

        var offspring = await crossover.CrossoverAsync(parentA, parentB);

        Assert.Equal(parentA.Id, offspring.ParentId);
        Assert.Equal("public static class Runner { public static int Execute() => 3; }", offspring.SourceCode);

        var capturedHistory = Assert.Single(chat.CapturedHistories);
        Assert.Equal(2, capturedHistory.Count);
        Assert.Equal(AuthorRole.System, capturedHistory[0].Role);
        Assert.Equal(AuthorRole.User, capturedHistory[1].Role);
        Assert.Contains("Objective:", capturedHistory[1].Content, StringComparison.Ordinal);
        Assert.Contains("Optimize for throughput and correctness.", capturedHistory[1].Content, StringComparison.Ordinal);
        Assert.Contains("Parent A C# source:", capturedHistory[1].Content, StringComparison.Ordinal);
        Assert.Contains(parentA.SourceCode, capturedHistory[1].Content, StringComparison.Ordinal);
        Assert.Contains("Parent B C# source:", capturedHistory[1].Content, StringComparison.Ordinal);
        Assert.Contains(parentB.SourceCode, capturedHistory[1].Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CrossoverAsync_Strips_Code_Fences_From_Model_Response()
    {
        var chat = new CapturingChatCompletionService("```csharp\npublic static class Runner { public static int Execute() => 5; }\n```");
        var crossover = new SemanticKernelEvolutionCrossover(chat, "Blend both implementations.");
        var parentA = new CandidateProgram("public static class Runner { public static int Execute() => 1; }");
        var parentB = new CandidateProgram("public static class Runner { public static int Execute() => 2; }");

        var offspring = await crossover.CrossoverAsync(parentA, parentB);

        Assert.Equal("public static class Runner { public static int Execute() => 5; }", offspring.SourceCode);
    }

    [Fact]
    public async Task CrossoverAsync_Strips_Code_Fences_When_Response_Contains_Extra_Text()
    {
        var chat = new CapturingChatCompletionService(
            "Result:\n```csharp\npublic static class Runner { public static int Execute() => 6; }\n```\nDone.");
        var crossover = new SemanticKernelEvolutionCrossover(chat, "Blend both implementations.");
        var parentA = new CandidateProgram("public static class Runner { public static int Execute() => 1; }");
        var parentB = new CandidateProgram("public static class Runner { public static int Execute() => 2; }");

        var offspring = await crossover.CrossoverAsync(parentA, parentB);

        Assert.Equal("public static class Runner { public static int Execute() => 6; }", offspring.SourceCode);
    }

    [Fact]
    public async Task CrossoverAsync_Returns_First_Parent_When_Model_Returns_Empty()
    {
        var chat = new CapturingChatCompletionService("   ");
        var crossover = new SemanticKernelEvolutionCrossover(chat, "Blend both implementations.");
        var parentA = new CandidateProgram("public static class Runner { public static int Execute() => 1; }");
        var parentB = new CandidateProgram("public static class Runner { public static int Execute() => 2; }");

        var offspring = await crossover.CrossoverAsync(parentA, parentB);

        Assert.Same(parentA, offspring);
    }

    private sealed class CapturingChatCompletionService(string response) : IChatCompletionService
    {
        public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

        public List<ChatHistory> CapturedHistories { get; } = [];

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            CapturedHistories.Add(new ChatHistory(chatHistory));

            IReadOnlyList<ChatMessageContent> messages =
            [
                new ChatMessageContent(AuthorRole.Assistant, response)
            ];

            return Task.FromResult(messages);
        }

        public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
            => EmptyAsync();

        private static async IAsyncEnumerable<StreamingChatMessageContent> EmptyAsync()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
