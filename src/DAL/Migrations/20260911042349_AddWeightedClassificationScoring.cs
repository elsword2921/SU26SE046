using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightedClassificationScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "ConditionQuestions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "ScoringSnapshot",
                table: "ClassifiedItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightedScore",
                table: "ClassifiedItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClassificationScoringRule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    GradeAMinimum = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GradeBMinimum = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassificationScoringRule", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ClassificationScoringRule",
                columns: new[] { "Id", "GradeAMinimum", "GradeBMinimum", "UpdatedAt" },
                values: new object[] { 1, 85m, 50m, null });
            migrationBuilder.Sql("""
                UPDATE ConditionQuestions SET Weight = CASE
                    WHEN QuestionText LIKE N'%Tình trạng vải%' THEN 35
                    WHEN QuestionText LIKE N'%Bề mặt%' THEN 20
                    WHEN QuestionText LIKE N'%Phụ kiện%' THEN 15
                    WHEN QuestionText LIKE N'%Vệ sinh%' THEN 30
                    ELSE 1 END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassificationScoringRule");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "ConditionQuestions");

            migrationBuilder.DropColumn(
                name: "ScoringSnapshot",
                table: "ClassifiedItems");

            migrationBuilder.DropColumn(
                name: "WeightedScore",
                table: "ClassifiedItems");
        }
    }
}
