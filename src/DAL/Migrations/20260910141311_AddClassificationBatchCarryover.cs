using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddClassificationBatchCarryover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassificationBatchTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntakeBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_ClassificationBatchTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassificationBatchTransfers_IntakeBatches_IntakeBatchId",
                        column: x => x.IntakeBatchId,
                        principalTable: "IntakeBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassificationBatchTransfers_OperationalTeams_FromTeamId",
                        column: x => x.FromTeamId,
                        principalTable: "OperationalTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassificationBatchTransfers_OperationalTeams_ToTeamId",
                        column: x => x.ToTeamId,
                        principalTable: "OperationalTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassificationBatchTransfers_Users_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationBatchTransfers_FromTeamId",
                table: "ClassificationBatchTransfers",
                column: "FromTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationBatchTransfers_IntakeBatchId",
                table: "ClassificationBatchTransfers",
                column: "IntakeBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationBatchTransfers_StaffId",
                table: "ClassificationBatchTransfers",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationBatchTransfers_ToTeamId",
                table: "ClassificationBatchTransfers",
                column: "ToTeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassificationBatchTransfers");
        }
    }
}
