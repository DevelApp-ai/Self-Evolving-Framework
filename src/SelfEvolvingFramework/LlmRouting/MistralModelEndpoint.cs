using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SelfEvolvingFramework.LlmRouting;

public sealed class MistralModelEndpoint(
    ModelEndpointOptions options,
    string apiKeyEnvironmentVariable,
    ModelProviderKind providerKind,
    HttpClient? httpClient = null) : IModelEndpoint
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient = httpClient ?? new HttpClient();
    private readonly ModelEndpointOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly string _apiKeyEnvironmentVariable = !string.IsNullOrWhiteSpace(apiKeyEnvironmentVariable)
        ? apiKeyEnvironmentVariable
        : throw new ArgumentException("API key environment variable cannot be null or whitespace.", nameof(apiKeyEnvironmentVariable));

    public string EndpointId { get; } = !string.IsNullOrWhiteSpace(options.EndpointId)
        ? options.EndpointId
        : throw new ArgumentException("Endpoint id cannot be null or whitespace.", nameof(options));

    public ModelProviderKind ProviderKind { get; } = providerKind;

    public int TimeoutMilliseconds { get; } = options.TimeoutMilliseconds > 0
        ? options.TimeoutMilliseconds
        : throw new ArgumentOutOfRangeException(nameof(options));

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        _ = kernel;
        ArgumentNullException.ThrowIfNull(chatHistory);

        var apiKey = Environment.GetEnvironmentVariable(_apiKeyEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException($"Mistral API key environment variable '{_apiKeyEnvironmentVariable}' is not set.");
        }

        var requestUri = new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + '/', UriKind.Absolute), "chat/completions");
        var requestPayload = new Dictionary<string, object?>
        {
            ["model"] = _options.ModelId,
            ["messages"] = chatHistory.Select(message => new Dictionary<string, object?>
            {
                ["role"] = ChatRoleMapper.ToApiRole(message.Role),
                ["content"] = message.Content ?? string.Empty
            }).ToArray()
        };

        if (executionSettings?.ExtensionData is not null &&
            executionSettings.ExtensionData.TryGetValue("prompt_cache_key", out var promptCacheKey) &&
            promptCacheKey is string cacheKeyValue &&
            !string.IsNullOrWhiteSpace(cacheKeyValue))
        {
            requestPayload["prompt_cache_key"] = cacheKeyValue;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(requestPayload, options: SerializerOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Mistral request failed with status {(int)response.StatusCode}: {body}");
        }

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Mistral returned no chat completion choices.");
        }

        var message = choices[0].GetProperty("message");
        var content = message.GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Mistral returned an empty assistant response.");
        }

        return [new ChatMessageContent(AuthorRole.Assistant, content)];
    }
}
