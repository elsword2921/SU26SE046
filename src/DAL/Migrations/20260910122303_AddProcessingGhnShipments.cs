using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingGhnShipments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GhnOrderCode",
                table: "ProcessingOperations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhnStatus",
                table: "ProcessingOperations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GhnUpdatedAt",
                table: "ProcessingOperations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProcessingShipmentEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingShipmentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingShipmentEvents_ProcessingOperations_ProcessingOperationId",
                        column: x => x.ProcessingOperationId,
                        principalTable: "ProcessingOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperations_GhnOrderCode",
                table: "ProcessingOperations",
                column: "GhnOrderCode",
                unique: true,
                filter: "[GhnOrderCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingShipmentEvents_ProcessingOperationId",
                table: "ProcessingShipmentEvents",
                column: "ProcessingOperationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessingShipmentEvents");

            migrationBuilder.DropIndex(
                name: "IX_ProcessingOperations_GhnOrderCode",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "GhnOrderCode",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "GhnStatus",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "GhnUpdatedAt",
                table: "ProcessingOperations");
        }
    }
}
