using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class JsonEvolutionMutatorTests
{
    [Fact]
    public async Task MutateAsync_Returns_Candidate_When_No_Response()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        mockChatService.Setup(x => x.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), It.IsAny<PromptExecutionSettings>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent>());

        var mutator = new JsonEvolutionMutator(mockChatService.Object);
        var candidate = CandidateProgram.FromJson("{'test': 1}");

        var result = await mutator.MutateAsync(candidate, Array.Empty<string>());

        Assert.Same(candidate, result);
    }

    [Fact]
    public async Task MutateAsync_Returns_Mutated_Json_From_Response()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var responseContent = "{'mutated': 2}";
        var messageContent = new ChatMessageContent(ChatRole.Assistant, responseContent);
        mockChatService.Setup(x => x.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), It.IsAny<PromptExecutionSettings>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { messageContent });

        var mutator = new JsonEvolutionMutator(mockChatService.Object);
        var candidate = CandidateProgram.FromJson("{'test': 1}");

        var result = await mutator.MutateAsync(candidate, Array.Empty<string>());

        Assert.NotSame(candidate, result);
        Assert.Equal(CandidateFormat.Json, result.Format);
        Assert.Contains("mutated", result.SourceMaterial);
    }

    [Fact]
    public async Task MutateAsync_Extracts_Json_From_Markdown()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var responseContent = "Here is the mutated JSON:\n```json\n{'mutated': 2}\n```";
        var messageContent = new ChatMessageContent(ChatRole.Assistant, responseContent);
        mockChatService.Setup(x => x.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), It.IsAny<PromptExecutionSettings>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { messageContent });

        var mutator = new JsonEvolutionMutator(mockChatService.Object);
        var candidate = CandidateProgram.FromJson("{'test': 1}");

        var result = await mutator.MutateAsync(candidate, Array.Empty<string>());

        Assert.NotSame(candidate, result);
        Assert.Equal("{'mutated': 2}", result.SourceMaterial);
    }

    [Fact]
    public async Task MutateAsync_Extracts_Json_Array()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var responseContent = "```json\n[1, 2, 3]\n```";
        var messageContent = new ChatMessageContent(ChatRole.Assistant, responseContent);
        mockChatService.Setup(x => x.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), It.IsAny<PromptExecutionSettings>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { messageContent });

        var mutator = new JsonEvolutionMutator(mockChatService.Object);
        var candidate = CandidateProgram.FromJson("[1, 2]");

        var result = await mutator.MutateAsync(candidate, Array.Empty<string>());

        Assert.Equal("[1, 2, 3]", result.SourceMaterial);
    }

    [Fact]
    public async Task MutateAsync_Throws_For_Non_Json_Candidate()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var mutator = new JsonEvolutionMutator(mockChatService.Object);
        var candidate = CandidateProgram.FromCSharp("public class Test { }");

        await Assert.ThrowsAsync<InvalidOperationException>(() => mutator.MutateAsync(candidate, Array.Empty<string>()));
    }

    [Fact]
    public void Format_Returns_Json()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var mutator = new JsonEvolutionMutator(mockChatService.Object);
        Assert.Equal(CandidateFormat.Json, mutator.Format);
    }

    [Fact]
    public async Task BuildMutationPrompt_Includes_Feedback()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var mutator = new JsonEvolutionMutator(mockChatService.Object);
        var json = "{'test': 1}";
        var feedback = new[] { "validation: field missing", "security: no issues" };

        var prompt = mutator.BuildMutationPrompt(json, feedback);

        Assert.Contains(json, prompt);
        Assert.Contains("field missing", prompt);
        Assert.Contains("no issues", prompt);
    }

    [Fact]
    public async Task BuildMutationPrompt_Includes_Empty_Feedback()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var mutator = new JsonEvolutionMutator(mockChatService.Object);
        var json = "{'test': 1}";

        var prompt = mutator.BuildMutationPrompt(json, Array.Empty<string>());

        Assert.Contains("- None", prompt);
    }

    [Fact]
    public async Task CreateChatHistory_Uses_Correct_System_Prompt()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var options = new JsonMutationOptions
        {
            SystemPrompt = "Custom system prompt"
        };
        var mutator = new JsonEvolutionMutator(mockChatService.Object, options);

        var history = mutator.CreateChatHistory("{'test': 1}", Array.Empty<string>());

        Assert.Equal("Custom system prompt", history[0].Content);
    }

    [Fact]
    public void ExtractJson_Handles_Null_Content()
    {
        var result = JsonEvolutionMutator.ExtractJson(null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ExtractJson_Handles_Empty_Content()
    {
        var result = JsonEvolutionMutator.ExtractJson("");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ExtractJson_Handles_Invalid_Json()
    {
        var result = JsonEvolutionMutator.ExtractJson("not valid json");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ExtractJson_Handles_Plain_Json()
    {
        var result = JsonEvolutionMutator.ExtractJson("{'test': 1}");
        Assert.Equal("{'test': 1}", result);
    }

    [Fact]
    public async Task MutateAsync_Preserves_ParentId_And_Id()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var responseContent = "{'mutated': 2}";
        var messageContent = new ChatMessageContent(ChatRole.Assistant, responseContent);
        mockChatService.Setup(x => x.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), It.IsAny<PromptExecutionSettings>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { messageContent });

        var mutator = new JsonEvolutionMutator(mockChatService.Object);
        var candidate = CandidateProgram.FromJson("{'test': 1}", "parent-123", "id-456");

        var result = await mutator.MutateAsync(candidate, Array.Empty<string>());

        Assert.Equal("parent-123", result.ParentId);
        Assert.Equal("id-456", result.Id);
    }
}
