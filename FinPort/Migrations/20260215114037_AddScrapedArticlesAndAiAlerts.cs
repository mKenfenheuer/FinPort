using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinPort.Migrations
{
    /// <inheritdoc />
    public partial class AddScrapedArticlesAndAiAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiAlerts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PortfolioId = table.Column<string>(type: "TEXT", nullable: true),
                    PositionId = table.Column<string>(type: "TEXT", nullable: true),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Analysis = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsNotified = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiAlerts_PortfolioPositions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "PortfolioPositions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AiAlerts_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScrapedArticles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PositionId = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    ScrapedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapedArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScrapedArticles_PortfolioPositions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "PortfolioPositions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiAlerts_PortfolioId",
                table: "AiAlerts",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAlerts_PositionId",
                table: "AiAlerts",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScrapedArticles_PositionId",
                table: "ScrapedArticles",
                column: "PositionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiAlerts");

            migrationBuilder.DropTable(
                name: "ScrapedArticles");
        }
    }
}
