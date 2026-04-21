using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <summary>
    /// Intentionally empty — DetectedCaptureType is a new property on the AiExtraction
    /// value object, which is persisted as JSONB via ToJson(). No schema change is needed;
    /// this migration exists only to update the EF Core model snapshot.
    /// </summary>
    public partial class DetectedCaptureTypeOnAiExtraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No schema change — AiExtraction is stored as JSONB; new fields appear automatically.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No schema change to revert.
        }
    }
}
