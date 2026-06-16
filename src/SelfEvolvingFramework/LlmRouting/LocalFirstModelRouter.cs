namespace SelfEvolvingFramework.LlmRouting;

public sealed class LocalFirstModelRouter(
    IFallbackPolicy fallbackPolicy,
    IEndpointHealthMonitor healthMonitor,
    RoutingPolicyOptions? options = null) : IModelRouter
{
    private readonly IFallbackPolicy _fallbackPolicy = fallbackPolicy ?? throw new ArgumentNullException(nameof(fallbackPolicy));
    private readonly IEndpointHealthMonitor _healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
    private readonly RoutingPolicyOptions _options = options ?? new();

    public IReadOnlyList<IModelEndpoint> BuildRoute(ModelInvocationContext invocationContext, IReadOnlyList<IModelEndpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var now = DateTimeOffset.UtcNow;
        var locals = endpoints
            .Where(e => e.ProviderKind is ModelProviderKind.LocalPrimary or ModelProviderKind.LocalDiagnostic)
            .Where(e => _healthMonitor.IsHealthy(e.EndpointId, now))
            .ToArray();
        var clouds = endpoints
            .Where(e => e.ProviderKind is not ModelProviderKind.LocalPrimary and not ModelProviderKind.LocalDiagnostic)
            .Where(e => _healthMonitor.IsHealthy(e.EndpointId, now))
            .ToArray();

        var route = new List<IModelEndpoint>(endpoints.Count);
        var bypassReason = _fallbackPolicy.EvaluateLocalBypass(invocationContext);

        if (bypassReason is not ModelFallbackReason.None)
        {
            route.AddRange(clouds);
            route.AddRange(locals);
            return route;
        }

        if (invocationContext.IsDiagnosticTask && _options.PreferDiagnosticModelForDiagnosticTasks)
        {
            route.AddRange(locals.Where(e => e.ProviderKind == ModelProviderKind.LocalDiagnostic));
            route.AddRange(locals.Where(e => e.ProviderKind == ModelProviderKind.LocalPrimary));
            route.AddRange(clouds);
            return route;
        }

        route.AddRange(locals.Where(e => e.ProviderKind == ModelProviderKind.LocalPrimary));
        route.AddRange(locals.Where(e => e.ProviderKind == ModelProviderKind.LocalDiagnostic));
        route.AddRange(clouds);
        return route;
    }
}
