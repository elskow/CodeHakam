using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace AccountService.Infrastructure;

public class RedisHealthCheck(IConnectionMultiplexer? connectionMultiplexer) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (connectionMultiplexer == null)
            {
                return Task.FromResult(
                    HealthCheckResult.Degraded("Redis connection multiplexer is not configured"));
            }

            if (!connectionMultiplexer.IsConnected)
            {
                return Task.FromResult(
                    HealthCheckResult.Degraded("Redis is not connected"));
            }

            // Try to ping Redis
            var database = connectionMultiplexer.GetDatabase();
            var pingTime = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints().First())
                .Ping();

            return Task.FromResult(
                HealthCheckResult.Healthy($"Redis is connected. Ping: {pingTime.TotalMilliseconds}ms"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Redis health check failed", ex));
        }
    }
}
