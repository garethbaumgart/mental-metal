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
            // Clear any existing non-JSON text values before converting to jsonb
            migrationBuilder.Sql("""UPDATE "Captures" SET "AiExtraction" = NULL WHERE "AiExtraction" IS NOT NULL""");

            // AlterColumn doesn't generate USING clause; PostgreSQL requires it for text→jsonb cast
            migrationBuilder.Sql("""ALTER TABLE "Captures" ALTER COLUMN "AiExtraction" TYPE jsonb USING "AiExtraction"::jsonb""");

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
