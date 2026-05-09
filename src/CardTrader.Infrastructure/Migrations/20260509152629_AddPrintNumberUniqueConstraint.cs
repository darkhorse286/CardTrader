using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardTrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintNumberUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_card_instances_CardId_PrintNumber",
                table: "card_instances",
                columns: new[] { "CardId", "PrintNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_card_instances_CardId_PrintNumber",
                table: "card_instances");
        }
    }
}
