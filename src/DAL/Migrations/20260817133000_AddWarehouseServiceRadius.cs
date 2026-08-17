using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260817133000_AddWarehouseServiceRadius")]
    public partial class AddWarehouseServiceRadius : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ServiceRadiusKm",
                table: "Warehouses",
                type: "float",
                nullable: false,
                defaultValue: 24.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceRadiusKm",
                table: "Warehouses");
        }
    }
}
