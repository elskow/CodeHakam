using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ContentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_profiles",
                schema: "content",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.user_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_updated_at",
                schema: "content",
                table: "user_profiles",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_username",
                schema: "content",
                table: "user_profiles",
                column: "username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_profiles",
                schema: "content");
        }
    }
}
