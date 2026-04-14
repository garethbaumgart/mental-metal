using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyCloseOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExtractionResolved",
                table: "Captures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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
                name: "DailyCloseOutLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedCount = table.Column<int>(type: "integer", nullable: false),
                    DiscardedCount = table.Column<int>(type: "integer", nullable: false),
                    RemainingCount = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyCloseOutLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyCloseOutLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Captures_UserId_Triaged",
                table: "Captures",
                columns: new[] { "UserId", "Triaged" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyCloseOutLogs_UserId_Date",
                table: "DailyCloseOutLogs",
                columns: new[] { "UserId", "Date" },
                unique: true);

            // Backfill: any capture not in the 'Processed' state has no extraction to resolve,
            // so mark it resolved so the close-out queue does not surface historical captures.
            migrationBuilder.Sql(
                "UPDATE \"Captures\" SET \"ExtractionResolved\" = TRUE WHERE \"ProcessingStatus\" <> 'Processed';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyCloseOutLogs");

            migrationBuilder.DropIndex(
                name: "IX_Captures_UserId_Triaged",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "ExtractionResolved",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "Triaged",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "TriagedAtUtc",
                table: "Captures");
        }
    }
}
