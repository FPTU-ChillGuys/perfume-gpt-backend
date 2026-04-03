using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Remove_OrderID_ContactAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactAddresses_Orders_OrderId",
                table: "ContactAddresses");

            migrationBuilder.DropIndex(
                name: "IX_ContactAddresses_OrderId",
                table: "ContactAddresses");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "ContactAddresses");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ContactAddressId",
                table: "Orders",
                column: "ContactAddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ContactAddresses_ContactAddressId",
                table: "Orders",
                column: "ContactAddressId",
                principalTable: "ContactAddresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ContactAddresses_ContactAddressId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ContactAddressId",
                table: "Orders");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "ContactAddresses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ContactAddresses_OrderId",
                table: "ContactAddresses",
                column: "OrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContactAddresses_Orders_OrderId",
                table: "ContactAddresses",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
