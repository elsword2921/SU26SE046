using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivingCollectionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickupAssignments_Users_ReceivingStaffId",
                table: "PickupAssignments");

            migrationBuilder.RenameColumn(
                name: "ReceivingStaffId",
                table: "PickupAssignments",
                newName: "TeamId");

            migrationBuilder.RenameIndex(
                name: "IX_PickupAssignments_ReceivingStaffId",
                table: "PickupAssignments",
                newName: "IX_PickupAssignments_TeamId");

            migrationBuilder.AddColumn<string>(
                name: "ShiftName",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AreaKey",
                table: "PickupAssignments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "IntakeBatchId",
                table: "PickupAssignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PickupAssignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "PickupAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RouteOrder",
                table: "PickupAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ShiftId",
                table: "PickupAssignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "PickupAssignments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "IntakeBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteName",
                table: "IntakeBatches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "IntakeBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickupAssignments_IntakeBatchId",
                table: "PickupAssignments",
                column: "IntakeBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PickupAssignments_ShiftId",
                table: "PickupAssignments",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickupAssignments_IntakeBatches_IntakeBatchId",
                table: "PickupAssignments",
                column: "IntakeBatchId",
                principalTable: "IntakeBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PickupAssignments_OperationalTeams_TeamId",
                table: "PickupAssignments",
                column: "TeamId",
                principalTable: "OperationalTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PickupAssignments_Shifts_ShiftId",
                table: "PickupAssignments",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickupAssignments_IntakeBatches_IntakeBatchId",
                table: "PickupAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_PickupAssignments_OperationalTeams_TeamId",
                table: "PickupAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_PickupAssignments_Shifts_ShiftId",
                table: "PickupAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PickupAssignments_IntakeBatchId",
                table: "PickupAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PickupAssignments_ShiftId",
                table: "PickupAssignments");

            migrationBuilder.DropColumn(
                name: "ShiftName",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "AreaKey",
                table: "PickupAssignments");

            migrationBuilder.DropColumn(
                name: "IntakeBatchId",
                table: "PickupAssignments");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PickupAssignments");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "PickupAssignments");

            migrationBuilder.DropColumn(
                name: "RouteOrder",
                table: "PickupAssignments");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "PickupAssignments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PickupAssignments");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "RouteName",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "IntakeBatches");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "PickupAssignments",
                newName: "ReceivingStaffId");

            migrationBuilder.RenameIndex(
                name: "IX_PickupAssignments_TeamId",
                table: "PickupAssignments",
                newName: "IX_PickupAssignments_ReceivingStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickupAssignments_Users_ReceivingStaffId",
                table: "PickupAssignments",
                column: "ReceivingStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
