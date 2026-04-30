using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StorePolicies_update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatchExpiringSoonThresholdInDays",
                table: "StorePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxAddressesPerUser",
                table: "StorePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrderRewardPointsInDays",
                table: "StorePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReturnOrderAllowanceInDays",
                table: "StorePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "StorePolicies",
                keyColumn: "Id",
                keyValue: new Guid("f6c2a71d-a76c-43cf-8f1f-315766251001"),
                columns: new[] { "BatchExpiringSoonThresholdInDays", "MaxAddressesPerUser", "OrderRewardPointsInDays", "ReturnOrderAllowanceInDays" },
                values: new object[] { 30, 5, 7, 7 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchExpiringSoonThresholdInDays",
                table: "StorePolicies");

            migrationBuilder.DropColumn(
                name: "MaxAddressesPerUser",
                table: "StorePolicies");

            migrationBuilder.DropColumn(
                name: "OrderRewardPointsInDays",
                table: "StorePolicies");

            migrationBuilder.DropColumn(
                name: "ReturnOrderAllowanceInDays",
                table: "StorePolicies");
        }
    }
}
