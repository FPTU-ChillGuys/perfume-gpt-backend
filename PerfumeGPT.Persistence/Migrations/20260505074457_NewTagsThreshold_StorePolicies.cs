using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NewTagsThreshold_StorePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NewTagThresholdInDays",
                table: "StorePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "StorePolicies",
                keyColumn: "Id",
                keyValue: new Guid("f6c2a71d-a76c-43cf-8f1f-315766251001"),
                columns: new[] { "NewTagThresholdInDays", "ReturnOrderAllowanceInDays", "StopSellingBeforeExpiryDays" },
                values: new object[] { 30, 5, 7 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewTagThresholdInDays",
                table: "StorePolicies");

            migrationBuilder.UpdateData(
                table: "StorePolicies",
                keyColumn: "Id",
                keyValue: new Guid("f6c2a71d-a76c-43cf-8f1f-315766251001"),
                columns: new[] { "ReturnOrderAllowanceInDays", "StopSellingBeforeExpiryDays" },
                values: new object[] { 7, 30 });
        }
    }
}
