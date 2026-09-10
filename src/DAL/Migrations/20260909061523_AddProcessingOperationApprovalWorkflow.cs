using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingOperationApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RejectionReason",
                table: "ProcessingOperations",
                newName: "OrganizationRejectionReason");

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByOrganizationId",
                table: "ProcessingOperations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerRejectionReason",
                table: "ProcessingOperations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManagerRespondedAt",
                table: "ProcessingOperations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrganizationRespondedAt",
                table: "ProcessingOperations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "ProcessingOperations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedByManagerId",
                table: "ProcessingOperations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperations_ApprovedByOrganizationId",
                table: "ProcessingOperations",
                column: "ApprovedByOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingOperations_RejectedByManagerId",
                table: "ProcessingOperations",
                column: "RejectedByManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessingOperations_Users_ApprovedByOrganizationId",
                table: "ProcessingOperations",
                column: "ApprovedByOrganizationId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessingOperations_Users_RejectedByManagerId",
                table: "ProcessingOperations",
                column: "RejectedByManagerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessingOperations_Users_ApprovedByOrganizationId",
                table: "ProcessingOperations");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcessingOperations_Users_RejectedByManagerId",
                table: "ProcessingOperations");

            migrationBuilder.DropIndex(
                name: "IX_ProcessingOperations_ApprovedByOrganizationId",
                table: "ProcessingOperations");

            migrationBuilder.DropIndex(
                name: "IX_ProcessingOperations_RejectedByManagerId",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "ApprovedByOrganizationId",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "ManagerRejectionReason",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "ManagerRespondedAt",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "OrganizationRespondedAt",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "ProcessingOperations");

            migrationBuilder.DropColumn(
                name: "RejectedByManagerId",
                table: "ProcessingOperations");

            migrationBuilder.RenameColumn(
                name: "OrganizationRejectionReason",
                table: "ProcessingOperations",
                newName: "RejectionReason");
        }
    }
}
