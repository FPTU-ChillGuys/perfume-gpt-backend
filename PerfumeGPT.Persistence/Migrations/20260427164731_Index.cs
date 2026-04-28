using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "UserVouchers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "GuestIdentifier",
                table: "UserVouchers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ProductVariants",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "ProductVariants",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "ProductVariants",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                table: "PaymentTransactions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionStatus",
                table: "PaymentTransactions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Orders",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Orders",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Campaigns",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_CampaignId_ExpiryDate_IsDeleted",
                table: "Vouchers",
                columns: new[] { "CampaignId", "ExpiryDate", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_ExpiryDate_IsDeleted",
                table: "Vouchers",
                columns: new[] { "ExpiryDate", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_IsDeleted",
                table: "Vouchers",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VariantSuppliers_ProductVariantId",
                table: "VariantSuppliers",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_GuestIdentifier_UserId",
                table: "UserVouchers",
                columns: new[] { "GuestIdentifier", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_UserId_Status",
                table: "UserVouchers",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_VoucherId_UserId",
                table: "UserVouchers",
                columns: new[] { "VoucherId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationReads_UserId",
                table: "UserNotificationReads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_OrderId",
                table: "StockReservations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_IsDeleted",
                table: "StockAdjustments",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_IsDeleted",
                table: "Reviews",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_CampaignId_IsActive_IsDeleted",
                table: "Promotions",
                columns: new[] { "CampaignId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_IsDeleted",
                table: "Promotions",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_TargetProductVariantId_IsActive_IsDeleted",
                table: "Promotions",
                columns: new[] { "TargetProductVariantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Barcode",
                table: "ProductVariants",
                column: "Barcode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_IsDeleted",
                table: "ProductVariants",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_Status_IsDeleted",
                table: "ProductVariants",
                columns: new[] { "ProductId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Sku",
                table: "ProductVariants",
                column: "Sku",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted",
                table: "Products",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CreatedAt_TransactionType_TransactionStatus",
                table: "PaymentTransactions",
                columns: new[] { "CreatedAt", "TransactionType", "TransactionStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Code",
                table: "Orders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_Status_CreatedAt",
                table: "Orders",
                columns: new[] { "CustomerId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentStatus_PaymentExpiresAt",
                table: "Orders",
                columns: new[] { "PaymentStatus", "PaymentExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StaffId_Status_CreatedAt",
                table: "Orders",
                columns: new[] { "StaffId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_CreatedAt",
                table: "Orders",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturnRequestDetails_ReturnRequestId",
                table: "OrderReturnRequestDetails",
                column: "ReturnRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Media_IsDeleted",
                table: "Media",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ImportTickets_IsDeleted",
                table: "ImportTickets",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_IsDeleted",
                table: "Campaigns",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_Status_IsDeleted_StartDate_EndDate",
                table: "Campaigns",
                columns: new[] { "Status", "IsDeleted", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserTokens_UserId",
                table: "AspNetUserTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IsDeleted",
                table: "AspNetUsers",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_UserId",
                table: "AspNetUserRoles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vouchers_CampaignId_ExpiryDate_IsDeleted",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_ExpiryDate_IsDeleted",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_IsDeleted",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_VariantSuppliers_ProductVariantId",
                table: "VariantSuppliers");

            migrationBuilder.DropIndex(
                name: "IX_UserVouchers_GuestIdentifier_UserId",
                table: "UserVouchers");

            migrationBuilder.DropIndex(
                name: "IX_UserVouchers_UserId_Status",
                table: "UserVouchers");

            migrationBuilder.DropIndex(
                name: "IX_UserVouchers_VoucherId_UserId",
                table: "UserVouchers");

            migrationBuilder.DropIndex(
                name: "IX_UserNotificationReads_UserId",
                table: "UserNotificationReads");

            migrationBuilder.DropIndex(
                name: "IX_StockReservations_OrderId",
                table: "StockReservations");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustments_IsDeleted",
                table: "StockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_IsDeleted",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_CampaignId_IsActive_IsDeleted",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_IsDeleted",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_TargetProductVariantId_IsActive_IsDeleted",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_Barcode",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_IsDeleted",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_Status_IsDeleted",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_Sku",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_CreatedAt_TransactionType_TransactionStatus",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Code",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId_Status_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentStatus_PaymentExpiresAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_StaffId_Status_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderReturnRequestDetails_ReturnRequestId",
                table: "OrderReturnRequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_Media_IsDeleted",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_ImportTickets_IsDeleted",
                table: "ImportTickets");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_IsDeleted",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_Status_IsDeleted_StartDate_EndDate",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserTokens_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IsDeleted",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "UserVouchers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "GuestIdentifier",
                table: "UserVouchers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                table: "PaymentTransactions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionStatus",
                table: "PaymentTransactions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
