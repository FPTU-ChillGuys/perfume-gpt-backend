using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Customer_Refund_Info : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VnpTransactionNo",
                table: "OrderReturnRequests",
                newName: "RefundTransactionReference");

            migrationBuilder.RenameColumn(
                name: "VnpTransactionNo",
                table: "OrderCancelRequests",
                newName: "RefundTransactionReference");

            migrationBuilder.AddColumn<string>(
                name: "RefundAccountName",
                table: "OrderReturnRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundAccountNumber",
                table: "OrderReturnRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundBankName",
                table: "OrderReturnRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundAccountName",
                table: "OrderCancelRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundAccountNumber",
                table: "OrderCancelRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundBankName",
                table: "OrderCancelRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundAccountName",
                table: "OrderReturnRequests");

            migrationBuilder.DropColumn(
                name: "RefundAccountNumber",
                table: "OrderReturnRequests");

            migrationBuilder.DropColumn(
                name: "RefundBankName",
                table: "OrderReturnRequests");

            migrationBuilder.DropColumn(
                name: "RefundAccountName",
                table: "OrderCancelRequests");

            migrationBuilder.DropColumn(
                name: "RefundAccountNumber",
                table: "OrderCancelRequests");

            migrationBuilder.DropColumn(
                name: "RefundBankName",
                table: "OrderCancelRequests");

            migrationBuilder.RenameColumn(
                name: "RefundTransactionReference",
                table: "OrderReturnRequests",
                newName: "VnpTransactionNo");

            migrationBuilder.RenameColumn(
                name: "RefundTransactionReference",
                table: "OrderCancelRequests",
                newName: "VnpTransactionNo");
        }
    }
}
