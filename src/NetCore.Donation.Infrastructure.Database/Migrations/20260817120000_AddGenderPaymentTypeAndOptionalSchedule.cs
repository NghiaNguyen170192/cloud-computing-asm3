using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetCore.Donation.Infrastructure.Database;

#nullable disable

namespace NetCore.Donation.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDatabaseContext))]
    [Migration("20260817120000_AddGenderPaymentTypeAndOptionalSchedule")]
    public partial class AddGenderPaymentTypeAndOptionalSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Contacts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "PaymentMethods",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Bank");

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "PaymentSchedules",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Bank");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_PaymentSchedules_PaymentScheduleId",
                table: "Transactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "PaymentScheduleId",
                table: "Transactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_PaymentSchedules_PaymentScheduleId",
                table: "Transactions",
                column: "PaymentScheduleId",
                principalTable: "PaymentSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_PaymentSchedules_PaymentScheduleId",
                table: "Transactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "PaymentScheduleId",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_PaymentSchedules_PaymentScheduleId",
                table: "Transactions",
                column: "PaymentScheduleId",
                principalTable: "PaymentSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "PaymentSchedules");
        }
    }
}
