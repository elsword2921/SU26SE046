using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddIntakeBatchReceivingStorageLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentStorageLocationId",
                table: "IntakeBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatches_CurrentStorageLocationId",
                table: "IntakeBatches",
                column: "CurrentStorageLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_StorageLocations_CurrentStorageLocationId",
                table: "IntakeBatches",
                column: "CurrentStorageLocationId",
                principalTable: "StorageLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
                INSERT INTO StorageLocations
                    (Id, WarehouseId, AreaId, AreaGroupId, LocationCode, AisleCode, RackCode,
                     ShelfCode, BinCode, PreferredProcessingDirection, CapacityKg,
                     CurrentWeightKg, Status, CreateAt, IsActive)
                SELECT NEWID(), area.WarehouseId, area.Id, areaGroup.Id,
                    CONCAT('RECEIVING-',
                        LEFT(REPLACE(CONVERT(varchar(36), areaGroup.Id), '-', ''), 8),
                        '-R01-S', position.Number, '-B01'),
                    CONCAT('A', RIGHT('0' + CONVERT(varchar(2),
                        DENSE_RANK() OVER (PARTITION BY area.Id ORDER BY areaGroup.GroupName)), 2)),
                    'R01', CONCAT('S', position.Number), 'B01', 'ReceivingStaging',
                    areaGroup.CapacityKg / 3, 0, 'Available',
                    DATEADD(HOUR, 7, SYSUTCDATETIME()), 1
                FROM WarehouseAreas area
                INNER JOIN AreaGroups areaGroup ON areaGroup.AreaId = area.Id
                CROSS JOIN (VALUES ('01'), ('02'), ('03')) position(Number)
                WHERE area.AreaType = 'Receiving'
                  AND area.IsActive = 1
                  AND areaGroup.IsActive = 1
                  AND NOT EXISTS (
                      SELECT 1
                      FROM StorageLocations existing
                      WHERE existing.AreaGroupId = areaGroup.Id
                        AND existing.ShelfCode = CONCAT('S', position.Number)
                        AND existing.IsActive = 1);

                UPDATE intakeBatch
                SET CurrentStorageLocationId = selectedLocation.Id
                FROM IntakeBatches intakeBatch
                OUTER APPLY (
                    SELECT TOP 1 location.Id
                    FROM StorageLocations location
                    WHERE location.AreaGroupId = intakeBatch.CurrentAreaGroupId
                      AND location.IsActive = 1
                    ORDER BY location.LocationCode
                ) selectedLocation
                WHERE intakeBatch.CurrentStorageLocationId IS NULL
                  AND intakeBatch.CurrentAreaGroupId IS NOT NULL
                  AND selectedLocation.Id IS NOT NULL
                  AND intakeBatch.CurrentAreaId IN (
                      SELECT Id FROM WarehouseAreas WHERE AreaType = 'Receiving');

                UPDATE location
                SET CurrentWeightKg = ISNULL(batchLoad.TotalWeight, 0)
                FROM StorageLocations location
                LEFT JOIN (
                    SELECT CurrentStorageLocationId, SUM(TotalWeight) TotalWeight
                    FROM IntakeBatches
                    WHERE CurrentStorageLocationId IS NOT NULL AND IsActive = 1
                    GROUP BY CurrentStorageLocationId
                ) batchLoad ON batchLoad.CurrentStorageLocationId = location.Id
                WHERE location.PreferredProcessingDirection = 'ReceivingStaging';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_StorageLocations_CurrentStorageLocationId",
                table: "IntakeBatches");

            migrationBuilder.DropIndex(
                name: "IX_IntakeBatches_CurrentStorageLocationId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "CurrentStorageLocationId",
                table: "IntakeBatches");

            migrationBuilder.Sql(@"
                DELETE FROM StorageLocations
                WHERE PreferredProcessingDirection = 'ReceivingStaging';
            ");
        }
    }
}
