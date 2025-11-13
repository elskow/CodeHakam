using Casbin;

namespace AccountService.Services.Interfaces;

public interface ICasbinPolicyService
{
    Task LoadPoliciesIntoEnforcerAsync(IEnforcer enforcer);
    Task ClearAllPoliciesAsync(IEnforcer enforcer);
}
