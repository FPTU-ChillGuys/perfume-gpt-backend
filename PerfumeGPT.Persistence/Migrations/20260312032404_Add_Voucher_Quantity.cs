using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Voucher_Quantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Vouchers_VoucherId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_VoucherId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "VoucherId",
                table: "Orders",
                newName: "UserVoucherId");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Vouchers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Vouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RemainingQuantity",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalQuantity",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "UserVouchers",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "GuestEmailOrPhone",
                table: "UserVouchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "UserVouchers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Code",
                table: "Vouchers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_OrderId",
                table: "UserVouchers",
                column: "OrderId",
                unique: true,
                filter: "[OrderId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_UserVouchers_Orders_OrderId",
                table: "UserVouchers",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserVouchers_Orders_OrderId",
                table: "UserVouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_Code",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_UserVouchers_OrderId",
                table: "UserVouchers");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "RemainingQuantity",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "TotalQuantity",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "GuestEmailOrPhone",
                table: "UserVouchers");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "UserVouchers");

            migrationBuilder.RenameColumn(
                name: "UserVoucherId",
                table: "Orders",
                newName: "VoucherId");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Vouchers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "UserVouchers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_VoucherId",
                table: "Orders",
                column: "VoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Vouchers_VoucherId",
                table: "Orders",
                column: "VoucherId",
                principalTable: "Vouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
