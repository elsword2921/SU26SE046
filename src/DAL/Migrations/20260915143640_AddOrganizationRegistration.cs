using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing deployments may have created this role with a different ID.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleName] = N'DisposalOrganization')
                    INSERT INTO [Roles] ([Id], [RoleName], [Description], [CreateAt], [IsActive])
                    VALUES ('88888888-8888-8888-8888-888888888888', N'DisposalOrganization',
                        N'Organization responsible for disposing unusable clothes', '2025-01-01', 1);
                """);
            migrationBuilder.AddColumn<string>(
                name: "CertificateImageUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("85555555-5555-5555-5555-555555555555"),
                columns: new[] { "CertificateImageUrl", "RepresentativeName", "TaxCode" },
                values: new object[] { null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateImageUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RepresentativeName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                table: "Users");
        }
    }
}
