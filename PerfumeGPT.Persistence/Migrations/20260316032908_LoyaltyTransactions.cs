using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LoyaltyTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoyaltyPoints");

            migrationBuilder.RenameColumn(
                name: "RejectQuantity",
                table: "ImportDetails",
                newName: "RejectedQuantity");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "ImportDetails",
                newName: "ExpectedQuantity");

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedDate",
                table: "ShippingInfos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "CustomerProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PointBalance",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LoyaltyTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PointsChanged = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("09097277-2705-40c2-bce5-51dbd1f4c1e6"),
                column: "PointBalance",
                value: 0);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("09097277-5555-40c2-bce5-51dbd1f4c1e6"),
                column: "PointBalance",
                value: 0);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("33f41895-b601-4aa1-8dc4-8229a9d07008"),
                column: "PointBalance",
                value: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_OrderId",
                table: "LoyaltyTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_UserId",
                table: "LoyaltyTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_VoucherId",
                table: "LoyaltyTransactions",
                column: "VoucherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoyaltyTransactions");

            migrationBuilder.DropColumn(
                name: "ShippedDate",
                table: "ShippingInfos");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "CustomerProfiles");

            migrationBuilder.DropColumn(
                name: "PointBalance",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "RejectedQuantity",
                table: "ImportDetails",
                newName: "RejectQuantity");

            migrationBuilder.RenameColumn(
                name: "ExpectedQuantity",
                table: "ImportDetails",
                newName: "Quantity");

            migrationBuilder.CreateTable(
                name: "LoyaltyPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PointBalance = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyPoints_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPoints_UserId",
                table: "LoyaltyPoints",
                column: "UserId",
                unique: true);
        }
    }
}
