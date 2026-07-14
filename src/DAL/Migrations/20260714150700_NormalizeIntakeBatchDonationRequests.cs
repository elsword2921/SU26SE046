using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeIntakeBatchDonationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonationRequests_IntakeBatches_BatchId",
                table: "DonationRequests");

            migrationBuilder.DropIndex(
                name: "IX_DonationRequests_BatchId",
                table: "DonationRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "ShiftId",
                table: "IntakeBatches",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "IntakeBatchDonationRequests",
                columns: table => new
                {
                    IntakeBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DonationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_IntakeBatchDonationRequests", x => new { x.IntakeBatchId, x.DonationRequestId });
                    table.ForeignKey(
                        name: "FK_IntakeBatchDonationRequests_DonationRequests_DonationRequestId",
                        column: x => x.DonationRequestId,
                        principalTable: "DonationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntakeBatchDonationRequests_IntakeBatches_IntakeBatchId",
                        column: x => x.IntakeBatchId,
                        principalTable: "IntakeBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntakeBatchDonationRequests_Users_AddedByStaffId",
                        column: x => x.AddedByStaffId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                UPDATE b
                SET b.ShiftId = t.ShiftId
                FROM IntakeBatches b
                INNER JOIN OperationalTeams t ON t.Id = b.ReceivingTeamId;

                INSERT INTO IntakeBatchDonationRequests
                    (IntakeBatchId, DonationRequestId, AddedAt, AddedByStaffId, Id, CreateAt, IsActive)
                SELECT d.BatchId, d.Id, COALESCE(d.UpdateAt, SYSUTCDATETIME()),
                       COALESCE(member.StaffId, '85555555-5555-5555-5555-555555555555'),
                       NEWID(), COALESCE(d.UpdateAt, SYSUTCDATETIME()), 1
                FROM DonationRequests d
                INNER JOIN IntakeBatches b ON b.Id = d.BatchId
                OUTER APPLY (
                    SELECT TOP 1 tm.StaffId
                    FROM TeamMembers tm
                    WHERE tm.TeamId = b.ReceivingTeamId AND ISNULL(tm.IsActive, 1) = 1
                    ORDER BY tm.CreateAt
                ) member
                WHERE d.BatchId IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "DonationRequests");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatches_ShiftId",
                table: "IntakeBatches",
                column: "ShiftId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatchDonationRequests_AddedByStaffId",
                table: "IntakeBatchDonationRequests",
                column: "AddedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatchDonationRequests_DonationRequestId",
                table: "IntakeBatchDonationRequests",
                column: "DonationRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_Shifts_ShiftId",
                table: "IntakeBatches",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_Shifts_ShiftId",
                table: "IntakeBatches");

            migrationBuilder.DropTable(
                name: "IntakeBatchDonationRequests");

            migrationBuilder.DropIndex(
                name: "IX_IntakeBatches_ShiftId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "IntakeBatches");

            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                table: "DonationRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonationRequests_BatchId",
                table: "DonationRequests",
                column: "BatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_DonationRequests_IntakeBatches_BatchId",
                table: "DonationRequests",
                column: "BatchId",
                principalTable: "IntakeBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
