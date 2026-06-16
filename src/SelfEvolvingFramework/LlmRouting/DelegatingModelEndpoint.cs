using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SelfEvolvingFramework.LlmRouting;

public sealed class DelegatingModelEndpoint(
    string endpointId,
    ModelProviderKind providerKind,
    int timeoutMilliseconds,
    Func<ChatHistory, PromptExecutionSettings?, Kernel?, CancellationToken, Task<IReadOnlyList<ChatMessageContent>>> handler) : IModelEndpoint
{
    private readonly Func<ChatHistory, PromptExecutionSettings?, Kernel?, CancellationToken, Task<IReadOnlyList<ChatMessageContent>>> _handler =
        handler ?? throw new ArgumentNullException(nameof(handler));

    public string EndpointId { get; } = !string.IsNullOrWhiteSpace(endpointId)
        ? endpointId
        : throw new ArgumentException("Endpoint id cannot be null or whitespace.", nameof(endpointId));

    public ModelProviderKind ProviderKind { get; } = providerKind;

    public int TimeoutMilliseconds { get; } = timeoutMilliseconds > 0
        ? timeoutMilliseconds
        : throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
        => _handler(chatHistory, executionSettings, kernel, cancellationToken);
}
