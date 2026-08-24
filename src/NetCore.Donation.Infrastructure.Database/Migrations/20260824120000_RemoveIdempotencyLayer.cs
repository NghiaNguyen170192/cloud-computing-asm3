using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetCore.Donation.Infrastructure.Database;

#nullable disable

namespace NetCore.Donation.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDatabaseContext))]
    [Migration("20260824120000_RemoveIdempotencyLayer")]
    public partial class RemoveIdempotencyLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyLogs");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_IdempotencyKey_MessageType",
                table: "OutboxMessages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdempotencyLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsExpired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RequestPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    RequestType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ResponseData = table.Column<string>(type: "text", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_IdempotencyKey_MessageType",
                table: "OutboxMessages",
                columns: new[] { "IdempotencyKey", "MessageType" },
                unique: true,
                filter: "\"IdempotencyKey\" <> ''");
        }
    }
}
