using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailedClassificationStages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClassificationAreaName",
                table: "IntakeBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClassificationCompletedAt",
                table: "IntakeBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClassificationCompletedByStaffId",
                table: "IntakeBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClassificationStartedAt",
                table: "IntakeBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClassificationStartedByStaffId",
                table: "IntakeBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClassifiedAreaPlacedAt",
                table: "IntakeBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClassifiedAreaPlacedByStaffId",
                table: "IntakeBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CountedAt",
                table: "IntakeBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CountedByStaffId",
                table: "IntakeBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountedItemCount",
                table: "IntakeBatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CountedTotalWeight",
                table: "IntakeBatches",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountingNotes",
                table: "IntakeBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassificationAreaName",
                table: "ClassifiedBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlacedInClassificationAreaAt",
                table: "ClassifiedBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlacedInClassificationAreaByStaffId",
                table: "ClassifiedBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RemovedFromClassificationAreaAt",
                table: "ClassifiedBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RemovedFromClassificationAreaByStaffId",
                table: "ClassifiedBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE IntakeBatches
                SET Status = 'AwaitingClassificationCount'
                WHERE Status = 'PendingClassification';

                UPDATE IntakeBatches
                SET CountedItemCount = (
                        SELECT COUNT(*) FROM ClassifiedItems i
                        WHERE i.BatchId = IntakeBatches.Id AND (i.IsActive = 1 OR i.IsActive IS NULL)),
                    CountedTotalWeight = TotalWeight,
                    CountedAt = COALESCE(UpdateAt, CreateAt, SYSUTCDATETIME())
                WHERE Status = 'Classifying'
                  AND EXISTS (SELECT 1 FROM ClassifiedItems i
                              WHERE i.BatchId = IntakeBatches.Id AND (i.IsActive = 1 OR i.IsActive IS NULL));

                UPDATE IntakeBatches
                SET Status = 'AwaitingClassificationCount'
                WHERE Status = 'Classifying' AND CountedItemCount IS NULL;

                UPDATE IntakeBatches
                SET Status = 'InClassifiedArea',
                    CountedItemCount = (SELECT COUNT(*) FROM ClassifiedItems i
                        WHERE i.BatchId = IntakeBatches.Id AND (i.IsActive = 1 OR i.IsActive IS NULL)),
                    CountedTotalWeight = TotalWeight,
                    CountedAt = COALESCE(UpdateAt, CreateAt, SYSUTCDATETIME()),
                    ClassificationCompletedAt = COALESCE(UpdateAt, CreateAt, SYSUTCDATETIME()),
                    ClassifiedAreaPlacedAt = COALESCE(UpdateAt, CreateAt, SYSUTCDATETIME()),
                    ClassificationAreaName = N'Khu vực đồ đã phân loại'
                WHERE Status = 'Classified';

                UPDATE cb
                SET cb.ClassificationAreaName = N'Khu vực đồ đã phân loại',
                    cb.PlacedInClassificationAreaAt = COALESCE(cb.UpdateAt, cb.CreateAt, SYSUTCDATETIME())
                FROM ClassifiedBatches cb
                WHERE cb.Status = 'Open'
                  AND EXISTS (
                      SELECT 1 FROM ClassifiedItems ci
                      INNER JOIN IntakeBatches ib ON ib.Id = ci.BatchId
                      WHERE ci.ClassifiedBatchId = cb.Id AND ib.Status = 'InClassifiedArea');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatches_ClassificationCompletedByStaffId",
                table: "IntakeBatches",
                column: "ClassificationCompletedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatches_ClassificationStartedByStaffId",
                table: "IntakeBatches",
                column: "ClassificationStartedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatches_ClassifiedAreaPlacedByStaffId",
                table: "IntakeBatches",
                column: "ClassifiedAreaPlacedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatches_CountedByStaffId",
                table: "IntakeBatches",
                column: "CountedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedBatches_PlacedInClassificationAreaByStaffId",
                table: "ClassifiedBatches",
                column: "PlacedInClassificationAreaByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedBatches_RemovedFromClassificationAreaByStaffId",
                table: "ClassifiedBatches",
                column: "RemovedFromClassificationAreaByStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassifiedBatches_Users_PlacedInClassificationAreaByStaffId",
                table: "ClassifiedBatches",
                column: "PlacedInClassificationAreaByStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassifiedBatches_Users_RemovedFromClassificationAreaByStaffId",
                table: "ClassifiedBatches",
                column: "RemovedFromClassificationAreaByStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_Users_ClassificationCompletedByStaffId",
                table: "IntakeBatches",
                column: "ClassificationCompletedByStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_Users_ClassificationStartedByStaffId",
                table: "IntakeBatches",
                column: "ClassificationStartedByStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_Users_ClassifiedAreaPlacedByStaffId",
                table: "IntakeBatches",
                column: "ClassifiedAreaPlacedByStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_Users_CountedByStaffId",
                table: "IntakeBatches",
                column: "CountedByStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE IntakeBatches SET Status = 'PendingClassification'
                WHERE Status IN ('AwaitingClassificationCount', 'ReadyForClassification');
                UPDATE IntakeBatches SET Status = 'Classified'
                WHERE Status = 'InClassifiedArea';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ClassifiedBatches_Users_PlacedInClassificationAreaByStaffId",
                table: "ClassifiedBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassifiedBatches_Users_RemovedFromClassificationAreaByStaffId",
                table: "ClassifiedBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_Users_ClassificationCompletedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_Users_ClassificationStartedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_Users_ClassifiedAreaPlacedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_Users_CountedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropIndex(
                name: "IX_IntakeBatches_ClassificationCompletedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropIndex(
                name: "IX_IntakeBatches_ClassificationStartedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropIndex(
                name: "IX_IntakeBatches_ClassifiedAreaPlacedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropIndex(
                name: "IX_IntakeBatches_CountedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropIndex(
                name: "IX_ClassifiedBatches_PlacedInClassificationAreaByStaffId",
                table: "ClassifiedBatches");

            migrationBuilder.DropIndex(
                name: "IX_ClassifiedBatches_RemovedFromClassificationAreaByStaffId",
                table: "ClassifiedBatches");

            migrationBuilder.DropColumn(
                name: "ClassificationAreaName",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "ClassificationCompletedAt",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "ClassificationCompletedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "ClassificationStartedAt",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "ClassificationStartedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "ClassifiedAreaPlacedAt",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "ClassifiedAreaPlacedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "CountedAt",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "CountedByStaffId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "CountedItemCount",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "CountedTotalWeight",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "CountingNotes",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "ClassificationAreaName",
                table: "ClassifiedBatches");

            migrationBuilder.DropColumn(
                name: "PlacedInClassificationAreaAt",
                table: "ClassifiedBatches");

            migrationBuilder.DropColumn(
                name: "PlacedInClassificationAreaByStaffId",
                table: "ClassifiedBatches");

            migrationBuilder.DropColumn(
                name: "RemovedFromClassificationAreaAt",
                table: "ClassifiedBatches");

            migrationBuilder.DropColumn(
                name: "RemovedFromClassificationAreaByStaffId",
                table: "ClassifiedBatches");
        }
    }
}
