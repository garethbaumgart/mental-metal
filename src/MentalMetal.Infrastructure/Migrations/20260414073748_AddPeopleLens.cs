using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPeopleLens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DeferralReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AchievedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Observations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Tag = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OccurredAt = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceCaptureId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OneOnOnes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    MoodRating = table.Column<int>(type: "integer", nullable: true),
                    Topics = table.Column<string[]>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneOnOnes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoalCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalCheckIns_Goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OneOnOneActionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    OneOnOneId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneOnOneActionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneOnOneActionItems_OneOnOnes_OneOnOneId",
                        column: x => x.OneOnOneId,
                        principalTable: "OneOnOnes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OneOnOneFollowUps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Resolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    OneOnOneId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneOnOneFollowUps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneOnOneFollowUps_OneOnOnes_OneOnOneId",
                        column: x => x.OneOnOneId,
                        principalTable: "OneOnOnes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoalCheckIns_GoalId",
                table: "GoalCheckIns",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_PersonId",
                table: "Goals",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_Status",
                table: "Goals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_UserId",
                table: "Goals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_PersonId",
                table: "Observations",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_Tag",
                table: "Observations",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_UserId",
                table: "Observations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OneOnOneActionItems_OneOnOneId",
                table: "OneOnOneActionItems",
                column: "OneOnOneId");

            migrationBuilder.CreateIndex(
                name: "IX_OneOnOneFollowUps_OneOnOneId",
                table: "OneOnOneFollowUps",
                column: "OneOnOneId");

            migrationBuilder.CreateIndex(
                name: "IX_OneOnOnes_PersonId",
                table: "OneOnOnes",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_OneOnOnes_UserId",
                table: "OneOnOnes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoalCheckIns");

            migrationBuilder.DropTable(
                name: "Observations");

            migrationBuilder.DropTable(
                name: "OneOnOneActionItems");

            migrationBuilder.DropTable(
                name: "OneOnOneFollowUps");

            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.DropTable(
                name: "OneOnOnes");
        }
    }
}
