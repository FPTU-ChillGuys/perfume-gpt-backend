using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Updat_Media_ForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Media_ProductVariants_EntityId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Products_EntityId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_EntityId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_EntityType_EntityId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Media");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "Media",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductVariantId",
                table: "Media",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Media_EntityType_ProductId",
                table: "Media",
                columns: new[] { "EntityType", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Media_EntityType_ProductVariantId",
                table: "Media",
                columns: new[] { "EntityType", "ProductVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Media_ProductId",
                table: "Media",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Media_ProductVariantId",
                table: "Media",
                column: "ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_ProductVariants_ProductVariantId",
                table: "Media",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Products_ProductId",
                table: "Media",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Media_ProductVariants_ProductVariantId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Products_ProductId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_EntityType_ProductId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_EntityType_ProductVariantId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_ProductId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_ProductVariantId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "Media");

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "Media",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Media_EntityId",
                table: "Media",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Media_EntityType_EntityId",
                table: "Media",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Media_ProductVariants_EntityId",
                table: "Media",
                column: "EntityId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Products_EntityId",
                table: "Media",
                column: "EntityId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
