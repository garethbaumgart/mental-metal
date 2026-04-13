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

            migrationBuilder.AlterColumn<string>(
                name: "AiExtraction",
                table: "Captures",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

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

            migrationBuilder.AlterColumn<string>(
                name: "AiExtraction",
                table: "Captures",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);
        }
    }
}
