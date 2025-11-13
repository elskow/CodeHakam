using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoUrlAndHintText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HintText",
                schema: "content",
                table: "problems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                schema: "content",
                table: "editorials",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HintText",
                schema: "content",
                table: "problems");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                schema: "content",
                table: "editorials");
        }
    }
}
