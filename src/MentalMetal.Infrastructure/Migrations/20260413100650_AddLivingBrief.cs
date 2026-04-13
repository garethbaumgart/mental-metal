using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLivingBrief : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PreferencesLivingBriefAutoApply",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BriefDesignDirectionHistory",
                table: "Initiatives",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "BriefKeyDecisions",
                table: "Initiatives",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "BriefRequirementsHistory",
                table: "Initiatives",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "BriefRisks",
                table: "Initiatives",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "BriefSummary",
                table: "Initiatives",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BriefSummaryLastRefreshedAt",
                table: "Initiatives",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BriefSummarySource",
                table: "Initiatives",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<Guid[]>(
                name: "BriefSummarySourceCaptureIds",
                table: "Initiatives",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AddColumn<int>(
                name: "BriefVersion",
                table: "Initiatives",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PendingBriefUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiativeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Proposal = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BriefVersionAtProposal = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingBriefUpdates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingBriefUpdates_UserId_InitiativeId_Status_CreatedAt",
                table: "PendingBriefUpdates",
                columns: new[] { "UserId", "InitiativeId", "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingBriefUpdates");

            migrationBuilder.DropColumn(
                name: "PreferencesLivingBriefAutoApply",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BriefDesignDirectionHistory",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "BriefKeyDecisions",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "BriefRequirementsHistory",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "BriefRisks",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "BriefSummary",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "BriefSummaryLastRefreshedAt",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "BriefSummarySource",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "BriefSummarySourceCaptureIds",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "BriefVersion",
                table: "Initiatives");
        }
    }
}
