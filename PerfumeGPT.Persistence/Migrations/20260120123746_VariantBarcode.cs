using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VariantBarcode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "RecipientInfos");

            migrationBuilder.AddColumn<int>(
                name: "DistrictId",
                table: "RecipientInfos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FullAddress",
                table: "RecipientInfos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WardCode",
                table: "RecipientInfos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "RecipientInfos");

            migrationBuilder.DropColumn(
                name: "FullAddress",
                table: "RecipientInfos");

            migrationBuilder.DropColumn(
                name: "WardCode",
                table: "RecipientInfos");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "ProductVariants");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "RecipientInfos",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
