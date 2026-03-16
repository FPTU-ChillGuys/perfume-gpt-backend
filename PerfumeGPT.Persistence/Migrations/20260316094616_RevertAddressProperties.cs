using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RevertAddressProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "RecipientInfos",
                newName: "RecipientPhoneNumber");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "RecipientInfos",
                newName: "RecipientName");

            migrationBuilder.AddColumn<string>(
                name: "RecipientName",
                table: "Addresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecipientPhoneNumber",
                table: "Addresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecipientName",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "RecipientPhoneNumber",
                table: "Addresses");

            migrationBuilder.RenameColumn(
                name: "RecipientPhoneNumber",
                table: "RecipientInfos",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "RecipientName",
                table: "RecipientInfos",
                newName: "FullName");
        }
    }
}
