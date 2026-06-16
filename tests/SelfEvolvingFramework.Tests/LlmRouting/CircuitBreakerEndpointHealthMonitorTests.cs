using SelfEvolvingFramework.LlmRouting;

namespace SelfEvolvingFramework.Tests.LlmRouting;

public sealed class CircuitBreakerEndpointHealthMonitorTests
{
    [Fact]
    public void IsHealthy_OpensCircuit_After_Failure_Threshold_And_Recovers_After_Window()
    {
        var monitor = new CircuitBreakerEndpointHealthMonitor(2, TimeSpan.FromSeconds(10));
        var now = DateTimeOffset.UtcNow;

        Assert.True(monitor.IsHealthy("local", now));
        monitor.RecordFailure("local", now);
        Assert.True(monitor.IsHealthy("local", now));

        monitor.RecordFailure("local", now);
        Assert.False(monitor.IsHealthy("local", now.AddSeconds(1)));
        Assert.True(monitor.IsHealthy("local", now.AddSeconds(11)));
    }

    [Fact]
    public void RecordSuccess_Resets_Circuit_State()
    {
        var monitor = new CircuitBreakerEndpointHealthMonitor(1, TimeSpan.FromSeconds(10));
        var now = DateTimeOffset.UtcNow;

        monitor.RecordFailure("local", now);
        Assert.False(monitor.IsHealthy("local", now.AddSeconds(1)));

        monitor.RecordSuccess("local", TimeSpan.FromMilliseconds(50), now.AddSeconds(2));
        Assert.True(monitor.IsHealthy("local", now.AddSeconds(3)));
    }
}
