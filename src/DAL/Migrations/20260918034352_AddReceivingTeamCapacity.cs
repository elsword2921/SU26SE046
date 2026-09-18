using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivingTeamCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxReceivingRequests",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 8);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxReceivingWeightKg",
                table: "Warehouses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 80m);

            migrationBuilder.AddColumn<int>(
                name: "MaxReceivingRequests",
                table: "OperationalTeams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxReceivingWeightKg",
                table: "OperationalTeams",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxReceivingRequests",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "MaxReceivingWeightKg",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "MaxReceivingRequests",
                table: "OperationalTeams");

            migrationBuilder.DropColumn(
                name: "MaxReceivingWeightKg",
                table: "OperationalTeams");
        }
    }
}
