using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2_ReclassifyCandidatePersonType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The V2 reshape removed the Candidate person type from the C# enum
            // but left existing rows with Type = 'Candidate' in the database.
            // Reclassify them to 'External' — the closest remaining type.
            migrationBuilder.Sql(
                "UPDATE \"People\" SET \"Type\" = 'External' WHERE \"Type\" = 'Candidate'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible — we cannot determine which External rows were formerly Candidate.
        }
    }
}
