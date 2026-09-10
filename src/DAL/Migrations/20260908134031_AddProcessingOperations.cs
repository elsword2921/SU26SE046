using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProcessingOperationOutputId",
                table: "ClassifiedBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProcessingOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IssuedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutputReturnedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrackingCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CarrierName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_ProcessingOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingOperations_Users_ApprovedByManagerId",
                        column: x => x.ApprovedByManagerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessingOperations_Users_IssuedByStaffId",
                        column: x => x.IssuedByStaffId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessingOperations_Users_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessingOperations_Users_RequestedByStaffId",
                        column: x => x.RequestedByStaffId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessingOperations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingOperationInputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassifiedBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedQuantity = table.Column<int>(type: "int", nullable: false),
                    RequestedWeight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IssuedQuantity = table.Column<int>(type: "int", nullable: false),
                    IssuedWeight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_ProcessingOperationInputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingOperationInputs_ClassifiedBatches_ClassifiedBatchId",
                        column: x => x.ClassifiedBatchId,
                        principalTable: "ClassifiedBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessingOperationInputs_Inventories_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "Inventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessingOperationInputs_ProcessingOperations_ProcessingOperationId",
                        column: x => x.ProcessingOperationId,
                        principalTable: "ProcessingOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingOperationOutputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutputType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReturnedQuantity = table.Column<int>(type: "int", nullable: false),
                    ReturnedWeight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RecordedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_ProcessingOperationOutputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingOperationOutputs_ProcessingOperations_ProcessingOperationId",
                        column: x => x.ProcessingOperationId,
                        principalTable: "ProcessingOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessingOperationOutputs_Users_RecordedByStaffId",
                        column: x => x.RecordedByStaffId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedBatches_ProcessingOperationOutputId",
                table: "ClassifiedBatches",
                column: "ProcessingOperationOutputId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperationInputs_ClassifiedBatchId",
                table: "ProcessingOperationInputs",
                column: "ClassifiedBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperationInputs_InventoryId",
                table: "ProcessingOperationInputs",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperationInputs_ProcessingOperationId_InventoryId",
                table: "ProcessingOperationInputs",
                columns: new[] { "ProcessingOperationId", "InventoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperationOutputs_ProcessingOperationId",
                table: "ProcessingOperationOutputs",
                column: "ProcessingOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperationOutputs_RecordedByStaffId",
                table: "ProcessingOperationOutputs",
                column: "RecordedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperations_ApprovedByManagerId",
                table: "ProcessingOperations",
                column: "ApprovedByManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperations_IssuedByStaffId",
                table: "ProcessingOperations",
                column: "IssuedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperations_OperationCode",
                table: "ProcessingOperations",
                column: "OperationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperations_OrganizationId",
                table: "ProcessingOperations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperations_RequestedByStaffId",
                table: "ProcessingOperations",
                column: "RequestedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperations_WarehouseId",
                table: "ProcessingOperations",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassifiedBatches_ProcessingOperationOutputs_ProcessingOperationOutputId",
                table: "ClassifiedBatches",
                column: "ProcessingOperationOutputId",
                principalTable: "ProcessingOperationOutputs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassifiedBatches_ProcessingOperationOutputs_ProcessingOperationOutputId",
                table: "ClassifiedBatches");

            migrationBuilder.DropTable(
                name: "ProcessingOperationInputs");

            migrationBuilder.DropTable(
                name: "ProcessingOperationOutputs");

            migrationBuilder.DropTable(
                name: "ProcessingOperations");

            migrationBuilder.DropIndex(
                name: "IX_ClassifiedBatches_ProcessingOperationOutputId",
                table: "ClassifiedBatches");

            migrationBuilder.DropColumn(
                name: "ProcessingOperationOutputId",
                table: "ClassifiedBatches");
        }
    }
}
