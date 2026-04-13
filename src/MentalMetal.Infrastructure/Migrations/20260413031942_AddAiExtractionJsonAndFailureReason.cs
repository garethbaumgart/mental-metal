using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiExtractionJsonAndFailureReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AlterColumn doesn't generate USING clause; PostgreSQL requires it for text→jsonb cast.
            // USING NULL::jsonb discards existing text values atomically — no race window with concurrent writes.
            migrationBuilder.Sql("""ALTER TABLE "Captures" ALTER COLUMN "AiExtraction" TYPE jsonb USING NULL::jsonb""");

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Captures",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Captures");

            migrationBuilder.Sql("""ALTER TABLE "Captures" ALTER COLUMN "AiExtraction" TYPE text USING "AiExtraction"::text""");
        }
    }
}
