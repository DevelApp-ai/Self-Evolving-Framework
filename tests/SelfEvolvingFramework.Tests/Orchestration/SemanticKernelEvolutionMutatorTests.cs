using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class SemanticKernelEvolutionMutatorTests
{
    [Fact]
    public async Task MutateAsync_Builds_Expected_Prompt_And_Returns_Model_Code()
    {
        var chat = new CapturingChatCompletionService("public static class Runner { public static int Execute() => 2; }");
        var mutator = new SemanticKernelEvolutionMutator(chat, "Optimize runtime performance.");
        var seed = new CandidateProgram("public static class Runner { public static int Execute() => 1; }");

        var mutated = await mutator.MutateAsync(seed, ["compilation failed", "security violation"]);

        Assert.Equal(seed.Id, mutated.ParentId);
        Assert.Equal("public static class Runner { public static int Execute() => 2; }", mutated.SourceCode);

        var capturedHistory = Assert.Single(chat.CapturedHistories);
        Assert.Equal(2, capturedHistory.Count);
        Assert.Equal(AuthorRole.System, capturedHistory[0].Role);
        Assert.Equal(AuthorRole.User, capturedHistory[1].Role);
        Assert.Contains("Objective:", capturedHistory[1].Content, StringComparison.Ordinal);
        Assert.Contains("Optimize runtime performance.", capturedHistory[1].Content, StringComparison.Ordinal);
        Assert.Contains("- compilation failed", capturedHistory[1].Content, StringComparison.Ordinal);
        Assert.Contains("- security violation", capturedHistory[1].Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MutateAsync_Strips_Code_Fences_From_Model_Response()
    {
        var chat = new CapturingChatCompletionService("```csharp\npublic static class Runner { public static int Execute() => 3; }\n```");
        var mutator = new SemanticKernelEvolutionMutator(chat, "Improve implementation.");
        var seed = new CandidateProgram("public static class Runner { public static int Execute() => 1; }");

        var mutated = await mutator.MutateAsync(seed, []);

        Assert.Equal("public static class Runner { public static int Execute() => 3; }", mutated.SourceCode);
    }

    [Fact]
    public async Task MutateAsync_Returns_Original_Candidate_When_Model_Returns_Empty()
    {
        var chat = new CapturingChatCompletionService("   ");
        var mutator = new SemanticKernelEvolutionMutator(chat, "Improve implementation.");
        var seed = new CandidateProgram("public static class Runner { public static int Execute() => 1; }");

        var mutated = await mutator.MutateAsync(seed, []);

        Assert.Same(seed, mutated);
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
