using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetCore.Donation.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdempotencyLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RequestPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ResponseData = table.Column<string>(type: "text", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsExpired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyLog_CorrelationId_RequestType",
                table: "IdempotencyLogs",
                columns: new[] { "CorrelationId", "RequestType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyLog_Expiry",
                table: "IdempotencyLogs",
                columns: new[] { "ExpiresAt", "IsExpired" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyLogs");
        }
    }
}
