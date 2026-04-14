using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaptureAudioFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CaptureType",
                table: "Captures",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "AudioBlobRef",
                table: "Captures",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AudioDiscardedAt",
                table: "Captures",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AudioDurationSeconds",
                table: "Captures",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioMimeType",
                table: "Captures",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranscriptionFailureReason",
                table: "Captures",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranscriptionStatus",
                table: "Captures",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotApplicable");

            migrationBuilder.CreateTable(
                name: "CaptureTranscriptSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartSeconds = table.Column<double>(type: "double precision", nullable: false),
                    EndSeconds = table.Column<double>(type: "double precision", nullable: false),
                    SpeakerLabel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LinkedPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CaptureId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaptureTranscriptSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaptureTranscriptSegments_Captures_CaptureId",
                        column: x => x.CaptureId,
                        principalTable: "Captures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaptureTranscriptSegments_CaptureId",
                table: "CaptureTranscriptSegments",
                column: "CaptureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaptureTranscriptSegments");

            migrationBuilder.DropColumn(
                name: "AudioBlobRef",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "AudioDiscardedAt",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "AudioDurationSeconds",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "AudioMimeType",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "TranscriptionFailureReason",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "TranscriptionStatus",
                table: "Captures");

            migrationBuilder.AlterColumn<string>(
                name: "CaptureType",
                table: "Captures",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);
        }
    }
}
