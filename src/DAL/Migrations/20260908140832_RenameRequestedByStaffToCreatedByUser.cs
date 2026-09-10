using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class RenameRequestedByStaffToCreatedByUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessingOperations_Users_RequestedByStaffId",
                table: "ProcessingOperations");

            migrationBuilder.RenameColumn(
                name: "RequestedByStaffId",
                table: "ProcessingOperations",
                newName: "CreatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessingOperations_RequestedByStaffId",
                table: "ProcessingOperations",
                newName: "IX_ProcessingOperations_CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessingOperations_Users_CreatedByUserId",
                table: "ProcessingOperations",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessingOperations_Users_CreatedByUserId",
                table: "ProcessingOperations");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "ProcessingOperations",
                newName: "RequestedByStaffId");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessingOperations_CreatedByUserId",
                table: "ProcessingOperations",
                newName: "IX_ProcessingOperations_RequestedByStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessingOperations_Users_RequestedByStaffId",
                table: "ProcessingOperations",
                column: "RequestedByStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
