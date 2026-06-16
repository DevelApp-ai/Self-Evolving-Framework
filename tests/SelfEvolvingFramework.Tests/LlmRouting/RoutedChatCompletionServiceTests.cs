using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SelfEvolvingFramework.LlmRouting;

namespace SelfEvolvingFramework.Tests.LlmRouting;

public sealed class RoutedChatCompletionServiceTests
{
    [Fact]
    public async Task GetChatMessageContentsAsync_Falls_Back_To_Cloud_When_Local_Fails_And_Applies_Prompt_Cache_Key()
    {
        PromptExecutionSettings? cloudSettings = null;

        var local = new DelegatingModelEndpoint(
            "local-primary",
            ModelProviderKind.LocalPrimary,
            1000,
            static (_, _, _, _) => throw new InvalidOperationException("local unavailable"));
        var cloud = new DelegatingModelEndpoint(
            "cloud-small",
            ModelProviderKind.CloudSmall,
            1000,
            (_, settings, _, _) =>
            {
                cloudSettings = settings;
                return Task.FromResult<IReadOnlyList<ChatMessageContent>>(
                [
                    new ChatMessageContent(AuthorRole.Assistant, "public static class Runner { public static int Execute() => 7; }")
                ]);
            });

        var policyOptions = new RoutingPolicyOptions();
        var service = new RoutedChatCompletionService(
            [local, cloud],
            new LocalFirstModelRouter(new DefaultFallbackPolicy(policyOptions), new CircuitBreakerEndpointHealthMonitor(), policyOptions),
            new DefaultFallbackPolicy(policyOptions),
            new CircuitBreakerEndpointHealthMonitor(),
            new CloudEndpointOptions(
                "MISTRAL_API_KEY",
                "https://api.mistral.ai/v1",
                "cache-key-v1",
                new ModelEndpointOptions("cloud-small", "https://api.mistral.ai/v1", "mistral-small")));

        var history = new ChatHistory("system");
        history.AddUserMessage("hello");
        var result = await service.GetChatMessageContentsAsync(history);

        Assert.Single(result);
        Assert.NotNull(cloudSettings);
        Assert.NotNull(cloudSettings!.ExtensionData);
        Assert.Equal("cache-key-v1", cloudSettings.ExtensionData["prompt_cache_key"]);
        Assert.NotNull(service.LastRoutingTelemetry);
        Assert.Equal("cloud-small", service.LastRoutingTelemetry!.SelectedEndpointId);
        Assert.Equal(1, service.LastRoutingTelemetry.ErrorCount);
    }

    [Fact]
    public async Task GetChatMessageContentsAsync_Does_Not_Fall_Back_When_Cloud_Fallback_Is_Disabled()
    {
        var cloudInvoked = false;

        var local = new DelegatingModelEndpoint(
            "local-primary",
            ModelProviderKind.LocalPrimary,
            1000,
            static (_, _, _, _) => throw new InvalidOperationException("local unavailable"));
        var cloud = new DelegatingModelEndpoint(
            "cloud-small",
            ModelProviderKind.CloudSmall,
            1000,
            (_, _, _, _) =>
            {
                cloudInvoked = true;
                return Task.FromResult<IReadOnlyList<ChatMessageContent>>([new ChatMessageContent(AuthorRole.Assistant, "cloud")]);
            });

        var policyOptions = new RoutingPolicyOptions(EnableCloudFallback: false);
        var service = new RoutedChatCompletionService(
            [local, cloud],
            new LocalFirstModelRouter(new DefaultFallbackPolicy(policyOptions), new CircuitBreakerEndpointHealthMonitor(), policyOptions),
            new DefaultFallbackPolicy(policyOptions),
            new CircuitBreakerEndpointHealthMonitor());

        var history = new ChatHistory("system");
        history.AddUserMessage("hello");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetChatMessageContentsAsync(history));

        Assert.Equal("All model endpoints failed. See LastRoutingTelemetry for routing details.", exception.Message);
        Assert.False(cloudInvoked);
    }
}
