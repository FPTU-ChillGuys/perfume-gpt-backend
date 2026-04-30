using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StorePolicies_Review_StockAdjust : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewRewardPoints",
                table: "StorePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockAdjustmentAutoApprovalThreshold",
                table: "StorePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "StorePolicies",
                keyColumn: "Id",
                keyValue: new Guid("f6c2a71d-a76c-43cf-8f1f-315766251001"),
                columns: new[] { "ReviewRewardPoints", "StockAdjustmentAutoApprovalThreshold" },
                values: new object[] { 50, 5 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewRewardPoints",
                table: "StorePolicies");

            migrationBuilder.DropColumn(
                name: "StockAdjustmentAutoApprovalThreshold",
                table: "StorePolicies");
        }
    }
}
