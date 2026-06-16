using System.Collections.Concurrent;

namespace SelfEvolvingFramework.LlmRouting;

public sealed class CircuitBreakerEndpointHealthMonitor(
    int failureThreshold = 3,
    TimeSpan? openDuration = null) : IEndpointHealthMonitor
{
    private readonly int _failureThreshold = failureThreshold > 0 ? failureThreshold : throw new ArgumentOutOfRangeException(nameof(failureThreshold));
    private readonly TimeSpan _openDuration = openDuration ?? TimeSpan.FromSeconds(60);
    private readonly ConcurrentDictionary<string, EndpointHealthState> _states = new(StringComparer.Ordinal);

    public bool IsHealthy(string endpointId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);
        if (!_states.TryGetValue(endpointId, out var state))
        {
            return true;
        }

        return !state.IsOpen(now);
    }

    public void RecordSuccess(string endpointId, TimeSpan latency, DateTimeOffset now)
    {
        _ = latency;
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);
        _states.AddOrUpdate(
            endpointId,
            _ => EndpointHealthState.Healthy(now),
            (_, _) => EndpointHealthState.Healthy(now));
    }

    public void RecordFailure(string endpointId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);
        _states.AddOrUpdate(
            endpointId,
            _ => EndpointHealthState.Failed(now, _failureThreshold, _openDuration),
            (_, existing) => existing.Fail(now, _failureThreshold, _openDuration));
    }

    private sealed record EndpointHealthState(int ConsecutiveFailures, DateTimeOffset? OpenUntil)
    {
        public static EndpointHealthState Healthy(DateTimeOffset now)
            => new(0, null);

        public static EndpointHealthState Failed(DateTimeOffset now, int failureThreshold, TimeSpan openDuration)
            => new(1, failureThreshold <= 1 ? now + openDuration : null);

        public EndpointHealthState Fail(DateTimeOffset now, int failureThreshold, TimeSpan openDuration)
        {
            var failures = ConsecutiveFailures + 1;
            var openUntil = failures >= failureThreshold ? now + openDuration : OpenUntil;
            return this with { ConsecutiveFailures = failures, OpenUntil = openUntil };
        }

        public bool IsOpen(DateTimeOffset now)
        {
            if (OpenUntil is null)
            {
                return false;
            }

            return OpenUntil.Value > now;
        }
    }
}
