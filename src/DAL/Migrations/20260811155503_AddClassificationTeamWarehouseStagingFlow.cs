using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddClassificationTeamWarehouseStagingFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AreaType",
                table: "WarehouseAreas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Storage");

            migrationBuilder.Sql("UPDATE [WarehouseAreas] SET [AreaType] = 'Storage' WHERE [AreaType] = '' OR [AreaType] IS NULL;");
            migrationBuilder.Sql(@"
                INSERT INTO [WarehouseAreas] ([Id], [WarehouseId], [AreaName], [AreaType], [Description],
                    [CapacityKg], [CurrentKg], [CreateAt], [IsActive])
                SELECT NEWID(), w.[Id], v.[AreaName], v.[AreaType], v.[Description],
                    w.[TotalCapacityKg], 0, DATEADD(HOUR, 7, SYSUTCDATETIME()), 1
                FROM [Warehouses] w
                CROSS JOIN (VALUES
                    (N'Khu nhận đồ', 'Receiving', N'Khu tiếp nhận lô hàng do nhân viên phân loại đưa về.'),
                    (N'Khu chưa phân loại', 'Unclassified', N'Khu lô hàng chờ nhân viên phân loại xử lý.'),
                    (N'Khu đã phân loại', 'Classified', N'Khu lô hàng đã hoàn tất phân loại.')
                ) v([AreaName], [AreaType], [Description])
                WHERE w.[IsActive] = 1 AND NOT EXISTS (
                    SELECT 1 FROM [WarehouseAreas] a
                    WHERE a.[WarehouseId] = w.[Id] AND a.[AreaType] = v.[AreaType] AND a.[IsActive] = 1
                );");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClassificationAssignedAt",
                table: "IntakeBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClassificationAssignedByManagerId",
                table: "IntakeBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClassificationTeamId",
                table: "IntakeBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentAreaId",
                table: "IntakeBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarehouseReceivedAt",
                table: "IntakeBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseReceivedByStaffId",
                table: "IntakeBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatches_ClassificationAssignedByManagerId",
                table: "IntakeBatches",
                column: "ClassificationAssignedByManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatches_ClassificationTeamId",
                table: "IntakeBatches",
                column: "ClassificationTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatches_CurrentAreaId",
                table: "IntakeBatches",
                column: "CurrentAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatches_WarehouseReceivedByStaffId",
                table: "IntakeBatches",
                column: "WarehouseReceivedByStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_OperationalTeams_ClassificationTeamId",
                table: "IntakeBatches",
                column: "ClassificationTeamId",
                principalTable: "OperationalTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_Users_ClassificationAssignedByManagerId",
                table: "IntakeBatches",
                column: "ClassificationAssignedByManagerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_Users_WarehouseReceivedByStaffId",
                table: "IntakeBatches",
                column: "WarehouseReceivedByStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_WarehouseAreas_CurrentAreaId",
                table: "IntakeBatches",
                column: "CurrentAreaId",
                principalTable: "WarehouseAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_OperationalTeams_ClassificationTeamId",
                table: "IntakeBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_Users_ClassificationAssignedByManagerId",
                table: "IntakeBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_Users_WarehouseReceivedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_WarehouseAreas_CurrentAreaId",
                table: "IntakeBatches");

            migrationBuilder.DropIndex(
                name: "IX_IntakeBatches_ClassificationAssignedByManagerId",
                table: "IntakeBatches");

            migrationBuilder.DropIndex(
                name: "IX_IntakeBatches_ClassificationTeamId",
                table: "IntakeBatches");

            migrationBuilder.DropIndex(
                name: "IX_IntakeBatches_CurrentAreaId",
                table: "IntakeBatches");

            migrationBuilder.DropIndex(
                name: "IX_IntakeBatches_WarehouseReceivedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "AreaType",
                table: "WarehouseAreas");

            migrationBuilder.DropColumn(
                name: "ClassificationAssignedAt",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "ClassificationAssignedByManagerId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "ClassificationTeamId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "CurrentAreaId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "WarehouseReceivedAt",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "WarehouseReceivedByStaffId",
                table: "IntakeBatches");
        }
    }
}
