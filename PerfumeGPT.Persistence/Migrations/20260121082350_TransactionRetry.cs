using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TransactionRetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_OrderId",
                table: "PaymentTransactions");

            migrationBuilder.AlterColumn<string>(
                name: "CarrierName",
                table: "ShippingInfos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalPaymentId",
                table: "PaymentTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryAttempt",
                table: "PaymentTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_OrderId",
                table: "PaymentTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_OriginalPaymentId",
                table: "PaymentTransactions",
                column: "OriginalPaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_PaymentTransactions_OriginalPaymentId",
                table: "PaymentTransactions",
                column: "OriginalPaymentId",
                principalTable: "PaymentTransactions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_PaymentTransactions_OriginalPaymentId",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_OrderId",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_OriginalPaymentId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "OriginalPaymentId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "RetryAttempt",
                table: "PaymentTransactions");

            migrationBuilder.AlterColumn<string>(
                name: "CarrierName",
                table: "ShippingInfos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_OrderId",
                table: "PaymentTransactions",
                column: "OrderId",
                unique: true);
        }
    }
}
