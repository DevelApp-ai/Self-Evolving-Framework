using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Orchestration;

public sealed class JsonEvolutionCrossoverTests
{
    [Fact]
    public async Task CrossoverAsync_Returns_ParentA_When_No_Response()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        mockChatService.Setup(x => x.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent>());

        var crossover = new JsonEvolutionCrossover(mockChatService.Object);
        var parentA = CandidateProgram.FromJson("{'a': 1}");
        var parentB = CandidateProgram.FromJson("{'b': 2}");

        var result = await crossover.CrossoverAsync(parentA, parentB);

        Assert.Same(parentA, result);
    }

    [Fact]
    public async Task CrossoverAsync_Returns_Offspring_From_Response()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var responseContent = "{'combined': 3}";
        var messageContent = new ChatMessageContent(AuthorRole.Assistant, responseContent);
        mockChatService.Setup(x => x.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { messageContent });

        var crossover = new JsonEvolutionCrossover(mockChatService.Object);
        var parentA = CandidateProgram.FromJson("{'a': 1}");
        var parentB = CandidateProgram.FromJson("{'b': 2}");

        var result = await crossover.CrossoverAsync(parentA, parentB);

        Assert.NotSame(parentA, result);
        Assert.Equal(CandidateFormat.Json, result.Format);
        Assert.Contains("combined", result.SourceMaterial);
    }

    [Fact]
    public async Task CrossoverAsync_Extracts_Json_From_Markdown()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var responseContent = "Here is the combined JSON:\n```json\n{'combined': 3}\n```";
        var messageContent = new ChatMessageContent(AuthorRole.Assistant, responseContent);
        mockChatService.Setup(x => x.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { messageContent });

        var crossover = new JsonEvolutionCrossover(mockChatService.Object);
        var parentA = CandidateProgram.FromJson("{'a': 1}");
        var parentB = CandidateProgram.FromJson("{'b': 2}");

        var result = await crossover.CrossoverAsync(parentA, parentB);

        Assert.NotSame(parentA, result);
        Assert.Equal("{'combined': 3}", result.SourceMaterial);
    }

    [Fact]
    public async Task CrossoverAsync_Extracts_Json_Array()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var responseContent = "```json\n[1, 2, 3]\n```";
        var messageContent = new ChatMessageContent(AuthorRole.Assistant, responseContent);
        mockChatService.Setup(x => x.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { messageContent });

        var crossover = new JsonEvolutionCrossover(mockChatService.Object);
        var parentA = CandidateProgram.FromJson("[1, 2]");
        var parentB = CandidateProgram.FromJson("[3, 4]");

        var result = await crossover.CrossoverAsync(parentA, parentB);

        Assert.Equal("[1, 2, 3]", result.SourceMaterial);
    }

    [Fact]
    public async Task CrossoverAsync_Throws_For_Non_Json_Parents()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var crossover = new JsonEvolutionCrossover(mockChatService.Object);
        var parentA = CandidateProgram.FromCSharp("public class Test { }");
        var parentB = CandidateProgram.FromJson("{'b': 2}");

        await Assert.ThrowsAsync<InvalidOperationException>(() => crossover.CrossoverAsync(parentA, parentB));
    }

    [Fact]
    public void Format_Returns_Json()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var crossover = new JsonEvolutionCrossover(mockChatService.Object);
        Assert.Equal(CandidateFormat.Json, crossover.Format);
    }

    [Fact]
    public async Task BuildCrossoverPrompt_Includes_Both_Parents()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var crossover = new JsonEvolutionCrossover(mockChatService.Object);
        var parentA = CandidateProgram.FromJson("{'a': 1}");
        var parentB = CandidateProgram.FromJson("{'b': 2}");

        var prompt = crossover.BuildCrossoverPrompt(parentA.SourceMaterial, parentB.SourceMaterial);

        Assert.Contains("{'a': 1}", prompt);
        Assert.Contains("{'b': 2}", prompt);
        Assert.Contains("Parent A JSON:", prompt);
        Assert.Contains("Parent B JSON:", prompt);
    }

    [Fact]
    public async Task CreateChatHistory_Uses_Correct_System_Prompt()
    {
        var mockChatService = new Mock<IChatCompletionService>();
        var options = new JsonCrossoverOptions
        {
            SystemPrompt = "Custom system prompt"
        };
        var crossover = new JsonEvolutionCrossover(mockChatService.Object, options);

        var history = crossover.CreateChatHistory("{'a': 1}", "{'b': 2}");

        Assert.Equal("Custom system prompt", history[0].Content);
    }

    [Fact]
    public void ExtractJson_Handles_Null_Content()
    {
        var result = JsonEvolutionCrossover.ExtractJson(null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ExtractJson_Handles_Empty_Content()
    {
        var result = JsonEvolutionCrossover.ExtractJson("");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ExtractJson_Handles_Invalid_Json()
    {
        var result = JsonEvolutionCrossover.ExtractJson("not valid json");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ExtractJson_Handles_Plain_Json()
    {
        var result = JsonEvolutionCrossover.ExtractJson("{'test': 1}");
        Assert.Equal("{'test': 1}", result);
    }
}
