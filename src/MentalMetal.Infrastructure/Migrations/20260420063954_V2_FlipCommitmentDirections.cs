using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2_FlipCommitmentDirections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Commitments" SET "Direction" = CASE
                    WHEN "Direction" = 'MineToThem' THEN 'TheirsToMe'
                    WHEN "Direction" = 'TheirsToMe' THEN 'MineToThem'
                    ELSE "Direction"
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Commitments" SET "Direction" = CASE
                    WHEN "Direction" = 'MineToThem' THEN 'TheirsToMe'
                    WHEN "Direction" = 'TheirsToMe' THEN 'MineToThem'
                    ELSE "Direction"
                END
                """);
        }
    }
}
