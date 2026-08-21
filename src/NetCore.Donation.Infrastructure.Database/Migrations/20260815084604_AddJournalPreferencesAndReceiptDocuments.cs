using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetCore.Donation.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalPreferencesAndReceiptDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentContentType",
                table: "Receipts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentFileName",
                table: "Receipts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentGeneratedAtUtc",
                table: "Receipts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentObjectKey",
                table: "Receipts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DocumentSizeBytes",
                table: "Receipts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DoNotEmail",
                table: "Contacts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DoNotSms",
                table: "Contacts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Journals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Journals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_DocumentObjectKey",
                table: "Receipts",
                column: "DocumentObjectKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Journals");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_DocumentObjectKey",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "DocumentContentType",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "DocumentFileName",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "DocumentGeneratedAtUtc",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "DocumentObjectKey",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "DocumentSizeBytes",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "DoNotEmail",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "DoNotSms",
                table: "Contacts");
        }
    }
}
