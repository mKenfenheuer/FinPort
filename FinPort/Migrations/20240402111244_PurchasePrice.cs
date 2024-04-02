using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinPort.Migrations
{
    /// <inheritdoc />
    public partial class PurchasePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PurchasePrice",
                table: "PortfolioPositions",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchasePrice",
                table: "PortfolioPositions");
        }
    }
}
