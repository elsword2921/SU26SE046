using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class Feedback1109_FeedbackLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxBatchItemCount",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 500);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxBatchVolumeLiters",
                table: "Warehouses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 1500m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxBatchWeightKg",
                table: "Warehouses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 200m);

            migrationBuilder.AddColumn<string>(
                name: "CertificateImageUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxKgPerShift",
                table: "OperationalTeams",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxOrdersPerShift",
                table: "OperationalTeams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "OperationalTeams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedItemCount",
                table: "DonationRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedVolumeLiters",
                table: "DonationRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "PickupLatitude",
                table: "DonationRequests",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PickupLongitude",
                table: "DonationRequests",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedClothingTypeId",
                table: "DistributionRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedGenderId",
                table: "DistributionRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestedQuantity",
                table: "DistributionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedSizeId",
                table: "DistributionRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedTargetUserId",
                table: "DistributionRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedWeightKg",
                table: "DistributionRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DailyRequestLimit",
                table: "AiPromptConfigurations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalRequestLimit",
                table: "AiPromptConfigurations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiUsageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Feature = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UsageDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiUsageLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "Description",
                value: "Chuyên viên xuất nhập kho");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("85555555-5555-5555-5555-555555555555"),
                columns: new[] { "CertificateImageUrl", "OrganizationName", "TaxCode" },
                values: new object[] { null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_Feature_UsageDate_UserId",
                table: "AiUsageLogs",
                columns: new[] { "Feature", "UsageDate", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_UserId",
                table: "AiUsageLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiUsageLogs");

            migrationBuilder.DropColumn(
                name: "MaxBatchItemCount",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "MaxBatchVolumeLiters",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "MaxBatchWeightKg",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CertificateImageUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizationName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MaxKgPerShift",
                table: "OperationalTeams");

            migrationBuilder.DropColumn(
                name: "MaxOrdersPerShift",
                table: "OperationalTeams");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "OperationalTeams");

            migrationBuilder.DropColumn(
                name: "EstimatedItemCount",
                table: "DonationRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedVolumeLiters",
                table: "DonationRequests");

            migrationBuilder.DropColumn(
                name: "PickupLatitude",
                table: "DonationRequests");

            migrationBuilder.DropColumn(
                name: "PickupLongitude",
                table: "DonationRequests");

            migrationBuilder.DropColumn(
                name: "RequestedClothingTypeId",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "RequestedGenderId",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "RequestedQuantity",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "RequestedSizeId",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "RequestedTargetUserId",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "RequestedWeightKg",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "DailyRequestLimit",
                table: "AiPromptConfigurations");

            migrationBuilder.DropColumn(
                name: "TotalRequestLimit",
                table: "AiPromptConfigurations");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "Description",
                value: "Staff responsible for warehouse operations");
        }
    }
}
