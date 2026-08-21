using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetCore.Donation.Infrastructure.Database;

#nullable disable

namespace NetCore.Donation.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDatabaseContext))]
    [Migration("20260817010000_AddRecordIdentifiers")]
    public partial class AddRecordIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Identifier",
                table: "PaymentSchedules",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Identifier",
                table: "Transactions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Identifier",
                table: "Journals",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Identifier",
                table: "Receipts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "PaymentSchedules"
                SET "Identifier" = 'PS-' || to_char("BookDate", 'YYYYMMDD') || '-' || substr(replace("Id"::text, '-', ''), 1, 8);

                UPDATE "Transactions"
                SET "Identifier" = 'TXN-' || to_char("BookDate", 'YYYYMMDD') || '-' || substr(replace("Id"::text, '-', ''), 1, 8);

                UPDATE "Journals"
                SET "Identifier" = 'JN-' || to_char(("CreatedDate" AT TIME ZONE 'UTC'), 'YYYYMMDD') || '-' || substr(replace("Id"::text, '-', ''), 1, 8);

                UPDATE "Receipts"
                SET "Identifier" = 'RC-' || to_char(("CreatedDate" AT TIME ZONE 'UTC'), 'YYYYMMDD') || '-' || substr(replace("Id"::text, '-', ''), 1, 8);
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Identifier",
                table: "PaymentSchedules",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Identifier",
                table: "Transactions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Identifier",
                table: "Journals",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Identifier",
                table: "Receipts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSchedules_Identifier",
                table: "PaymentSchedules",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Identifier",
                table: "Transactions",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Journals_Identifier",
                table: "Journals",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_Identifier",
                table: "Receipts",
                column: "Identifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentSchedules_Identifier",
                table: "PaymentSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Identifier",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Journals_Identifier",
                table: "Journals");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_Identifier",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "Identifier",
                table: "PaymentSchedules");

            migrationBuilder.DropColumn(
                name: "Identifier",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Identifier",
                table: "Journals");

            migrationBuilder.DropColumn(
                name: "Identifier",
                table: "Receipts");
        }
    }
}
