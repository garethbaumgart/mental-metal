using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Team = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CareerDetails_Level = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CareerDetails_Aspirations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CareerDetails_GrowthAreas = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CandidateDetails_PipelineStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CandidateDetails_CvNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CandidateDetails_SourceChannel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_People_UserId",
                table: "People",
                column: "UserId");

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "IX_People_UserId_LowerName"
                ON "People" ("UserId", lower("Name"))
                WHERE "IsArchived" = false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_People_UserId_LowerName";""");

            migrationBuilder.DropTable(
                name: "People");
        }
    }
}
