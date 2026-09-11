using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseAreaProcessingDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessingDirection",
                table: "WarehouseAreas",
                type: "nvarchar(max)",
                nullable: true);
            // Infer only when every active location has the same known direction.
            migrationBuilder.Sql("""
                UPDATE a SET ProcessingDirection = d.Direction
                FROM WarehouseAreas a
                CROSS APPLY (
                    SELECT MIN(l.PreferredProcessingDirection) Direction
                    FROM StorageLocations l
                    WHERE l.AreaId=a.Id AND l.IsActive<>0
                    HAVING COUNT(*)>0
                       AND COUNT(*)=COUNT(l.PreferredProcessingDirection)
                       AND MIN(l.PreferredProcessingDirection)=MAX(l.PreferredProcessingDirection)
                       AND MIN(l.PreferredProcessingDirection) IN ('Charity','Recycling','Disposal')
                ) d
                WHERE a.AreaType='Storage' AND a.IsActive<>0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingDirection",
                table: "WarehouseAreas");
        }
    }
}
