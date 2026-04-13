using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExtractionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExtractionStatus",
                table: "Captures",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtractionStatus",
                table: "Captures");
        }
    }
}
