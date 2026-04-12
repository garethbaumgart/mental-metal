using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaptures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Captures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawContent = table.Column<string>(type: "text", nullable: false),
                    CaptureType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProcessingStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AiExtraction = table.Column<string>(type: "text", nullable: true),
                    LinkedPersonIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    LinkedInitiativeIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    SpawnedCommitmentIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    SpawnedDelegationIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    SpawnedObservationIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Captures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Captures_UserId",
                table: "Captures",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Captures");
        }
    }
}
