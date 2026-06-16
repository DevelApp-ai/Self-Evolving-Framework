using System.Diagnostics;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SelfEvolvingFramework.LlmRouting;

public sealed class RoutedChatCompletionService(
    IReadOnlyList<IModelEndpoint> endpoints,
    IModelRouter modelRouter,
    IFallbackPolicy fallbackPolicy,
    IEndpointHealthMonitor healthMonitor,
    CloudEndpointOptions? cloudEndpointOptions = null,
    RoutingPolicyOptions? routingPolicyOptions = null) : IChatCompletionService
{
    private readonly IReadOnlyList<IModelEndpoint> _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
    private readonly IModelRouter _modelRouter = modelRouter ?? throw new ArgumentNullException(nameof(modelRouter));
    private readonly IFallbackPolicy _fallbackPolicy = fallbackPolicy ?? throw new ArgumentNullException(nameof(fallbackPolicy));
    private readonly IEndpointHealthMonitor _healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
    private readonly CloudEndpointOptions? _cloudEndpointOptions = cloudEndpointOptions;
    private readonly RoutingPolicyOptions _routingPolicyOptions = routingPolicyOptions ?? new();

    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public ModelRoutingTelemetry? LastRoutingTelemetry { get; private set; }

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
        => ExecuteWithRoutingAsync(chatHistory, executionSettings, kernel, cancellationToken);

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var results = await ExecuteWithRoutingAsync(chatHistory, executionSettings, kernel, cancellationToken);
        foreach (var result in results)
        {
            yield return new StreamingChatMessageContent(result.Role, result.Content);
        }
    }

    private async Task<IReadOnlyList<ChatMessageContent>> ExecuteWithRoutingAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings,
        Kernel? kernel,
        CancellationToken cancellationToken)
    {
        var invocationContext = BuildInvocationContext(chatHistory, executionSettings);
        var route = _modelRouter.BuildRoute(invocationContext, _endpoints);
        if (route.Count == 0)
        {
            throw new InvalidOperationException("No model endpoints are available for invocation.");
        }

        var attempts = new List<ModelEndpointAttemptTelemetry>(route.Count);
        var timeoutCount = 0;
        var errorCount = 0;
        var promptCacheApplied = false;
        var bypassReason = _fallbackPolicy.EvaluateLocalBypass(invocationContext);
        var invocationStopwatch = Stopwatch.StartNew();

        foreach (var endpoint in route)
        {
            var started = Stopwatch.StartNew();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(GetEffectiveTimeoutMilliseconds(endpoint.TimeoutMilliseconds, executionSettings, invocationStopwatch.Elapsed)));

            try
            {
                var endpointSettings = ApplyEndpointSettings(executionSettings, endpoint);
                if (endpointSettings is not null &&
                    endpoint.ProviderKind is not ModelProviderKind.LocalPrimary and not ModelProviderKind.LocalDiagnostic &&
                    endpointSettings.ExtensionData?.ContainsKey("prompt_cache_key") == true)
                {
                    promptCacheApplied = true;
                }

                var response = await endpoint.GetChatMessageContentsAsync(chatHistory, endpointSettings, kernel, timeoutCts.Token);
                started.Stop();
                _healthMonitor.RecordSuccess(endpoint.EndpointId, started.Elapsed, DateTimeOffset.UtcNow);
                var successAttempt = new ModelEndpointAttemptTelemetry(
                    endpoint.EndpointId,
                    endpoint.ProviderKind,
                    started.Elapsed,
                    true,
                    false,
                    null,
                    attempts.Count == 0 ? bypassReason : ModelFallbackReason.None);
                attempts.Add(successAttempt);
                LastRoutingTelemetry = new ModelRoutingTelemetry(
                    endpoint.EndpointId,
                    endpoint.ProviderKind,
                    successAttempt.FailureReason,
                    invocationContext.PromptCharacterCount,
                    EstimateTokens(invocationContext.PromptCharacterCount),
                    promptCacheApplied,
                    timeoutCount,
                    errorCount,
                    attempts.ToArray());
                return response;
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                started.Stop();
                timeoutCount++;
                _healthMonitor.RecordFailure(endpoint.EndpointId, DateTimeOffset.UtcNow);
                var failedAttempt = new ModelEndpointAttemptTelemetry(
                    endpoint.EndpointId,
                    endpoint.ProviderKind,
                    started.Elapsed,
                    false,
                    true,
                    ex.Message,
                    ModelFallbackReason.EndpointTimedOut);
                attempts.Add(failedAttempt);
                if (!_fallbackPolicy.ShouldFallback(invocationContext, failedAttempt))
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                started.Stop();
                errorCount++;
                _healthMonitor.RecordFailure(endpoint.EndpointId, DateTimeOffset.UtcNow);
                var failedAttempt = new ModelEndpointAttemptTelemetry(
                    endpoint.EndpointId,
                    endpoint.ProviderKind,
                    started.Elapsed,
                    false,
                    false,
                    ex.Message,
                    ModelFallbackReason.EndpointFailure);
                attempts.Add(failedAttempt);
                if (!_fallbackPolicy.ShouldFallback(invocationContext, failedAttempt))
                {
                    break;
                }
            }
        }

        LastRoutingTelemetry = new ModelRoutingTelemetry(
            route[0].EndpointId,
            route[0].ProviderKind,
            ModelFallbackReason.ExhaustedAllEndpoints,
            invocationContext.PromptCharacterCount,
            EstimateTokens(invocationContext.PromptCharacterCount),
            promptCacheApplied,
            timeoutCount,
            errorCount,
            attempts.ToArray());
        throw new InvalidOperationException("All model endpoints failed. See LastRoutingTelemetry for routing details.");
    }

    private ModelInvocationContext BuildInvocationContext(ChatHistory chatHistory, PromptExecutionSettings? executionSettings)
    {
        var promptCharacterCount = chatHistory.Sum(message => message.Content?.Length ?? 0);
        var extensionData = executionSettings?.ExtensionData;
        return new ModelInvocationContext(
            promptCharacterCount,
            GetBooleanFlag(extensionData, "routing.requires_high_complexity"),
            GetBooleanFlag(extensionData, "routing.requires_architect_reasoning"),
            GetBooleanFlag(extensionData, "routing.is_diagnostic_task"));
    }

    private static bool GetBooleanFlag(IDictionary<string, object>? data, string key)
    {
        if (data is null || !data.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var parsed) => parsed,
            _ => false
        };
    }

    private int GetEffectiveTimeoutMilliseconds(
        int endpointTimeoutMilliseconds,
        PromptExecutionSettings? executionSettings,
        TimeSpan elapsed)
    {
        var executionBudgetMilliseconds = GetIntegerValue(executionSettings?.ExtensionData, RoutingExecutionSettingsKeys.ExecutionBudgetMilliseconds);
        if (executionBudgetMilliseconds is null or <= 0)
        {
            return endpointTimeoutMilliseconds;
        }

        var remainingBudget = executionBudgetMilliseconds.Value - (int)elapsed.TotalMilliseconds - _routingPolicyOptions.TimeoutBufferMilliseconds;
        if (remainingBudget <= 0)
        {
            return 1;
        }

        return Math.Min(endpointTimeoutMilliseconds, remainingBudget);
    }

    private static int? GetIntegerValue(IDictionary<string, object>? data, string key)
    {
        if (data is null || !data.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            int i => i,
            long l when l <= int.MaxValue && l >= int.MinValue => (int)l,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }

    private PromptExecutionSettings? ApplyEndpointSettings(PromptExecutionSettings? sourceSettings, IModelEndpoint endpoint)
    {
        if (sourceSettings is null && _cloudEndpointOptions is null)
        {
            return null;
        }

        var clone = sourceSettings?.Clone() ?? new PromptExecutionSettings();
        clone.ServiceId = endpoint.EndpointId;
        if (clone.ExtensionData is null)
        {
            clone.ExtensionData = new Dictionary<string, object>(StringComparer.Ordinal);
        }

        if (_cloudEndpointOptions is not null &&
            endpoint.ProviderKind is not ModelProviderKind.LocalPrimary and not ModelProviderKind.LocalDiagnostic &&
            !string.IsNullOrWhiteSpace(_cloudEndpointOptions.PromptCacheKey))
        {
            clone.ExtensionData["prompt_cache_key"] = _cloudEndpointOptions.PromptCacheKey;
        }

        return clone;
    }

    private static int EstimateTokens(int promptCharacterCount) => Math.Max(1, promptCharacterCount / 4);
}
