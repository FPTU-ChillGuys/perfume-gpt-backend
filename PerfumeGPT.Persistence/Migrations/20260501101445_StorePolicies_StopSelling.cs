using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StorePolicies_StopSelling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StopSellingBeforeExpiryDays",
                table: "StorePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "StorePolicies",
                keyColumn: "Id",
                keyValue: new Guid("f6c2a71d-a76c-43cf-8f1f-315766251001"),
                column: "StopSellingBeforeExpiryDays",
                value: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StopSellingBeforeExpiryDays",
                table: "StorePolicies");
        }
    }
}
