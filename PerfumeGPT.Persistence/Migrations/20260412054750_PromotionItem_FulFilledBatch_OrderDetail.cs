using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PromotionItem_FulFilledBatch_OrderDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMemberOnly",
                table: "Vouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "FulfilledBatchId",
                table: "OrderDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PromotionDiscountAmount",
                table: "OrderDetails",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "PromotionItemId",
                table: "OrderDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_FulfilledBatchId",
                table: "OrderDetails",
                column: "FulfilledBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_PromotionItemId",
                table: "OrderDetails",
                column: "PromotionItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Batches_FulfilledBatchId",
                table: "OrderDetails",
                column: "FulfilledBatchId",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Promotions_PromotionItemId",
                table: "OrderDetails",
                column: "PromotionItemId",
                principalTable: "Promotions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Batches_FulfilledBatchId",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Promotions_PromotionItemId",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_FulfilledBatchId",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_PromotionItemId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "IsMemberOnly",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "FulfilledBatchId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "PromotionDiscountAmount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "PromotionItemId",
                table: "OrderDetails");
        }
    }
}
