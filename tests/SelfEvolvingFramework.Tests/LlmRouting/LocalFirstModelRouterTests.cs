using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SelfEvolvingFramework.LlmRouting;

namespace SelfEvolvingFramework.Tests.LlmRouting;

public sealed class LocalFirstModelRouterTests
{
    [Fact]
    public void BuildRoute_Prefers_Diagnostic_Local_Model_For_Diagnostic_Tasks()
    {
        var router = CreateRouter();
        var endpoints = CreateEndpoints();

        var route = router.BuildRoute(new ModelInvocationContext(100, IsDiagnosticTask: true), endpoints);

        Assert.Equal("local-diagnostic", route[0].EndpointId);
        Assert.Equal("local-primary", route[1].EndpointId);
    }

    [Fact]
    public void BuildRoute_Bypasses_Local_For_High_Complexity()
    {
        var router = CreateRouter(new RoutingPolicyOptions(PreferCloudForHighComplexity: true));
        var endpoints = CreateEndpoints();

        var route = router.BuildRoute(new ModelInvocationContext(100, RequiresHighComplexity: true), endpoints);

        Assert.Equal("cloud-small", route[0].EndpointId);
    }

    [Fact]
    public void BuildRoute_Uses_Cloud_First_When_Local_Routing_Is_Disabled()
    {
        var router = CreateRouter(new RoutingPolicyOptions(EnableLocalRouting: false));
        var endpoints = CreateEndpoints();

        var route = router.BuildRoute(new ModelInvocationContext(100), endpoints);

        Assert.Equal("cloud-small", route[0].EndpointId);
    }

    [Fact]
    public void BuildRoute_Does_Not_Include_Cloud_When_Cloud_Fallback_Is_Disabled()
    {
        var router = CreateRouter(new RoutingPolicyOptions(EnableCloudFallback: false));
        var endpoints = CreateEndpoints();

        var route = router.BuildRoute(new ModelInvocationContext(100), endpoints);

        Assert.DoesNotContain(route, endpoint => endpoint.ProviderKind is not ModelProviderKind.LocalPrimary and not ModelProviderKind.LocalDiagnostic);
    }

    private static LocalFirstModelRouter CreateRouter(RoutingPolicyOptions? options = null)
        => new(new DefaultFallbackPolicy(options), new CircuitBreakerEndpointHealthMonitor(), options);

    private static IReadOnlyList<IModelEndpoint> CreateEndpoints()
        => [
            new DelegatingModelEndpoint(
                "local-primary",
                ModelProviderKind.LocalPrimary,
                1000,
                static (_, _, _, _) => Task.FromResult<IReadOnlyList<ChatMessageContent>>([new ChatMessageContent(AuthorRole.Assistant, "local")])),
            new DelegatingModelEndpoint(
                "local-diagnostic",
                ModelProviderKind.LocalDiagnostic,
                1000,
                static (_, _, _, _) => Task.FromResult<IReadOnlyList<ChatMessageContent>>([new ChatMessageContent(AuthorRole.Assistant, "diag")])),
            new DelegatingModelEndpoint(
                "cloud-small",
                ModelProviderKind.CloudSmall,
                1000,
                static (_, _, _, _) => Task.FromResult<IReadOnlyList<ChatMessageContent>>([new ChatMessageContent(AuthorRole.Assistant, "cloud")]))
        ];
}
