using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinPort.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "ScrapedArticles",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "ScrapedArticles");
        }
    }
}
