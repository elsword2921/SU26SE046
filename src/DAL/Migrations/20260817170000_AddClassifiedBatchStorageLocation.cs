using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817170000_AddClassifiedBatchStorageLocation")]
public partial class AddClassifiedBatchStorageLocation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "StorageLocationId",
            table: "ClassifiedBatches",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ClassifiedBatches_StorageLocationId",
            table: "ClassifiedBatches",
            column: "StorageLocationId");

        migrationBuilder.AddForeignKey(
            name: "FK_ClassifiedBatches_StorageLocations_StorageLocationId",
            table: "ClassifiedBatches",
            column: "StorageLocationId",
            principalTable: "StorageLocations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ClassifiedBatches_StorageLocations_StorageLocationId",
            table: "ClassifiedBatches");
        migrationBuilder.DropIndex(
            name: "IX_ClassifiedBatches_StorageLocationId",
            table: "ClassifiedBatches");
        migrationBuilder.DropColumn(
            name: "StorageLocationId",
            table: "ClassifiedBatches");
    }
}
