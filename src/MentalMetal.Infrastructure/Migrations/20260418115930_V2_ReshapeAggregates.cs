using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2_ReshapeAggregates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InitiativeMilestones");

            migrationBuilder.DropIndex(
                name: "IX_Captures_UserId_Triaged",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "CandidateDetails_CvNotes",
                table: "People");

            migrationBuilder.DropColumn(
                name: "CandidateDetails_PipelineStatus",
                table: "People");

            migrationBuilder.DropColumn(
                name: "CandidateDetails_SourceChannel",
                table: "People");

            migrationBuilder.DropColumn(
                name: "CareerDetails_Aspirations",
                table: "People");

            migrationBuilder.DropColumn(
                name: "CareerDetails_GrowthAreas",
                table: "People");

            migrationBuilder.DropColumn(
                name: "CareerDetails_Level",
                table: "People");

            migrationBuilder.DropColumn(
                name: "LinkedPersonIds",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "ExtractionResolved",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "ExtractionStatus",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "SpawnedDelegationIds",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "SpawnedObservationIds",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "Triaged",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "TriagedAtUtc",
                table: "Captures");

            migrationBuilder.AddColumn<string[]>(
                name: "Aliases",
                table: "People",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "AutoSummary",
                table: "Initiatives",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSummaryRefreshedAt",
                table: "Initiatives",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Confidence",
                table: "Commitments",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "High");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DismissedAt",
                table: "Commitments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaptureSource",
                table: "Captures",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commitments_SourceCaptureId",
                table: "Commitments",
                column: "SourceCaptureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Commitments_SourceCaptureId",
                table: "Commitments");

            migrationBuilder.DropColumn(
                name: "Aliases",
                table: "People");

            migrationBuilder.DropColumn(
                name: "AutoSummary",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "LastSummaryRefreshedAt",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "Commitments");

            migrationBuilder.DropColumn(
                name: "DismissedAt",
                table: "Commitments");

            migrationBuilder.DropColumn(
                name: "CaptureSource",
                table: "Captures");

            migrationBuilder.AddColumn<string>(
                name: "CandidateDetails_CvNotes",
                table: "People",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidateDetails_PipelineStatus",
                table: "People",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidateDetails_SourceChannel",
                table: "People",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CareerDetails_Aspirations",
                table: "People",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CareerDetails_GrowthAreas",
                table: "People",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CareerDetails_Level",
                table: "People",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid[]>(
                name: "LinkedPersonIds",
                table: "Initiatives",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AddColumn<bool>(
                name: "ExtractionResolved",
                table: "Captures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExtractionStatus",
                table: "Captures",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Captures",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid[]>(
                name: "SpawnedDelegationIds",
                table: "Captures",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AddColumn<Guid[]>(
                name: "SpawnedObservationIds",
                table: "Captures",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AddColumn<bool>(
                name: "Triaged",
                table: "Captures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TriagedAtUtc",
                table: "Captures",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InitiativeMilestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InitiativeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InitiativeMilestones_Initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalTable: "Initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Captures_UserId_Triaged",
                table: "Captures",
                columns: new[] { "UserId", "Triaged" });

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeMilestones_InitiativeId",
                table: "InitiativeMilestones",
                column: "InitiativeId");
        }
    }
}
