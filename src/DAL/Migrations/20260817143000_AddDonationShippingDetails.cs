using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260817143000_AddDonationShippingDetails")]
    public partial class AddDonationShippingDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "DropOffMethod", table: "DonationRequests", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CarrierName", table: "DonationRequests", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "TrackingCode", table: "DonationRequests", type: "nvarchar(max)", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DropOffMethod", table: "DonationRequests");
            migrationBuilder.DropColumn(name: "CarrierName", table: "DonationRequests");
            migrationBuilder.DropColumn(name: "TrackingCode", table: "DonationRequests");
        }
    }
}
