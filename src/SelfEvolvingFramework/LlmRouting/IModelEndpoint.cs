using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SelfEvolvingFramework.LlmRouting;

public interface IModelEndpoint
{
    string EndpointId { get; }

    ModelProviderKind ProviderKind { get; }

    int TimeoutMilliseconds { get; }

    Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default);
}
