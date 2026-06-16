namespace SelfEvolvingFramework.LlmRouting;

public interface IEndpointHealthMonitor
{
    bool IsHealthy(string endpointId, DateTimeOffset now);

    void RecordSuccess(string endpointId, TimeSpan latency, DateTimeOffset now);

    void RecordFailure(string endpointId, DateTimeOffset now);
}
