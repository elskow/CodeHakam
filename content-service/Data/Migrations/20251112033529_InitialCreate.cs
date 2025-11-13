using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ContentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content");

            migrationBuilder.CreateTable(
                name: "problem_lists",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    owner_id = table.Column<long>(type: "bigint", nullable: false),
                    problem_ids = table.Column<long[]>(type: "bigint[]", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_problem_lists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "problems",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    input_format = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    output_format = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    constraints = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    time_limit = table.Column<int>(type: "integer", nullable: false),
                    memory_limit = table.Column<int>(type: "integer", nullable: false),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    visibility = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    submission_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    accepted_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    acceptance_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_problems", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discussions",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    problem_id = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    vote_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    comment_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discussions", x => x.id);
                    table.ForeignKey(
                        name: "FK_discussions_problems_problem_id",
                        column: x => x.problem_id,
                        principalSchema: "content",
                        principalTable: "problems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "editorials",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    problem_id = table.Column<long>(type: "bigint", nullable: false),
                    content = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    approach = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    time_complexity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    space_complexity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    solution_code = table.Column<string>(type: "jsonb", nullable: true),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_editorials", x => x.id);
                    table.ForeignKey(
                        name: "FK_editorials_problems_problem_id",
                        column: x => x.problem_id,
                        principalSchema: "content",
                        principalTable: "problems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "problem_tags",
                schema: "content",
                columns: table => new
                {
                    problem_id = table.Column<long>(type: "bigint", nullable: false),
                    tag = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_problem_tags", x => new { x.problem_id, x.tag });
                    table.ForeignKey(
                        name: "FK_problem_tags_problems_problem_id",
                        column: x => x.problem_id,
                        principalSchema: "content",
                        principalTable: "problems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_cases",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    problem_id = table.Column<long>(type: "bigint", nullable: false),
                    test_number = table.Column<int>(type: "integer", nullable: false),
                    is_sample = table.Column<bool>(type: "boolean", nullable: false),
                    input_file_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    output_file_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    input_size = table.Column<long>(type: "bigint", nullable: false),
                    output_size = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_cases", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_cases_problems_problem_id",
                        column: x => x.problem_id,
                        principalSchema: "content",
                        principalTable: "problems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "discussion_comments",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    discussion_id = table.Column<long>(type: "bigint", nullable: false),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    vote_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_accepted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discussion_comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_discussion_comments_discussion_comments_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "content",
                        principalTable: "discussion_comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_discussion_comments_discussions_discussion_id",
                        column: x => x.discussion_id,
                        principalSchema: "content",
                        principalTable: "discussions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_discussion_comments_discussion_id",
                schema: "content",
                table: "discussion_comments",
                column: "discussion_id");

            migrationBuilder.CreateIndex(
                name: "ix_discussion_comments_parent_id",
                schema: "content",
                table: "discussion_comments",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_discussion_comments_user_id",
                schema: "content",
                table: "discussion_comments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_discussions_created_at",
                schema: "content",
                table: "discussions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_discussions_problem_id",
                schema: "content",
                table: "discussions",
                column: "problem_id");

            migrationBuilder.CreateIndex(
                name: "ix_discussions_user_id",
                schema: "content",
                table: "discussions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_editorials_problem_id",
                schema: "content",
                table: "editorials",
                column: "problem_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_problem_lists_is_public",
                schema: "content",
                table: "problem_lists",
                column: "is_public");

            migrationBuilder.CreateIndex(
                name: "ix_problem_lists_owner_id",
                schema: "content",
                table: "problem_lists",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_problem_tags_tag",
                schema: "content",
                table: "problem_tags",
                column: "tag");

            migrationBuilder.CreateIndex(
                name: "ix_problems_author_id",
                schema: "content",
                table: "problems",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_problems_created_at",
                schema: "content",
                table: "problems",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_problems_difficulty",
                schema: "content",
                table: "problems",
                column: "difficulty");

            migrationBuilder.CreateIndex(
                name: "ix_problems_slug",
                schema: "content",
                table: "problems",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_problems_visibility",
                schema: "content",
                table: "problems",
                column: "visibility");

            migrationBuilder.CreateIndex(
                name: "ix_test_cases_problem_id",
                schema: "content",
                table: "test_cases",
                column: "problem_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_cases_problem_test_number",
                schema: "content",
                table: "test_cases",
                columns: new[] { "problem_id", "test_number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discussion_comments",
                schema: "content");

            migrationBuilder.DropTable(
                name: "editorials",
                schema: "content");

            migrationBuilder.DropTable(
                name: "problem_lists",
                schema: "content");

            migrationBuilder.DropTable(
                name: "problem_tags",
                schema: "content");

            migrationBuilder.DropTable(
                name: "test_cases",
                schema: "content");

            migrationBuilder.DropTable(
                name: "discussions",
                schema: "content");

            migrationBuilder.DropTable(
                name: "problems",
                schema: "content");
        }
    }
}
