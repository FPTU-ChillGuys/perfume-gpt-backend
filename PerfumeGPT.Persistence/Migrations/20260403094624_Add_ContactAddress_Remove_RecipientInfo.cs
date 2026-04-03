using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_ContactAddress_Remove_RecipientInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShippingInfos_Orders_OrderId",
                table: "ShippingInfos");

            migrationBuilder.DropTable(
                name: "RecipientInfos");

            migrationBuilder.DropIndex(
                name: "IX_ShippingInfos_OrderId",
                table: "ShippingInfos");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "ShippingInfos");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "ShippingInfos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ContactAddressId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ForwardShippingId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PickupAddressId",
                table: "OrderReturnRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReturnShippingId",
                table: "OrderReturnRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContactAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DistrictId = table.Column<int>(type: "int", nullable: false),
                    DistrictName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WardCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WardName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProvinceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullAddress = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactAddresses_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ForwardShippingId",
                table: "Orders",
                column: "ForwardShippingId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturnRequests_PickupAddressId",
                table: "OrderReturnRequests",
                column: "PickupAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturnRequests_ReturnShippingId",
                table: "OrderReturnRequests",
                column: "ReturnShippingId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactAddresses_OrderId",
                table: "ContactAddresses",
                column: "OrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderReturnRequests_ContactAddresses_PickupAddressId",
                table: "OrderReturnRequests",
                column: "PickupAddressId",
                principalTable: "ContactAddresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderReturnRequests_ShippingInfos_ReturnShippingId",
                table: "OrderReturnRequests",
                column: "ReturnShippingId",
                principalTable: "ShippingInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ShippingInfos_ForwardShippingId",
                table: "Orders",
                column: "ForwardShippingId",
                principalTable: "ShippingInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderReturnRequests_ContactAddresses_PickupAddressId",
                table: "OrderReturnRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderReturnRequests_ShippingInfos_ReturnShippingId",
                table: "OrderReturnRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ShippingInfos_ForwardShippingId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "ContactAddresses");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ForwardShippingId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderReturnRequests_PickupAddressId",
                table: "OrderReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_OrderReturnRequests_ReturnShippingId",
                table: "OrderReturnRequests");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ShippingInfos");

            migrationBuilder.DropColumn(
                name: "ContactAddressId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ForwardShippingId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickupAddressId",
                table: "OrderReturnRequests");

            migrationBuilder.DropColumn(
                name: "ReturnShippingId",
                table: "OrderReturnRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "ShippingInfos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "RecipientInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DistrictId = table.Column<int>(type: "int", nullable: false),
                    DistrictName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProvinceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecipientPhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WardCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WardName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipientInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipientInfos_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingInfos_OrderId",
                table: "ShippingInfos",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipientInfos_OrderId",
                table: "RecipientInfos",
                column: "OrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingInfos_Orders_OrderId",
                table: "ShippingInfos",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
