using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class SeedReceivingStaffUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "AvatarUrl", "CreateAt", "CreatedBy", "DeleteAt", "DeletedBy", "DonationPoint", "Email", "FullName", "IsActive", "PasswordHash", "PhoneNumber", "RoleId", "UpdateAt", "UpdatedBy", "UserName", "UserStatus", "WarehouseId" },
                values: new object[] { new Guid("85555555-5555-5555-5555-555555555555"), "Ho Chi Minh City", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, "receiving.staff@rethreads.local", "Receiving Staff Demo", true, "$2a$11$TCC0aSnsg3xBXrySfOn18OsY5Bme6jTvPnd6kVhAfR/XJIFODASVa", "0900000001", new Guid("55555555-5555-5555-5555-555555555555"), null, null, "receiving.staff", "Active", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("85555555-5555-5555-5555-555555555555"));
        }
    }
}
