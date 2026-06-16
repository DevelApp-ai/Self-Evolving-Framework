using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.LlmRouting;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class SemanticKernelEvolutionMutatorTests
{
    [Fact]
    public async Task MutateAsync_Builds_Deterministic_Diagnostics_Prompt_And_Returns_Model_Code()
    {
        var chat = new CapturingChatCompletionService("public static class Runner { public static int Execute() => 2; }");
        var mutator = new SemanticKernelEvolutionMutator(chat, "Optimize runtime performance.");
        var seed = new CandidateProgram("public static class Runner { public static int Execute() => 1; }");

        var mutated = await mutator.MutateAsync(seed,
        [
            "compiler: CS1002 ; expected",
            "security: Invocation 'System.IO.File.ReadAllText' is disallowed.",
            "runtime: System.TimeoutException during Execute",
            "Prefer linear-time operations"
        ]);

        Assert.Equal(seed.Id, mutated.ParentId);
        Assert.Equal("public static class Runner { public static int Execute() => 2; }", mutated.SourceCode);

        var capturedHistory = Assert.Single(chat.CapturedHistories);
        Assert.Equal(2, capturedHistory.Count);
        Assert.Equal(AuthorRole.System, capturedHistory[0].Role);
        Assert.Equal(AuthorRole.User, capturedHistory[1].Role);
        Assert.Equal(
            """
            Objective:
            Optimize runtime performance.

            Current C# source:
            public static class Runner { public static int Execute() => 1; }

            Compiler diagnostics:
            - CS1002 ; expected
            Security diagnostics:
            - Invocation 'System.IO.File.ReadAllText' is disallowed.
            Runtime diagnostics:
            - System.TimeoutException during Execute
            Additional feedback:
            - Prefer linear-time operations

            Return only the full revised C# source code.

            """,
            capturedHistory[1].Content);
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
    public async Task MutateAsync_Strips_Code_Fences_When_Response_Contains_Extra_Text()
    {
        var chat = new CapturingChatCompletionService(
            "Here is the updated code:\n```csharp\npublic static class Runner { public static int Execute() => 4; }\n```\nThis should help.");
        var mutator = new SemanticKernelEvolutionMutator(chat, "Improve implementation.");
        var seed = new CandidateProgram("public static class Runner { public static int Execute() => 1; }");

        var mutated = await mutator.MutateAsync(seed, []);

        Assert.Equal("public static class Runner { public static int Execute() => 4; }", mutated.SourceCode);
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

    [Fact]
    public async Task MutateAsync_Propagates_Execution_Budget_To_Routing_Settings()
    {
        var chat = new CapturingChatCompletionService("public static class Runner { public static int Execute() => 5; }");
        var mutator = new SemanticKernelEvolutionMutator(chat, "Improve implementation.");
        var seed = new CandidateProgram("public static class Runner { public static int Execute() => 1; }");

        using var scope = ExecutionBudgetContext.BeginScope(250);
        _ = await mutator.MutateAsync(seed, []);

        var capturedSettings = Assert.Single(chat.CapturedExecutionSettings);
        Assert.NotNull(capturedSettings);
        Assert.NotNull(capturedSettings!.ExtensionData);
        Assert.Equal(250, capturedSettings.ExtensionData[RoutingExecutionSettingsKeys.ExecutionBudgetMilliseconds]);
    }

    private sealed class CapturingChatCompletionService(string response) : IChatCompletionService
    {
        public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

        public List<ChatHistory> CapturedHistories { get; } = [];
        public List<PromptExecutionSettings?> CapturedExecutionSettings { get; } = [];

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            CapturedHistories.Add(new ChatHistory(chatHistory));
            CapturedExecutionSettings.Add(executionSettings?.Clone());

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
