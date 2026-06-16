using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SelfEvolvingFramework.LlmRouting;

namespace SelfEvolvingFramework.Tests.LlmRouting;

public sealed class ConcreteModelEndpointTests
{
    [Fact]
    public async Task OllamaModelEndpoint_Maps_Chat_Request_And_Parses_Response()
    {
        string? capturedJson = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            capturedJson = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"message":{"content":"public static class Runner { public static int Execute() => 9; }"}}""", Encoding.UTF8, "application/json")
            };
        });
        var endpoint = new OllamaModelEndpoint(
            new ModelEndpointOptions("ollama-primary", "http://localhost:11434", "qwen2.5-coder:14b"),
            ModelProviderKind.LocalPrimary,
            new HttpClient(handler));

        var history = new ChatHistory("system");
        history.AddUserMessage("Generate code.");

        var result = await endpoint.GetChatMessageContentsAsync(history);

        Assert.Single(result);
        Assert.Equal("public static class Runner { public static int Execute() => 9; }", result[0].Content);
        Assert.Contains("\"model\":\"qwen2.5-coder:14b\"", capturedJson, StringComparison.Ordinal);
        Assert.Contains("\"stream\":false", capturedJson, StringComparison.Ordinal);
        Assert.Contains("\"messages\":[", capturedJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MistralModelEndpoint_Sets_Authorization_And_Prompt_Cache_Key()
    {
        var envVar = "TEST_MISTRAL_API_KEY";
        Environment.SetEnvironmentVariable(envVar, "test-key");
        AuthenticationHeaderValue? capturedAuthorization = null;
        string? capturedJson = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            capturedAuthorization = request.Headers.Authorization;
            capturedJson = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"choices":[{"message":{"content":"ok"}}]}""", Encoding.UTF8, "application/json")
            };
        });

        try
        {
            var endpoint = new MistralModelEndpoint(
                new ModelEndpointOptions("cloud-small", "https://api.mistral.ai/v1", "mistral-small-latest"),
                envVar,
                ModelProviderKind.CloudSmall,
                new HttpClient(handler));
            var settings = new PromptExecutionSettings
            {
                ExtensionData = new Dictionary<string, object>
                {
                    ["prompt_cache_key"] = "routing-cache-v1"
                }
            };

            var history = new ChatHistory("system");
            history.AddUserMessage("hello");
            var result = await endpoint.GetChatMessageContentsAsync(history, settings);

            Assert.Single(result);
            Assert.Equal("ok", result[0].Content);
            Assert.NotNull(capturedAuthorization);
            Assert.Equal("Bearer", capturedAuthorization!.Scheme);
            Assert.Equal("test-key", capturedAuthorization.Parameter);
            Assert.Contains("\"prompt_cache_key\":\"routing-cache-v1\"", capturedJson, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVar, null);
        }
    }

    [Fact]
    public async Task MistralModelEndpoint_Throws_When_Api_Key_Is_Missing()
    {
        var envVar = "TEST_MISTRAL_MISSING_KEY";
        Environment.SetEnvironmentVariable(envVar, null);
        var endpoint = new MistralModelEndpoint(
            new ModelEndpointOptions("cloud-small", "https://api.mistral.ai/v1", "mistral-small-latest"),
            envVar,
            ModelProviderKind.CloudSmall,
            new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("should not call http"))));
        var history = new ChatHistory("system");
        history.AddUserMessage("hello");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => endpoint.GetChatMessageContentsAsync(history));

        Assert.Contains(envVar, ex.Message, StringComparison.Ordinal);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request);
    }
}
