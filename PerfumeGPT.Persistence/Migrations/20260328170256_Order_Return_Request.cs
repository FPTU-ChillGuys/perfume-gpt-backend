using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Order_Return_Request : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrderReturnRequestId",
                table: "Media",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderReturnRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InspectedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StaffNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InspectionNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedRefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedRefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsRefunded = table.Column<bool>(type: "bit", nullable: false),
                    VnpTransactionNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRestocked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderReturnRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderReturnRequests_AspNetUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderReturnRequests_AspNetUsers_InspectedById",
                        column: x => x.InspectedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderReturnRequests_AspNetUsers_ProcessedById",
                        column: x => x.ProcessedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderReturnRequests_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderReturnRequestDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnedQuantity = table.Column<int>(type: "int", nullable: false),
                    IsRestocked = table.Column<bool>(type: "bit", nullable: true),
                    InspectionNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "IX_Media_EntityType_OrderReturnRequestId",
                table: "Media",
                columns: new[] { "EntityType", "OrderReturnRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_Media_OrderReturnRequestId",
                table: "Media",
                column: "OrderReturnRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturnRequestDetails_OrderDetailId",
                table: "OrderReturnRequestDetails",
                column: "OrderDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturnRequestDetails_ReturnRequestId_OrderDetailId",
                table: "OrderReturnRequestDetails",
                columns: new[] { "ReturnRequestId", "OrderDetailId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturnRequests_CustomerId",
                table: "OrderReturnRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturnRequests_InspectedById",
                table: "OrderReturnRequests",
                column: "InspectedById");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturnRequests_OrderId",
                table: "OrderReturnRequests",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturnRequests_ProcessedById",
                table: "OrderReturnRequests",
                column: "ProcessedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_OrderReturnRequests_OrderReturnRequestId",
                table: "Media",
                column: "OrderReturnRequestId",
                principalTable: "OrderReturnRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Media_OrderReturnRequests_OrderReturnRequestId",
                table: "Media");

            migrationBuilder.DropTable(
                name: "OrderReturnRequestDetails");

            migrationBuilder.DropTable(
                name: "OrderReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_Media_EntityType_OrderReturnRequestId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_OrderReturnRequestId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "OrderReturnRequestId",
                table: "Media");
        }
    }
}
