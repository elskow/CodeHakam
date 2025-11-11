using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AccountService.Migrations
{
    /// <inheritdoc />
    public partial class AddCasbinRuleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "casbin_rule",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ptype = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    v0 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    v1 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    v2 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    v3 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    v4 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    v5 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_casbin_rule", x => x.id);
                });

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 1L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 892, DateTimeKind.Utc).AddTicks(8980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 2L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 892, DateTimeKind.Utc).AddTicks(9120));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 3L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 892, DateTimeKind.Utc).AddTicks(9120));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 4L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 892, DateTimeKind.Utc).AddTicks(9130));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 5L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 892, DateTimeKind.Utc).AddTicks(9130));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 6L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 892, DateTimeKind.Utc).AddTicks(9130));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 7L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 892, DateTimeKind.Utc).AddTicks(9130));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 8L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(90));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 9L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(160));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 10L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(190));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 11L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(190));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 12L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(190));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 13L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(190));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 14L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(190));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 15L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(190));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 16L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(200));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 17L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(600));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 18L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 19L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 20L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 21L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 22L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 23L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 24L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 25L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 26L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 27L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 28L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 29L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 30L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(650));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 31L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 32L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 33L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 34L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 35L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 36L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 37L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 38L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 39L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 40L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 41L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 42L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 43L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 44L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 45L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 46L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 47L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 48L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 49L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 50L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 51L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 52L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 53L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 54L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 55L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 56L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 57L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 58L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 59L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 60L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 61L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 62L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 63L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 64L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 65L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 66L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 10, 20, 52, 893, DateTimeKind.Utc).AddTicks(1020));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "casbin_rule",
                schema: "users");

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 1L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1060));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 2L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1200));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 3L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1200));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 4L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1200));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 5L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1200));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 6L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1200));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 7L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(1200));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 8L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2140));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 9L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2200));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 10L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2240));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 11L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2250));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 12L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2250));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 13L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2250));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 14L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2250));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 15L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2250));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 16L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2250));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 17L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2630));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 18L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2630));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 19L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 20L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 21L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 22L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 23L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 24L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 25L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 26L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 27L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 28L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 29L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2640));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 30L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2650));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 31L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 32L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 33L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 34L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 35L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 36L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 37L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 38L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 39L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 40L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 41L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 42L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 43L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2980));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 44L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 45L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 46L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 47L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 48L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 49L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 50L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 51L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 52L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 53L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 54L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 55L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 56L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 57L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(2990));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 58L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 59L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 60L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 61L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 62L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 63L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 64L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 65L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 66L,
                column: "created_at",
                value: new DateTime(2025, 11, 10, 9, 37, 28, 421, DateTimeKind.Utc).AddTicks(3000));
        }
    }
}
