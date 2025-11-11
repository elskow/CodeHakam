using AccountService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, IdentityRole<long>, long>(options)
{
    // DbSets
    public DbSet<UserStatistics> UserStatistics { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Achievement> Achievements { get; set; }
    public DbSet<RatingHistory> RatingHistory { get; set; }

    // RBAC
    public new DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public new DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<CasbinRule> CasbinRules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema
        modelBuilder.HasDefaultSchema("users");

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserName).HasColumnName("username").HasMaxLength(50).IsRequired();
            entity.Property(e => e.NormalizedUserName).HasColumnName("normalized_username").HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
            entity.Property(e => e.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(256);
            entity.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.SecurityStamp).HasColumnName("security_stamp");
            entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
            entity.Property(e => e.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            entity.Property(e => e.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            entity.Property(e => e.LockoutEnd).HasColumnName("lockout_end");
            entity.Property(e => e.LockoutEnabled).HasColumnName("lockout_enabled");
            entity.Property(e => e.AccessFailedCount).HasColumnName("access_failed_count");

            entity.Property(e => e.Rating).HasColumnName("rating").HasDefaultValue(1500);
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(100);
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(500);
            entity.Property(e => e.Bio).HasColumnName("bio").HasMaxLength(1000);
            entity.Property(e => e.Country).HasColumnName("country").HasMaxLength(2);
            entity.Property(e => e.Organization).HasColumnName("organization").HasMaxLength(200);
            entity.Property(e => e.IsVerified).HasColumnName("is_verified").HasDefaultValue(false);
            entity.Property(e => e.IsBanned).HasColumnName("is_banned").HasDefaultValue(false);
            entity.Property(e => e.BanReason).HasColumnName("ban_reason").HasMaxLength(500);
            entity.Property(e => e.BannedAt).HasColumnName("banned_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.VerificationToken).HasColumnName("verification_token").HasMaxLength(256);
            entity.Property(e => e.VerificationTokenExpiry).HasColumnName("verification_token_expiry");
            entity.Property(e => e.PasswordResetToken).HasColumnName("password_reset_token").HasMaxLength(256);
            entity.Property(e => e.PasswordResetTokenExpiry).HasColumnName("password_reset_token_expiry");

            // Indexes
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("idx_users_email");
            entity.HasIndex(e => e.UserName).IsUnique().HasDatabaseName("idx_users_username");
            entity.HasIndex(e => e.Rating).HasDatabaseName("idx_users_rating");
            entity.HasIndex(e => e.NormalizedEmail).HasDatabaseName("idx_users_normalized_email");
            entity.HasIndex(e => e.NormalizedUserName).HasDatabaseName("idx_users_normalized_username");

            // Relationships
            entity.HasOne(e => e.Statistics)
                .WithOne(s => s.User)
                .HasForeignKey<UserStatistics>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Settings)
                .WithOne(s => s.User)
                .HasForeignKey<UserSettings>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.RefreshTokens)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Achievements)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.RatingHistory)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Identity tables
        modelBuilder.Entity<IdentityRole<long>>(entity =>
        {
            entity.ToTable("asp_net_roles");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.NormalizedName).HasColumnName("normalized_name");
            entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp");
        });

        modelBuilder.Entity<IdentityUserClaim<long>>(entity =>
        {
            entity.ToTable("asp_net_user_claims");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ClaimType).HasColumnName("claim_type");
            entity.Property(e => e.ClaimValue).HasColumnName("claim_value");
        });

        modelBuilder.Entity<IdentityUserLogin<long>>(entity =>
        {
            entity.ToTable("asp_net_user_logins");
            entity.Property(e => e.LoginProvider).HasColumnName("login_provider");
            entity.Property(e => e.ProviderKey).HasColumnName("provider_key");
            entity.Property(e => e.ProviderDisplayName).HasColumnName("provider_display_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<IdentityUserToken<long>>(entity =>
        {
            entity.ToTable("asp_net_user_tokens");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.LoginProvider).HasColumnName("login_provider");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<IdentityRoleClaim<long>>(entity =>
        {
            entity.ToTable("asp_net_role_claims");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.ClaimType).HasColumnName("claim_type");
            entity.Property(e => e.ClaimValue).HasColumnName("claim_value");
        });

        modelBuilder.Entity<IdentityUserRole<long>>(entity =>
        {
            entity.ToTable("asp_net_user_roles");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
        });

        // Configure UserStatistics
        modelBuilder.Entity<UserStatistics>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.GlobalRank).HasDatabaseName("idx_user_statistics_global_rank");
        });

        // Configure RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_refresh_tokens_user_id");
            entity.HasIndex(e => e.TokenHash).HasDatabaseName("idx_refresh_tokens_token_hash");
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("idx_refresh_tokens_expires_at");
        });

        // Configure Achievement
        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.ToTable("achievements");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AchievementType).HasColumnName("achievement_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.IconUrl).HasColumnName("icon_url").HasMaxLength(500);
            entity.Property(e => e.Points).HasColumnName("points").HasDefaultValue(0);
            entity.Property(e => e.EarnedAt).HasColumnName("earned_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_achievements_user_id");
            entity.HasIndex(e => new { e.UserId, e.AchievementType }).HasDatabaseName("idx_achievements_user_type");
        });

        // Configure RatingHistory
        modelBuilder.Entity<RatingHistory>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ChangedAt })
                .HasDatabaseName("idx_rating_history_user_changed");
            entity.HasIndex(e => e.ContestId).HasDatabaseName("idx_rating_history_contest");
        });

        // Configure RBAC entities
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("idx_roles_name");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(e => new { e.Resource, e.Action })
                .IsUnique()
                .HasDatabaseName("idx_permissions_resource_action");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.RoleId })
                .IsUnique()
                .HasDatabaseName("idx_user_roles_user_role");
            entity.HasIndex(e => e.RoleId).HasDatabaseName("idx_user_roles_role");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasIndex(e => new { e.RoleId, e.PermissionId })
                .IsUnique()
                .HasDatabaseName("idx_role_permissions_role_permission");
            entity.HasIndex(e => e.PermissionId).HasDatabaseName("idx_role_permissions_permission");
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed system roles (use fixed date to prevent model changes detection)
        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var roles = new[]
        {
            new Role { Id = 1, Name = "user", Description = "Regular user with basic permissions", IsSystemRole = true, CreatedAt = seedDate },
            new Role { Id = 2, Name = "setter", Description = "Problem setter who can create problems", IsSystemRole = true, CreatedAt = seedDate },
            new Role { Id = 3, Name = "moderator", Description = "Moderator with content moderation permissions", IsSystemRole = true, CreatedAt = seedDate },
            new Role { Id = 4, Name = "admin", Description = "Administrator with full system access", IsSystemRole = true, CreatedAt = seedDate },
            new Role { Id = 5, Name = "super_admin", Description = "Super administrator with unrestricted access", IsSystemRole = true, CreatedAt = seedDate }
        };
        modelBuilder.Entity<Role>().HasData(roles);

        // Seed permissions
        var permissions = new List<Permission>
        {
            // User permissions
            new Permission { Id = 1, Name = "view_profile", Resource = "user", Action = "read", Description = "View user profiles", CreatedAt = seedDate },
            new Permission { Id = 2, Name = "edit_own_profile", Resource = "user", Action = "update", Description = "Edit own profile", CreatedAt = seedDate },
            new Permission { Id = 3, Name = "delete_own_account", Resource = "user", Action = "delete", Description = "Delete own account", CreatedAt = seedDate },

            // Problem permissions
            new Permission { Id = 4, Name = "view_problems", Resource = "problem", Action = "read", Description = "View problems", CreatedAt = seedDate },
            new Permission { Id = 5, Name = "submit_solution", Resource = "submission", Action = "create", Description = "Submit problem solutions", CreatedAt = seedDate },
            new Permission { Id = 6, Name = "create_problem", Resource = "problem", Action = "create", Description = "Create new problems", CreatedAt = seedDate },
            new Permission { Id = 7, Name = "edit_problem", Resource = "problem", Action = "update", Description = "Edit problems", CreatedAt = seedDate },
            new Permission { Id = 8, Name = "delete_problem", Resource = "problem", Action = "delete", Description = "Delete problems", CreatedAt = seedDate },

            // Contest permissions
            new Permission { Id = 9, Name = "view_contests", Resource = "contest", Action = "read", Description = "View contests", CreatedAt = seedDate },
            new Permission { Id = 10, Name = "participate_contest", Resource = "contest_participation", Action = "create", Description = "Participate in contests", CreatedAt = seedDate },
            new Permission { Id = 11, Name = "create_contest", Resource = "contest", Action = "create", Description = "Create contests", CreatedAt = seedDate },
            new Permission { Id = 12, Name = "edit_contest", Resource = "contest", Action = "update", Description = "Edit contests", CreatedAt = seedDate },
            new Permission { Id = 13, Name = "delete_contest", Resource = "contest", Action = "delete", Description = "Delete contests", CreatedAt = seedDate },

            // Admin permissions
            new Permission { Id = 14, Name = "manage_users", Resource = "user_management", Action = "manage", Description = "Manage user accounts", CreatedAt = seedDate },
            new Permission { Id = 15, Name = "manage_roles", Resource = "role_management", Action = "manage", Description = "Manage roles and permissions", CreatedAt = seedDate },
            new Permission { Id = 16, Name = "view_analytics", Resource = "analytics", Action = "read", Description = "View system analytics", CreatedAt = seedDate },
            new Permission { Id = 17, Name = "moderate_content", Resource = "content_moderation", Action = "manage", Description = "Moderate user content", CreatedAt = seedDate },
            new Permission { Id = 18, Name = "ban_users", Resource = "user_ban", Action = "manage", Description = "Ban/unban users", CreatedAt = seedDate },
        };
        modelBuilder.Entity<Permission>().HasData(permissions);

        // Seed role-permission mappings
        var permissionId = 1L;

        // User role (1) - basic permissions
        var rolePermissions = new[] { 1, 2, 3, 4, 5, 9, 10 }.Select(id => new RolePermission { Id = permissionId++, RoleId = 1, PermissionId = id, CreatedAt = DateTime.UtcNow }).ToList();

        // Setter role (2) - user permissions + problem creation
        rolePermissions.AddRange(new[] { 1, 2, 3, 4, 5, 6, 7, 9, 10 }.Select(id => new RolePermission { Id = permissionId++, RoleId = 2, PermissionId = id, CreatedAt = DateTime.UtcNow }));

        // Moderator role (3) - setter permissions + moderation
        rolePermissions.AddRange(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 17, 18 }.Select(id => new RolePermission { Id = permissionId++, RoleId = 3, PermissionId = id, CreatedAt = DateTime.UtcNow }));

        // Admin role (4) - most permissions
        rolePermissions.AddRange(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 }.Select(id => new RolePermission { Id = permissionId++, RoleId = 4, PermissionId = id, CreatedAt = DateTime.UtcNow }));

        // Super Admin role (5) - all permissions
        for (var id = 1; id <= 18; id++)
        {
            rolePermissions.Add(new RolePermission { Id = permissionId++, RoleId = 5, PermissionId = id, CreatedAt = DateTime.UtcNow });
        }

        modelBuilder.Entity<RolePermission>().HasData(rolePermissions);
    }
}
