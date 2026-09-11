using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class HardenProcessingOperationConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProcessingOperations",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Inventories",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ReferenceId",
                table: "InventoryTransactions",
                column: "ReferenceId",
                unique: true,
                filter: "[ReferenceType] = 'ProcessingOperation' AND [ReferenceId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ReferenceId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Inventories");
        }
    }
}
