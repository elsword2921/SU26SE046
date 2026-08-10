using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalTeamLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "OperationalTeams",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompletedByStaffId",
                table: "OperationalTeams",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "OperationalTeams",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StartedByStaffId",
                table: "OperationalTeams",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "OperationalTeams",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Scheduled");

            migrationBuilder.Sql("""
                UPDATE [team]
                SET [StartedAt] = (
                        SELECT MIN([batch].[StartedAt])
                        FROM [IntakeBatches] AS [batch]
                        WHERE [batch].[ReceivingTeamId] = [team].[Id] AND [batch].[IsActive] <> 0),
                    [CompletedAt] = (
                        SELECT MAX([batch].[CompletedAt])
                        FROM [IntakeBatches] AS [batch]
                        WHERE [batch].[ReceivingTeamId] = [team].[Id] AND [batch].[IsActive] <> 0),
                    [Status] = CASE
                        WHEN EXISTS (
                            SELECT 1 FROM [IntakeBatches] AS [batch]
                            WHERE [batch].[ReceivingTeamId] = [team].[Id]
                              AND [batch].[IsActive] <> 0
                              AND ([batch].[CompletedAt] IS NOT NULL OR [batch].[Status] IN ('Completed', 'SentToClassification')))
                            THEN 'Completed'
                        WHEN EXISTS (
                            SELECT 1 FROM [IntakeBatches] AS [batch]
                            WHERE [batch].[ReceivingTeamId] = [team].[Id]
                              AND [batch].[IsActive] <> 0
                              AND ([batch].[StartedAt] IS NOT NULL OR [batch].[Status] = 'Receiving'))
                            THEN 'InProgress'
                        ELSE 'Scheduled'
                    END
                FROM [OperationalTeams] AS [team];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "OperationalTeams");

            migrationBuilder.DropColumn(
                name: "CompletedByStaffId",
                table: "OperationalTeams");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "OperationalTeams");

            migrationBuilder.DropColumn(
                name: "StartedByStaffId",
                table: "OperationalTeams");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OperationalTeams");
        }
    }
}
