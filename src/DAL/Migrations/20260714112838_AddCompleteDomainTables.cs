using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCompleteDomainTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Carts_CartId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Profile_ProfileId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Users_UserId",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_DonationRequests_IntakeBatches_BatchId",
                table: "DonationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_DonationRequests_Warehouses_WarehouseId",
                table: "DonationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_Warehouses_WarehouseId",
                table: "IntakeBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Categories_CategoryId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_IntakeBatches_IntakeBatchId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Inventories_InventoryId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_PickupAssignments_DonationRequests_DonorRequestId",
                table: "PickupAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_PickupAssignments_Users_ReceivingStaffId",
                table: "PickupAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Warehouses_WarehouseId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ClassificationItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_IntakeBatchId",
                table: "InventoryTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Profile",
                table: "Profile");

            migrationBuilder.DropColumn(
                name: "IntakeBatchId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "NewQuantity",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "PrevQuantity",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "ClassificationItemId",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "ClothCondition",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "ClothGender",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "ClothSize",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "IntakeBatchId",
                table: "Inventories");

            migrationBuilder.RenameTable(
                name: "Profile",
                newName: "Profiles");

            migrationBuilder.RenameColumn(
                name: "InventoryId",
                table: "InventoryTransactions",
                newName: "WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransactions_InventoryId",
                table: "InventoryTransactions",
                newName: "IX_InventoryTransactions_WarehouseId");

            migrationBuilder.RenameColumn(
                name: "TotalCloth",
                table: "Inventories",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "ClothTargetUser",
                table: "Inventories",
                newName: "ConditionRating");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "Inventories",
                newName: "WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_Inventories_CategoryId",
                table: "Inventories",
                newName: "IX_Inventories_WarehouseId");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentWeight",
                table: "Warehouses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCapacityKg",
                table: "Warehouses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "InventoryTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReferenceId",
                table: "InventoryTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceType",
                table: "InventoryTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "InventoryTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransactionCode",
                table: "InventoryTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "InventoryTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "AreaGroupId",
                table: "Inventories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileId",
                table: "Inventories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "TotalWeight",
                table: "Inventories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BatchCode",
                table: "IntakeBatches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BatchImages",
                table: "IntakeBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceivingTeamId",
                table: "IntakeBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualDeliveryTime",
                table: "DistributionRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "DistributionRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarrierName",
                table: "DistributionRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedDeliveryTime",
                table: "DistributionRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "DistributionRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestNotes",
                table: "DistributionRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedAt",
                table: "DistributionRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFee",
                table: "DistributionRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ShippingPaymentType",
                table: "DistributionRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DistributionRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ToAddress",
                table: "DistributionRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TrackingCode",
                table: "DistributionRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "DistributionRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "DistributionRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "ConditionRating",
                table: "DistributionItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "DistributionRequestId",
                table: "DistributionItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "DistributionItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileId",
                table: "DistributionItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "RequestedQuantity",
                table: "DistributionItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Profiles",
                table: "Profiles",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ClassificationCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriteriaName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CriteriaDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ClassificationCriteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConditionQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsCritical = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ConditionQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttributeValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_ProfileDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileDetails_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
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
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shifts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseAreas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CapacityKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_WarehouseAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseAreas_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassificationCriteriaOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ClassificationCriteriaOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassificationCriteriaOptions_ClassificationCriteria_CriteriaId",
                        column: x => x.CriteriaId,
                        principalTable: "ClassificationCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConditionAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConditionQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConditionRating = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ConditionAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConditionAnswers_ConditionQuestions_ConditionQuestionId",
                        column: x => x.ConditionQuestionId,
                        principalTable: "ConditionQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperationalTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TeamName = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_OperationalTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationalTeams_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AreaGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CapacityKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_AreaGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AreaGroups_WarehouseAreas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "WarehouseAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_TransferRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferRequests_WarehouseAreas_FromAreaId",
                        column: x => x.FromAreaId,
                        principalTable: "WarehouseAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferRequests_WarehouseAreas_ToAreaId",
                        column: x => x.ToAreaId,
                        principalTable: "WarehouseAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferRequests_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassificationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_ClassificationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassificationResults_ClassificationCriteriaOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "ClassificationCriteriaOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassificationResults_ClassificationCriteria_CriteriaId",
                        column: x => x.CriteriaId,
                        principalTable: "ClassificationCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_OperationalTeams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "OperationalTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Users_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassifiedBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConditionRating = table.Column<int>(type: "int", nullable: false),
                    BatchCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalWeight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalItem = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_ClassifiedBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassifiedBatches_AreaGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "AreaGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassifiedBatches_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassifiedBatches_WarehouseAreas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "WarehouseAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassifiedBatches_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassifiedItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassifiedBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConditionRating = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ClassifiedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassifiedItems_ClassifiedBatches_ClassifiedBatchId",
                        column: x => x.ClassifiedBatchId,
                        principalTable: "ClassifiedBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassifiedItems_IntakeBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "IntakeBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassifiedItems_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransactionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassifiedBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_TransactionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionItems_ClassifiedBatches_ClassifiedBatchId",
                        column: x => x.ClassifiedBatchId,
                        principalTable: "ClassifiedBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionItems_Inventories_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "Inventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionItems_InventoryTransactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClassifiedBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApproveStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_TransferItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferItems_ClassifiedBatches_ClassifiedBatchId",
                        column: x => x.ClassifiedBatchId,
                        principalTable: "ClassifiedBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferItems_IntakeBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "IntakeBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferItems_TransferRequests_TransferId",
                        column: x => x.TransferId,
                        principalTable: "TransferRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferItems_Users_ApproveStaffId",
                        column: x => x.ApproveStaffId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferItems_Users_RequestStaffId",
                        column: x => x.RequestStaffId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferItems_WarehouseAreas_ToAreaId",
                        column: x => x.ToAreaId,
                        principalTable: "WarehouseAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InspectionAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassifiedItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConditionQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConditionAnswerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_InspectionAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionAnswers_ClassifiedItems_ClassifiedItemId",
                        column: x => x.ClassifiedItemId,
                        principalTable: "ClassifiedItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InspectionAnswers_ConditionAnswers_ConditionAnswerId",
                        column: x => x.ConditionAnswerId,
                        principalTable: "ConditionAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InspectionAnswers_ConditionQuestions_ConditionQuestionId",
                        column: x => x.ConditionQuestionId,
                        principalTable: "ConditionQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_AreaGroupId",
                table: "Inventories",
                column: "AreaGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProfileId",
                table: "Inventories",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeBatches_ReceivingTeamId",
                table: "IntakeBatches",
                column: "ReceivingTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionRequests_UserId",
                table: "DistributionRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionRequests_WarehouseId",
                table: "DistributionRequests",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionItems_DistributionRequestId",
                table: "DistributionItems",
                column: "DistributionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionItems_ProfileId",
                table: "DistributionItems",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaGroups_AreaId",
                table: "AreaGroups",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationCriteriaOptions_CriteriaId",
                table: "ClassificationCriteriaOptions",
                column: "CriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationResults_CriteriaId",
                table: "ClassificationResults",
                column: "CriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationResults_OptionId",
                table: "ClassificationResults",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedBatches_AreaId",
                table: "ClassifiedBatches",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedBatches_GroupId",
                table: "ClassifiedBatches",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedBatches_ProfileId",
                table: "ClassifiedBatches",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedBatches_WarehouseId",
                table: "ClassifiedBatches",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedItems_BatchId",
                table: "ClassifiedItems",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedItems_ClassifiedBatchId",
                table: "ClassifiedItems",
                column: "ClassifiedBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifiedItems_ProfileId",
                table: "ClassifiedItems",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ConditionAnswers_ConditionQuestionId",
                table: "ConditionAnswers",
                column: "ConditionQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionAnswers_ClassifiedItemId",
                table: "InspectionAnswers",
                column: "ClassifiedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionAnswers_ConditionAnswerId",
                table: "InspectionAnswers",
                column: "ConditionAnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionAnswers_ConditionQuestionId",
                table: "InspectionAnswers",
                column: "ConditionQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalTeams_ShiftId",
                table: "OperationalTeams",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileDetails_ProfileId",
                table: "ProfileDetails",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_WarehouseId",
                table: "Shifts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_StaffId",
                table: "TeamMembers",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId_StaffId",
                table: "TeamMembers",
                columns: new[] { "TeamId", "StaffId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionItems_ClassifiedBatchId",
                table: "TransactionItems",
                column: "ClassifiedBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionItems_InventoryId",
                table: "TransactionItems",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionItems_TransactionId",
                table: "TransactionItems",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferItems_ApproveStaffId",
                table: "TransferItems",
                column: "ApproveStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferItems_BatchId",
                table: "TransferItems",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferItems_ClassifiedBatchId",
                table: "TransferItems",
                column: "ClassifiedBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferItems_RequestStaffId",
                table: "TransferItems",
                column: "RequestStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferItems_ToAreaId",
                table: "TransferItems",
                column: "ToAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferItems_TransferId",
                table: "TransferItems",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequests_FromAreaId",
                table: "TransferRequests",
                column: "FromAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequests_ToAreaId",
                table: "TransferRequests",
                column: "ToAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequests_WarehouseId",
                table: "TransferRequests",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseAreas_WarehouseId",
                table: "WarehouseAreas",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Carts_CartId",
                table: "CartItems",
                column: "CartId",
                principalTable: "Carts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Profiles_ProfileId",
                table: "CartItems",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Users_UserId",
                table: "Carts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionItems_DistributionRequests_DistributionRequestId",
                table: "DistributionItems",
                column: "DistributionRequestId",
                principalTable: "DistributionRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionItems_Profiles_ProfileId",
                table: "DistributionItems",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionRequests_Users_UserId",
                table: "DistributionRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionRequests_Warehouses_WarehouseId",
                table: "DistributionRequests",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DonationRequests_IntakeBatches_BatchId",
                table: "DonationRequests",
                column: "BatchId",
                principalTable: "IntakeBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DonationRequests_Warehouses_WarehouseId",
                table: "DonationRequests",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_OperationalTeams_ReceivingTeamId",
                table: "IntakeBatches",
                column: "ReceivingTeamId",
                principalTable: "OperationalTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_Warehouses_WarehouseId",
                table: "IntakeBatches",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_AreaGroups_AreaGroupId",
                table: "Inventories",
                column: "AreaGroupId",
                principalTable: "AreaGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Profiles_ProfileId",
                table: "Inventories",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Warehouses_WarehouseId",
                table: "Inventories",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Warehouses_WarehouseId",
                table: "InventoryTransactions",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PickupAssignments_DonationRequests_DonorRequestId",
                table: "PickupAssignments",
                column: "DonorRequestId",
                principalTable: "DonationRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PickupAssignments_Users_ReceivingStaffId",
                table: "PickupAssignments",
                column: "ReceivingStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Warehouses_WarehouseId",
                table: "Users",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Carts_CartId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Profiles_ProfileId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Users_UserId",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributionItems_DistributionRequests_DistributionRequestId",
                table: "DistributionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributionItems_Profiles_ProfileId",
                table: "DistributionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributionRequests_Users_UserId",
                table: "DistributionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributionRequests_Warehouses_WarehouseId",
                table: "DistributionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_DonationRequests_IntakeBatches_BatchId",
                table: "DonationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_DonationRequests_Warehouses_WarehouseId",
                table: "DonationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_OperationalTeams_ReceivingTeamId",
                table: "IntakeBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_IntakeBatches_Warehouses_WarehouseId",
                table: "IntakeBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_AreaGroups_AreaGroupId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Profiles_ProfileId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Warehouses_WarehouseId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Warehouses_WarehouseId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_PickupAssignments_DonationRequests_DonorRequestId",
                table: "PickupAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_PickupAssignments_Users_ReceivingStaffId",
                table: "PickupAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Warehouses_WarehouseId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ClassificationResults");

            migrationBuilder.DropTable(
                name: "InspectionAnswers");

            migrationBuilder.DropTable(
                name: "ProfileDetails");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "TransactionItems");

            migrationBuilder.DropTable(
                name: "TransferItems");

            migrationBuilder.DropTable(
                name: "ClassificationCriteriaOptions");

            migrationBuilder.DropTable(
                name: "ClassifiedItems");

            migrationBuilder.DropTable(
                name: "ConditionAnswers");

            migrationBuilder.DropTable(
                name: "OperationalTeams");

            migrationBuilder.DropTable(
                name: "TransferRequests");

            migrationBuilder.DropTable(
                name: "ClassificationCriteria");

            migrationBuilder.DropTable(
                name: "ClassifiedBatches");

            migrationBuilder.DropTable(
                name: "ConditionQuestions");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "AreaGroups");

            migrationBuilder.DropTable(
                name: "WarehouseAreas");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_AreaGroupId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_ProfileId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_IntakeBatches_ReceivingTeamId",
                table: "IntakeBatches");

            migrationBuilder.DropIndex(
                name: "IX_DistributionRequests_UserId",
                table: "DistributionRequests");

            migrationBuilder.DropIndex(
                name: "IX_DistributionRequests_WarehouseId",
                table: "DistributionRequests");

            migrationBuilder.DropIndex(
                name: "IX_DistributionItems_DistributionRequestId",
                table: "DistributionItems");

            migrationBuilder.DropIndex(
                name: "IX_DistributionItems_ProfileId",
                table: "DistributionItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Profiles",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "CurrentWeight",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "TotalCapacityKg",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "ReferenceType",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionCode",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "AreaGroupId",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "TotalWeight",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "BatchCode",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "BatchImages",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "ReceivingTeamId",
                table: "IntakeBatches");

            migrationBuilder.DropColumn(
                name: "ActualDeliveryTime",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "CarrierName",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryTime",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "RequestNotes",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "RequestedAt",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "ShippingFee",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "ShippingPaymentType",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "ToAddress",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "TrackingCode",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "DistributionRequests");

            migrationBuilder.DropColumn(
                name: "ConditionRating",
                table: "DistributionItems");

            migrationBuilder.DropColumn(
                name: "DistributionRequestId",
                table: "DistributionItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "DistributionItems");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "DistributionItems");

            migrationBuilder.DropColumn(
                name: "RequestedQuantity",
                table: "DistributionItems");

            migrationBuilder.RenameTable(
                name: "Profiles",
                newName: "Profile");

            migrationBuilder.RenameColumn(
                name: "WarehouseId",
                table: "InventoryTransactions",
                newName: "InventoryId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransactions_WarehouseId",
                table: "InventoryTransactions",
                newName: "IX_InventoryTransactions_InventoryId");

            migrationBuilder.RenameColumn(
                name: "WarehouseId",
                table: "Inventories",
                newName: "CategoryId");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "Inventories",
                newName: "TotalCloth");

            migrationBuilder.RenameColumn(
                name: "ConditionRating",
                table: "Inventories",
                newName: "ClothTargetUser");

            migrationBuilder.RenameIndex(
                name: "IX_Inventories_WarehouseId",
                table: "Inventories",
                newName: "IX_Inventories_CategoryId");

            migrationBuilder.AddColumn<Guid>(
                name: "IntakeBatchId",
                table: "InventoryTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "NewQuantity",
                table: "InventoryTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrevQuantity",
                table: "InventoryTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "InventoryTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ClassificationItemId",
                table: "Inventories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ClothCondition",
                table: "Inventories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClothGender",
                table: "Inventories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClothSize",
                table: "Inventories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IntakeBatchId",
                table: "Inventories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Profile",
                table: "Profile",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ClassificationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntakeBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassifyAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClothCondition = table.Column<int>(type: "int", nullable: false),
                    ClothGender = table.Column<int>(type: "int", nullable: false),
                    ClothSize = table.Column<int>(type: "int", nullable: false),
                    ClothTargetUser = table.Column<int>(type: "int", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DamagedNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    MyProperty = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassificationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassificationItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassificationItems_IntakeBatches_IntakeBatchId",
                        column: x => x.IntakeBatchId,
                        principalTable: "IntakeBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_IntakeBatchId",
                table: "InventoryTransactions",
                column: "IntakeBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationItems_CategoryId",
                table: "ClassificationItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationItems_IntakeBatchId",
                table: "ClassificationItems",
                column: "IntakeBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Carts_CartId",
                table: "CartItems",
                column: "CartId",
                principalTable: "Carts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Profile_ProfileId",
                table: "CartItems",
                column: "ProfileId",
                principalTable: "Profile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Users_UserId",
                table: "Carts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DonationRequests_IntakeBatches_BatchId",
                table: "DonationRequests",
                column: "BatchId",
                principalTable: "IntakeBatches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DonationRequests_Warehouses_WarehouseId",
                table: "DonationRequests",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IntakeBatches_Warehouses_WarehouseId",
                table: "IntakeBatches",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Categories_CategoryId",
                table: "Inventories",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_IntakeBatches_IntakeBatchId",
                table: "InventoryTransactions",
                column: "IntakeBatchId",
                principalTable: "IntakeBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Inventories_InventoryId",
                table: "InventoryTransactions",
                column: "InventoryId",
                principalTable: "Inventories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PickupAssignments_DonationRequests_DonorRequestId",
                table: "PickupAssignments",
                column: "DonorRequestId",
                principalTable: "DonationRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PickupAssignments_Users_ReceivingStaffId",
                table: "PickupAssignments",
                column: "ReceivingStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Warehouses_WarehouseId",
                table: "Users",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }
    }
}
