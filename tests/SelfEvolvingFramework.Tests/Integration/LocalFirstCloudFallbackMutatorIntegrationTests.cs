using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SelfEvolvingFramework.Core;
using SelfEvolvingFramework.LlmRouting;
using SelfEvolvingFramework.Orchestration;

namespace SelfEvolvingFramework.Tests.Integration;

public sealed class LocalFirstCloudFallbackMutatorIntegrationTests
{
    [Fact]
    public async Task MutateAsync_Uses_Cloud_When_Local_Endpoint_Fails()
    {
        var local = new DelegatingModelEndpoint(
            "local-primary",
            ModelProviderKind.LocalPrimary,
            1000,
            static (_, _, _, _) => throw new InvalidOperationException("local endpoint down"));
        var cloud = new DelegatingModelEndpoint(
            "cloud-small",
            ModelProviderKind.CloudSmall,
            1000,
            static (_, _, _, _) => Task.FromResult<IReadOnlyList<ChatMessageContent>>(
            [
                new ChatMessageContent(AuthorRole.Assistant, "public static class Runner { public static int Execute() => 42; }")
            ]));

        var policy = new DefaultFallbackPolicy();
        var healthMonitor = new CircuitBreakerEndpointHealthMonitor();
        var routingService = new RoutedChatCompletionService(
            [local, cloud],
            new LocalFirstModelRouter(policy, healthMonitor),
            policy,
            healthMonitor,
            new CloudEndpointOptions(
                "MISTRAL_API_KEY",
                "https://api.mistral.ai/v1",
                "routing-cache-v1",
                new ModelEndpointOptions("cloud-small", "https://api.mistral.ai/v1", "mistral-small-latest")));

        var mutator = new SemanticKernelEvolutionMutator(routingService, "Improve correctness.");
        var seed = new CandidateProgram("public static class Runner { public static int Execute() => 1; }");

        var mutated = await mutator.MutateAsync(seed, []);

        Assert.Equal("public static class Runner { public static int Execute() => 42; }", mutated.SourceCode);
        Assert.NotNull(routingService.LastRoutingTelemetry);
        Assert.Equal("cloud-small", routingService.LastRoutingTelemetry!.SelectedEndpointId);
    }
}
