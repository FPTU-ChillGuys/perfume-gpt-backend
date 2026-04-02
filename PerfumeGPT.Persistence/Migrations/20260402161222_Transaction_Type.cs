using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Transaction_Type : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_PaymentTransactions_OriginalPaymentId",
                table: "PaymentTransactions");

            migrationBuilder.AddColumn<string>(
                name: "GatewayTransactionNo",
                table: "PaymentTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "PaymentTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_PaymentTransactions_OriginalPaymentId",
                table: "PaymentTransactions",
                column: "OriginalPaymentId",
                principalTable: "PaymentTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_PaymentTransactions_OriginalPaymentId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "GatewayTransactionNo",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "PaymentTransactions");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_PaymentTransactions_OriginalPaymentId",
                table: "PaymentTransactions",
                column: "OriginalPaymentId",
                principalTable: "PaymentTransactions",
                principalColumn: "Id");
        }
    }
}
