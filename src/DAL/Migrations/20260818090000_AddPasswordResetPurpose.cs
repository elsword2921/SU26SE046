using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace DAL.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260818090000_AddPasswordResetPurpose")]
public partial class AddPasswordResetPurpose : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Purpose",
            table: "UserVerificationCodes",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Registration");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Purpose", table: "UserVerificationCodes");
    }
}
