using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AccountService.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "users",
                table: "user_roles",
                keyColumn: "id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "user_settings",
                keyColumn: "user_id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "user_statistics",
                keyColumn: "user_id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                schema: "users",
                table: "users",
                keyColumn: "id",
                keyValue: 1L);

            migrationBuilder.CreateTable(
                name: "outbox_events",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    aggregate_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_events", x => x.id);
                });

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 1L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(4320));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 2L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(4490));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 3L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(4490));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 4L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(4490));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 5L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(4490));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 6L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(4490));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 7L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(4500));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 8L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(5810));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 9L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(5920));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 10L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 11L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 12L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 13L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 14L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 15L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 16L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 17L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6500));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 18L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6510));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 19L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6510));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 20L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6510));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 21L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6510));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 22L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6510));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 23L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6510));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 24L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6510));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 25L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6510));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 26L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6510));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 27L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6510));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 28L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6510));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 29L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6510));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 30L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6520));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 31L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6890));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 32L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6890));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 33L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6890));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 34L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6890));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 35L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6890));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 36L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6890));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 37L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6890));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 38L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6890));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 39L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6890));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 40L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6890));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 41L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6900));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 42L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6900));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 43L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6900));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 44L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6900));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 45L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6900));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 46L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6900));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 47L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6900));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 48L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6900));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 49L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6900));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 50L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6910));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 51L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6910));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 52L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6910));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 53L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6910));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 54L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6910));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 55L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6910));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 56L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6910));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 57L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(6910));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 58L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(7000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 59L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(7000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 60L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(7000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 61L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(7000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 62L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(7000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 63L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(7000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 64L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(7000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 65L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(7000));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 66L,
                column: "created_at",
                value: new DateTime(2025, 11, 12, 17, 19, 16, 240, DateTimeKind.Utc).AddTicks(7000));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_events",
                schema: "users");

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 1L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(6030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 2L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(6230));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 3L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(6230));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 4L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(6230));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 5L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(6230));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 6L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(6230));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 7L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(6230));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 8L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7170));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 9L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7240));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 10L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7280));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 11L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7280));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 12L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7280));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 13L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7280));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 14L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7280));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 15L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7290));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 16L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7290));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 17L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7670));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 18L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7670));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 19L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 20L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 21L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 22L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 23L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 24L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 25L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 26L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 27L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 28L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 29L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 30L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 31L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 32L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 33L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 34L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 35L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 36L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8010));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 37L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 38L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 39L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 40L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 41L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 42L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 43L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 44L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 45L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 46L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 47L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 48L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 49L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 50L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 51L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 52L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 53L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 54L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 55L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 56L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 57L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 58L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 59L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 60L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 61L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 62L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8030));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 63L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8040));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 64L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8040));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 65L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8040));

            migrationBuilder.UpdateData(
                schema: "users",
                table: "role_permissions",
                keyColumn: "id",
                keyValue: 66L,
                column: "created_at",
                value: new DateTime(2025, 11, 11, 7, 8, 27, 222, DateTimeKind.Utc).AddTicks(8040));

            migrationBuilder.InsertData(
                schema: "users",
                table: "users",
                columns: new[] { "id", "access_failed_count", "avatar_url", "ban_reason", "banned_at", "bio", "concurrency_stamp", "country", "created_at", "email", "email_confirmed", "full_name", "is_verified", "last_login_at", "lockout_enabled", "lockout_end", "normalized_email", "normalized_username", "organization", "password_hash", "password_reset_token", "password_reset_token_expiry", "phone_number", "phone_number_confirmed", "security_stamp", "two_factor_enabled", "updated_at", "username", "verification_token", "verification_token_expiry" },
                values: new object[] { 1L, 0, null, null, null, null, "71b786ed-90f6-4a30-90ab-e393b85731a9", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@codehakam.com", true, "System Administrator", true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "ADMIN@CODEHAKAM.COM", "ADMIN", null, "AQAAAAIAAYagAAAAEMp3uwRc6q3zylMiS1MVfhgoQa+QoVIDKOJxPKEWrPm2Z+vlNyxbNqUqbKVhcA/ZdA==", null, null, null, false, "9a5efae2-171b-467c-b955-2ddb0727e8e5", false, null, "admin", null, null });

            migrationBuilder.InsertData(
                schema: "users",
                table: "user_roles",
                columns: new[] { "id", "assigned_at", "assigned_by", "expires_at", "role_id", "user_id" },
                values: new object[] { 1L, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 5L, 1L });

            migrationBuilder.InsertData(
                schema: "users",
                table: "user_settings",
                columns: new[] { "user_id", "contest_reminders", "email_notifications", "language_preference", "show_rating", "solution_visibility", "theme", "timezone", "updated_at" },
                values: new object[] { 1L, true, true, "en", true, "public", "dark", "UTC", new DateTime(2025, 11, 11, 7, 8, 27, 221, DateTimeKind.Utc).AddTicks(8950) });

            migrationBuilder.InsertData(
                schema: "users",
                table: "user_statistics",
                columns: new[] { "user_id", "acceptance_rate", "accepted_submissions", "contests_participated", "country_rank", "current_streak", "easy_solved", "global_rank", "hard_solved", "last_submission_date", "max_streak", "medium_solved", "problems_solved", "total_submissions", "updated_at" },
                values: new object[] { 1L, 0m, 0, 0, 0, 0, 0, 0, 0, null, 0, 0, 0, 0, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }
    }
}
