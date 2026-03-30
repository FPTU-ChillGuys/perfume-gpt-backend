using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Remove_Return_Order_Details : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderReturnRequestDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderReturnRequestDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRestocked = table.Column<bool>(type: "bit", nullable: true),
                    ReturnedQuantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderReturnRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderReturnRequestDetails_OrderDetails_OrderDetailId",
                        column: x => x.OrderDetailId,
                        principalTable: "OrderDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderReturnRequestDetails_OrderReturnRequests_ReturnRequestId",
                        column: x => x.ReturnRequestId,
                        principalTable: "OrderReturnRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturnRequestDetails_OrderDetailId",
                table: "OrderReturnRequestDetails",
                column: "OrderDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturnRequestDetails_ReturnRequestId_OrderDetailId",
                table: "OrderReturnRequestDetails",
                columns: new[] { "ReturnRequestId", "OrderDetailId" },
                unique: true);
        }
    }
}
