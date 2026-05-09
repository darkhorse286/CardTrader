using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardTrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameCollectionToRoster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "collections",
                newName: "rosters");

            migrationBuilder.RenameColumn(
                name: "collection_id",
                table: "card_instances",
                newName: "roster_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "rosters",
                newName: "collections");

            migrationBuilder.RenameColumn(
                name: "roster_id",
                table: "card_instances",
                newName: "collection_id");
        }
    }
}
