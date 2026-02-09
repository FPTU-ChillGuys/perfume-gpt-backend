using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Product_Attributes_Value : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ValueId",
                table: "ProductAttributes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_ValueId",
                table: "ProductAttributes",
                column: "ValueId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductAttributes_AttributeValues_ValueId",
                table: "ProductAttributes",
                column: "ValueId",
                principalTable: "AttributeValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductAttributes_AttributeValues_ValueId",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_ValueId",
                table: "ProductAttributes");

            migrationBuilder.DropColumn(
                name: "ValueId",
                table: "ProductAttributes");
        }
    }
}
