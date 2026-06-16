using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SelfEvolvingFramework.LlmRouting;

public sealed class OllamaModelEndpoint(
    ModelEndpointOptions options,
    ModelProviderKind providerKind,
    HttpClient? httpClient = null) : IModelEndpoint
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient = httpClient ?? new HttpClient();
    private readonly ModelEndpointOptions _options = options ?? throw new ArgumentNullException(nameof(options));

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
        _ = executionSettings;
        _ = kernel;
        ArgumentNullException.ThrowIfNull(chatHistory);

        var requestUri = new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + '/', UriKind.Absolute), "api/chat");
        var payload = new
        {
            model = _options.ModelId,
            stream = false,
            messages = chatHistory.Select(message => new
            {
                role = ChatRoleMapper.ToApiRole(message.Role),
                content = message.Content ?? string.Empty
            })
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Ollama request failed with status {(int)response.StatusCode}: {body}");
        }

        using var document = JsonDocument.Parse(body);
        var content = document.RootElement.GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Ollama returned an empty assistant response.");
        }

        return [new ChatMessageContent(AuthorRole.Assistant, content)];
    }
}
