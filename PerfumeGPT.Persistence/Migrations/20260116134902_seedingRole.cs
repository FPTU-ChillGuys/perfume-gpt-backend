using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class seedingRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("3631e38b-60dd-4d1a-af7f-a26f21c2ef82"), "seed-1", "admin", "ADMIN" },
                    { new Guid("51ef7e08-ff07-459b-8c55-c7ebac505103"), "seed-2", "user", "USER" },
                    { new Guid("8f6e1c3d-2d3b-4f4a-9f4a-2e5d6c7b8a9b"), "seed-3", "staff", "STAFF" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("3631e38b-60dd-4d1a-af7f-a26f21c2ef82"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("51ef7e08-ff07-459b-8c55-c7ebac505103"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("8f6e1c3d-2d3b-4f4a-9f4a-2e5d6c7b8a9b"));
        }
    }
}
