using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccountService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "users");

            migrationBuilder.CreateTable(
                name: "asp_net_roles",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_system_role = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rating = table.Column<int>(type: "integer", nullable: false, defaultValue: 1500),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    organization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_banned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ban_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    banned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verification_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    verification_token_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    password_reset_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    password_reset_token_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    normalized_username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_role_claims",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "FK_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "users",
                        principalTable: "asp_net_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalSchema: "users",
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "users",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "achievements",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    achievement_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    icon_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    points = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    earned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievements", x => x.id);
                    table.ForeignKey(
                        name: "FK_achievements_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_claims",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "FK_asp_net_user_claims_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_logins",
                schema: "users",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "FK_asp_net_user_logins_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_roles",
                schema: "users",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "users",
                        principalTable: "asp_net_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_asp_net_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_tokens",
                schema: "users",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "FK_asp_net_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rating_history",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    contest_id = table.Column<long>(type: "bigint", nullable: true),
                    old_rating = table.Column<int>(type: "integer", nullable: false),
                    new_rating = table.Column<int>(type: "integer", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rating_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_rating_history_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    revoked_by_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_by = table.Column<long>(type: "bigint", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "users",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_assigned_by",
                        column: x => x.assigned_by,
                        principalSchema: "users",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_settings",
                schema: "users",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    language_preference = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email_notifications = table.Column<bool>(type: "boolean", nullable: false),
                    contest_reminders = table.Column<bool>(type: "boolean", nullable: false),
                    solution_visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    show_rating = table.Column<bool>(type: "boolean", nullable: false),
                    timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_user_settings_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_statistics",
                schema: "users",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    problems_solved = table.Column<int>(type: "integer", nullable: false),
                    contests_participated = table.Column<int>(type: "integer", nullable: false),
                    total_submissions = table.Column<int>(type: "integer", nullable: false),
                    accepted_submissions = table.Column<int>(type: "integer", nullable: false),
                    acceptance_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    max_streak = table.Column<int>(type: "integer", nullable: false),
                    current_streak = table.Column<int>(type: "integer", nullable: false),
                    last_submission_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    easy_solved = table.Column<int>(type: "integer", nullable: false),
                    medium_solved = table.Column<int>(type: "integer", nullable: false),
                    hard_solved = table.Column<int>(type: "integer", nullable: false),
                    global_rank = table.Column<int>(type: "integer", nullable: true),
                    country_rank = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_statistics", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_user_statistics_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "permissions",
                columns: new[] { "id", "action", "created_at", "description", "name", "resource" },
                values: new object[,]
                {
                    { 1L, "read", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "View user profiles", "view_profile", "user" },
                    { 2L, "update", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Edit own profile", "edit_own_profile", "user" },
                    { 3L, "delete", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Delete own account", "delete_own_account", "user" },
                    { 4L, "read", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "View problems", "view_problems", "problem" },
                    { 5L, "create", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Submit problem solutions", "submit_solution", "submission" },
                    { 6L, "create", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Create new problems", "create_problem", "problem" },
                    { 7L, "update", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Edit problems", "edit_problem", "problem" },
                    { 8L, "delete", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Delete problems", "delete_problem", "problem" },
                    { 9L, "read", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "View contests", "view_contests", "contest" },
                    { 10L, "create", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Participate in contests", "participate_contest", "contest_participation" },
                    { 11L, "create", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Create contests", "create_contest", "contest" },
                    { 12L, "update", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Edit contests", "edit_contest", "contest" },
                    { 13L, "delete", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Delete contests", "delete_contest", "contest" },
                    { 14L, "manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manage user accounts", "manage_users", "user_management" },
                    { 15L, "manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manage roles and permissions", "manage_roles", "role_management" },
                    { 16L, "read", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "View system analytics", "view_analytics", "analytics" },
                    { 17L, "manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Moderate user content", "moderate_content", "content_moderation" },
                    { 18L, "manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ban/unban users", "ban_users", "user_ban" }
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "roles",
                columns: new[] { "id", "created_at", "description", "is_system_role", "name", "updated_at" },
                values: new object[,]
                {
                    { 1L, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Regular user with basic permissions", true, "user", null },
                    { 2L, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Problem setter who can create problems", true, "setter", null },
                    { 3L, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Moderator with content moderation permissions", true, "moderator", null },
                    { 4L, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Administrator with full system access", true, "admin", null },
                    { 5L, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Super administrator with unrestricted access", true, "super_admin", null }
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "role_permissions",
                columns: new[] { "id", "created_at", "permission_id", "role_id" },
                values: new object[,]
                {
                    { 1L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1060), 1L, 1L },
                    { 2L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1200), 2L, 1L },
                    { 3L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1200), 3L, 1L },
                    { 4L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1200), 4L, 1L },
                    { 5L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1200), 5L, 1L },
                    { 6L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1200), 9L, 1L },
                    { 7L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1200), 10L, 1L },
                    { 8L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2140), 1L, 2L },
                    { 9L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2200), 2L, 2L },
                    { 10L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2240), 3L, 2L },
                    { 11L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2250), 4L, 2L },
                    { 12L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2250), 5L, 2L },
                    { 13L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2250), 6L, 2L },
                    { 14L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2250), 7L, 2L },
                    { 15L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2250), 9L, 2L },
                    { 16L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2250), 10L, 2L },
                    { 17L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2630), 1L, 3L },
                    { 18L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2630), 2L, 3L },
                    { 19L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640), 3L, 3L },
                    { 20L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640), 4L, 3L },
                    { 21L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640), 5L, 3L },
                    { 22L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640), 6L, 3L },
                    { 23L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640), 7L, 3L },
                    { 24L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640), 8L, 3L },
                    { 25L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640), 9L, 3L },
                    { 26L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640), 10L, 3L },
                    { 27L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640), 11L, 3L },
                    { 28L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640), 12L, 3L },
                    { 29L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640), 17L, 3L },
                    { 30L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2650), 18L, 3L },
                    { 31L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980), 1L, 4L },
                    { 32L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980), 2L, 4L },
                    { 33L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980), 3L, 4L },
                    { 34L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980), 4L, 4L },
                    { 35L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980), 5L, 4L },
                    { 36L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980), 6L, 4L },
                    { 37L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980), 7L, 4L },
                    { 38L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980), 8L, 4L },
                    { 39L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980), 9L, 4L },
                    { 40L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980), 10L, 4L },
                    { 41L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980), 11L, 4L },
                    { 42L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980), 12L, 4L },
                    { 43L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980), 13L, 4L },
                    { 44L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 14L, 4L },
                    { 45L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 15L, 4L },
                    { 46L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 16L, 4L },
                    { 47L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 17L, 4L },
                    { 48L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 18L, 4L },
                    { 49L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 1L, 5L },
                    { 50L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 2L, 5L },
                    { 51L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 3L, 5L },
                    { 52L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 4L, 5L },
                    { 53L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 5L, 5L },
                    { 54L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 6L, 5L },
                    { 55L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 7L, 5L },
                    { 56L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 8L, 5L },
                    { 57L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990), 9L, 5L },
                    { 58L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000), 10L, 5L },
                    { 59L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000), 11L, 5L },
                    { 60L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000), 12L, 5L },
                    { 61L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000), 13L, 5L },
                    { 62L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000), 14L, 5L },
                    { 63L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000), 15L, 5L },
                    { 64L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000), 16L, 5L },
                    { 65L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000), 17L, 5L },
                    { 66L, new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000), 18L, 5L }
                });

            migrationBuilder.CreateIndex(
                name: "idx_achievements_user_id",
                schema: "users",
                table: "achievements",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_achievements_user_type",
                schema: "users",
                table: "achievements",
                columns: new[] { "user_id", "achievement_type" });

            migrationBuilder.CreateIndex(
                name: "IX_asp_net_role_claims_role_id",
                schema: "users",
                table: "asp_net_role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "users",
                table: "asp_net_roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asp_net_user_claims_user_id",
                schema: "users",
                table: "asp_net_user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_asp_net_user_logins_user_id",
                schema: "users",
                table: "asp_net_user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_asp_net_user_roles_role_id",
                schema: "users",
                table: "asp_net_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "idx_permissions_resource_action",
                schema: "users",
                table: "permissions",
                columns: new[] { "resource", "action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_rating_history_contest",
                schema: "users",
                table: "rating_history",
                column: "contest_id");

            migrationBuilder.CreateIndex(
                name: "idx_rating_history_user_changed",
                schema: "users",
                table: "rating_history",
                columns: new[] { "user_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_expires_at",
                schema: "users",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_token_hash",
                schema: "users",
                table: "refresh_tokens",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_user_id",
                schema: "users",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_role_permissions_permission",
                schema: "users",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "idx_role_permissions_role_permission",
                schema: "users",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_roles_name",
                schema: "users",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_roles_role",
                schema: "users",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_roles_user_role",
                schema: "users",
                table: "user_roles",
                columns: new[] { "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_assigned_by",
                schema: "users",
                table: "user_roles",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "idx_user_statistics_global_rank",
                schema: "users",
                table: "user_statistics",
                column: "global_rank");

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                schema: "users",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_normalized_email",
                schema: "users",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "idx_users_normalized_username",
                schema: "users",
                table: "users",
                column: "normalized_username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_rating",
                schema: "users",
                table: "users",
                column: "rating");

            migrationBuilder.CreateIndex(
                name: "idx_users_username",
                schema: "users",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "achievements",
                schema: "users");

            migrationBuilder.DropTable(
                name: "asp_net_role_claims",
                schema: "users");

            migrationBuilder.DropTable(
                name: "asp_net_user_claims",
                schema: "users");

            migrationBuilder.DropTable(
                name: "asp_net_user_logins",
                schema: "users");

            migrationBuilder.DropTable(
                name: "asp_net_user_roles",
                schema: "users");

            migrationBuilder.DropTable(
                name: "asp_net_user_tokens",
                schema: "users");

            migrationBuilder.DropTable(
                name: "rating_history",
                schema: "users");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "users");

            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "users");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "users");

            migrationBuilder.DropTable(
                name: "user_settings",
                schema: "users");

            migrationBuilder.DropTable(
                name: "user_statistics",
                schema: "users");

            migrationBuilder.DropTable(
                name: "asp_net_roles",
                schema: "users");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "users");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "users");

            migrationBuilder.DropTable(
                name: "users",
                schema: "users");
        }
    }
}
