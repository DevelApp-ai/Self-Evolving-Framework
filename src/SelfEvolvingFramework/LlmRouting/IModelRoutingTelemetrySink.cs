namespace SelfEvolvingFramework.LlmRouting;

public interface IModelRoutingTelemetrySink
{
    ValueTask PublishAsync(ModelRoutingTelemetry telemetry, CancellationToken cancellationToken = default);
}
