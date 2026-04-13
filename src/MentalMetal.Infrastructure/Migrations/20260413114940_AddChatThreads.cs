using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalMetal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatThreads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatThreads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContextScopeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContextInitiativeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MessageCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Messages = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatThreads", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatThreads_UserId_Status",
                table: "ChatThreads",
                columns: new[] { "UserId", "Status" });

            // Supports ListForInitiativeAsync: WHERE UserId=@u AND ContextScopeType=@t AND ContextInitiativeId=@i.
            // Declared via raw SQL because the index spans a parent column (UserId) and owned-type
            // columns (ContextScopeType, ContextInitiativeId), which the snapshot cannot represent.
            migrationBuilder.Sql(
                "CREATE INDEX \"IX_ChatThreads_UserId_ContextScopeType_ContextInitiativeId\" " +
                "ON \"ChatThreads\" (\"UserId\", \"ContextScopeType\", \"ContextInitiativeId\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_ChatThreads_UserId_ContextScopeType_ContextInitiativeId\";");

            migrationBuilder.DropTable(
                name: "ChatThreads");
        }
    }
}
