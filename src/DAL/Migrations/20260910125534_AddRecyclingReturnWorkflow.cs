using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddRecyclingReturnWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedReturnDate",
                table: "ProcessingOperations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnCarrierName",
                table: "ProcessingOperations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnDispatchedAt",
                table: "ProcessingOperations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnNotes",
                table: "ProcessingOperations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnTrackingCode",
                table: "ProcessingOperations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessingOperationOutputId",
                table: "IntakeBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IntakeBatches",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatches_ProcessingOperationOutputId",
                table: "IntakeBatches",
                column: "ProcessingOperationOutputId",
                unique: true,
                filter: "[ProcessingOperationOutputId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_ProcessingOperationOutputs_ProcessingOperationOutputId",
                table: "IntakeBatches",
                column: "ProcessingOperationOutputId",
                principalTable: "ProcessingOperationOutputs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_ProcessingOperationOutputs_ProcessingOperationOutputId",
                table: "IntakeBatches");

            migrationBuilder.DropIndex(
                name: "IX_IntakeBatches_ProcessingOperationOutputId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "ExpectedReturnDate",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "ReturnCarrierName",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "ReturnDispatchedAt",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "ReturnNotes",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "ReturnTrackingCode",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "ProcessingOperationOutputId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IntakeBatches");
        }
    }
}
