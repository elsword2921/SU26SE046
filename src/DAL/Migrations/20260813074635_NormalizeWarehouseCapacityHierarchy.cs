using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeWarehouseCapacityHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Older bootstrap logic assigned the warehouse capacity to several areas,
            // while Warehouses.TotalCapacityKg still kept the original smaller value.
            // Preserve the already configured layout once, then all subsequent writes
            // are protected by the service-level warehouse -> area -> row -> location
            // capacity validations.
            migrationBuilder.Sql(@"
                UPDATE warehouse
                SET warehouse.TotalCapacityKg = allocated.TotalAreaCapacity
                FROM Warehouses AS warehouse
                INNER JOIN
                (
                    SELECT WarehouseId, SUM(CapacityKg) AS TotalAreaCapacity
                    FROM WarehouseAreas
                    WHERE IsActive = 1
                    GROUP BY WarehouseId
                ) AS allocated ON allocated.WarehouseId = warehouse.Id
                WHERE allocated.TotalAreaCapacity > warehouse.TotalCapacityKg;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
