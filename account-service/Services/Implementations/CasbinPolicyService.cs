using AccountService.Data;
using Casbin;
using Microsoft.EntityFrameworkCore;

using AccountService.Services.Interfaces;
namespace AccountService.Services.Implementations;

public sealed class CasbinPolicyService(
    ApplicationDbContext context,
    ILogger<CasbinPolicyService> logger) : ICasbinPolicyService
{
    public async Task LoadPoliciesIntoEnforcerAsync(IEnforcer enforcer)
    {
        try
        {
            await LoadRolePermissionsAsync(enforcer);
            await LoadUserRolesAsync(enforcer);

            logger.LogInformation("Successfully loaded policies into Casbin enforcer");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load policies into enforcer");
            throw;
        }
    }

    public async Task ClearAllPoliciesAsync(IEnforcer enforcer)
    {
        await enforcer.RemoveFilteredPolicyAsync(fieldIndex: 0, "p");
        await enforcer.RemoveFilteredGroupingPolicyAsync(fieldIndex: 0, "g");
        logger.LogDebug("Cleared all policies from enforcer");
    }

    private async Task LoadRolePermissionsAsync(IEnforcer enforcer)
    {
        var rolePermissions = await context.RolePermissions
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission)
            .AsNoTracking()
            .ToListAsync();

        foreach (var rolePermission in rolePermissions)
        {
            await enforcer.AddPolicyAsync(
                rolePermission.Role.Name,
                rolePermission.Permission.Resource,
                rolePermission.Permission.Action);
        }

        logger.LogDebug("Loaded {Count} role-permission policies", rolePermissions.Count);
    }

    private async Task LoadUserRolesAsync(IEnforcer enforcer)
    {
        var userRoles = await context.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .AsNoTracking()
            .ToListAsync();

        foreach (var userRole in userRoles)
        {
            await enforcer.AddGroupingPolicyAsync(
                userRole.User.UserName,
                userRole.Role.Name);
        }

        logger.LogDebug("Loaded {Count} user-role groupings", userRoles.Count);
    }
}
