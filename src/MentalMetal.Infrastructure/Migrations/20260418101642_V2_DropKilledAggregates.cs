using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2_DropKilledAggregates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Briefings");

            migrationBuilder.DropTable(
                name: "ChatThreads");

            migrationBuilder.DropTable(
                name: "DailyCloseOutLogs");

            migrationBuilder.DropTable(
                name: "Delegations");

            migrationBuilder.DropTable(
                name: "GoalCheckIns");

            migrationBuilder.DropTable(
                name: "InterviewScorecards");

            migrationBuilder.DropTable(
                name: "Nudges");

            migrationBuilder.DropTable(
                name: "Observations");

            migrationBuilder.DropTable(
                name: "OneOnOneActionItems");

            migrationBuilder.DropTable(
                name: "OneOnOneFollowUps");

            migrationBuilder.DropTable(
                name: "PendingBriefUpdates");

            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.DropTable(
                name: "Interviews");

            migrationBuilder.DropTable(
                name: "OneOnOnes");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("V2 product pivot migration is forward-only and cannot be rolled back.");
        }
    }
}
