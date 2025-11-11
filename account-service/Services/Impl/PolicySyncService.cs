using Casbin;

namespace AccountService.Services.Impl;

public sealed class PolicySyncService(
    IServiceProvider serviceProvider,
    ILogger<PolicySyncService> logger)
    : IHostedService
{
    private Timer? _timer;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Policy sync service starting");

        await InitializePoliciesAsync();

        _timer = new Timer(
            SyncPolicies,
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));

        logger.LogInformation("Policy sync service started");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Policy sync service stopping");

        _timer?.Change(Timeout.Infinite, 0);
        _timer?.Dispose();

        logger.LogInformation("Policy sync service stopped");

        return Task.CompletedTask;
    }

    private async Task InitializePoliciesAsync()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var policyService = scope.ServiceProvider.GetRequiredService<ICasbinPolicyService>();
            var enforcer = serviceProvider.GetRequiredService<IEnforcer>();

            await policyService.ClearAllPoliciesAsync(enforcer);
            await policyService.LoadPoliciesIntoEnforcerAsync(enforcer);

            logger.LogInformation("Policies initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize policies");
            throw;
        }
    }

    private async void SyncPolicies(object? state)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var policyService = scope.ServiceProvider.GetRequiredService<ICasbinPolicyService>();
            var enforcer = serviceProvider.GetRequiredService<IEnforcer>();

            await policyService.ClearAllPoliciesAsync(enforcer);
            await policyService.LoadPoliciesIntoEnforcerAsync(enforcer);

            logger.LogDebug("Policies synchronized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync policies");
        }
    }
}
