using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommitmentSourceOffsets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceEndOffset",
                table: "Commitments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceStartOffset",
                table: "Commitments",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceEndOffset",
                table: "Commitments");

            migrationBuilder.DropColumn(
                name: "SourceStartOffset",
                table: "Commitments");
        }
    }
}
