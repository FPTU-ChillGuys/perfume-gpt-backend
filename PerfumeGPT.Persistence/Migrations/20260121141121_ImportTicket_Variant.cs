using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImportTicket_Variant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportDetails_ProductVariants_ProductVariantId",
                table: "ImportDetails");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "ImportDetails");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportDetails_ProductVariants_ProductVariantId",
                table: "ImportDetails",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportDetails_ProductVariants_ProductVariantId",
                table: "ImportDetails");

            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                table: "ImportDetails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddForeignKey(
                name: "FK_ImportDetails_ProductVariants_ProductVariantId",
                table: "ImportDetails",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
