using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetCore.Donation.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalTransactionAndReceiptSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing journals have no transaction; required FK cannot backfill Guid.Empty.
            migrationBuilder.Sql("""DELETE FROM "Journals";""");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentScheduleId",
                table: "Receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionId",
                table: "Journals",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_PaymentScheduleId",
                table: "Receipts",
                column: "PaymentScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Journals_TransactionId",
                table: "Journals",
                column: "TransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Journals_Transactions_TransactionId",
                table: "Journals",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_PaymentSchedules_PaymentScheduleId",
                table: "Receipts",
                column: "PaymentScheduleId",
                principalTable: "PaymentSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Journals_Transactions_TransactionId",
                table: "Journals");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_PaymentSchedules_PaymentScheduleId",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_PaymentScheduleId",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_Journals_TransactionId",
                table: "Journals");

            migrationBuilder.DropColumn(
                name: "PaymentScheduleId",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "Journals");
        }
    }
}
