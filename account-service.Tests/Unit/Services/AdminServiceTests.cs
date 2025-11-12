using AccountService.Data;
using AccountService.DTOs;
using AccountService.Models;
using AccountService.Services;
using AccountService.Services.Impl;
using AccountService.Tests.Helpers;
using Casbin;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountService.Tests.Unit.Services;

public class AdminServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICasbinPolicyService> _policyServiceMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<AdminService>> _loggerMock;
    private readonly Mock<IEnforcer> _enforcerMock;
    private readonly AdminService _adminService;

    public AdminServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"AdminServiceTests_{Guid.NewGuid()}");
        _policyServiceMock = new Mock<ICasbinPolicyService>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<AdminService>>();
        _enforcerMock = new Mock<IEnforcer>();

        _adminService = new AdminService(
            _context,
            _policyServiceMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object,
            _enforcerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetUsersAsync_WithNoFilters_ShouldReturnAllUsers()
    {
        var user1 = TestDataBuilder.CreateTestUser(username: "admin", rating: 2000);
        var user2 = TestDataBuilder.CreateTestUser(username: "user", email: "user@example.com", rating: 1500);

        await _context.Users.AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        var request = new UserSearchRequest { Page = 1, PageSize = 10 };

        var result = await _adminService.GetUsersAsync(request);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items[0].Rating.Should().Be(2000);
    }

    [Fact]
    public async Task GetUsersAsync_WithPagination_ShouldReturnCorrectPage()
    {
        for (int i = 1; i <= 15; i++)
        {
            var user = TestDataBuilder.CreateTestUser(
                username: $"user{i}",
                email: $"user{i}@example.com");
            await _context.Users.AddAsync(user);
        }
        await _context.SaveChangesAsync();

        var request = new UserSearchRequest { Page = 2, PageSize = 10 };

        var result = await _adminService.GetUsersAsync(request);

        result.Items.Should().HaveCount(5);
        result.TotalPages.Should().Be(2);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserByIdAsync_WithExistingUser_ShouldReturnProfile()
    {
        var user = TestDataBuilder.CreateTestUser(username: "testuser");

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _adminService.GetUserByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.Email.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistentUser_ShouldReturnNull()
    {
        var result = await _adminService.GetUserByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task BanUserAsync_WithValidUser_ShouldBanSuccessfully()
    {
        var user = TestDataBuilder.CreateTestUser(isBanned: false);
        var adminId = 100L;
        var reason = "Violation of terms of service";

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _adminService.BanUserAsync(user.Id, reason, adminId);

        result.Should().BeTrue();

        var bannedUser = await _context.Users.FindAsync(user.Id);
        bannedUser!.IsBanned.Should().BeTrue();
        bannedUser.BanReason.Should().Be(reason);
        bannedUser.BannedAt.Should().NotBeNull();
        bannedUser.BannedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _eventPublisherMock.Verify(
            x => x.PublishUserBannedAsync(It.Is<UserBannedEvent>(e =>
                e.UserId == user.Id &&
                e.BannedBy == adminId &&
                e.Reason == reason)),
            Times.Once);
    }

    [Fact]
    public async Task BanUserAsync_WithAlreadyBannedUser_ShouldReturnFalse()
    {
        var user = TestDataBuilder.CreateTestUser(isBanned: true);

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _adminService.BanUserAsync(user.Id, "Reason", 100L);

        result.Should().BeFalse();
        _eventPublisherMock.Verify(x => x.PublishUserBannedAsync(It.IsAny<UserBannedEvent>()), Times.Never);
    }

    [Fact]
    public async Task BanUserAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        var result = await _adminService.BanUserAsync(999, "Reason", 100L);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnbanUserAsync_WithBannedUser_ShouldUnbanSuccessfully()
    {
        var user = TestDataBuilder.CreateTestUser(isBanned: true);
        var adminId = 100L;

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _adminService.UnbanUserAsync(user.Id, adminId);

        result.Should().BeTrue();

        var unbannedUser = await _context.Users.FindAsync(user.Id);
        unbannedUser!.IsBanned.Should().BeFalse();
        unbannedUser.BanReason.Should().BeNull();
        unbannedUser.BannedAt.Should().BeNull();
    }

    [Fact]
    public async Task UnbanUserAsync_WithNotBannedUser_ShouldReturnFalse()
    {
        var user = TestDataBuilder.CreateTestUser(isBanned: false);

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _adminService.UnbanUserAsync(user.Id, 100L);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnbanUserAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        var result = await _adminService.UnbanUserAsync(999, 100L);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AssignRoleAsync_WithValidData_ShouldAssignRoleSuccessfully()
    {
        var user = TestDataBuilder.CreateTestUser();
        var role = TestDataBuilder.CreateRole(name: "Moderator");
        var adminId = 100L;

        await _context.Users.AddAsync(user);
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        var result = await _adminService.AssignRoleAsync(user.Id, role.Id, adminId);

        result.Should().BeTrue();

        var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        userRole.Should().NotBeNull();
        userRole!.AssignedBy.Should().Be(adminId);
        userRole.AssignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _policyServiceMock.Verify(
            x => x.LoadPoliciesIntoEnforcerAsync(_enforcerMock.Object),
            Times.Once);

        _eventPublisherMock.Verify(
            x => x.PublishRoleAssignedAsync(It.Is<RoleAssignedEvent>(e =>
                e.UserId == user.Id &&
                e.RoleName == "Moderator" &&
                e.AssignedBy == adminId)),
            Times.Once);
    }

    [Fact]
    public async Task AssignRoleAsync_WhenUserAlreadyHasRole_ShouldReturnFalse()
    {
        var user = TestDataBuilder.CreateTestUser();

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var existingRole = await _context.Roles.FirstAsync();

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = existingRole.Id,
            AssignedAt = DateTime.UtcNow
        };
        await _context.UserRoles.AddAsync(userRole);
        await _context.SaveChangesAsync();

        var result = await _adminService.AssignRoleAsync(user.Id, existingRole.Id, 100L);

        result.Should().BeFalse();
        _policyServiceMock.Verify(
            x => x.LoadPoliciesIntoEnforcerAsync(It.IsAny<IEnforcer>()),
            Times.Never);
    }

    [Fact]
    public async Task AssignRoleAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        var existingRole = await _context.Roles.FirstAsync();

        var result = await _adminService.AssignRoleAsync(999, existingRole.Id, 100L);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AssignRoleAsync_WithNonExistentRole_ShouldReturnFalse()
    {
        var user = TestDataBuilder.CreateTestUser();

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _adminService.AssignRoleAsync(user.Id, 999, 100L);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveRoleAsync_WithValidData_ShouldRemoveRoleSuccessfully()
    {
        var user = TestDataBuilder.CreateTestUser();

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var existingRoles = await _context.Roles.Take(2).ToListAsync();
        var role1 = existingRoles[0];
        var role2 = existingRoles[1];

        var userRole1 = new UserRole { UserId = user.Id, RoleId = role1.Id, Role = role1, AssignedAt = DateTime.UtcNow };
        var userRole2 = new UserRole { UserId = user.Id, RoleId = role2.Id, Role = role2, AssignedAt = DateTime.UtcNow };
        await _context.UserRoles.AddRangeAsync(userRole1, userRole2);
        await _context.SaveChangesAsync();

        var result = await _adminService.RemoveRoleAsync(user.Id, role2.Id, 100L);

        result.Should().BeTrue();

        var removedRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role2.Id);
        removedRole.Should().BeNull();

        _policyServiceMock.Verify(
            x => x.LoadPoliciesIntoEnforcerAsync(_enforcerMock.Object),
            Times.Once);
    }

    [Fact]
    public async Task RemoveRoleAsync_WhenUserDoesNotHaveRole_ShouldReturnFalse()
    {
        var user = TestDataBuilder.CreateTestUser();

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var existingRole = await _context.Roles.FirstAsync();

        var result = await _adminService.RemoveRoleAsync(user.Id, existingRole.Id, 100L);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveRoleAsync_WhenRemovingLastRole_ShouldReturnFalse()
    {
        var user = TestDataBuilder.CreateTestUser();

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var existingRole = await _context.Roles.FirstAsync();

        var userRole = new UserRole { UserId = user.Id, RoleId = existingRole.Id, Role = existingRole, AssignedAt = DateTime.UtcNow };
        await _context.UserRoles.AddAsync(userRole);
        await _context.SaveChangesAsync();

        var result = await _adminService.RemoveRoleAsync(user.Id, existingRole.Id, 100L);

        result.Should().BeFalse();

        var stillHasRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == existingRole.Id);
        stillHasRole.Should().NotBeNull();

        _policyServiceMock.Verify(
            x => x.LoadPoliciesIntoEnforcerAsync(It.IsAny<IEnforcer>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAllRolesAsync_ShouldReturnAllRolesOrderedByName()
    {
        var result = await _adminService.GetAllRolesAsync();

        result.Should().NotBeEmpty();
        result.Should().BeInAscendingOrder(r => r.Name);
        result.Should().Contain(r => r.Name == "user");
    }

    [Fact]
    public async Task GetAllRolesAsync_ShouldIncludeSystemRoles()
    {
        var result = await _adminService.GetAllRolesAsync();

        result.Should().Contain(r => r.IsSystemRole);
    }

    [Fact]
    public async Task GetAllPermissionsAsync_ShouldReturnAllPermissionsOrderedByResourceAndAction()
    {
        var result = await _adminService.GetAllPermissionsAsync();

        result.Should().NotBeEmpty();
        var previousResource = string.Empty;
        var previousAction = string.Empty;

        foreach (var permission in result)
        {
            if (permission.Resource == previousResource)
            {
                (string.Compare(permission.Action, previousAction, StringComparison.Ordinal) >= 0).Should().BeTrue();
            }
            else
            {
                (string.Compare(permission.Resource, previousResource, StringComparison.Ordinal) >= 0).Should().BeTrue();
            }
            previousResource = permission.Resource;
            previousAction = permission.Action;
        }
    }

    [Fact]
    public async Task GetUserRolesAsync_WithUserHavingRoles_ShouldReturnRoles()
    {
        var user = TestDataBuilder.CreateTestUser();

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var existingRoles = await _context.Roles.Take(2).ToListAsync();
        var role1 = existingRoles[0];
        var role2 = existingRoles[1];

        var userRole1 = new UserRole { UserId = user.Id, RoleId = role1.Id, Role = role1, AssignedAt = DateTime.UtcNow };
        var userRole2 = new UserRole { UserId = user.Id, RoleId = role2.Id, Role = role2, AssignedAt = DateTime.UtcNow };
        await _context.UserRoles.AddRangeAsync(userRole1, userRole2);
        await _context.SaveChangesAsync();

        var result = await _adminService.GetUserRolesAsync(user.Id);

        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Id == role1.Id);
        result.Should().Contain(r => r.Id == role2.Id);
    }

    [Fact]
    public async Task GetUserRolesAsync_WithUserHavingNoRoles_ShouldReturnEmptyList()
    {
        var user = TestDataBuilder.CreateTestUser();

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _adminService.GetUserRolesAsync(user.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRolePermissionsAsync_WithRoleHavingPermissions_ShouldReturnPermissions()
    {
        var existingRole = await _context.Roles.FirstAsync();
        var existingPermissions = await _context.Permissions.Take(2).ToListAsync();

        var rolePermission1 = new RolePermission
        {
            RoleId = existingRole.Id,
            PermissionId = existingPermissions[0].Id,
            Permission = existingPermissions[0]
        };
        var rolePermission2 = new RolePermission
        {
            RoleId = existingRole.Id,
            PermissionId = existingPermissions[1].Id,
            Permission = existingPermissions[1]
        };
        await _context.RolePermissions.AddRangeAsync(rolePermission1, rolePermission2);
        await _context.SaveChangesAsync();

        var result = await _adminService.GetRolePermissionsAsync(existingRole.Id);

        result.Should().HaveCountGreaterOrEqualTo(2);
        result.Should().Contain(p => p.Id == existingPermissions[0].Id);
        result.Should().Contain(p => p.Id == existingPermissions[1].Id);
    }

    [Fact]
    public async Task GetRolePermissionsAsync_WithRoleHavingNoPermissions_ShouldReturnEmptyList()
    {
        var newRole = TestDataBuilder.CreateRole(name: "TestRole_NoPermissions");

        await _context.Roles.AddAsync(newRole);
        await _context.SaveChangesAsync();

        var result = await _adminService.GetRolePermissionsAsync(newRole.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReloadPoliciesAsync_ShouldCallPolicyServiceLoadMethod()
    {
        await _adminService.ReloadPoliciesAsync();

        _policyServiceMock.Verify(
            x => x.LoadPoliciesIntoEnforcerAsync(_enforcerMock.Object),
            Times.Once);
    }

    [Fact]
    public async Task VerifyUserEmailAsync_WithUnverifiedUser_ShouldVerifySuccessfully()
    {
        var user = TestDataBuilder.CreateTestUser(isVerified: false);
        user.VerificationToken = "some_token";
        user.VerificationTokenExpiry = DateTime.UtcNow.AddDays(1);
        var adminId = 100L;

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _adminService.VerifyUserEmailAsync(user.Id, adminId);

        result.Should().BeTrue();

        var verifiedUser = await _context.Users.FindAsync(user.Id);
        verifiedUser!.IsVerified.Should().BeTrue();
        verifiedUser.EmailConfirmed.Should().BeTrue();
        verifiedUser.VerificationToken.Should().BeNull();
        verifiedUser.VerificationTokenExpiry.Should().BeNull();
    }

    [Fact]
    public async Task VerifyUserEmailAsync_WithAlreadyVerifiedUser_ShouldReturnFalse()
    {
        var user = TestDataBuilder.CreateTestUser(isVerified: true);

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var result = await _adminService.VerifyUserEmailAsync(user.Id, 100L);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyUserEmailAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        var result = await _adminService.VerifyUserEmailAsync(999, 100L);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetSystemStatisticsAsync_ShouldReturnCorrectStatistics()
    {
        var user1 = TestDataBuilder.CreateTestUser(username: "user1", isVerified: true, isBanned: false);
        user1.LastLoginAt = DateTime.UtcNow.AddDays(-5);
        var user2 = TestDataBuilder.CreateTestUser(username: "user2", email: "user2@example.com", isVerified: true, isBanned: false);
        user2.LastLoginAt = DateTime.UtcNow.AddDays(-35);
        var user3 = TestDataBuilder.CreateTestUser(username: "user3", email: "user3@example.com", isVerified: false, isBanned: true);
        user3.LastLoginAt = DateTime.UtcNow.AddDays(-10);

        await _context.Users.AddRangeAsync(user1, user2, user3);
        await _context.SaveChangesAsync();

        var result = await _adminService.GetSystemStatisticsAsync();

        result.TotalUsers.Should().Be(3);
        result.ActiveUsers.Should().Be(2);
        result.BannedUsers.Should().Be(1);
        result.VerifiedUsers.Should().Be(2);
        result.LastUserRegistration.Should().NotBeNull();
        result.LastUserRegistration.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetSystemStatisticsAsync_WithNoUsers_ShouldReturnZeroStatistics()
    {
        var result = await _adminService.GetSystemStatisticsAsync();

        result.TotalUsers.Should().Be(0);
        result.ActiveUsers.Should().Be(0);
        result.BannedUsers.Should().Be(0);
        result.VerifiedUsers.Should().Be(0);
        result.LastUserRegistration.Should().BeNull();
    }
}
