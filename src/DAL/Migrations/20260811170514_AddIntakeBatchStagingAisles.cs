using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddIntakeBatchStagingAisles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentAreaGroupId",
                table: "IntakeBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatches_CurrentAreaGroupId",
                table: "IntakeBatches",
                column: "CurrentAreaGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_AreaGroups_CurrentAreaGroupId",
                table: "IntakeBatches",
                column: "CurrentAreaGroupId",
                principalTable: "AreaGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
                INSERT INTO AreaGroups
                    (Id, AreaId, GroupName, Description, CapacityKg, CurrentKg, CreateAt, IsActive)
                SELECT NEWID(), a.Id,
                    CONCAT(N'Dãy ',
                        CASE a.AreaType
                            WHEN 'Receiving' THEN 'RECEIVING'
                            WHEN 'Unclassified' THEN 'UNCLASSIFIED'
                            ELSE 'CLASSIFIED'
                        END,
                        '-', n.No),
                    N'Dãy nghiệp vụ thuộc ' + a.AreaName,
                    a.CapacityKg / 2, 0, DATEADD(HOUR, 7, SYSUTCDATETIME()), 1
                FROM WarehouseAreas a
                CROSS JOIN (VALUES ('01'), ('02')) n(No)
                WHERE a.AreaType IN ('Receiving', 'Unclassified', 'Classified')
                  AND a.IsActive = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM AreaGroups g
                      WHERE g.AreaId = a.Id
                        AND g.GroupName = CONCAT(N'Dãy ',
                            CASE a.AreaType
                                WHEN 'Receiving' THEN 'RECEIVING'
                                WHEN 'Unclassified' THEN 'UNCLASSIFIED'
                                ELSE 'CLASSIFIED'
                            END,
                            '-', n.No));

                UPDATE ib
                SET CurrentAreaGroupId = selected.Id
                FROM IntakeBatches ib
                OUTER APPLY (
                    SELECT TOP 1 g.Id
                    FROM AreaGroups g
                    WHERE g.AreaId = ib.CurrentAreaId AND g.IsActive = 1
                    ORDER BY g.GroupName
                ) selected
                WHERE ib.CurrentAreaId IS NOT NULL
                  AND ib.CurrentAreaGroupId IS NULL
                  AND selected.Id IS NOT NULL;

                UPDATE g
                SET CurrentKg = ISNULL(loads.TotalWeight, 0)
                FROM AreaGroups g
                LEFT JOIN (
                    SELECT CurrentAreaGroupId, SUM(TotalWeight) TotalWeight
                    FROM IntakeBatches
                    WHERE CurrentAreaGroupId IS NOT NULL AND IsActive = 1
                    GROUP BY CurrentAreaGroupId
                ) loads ON loads.CurrentAreaGroupId = g.Id
                WHERE g.AreaId IN (
                    SELECT Id FROM WarehouseAreas
                    WHERE AreaType IN ('Receiving', 'Unclassified', 'Classified'));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_AreaGroups_CurrentAreaGroupId",
                table: "IntakeBatches");

            migrationBuilder.DropIndex(
                name: "IX_IntakeBatches_CurrentAreaGroupId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "CurrentAreaGroupId",
                table: "IntakeBatches");
        }
    }
}
