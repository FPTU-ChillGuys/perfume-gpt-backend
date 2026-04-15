using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Variant_Supplier_V2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariantSuppliers_ProductVariants_ProductVariantId",
                table: "VariantSuppliers");

            migrationBuilder.AddForeignKey(
                name: "FK_VariantSuppliers_ProductVariants_ProductVariantId",
                table: "VariantSuppliers",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariantSuppliers_ProductVariants_ProductVariantId",
                table: "VariantSuppliers");

            migrationBuilder.AddForeignKey(
                name: "FK_VariantSuppliers_ProductVariants_ProductVariantId",
                table: "VariantSuppliers",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
