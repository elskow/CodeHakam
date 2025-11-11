using Casbin;

namespace AccountService.Services;

public interface ICasbinPolicyService
{
    Task LoadPoliciesIntoEnforcerAsync(IEnforcer enforcer);
    Task ClearAllPoliciesAsync(IEnforcer enforcer);
}
