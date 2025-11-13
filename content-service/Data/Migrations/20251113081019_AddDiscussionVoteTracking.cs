using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ContentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscussionVoteTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "discussion_votes",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    discussion_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    is_upvote = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discussion_votes", x => x.id);
                    table.ForeignKey(
                        name: "FK_discussion_votes_discussions_discussion_id",
                        column: x => x.discussion_id,
                        principalSchema: "content",
                        principalTable: "discussions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_discussion_votes_discussion_id",
                schema: "content",
                table: "discussion_votes",
                column: "discussion_id");

            migrationBuilder.CreateIndex(
                name: "ix_discussion_votes_discussion_user",
                schema: "content",
                table: "discussion_votes",
                columns: new[] { "discussion_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_discussion_votes_user_id",
                schema: "content",
                table: "discussion_votes",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discussion_votes",
                schema: "content");
        }
    }
}
