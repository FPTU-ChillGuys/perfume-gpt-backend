using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemPolicies",
                columns: table => new
                {
                    PolicyCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HtmlContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemPolicies", x => x.PolicyCode);
                });

            migrationBuilder.InsertData(
                table: "SystemPolicies",
                columns: new[] { "PolicyCode", "HtmlContent", "LastUpdated", "Title" },
                values: new object[,]
                {
                    { "SHIPPING_RETURN", "<p>Chính sách freeship...</p>", new DateTime(2026, 4, 13, 10, 0, 0, 0, DateTimeKind.Utc), "Vận chuyển và đổi trả" },
                    { "USAGE_STORAGE", "<ul><li>Cách sử dụng được PerfumeGPT...</li></ul>", new DateTime(2026, 4, 13, 10, 0, 0, 0, DateTimeKind.Utc), "Sử dụng và bảo quản" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemPolicies");
        }
    }
}
