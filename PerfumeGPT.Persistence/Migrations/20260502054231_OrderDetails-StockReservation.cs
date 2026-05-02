using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderDetailsStockReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrderDetailId",
                table: "StockReservations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_OrderDetailId",
                table: "StockReservations",
                column: "OrderDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockReservations_OrderDetails_OrderDetailId",
                table: "StockReservations",
                column: "OrderDetailId",
                principalTable: "OrderDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockReservations_OrderDetails_OrderDetailId",
                table: "StockReservations");

            migrationBuilder.DropIndex(
                name: "IX_StockReservations_OrderDetailId",
                table: "StockReservations");

            migrationBuilder.DropColumn(
                name: "OrderDetailId",
                table: "StockReservations");
        }
    }
}
