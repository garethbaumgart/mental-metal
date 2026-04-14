using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Interviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidatePersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Stage = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Decision = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Transcript_RawText = table.Column<string>(type: "text", nullable: true),
                    Transcript_Summary = table.Column<string>(type: "text", nullable: true),
                    Transcript_RecommendedDecision = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Transcript_RiskSignals = table.Column<string>(type: "jsonb", nullable: true),
                    Transcript_AnalyzedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Transcript_Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterviewScorecards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Competency = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InterviewId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewScorecards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewScorecards_Interviews_InterviewId",
                        column: x => x.InterviewId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_CandidatePersonId",
                table: "Interviews",
                column: "CandidatePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_UserId",
                table: "Interviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewScorecards_InterviewId",
                table: "InterviewScorecards",
                column: "InterviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewScorecards");

            migrationBuilder.DropTable(
                name: "Interviews");
        }
    }
}
