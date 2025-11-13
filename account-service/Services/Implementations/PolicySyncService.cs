using AccountService.Constants;
using Casbin;

using AccountService.Services.Interfaces;
namespace AccountService.Services.Implementations;

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
            state: null,
            TimeSpan.FromMinutes(ApplicationConstants.Intervals.PolicySyncMinutes),
            TimeSpan.FromMinutes(ApplicationConstants.Intervals.PolicySyncMinutes));

        logger.LogInformation("Policy sync service started");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Policy sync service stopping");

        _timer?.Change(Timeout.Infinite, period: 0);
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
