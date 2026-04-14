using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNudges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nudges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CadenceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CadenceCustomIntervalDays = table.Column<int>(type: "integer", nullable: true),
                    CadenceDayOfWeek = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CadenceDayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NextDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LastNudgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiativeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nudges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nudges_InitiativeId",
                table: "Nudges",
                column: "InitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_Nudges_IsActive",
                table: "Nudges",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Nudges_NextDueDate",
                table: "Nudges",
                column: "NextDueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Nudges_PersonId",
                table: "Nudges",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Nudges_UserId",
                table: "Nudges",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Nudges");
        }
    }
}
