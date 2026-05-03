using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderReturnRequest_InStoreReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderReturnRequests_ContactAddresses_PickupAddressId",
                table: "OrderReturnRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "PickupAddressId",
                table: "OrderReturnRequests",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "IsReturnInStore",
                table: "OrderReturnRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderReturnRequests_ContactAddresses_PickupAddressId",
                table: "OrderReturnRequests",
                column: "PickupAddressId",
                principalTable: "ContactAddresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderReturnRequests_ContactAddresses_PickupAddressId",
                table: "OrderReturnRequests");

            migrationBuilder.DropColumn(
                name: "IsReturnInStore",
                table: "OrderReturnRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "PickupAddressId",
                table: "OrderReturnRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderReturnRequests_ContactAddresses_PickupAddressId",
                table: "OrderReturnRequests",
                column: "PickupAddressId",
                principalTable: "ContactAddresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
