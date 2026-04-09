using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Update_Campaign_Promotion_Voucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_ProductVariants_ProductVariantId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "IsUsed",
                table: "UserVouchers");

            migrationBuilder.RenameColumn(
                name: "GuestEmailOrPhone",
                table: "UserVouchers",
                newName: "GuestIdentifier");

            migrationBuilder.RenameColumn(
                name: "ProductVariantId",
                table: "Promotions",
                newName: "TargetProductVariantId");

            migrationBuilder.RenameIndex(
                name: "IX_Promotions_ProductVariantId",
                table: "Promotions",
                newName: "IX_Promotions_TargetProductVariantId");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountAmount",
                table: "Vouchers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsagePerUser",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Promotions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "Promotions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "Promotions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_CategoryId",
                table: "Promotions",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_Categories_CategoryId",
                table: "Promotions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_ProductVariants_TargetProductVariantId",
                table: "Promotions",
                column: "TargetProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_Categories_CategoryId",
                table: "Promotions");

            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_ProductVariants_TargetProductVariantId",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_CategoryId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MaxDiscountAmount",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "MaxUsagePerUser",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "Promotions");

            migrationBuilder.RenameColumn(
                name: "GuestIdentifier",
                table: "UserVouchers",
                newName: "GuestEmailOrPhone");

            migrationBuilder.RenameColumn(
                name: "TargetProductVariantId",
                table: "Promotions",
                newName: "ProductVariantId");

            migrationBuilder.RenameIndex(
                name: "IX_Promotions_TargetProductVariantId",
                table: "Promotions",
                newName: "IX_Promotions_ProductVariantId");

            migrationBuilder.AddColumn<bool>(
                name: "IsUsed",
                table: "UserVouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_ProductVariants_ProductVariantId",
                table: "Promotions",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
