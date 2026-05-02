using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Remove_OrderDetails_Batches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Batches_FulfilledBatchId",
                table: "OrderDetails");

            migrationBuilder.RenameColumn(
                name: "FulfilledBatchId",
                table: "OrderDetails",
                newName: "BatchId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderDetails_FulfilledBatchId",
                table: "OrderDetails",
                newName: "IX_OrderDetails_BatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Batches_BatchId",
                table: "OrderDetails",
                column: "BatchId",
                principalTable: "Batches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Batches_BatchId",
                table: "OrderDetails");

            migrationBuilder.RenameColumn(
                name: "BatchId",
                table: "OrderDetails",
                newName: "FulfilledBatchId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderDetails_BatchId",
                table: "OrderDetails",
                newName: "IX_OrderDetails_FulfilledBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Batches_FulfilledBatchId",
                table: "OrderDetails",
                column: "FulfilledBatchId",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
