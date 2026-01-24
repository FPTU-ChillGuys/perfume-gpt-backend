using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedingUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedAt", "DeletedAt", "Email", "EmailConfirmed", "FullName", "IsActive", "IsDeleted", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "ProfilePictureUrl", "SecurityStamp", "TwoFactorEnabled", "UpdatedAt", "UserName" },
                values: new object[,]
                {
                    { new Guid("09097277-2705-40c2-bce5-51dbd1f4c1e6"), 0, "seed-7", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "user@example.com", true, "", true, false, false, null, "USER@EXAMPLE.COM", "USER", "AQAAAAIAAYagAAAAEJNw8/gF2YQZvKjKHqB0v7FmXqKhWU5nf0K9w8xYzJ5L3qW8dN2mR1pQ4vT7sA==", null, false, null, "seed-6", false, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "user" },
                    { new Guid("09097277-5555-40c2-bce5-51dbd1f4c1e6"), 0, "seed-9", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "staff@example.com", true, "", true, false, false, null, "STAFF@EXAMPLE.COM", "STAFF", "AQAAAAIAAYagAAAAEJNw8/gF2YQZvKjKHqB0v7FmXqKhWU5nf0K9w8xYzJ5L3qW8dN2mR1pQ4vT7sA==", null, false, null, "seed-8", false, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "staff" },
                    { new Guid("33f41895-b601-4aa1-8dc4-8229a9d07008"), 0, "seed-5", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "admin@example.com", true, "", true, false, false, null, "ADMIN@EXAMPLE.COM", "ADMIN", "AQAAAAIAAYagAAAAEJNw8/gF2YQZvKjKHqB0v7FmXqKhWU5nf0K9w8xYzJ5L3qW8dN2mR1pQ4vT7sA==", null, false, null, "seed-4", false, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { new Guid("51ef7e08-ff07-459b-8c55-c7ebac505103"), new Guid("09097277-2705-40c2-bce5-51dbd1f4c1e6") },
                    { new Guid("8f6e1c3d-2d3b-4f4a-9f4a-2e5d6c7b8a9b"), new Guid("09097277-5555-40c2-bce5-51dbd1f4c1e6") },
                    { new Guid("3631e38b-60dd-4d1a-af7f-a26f21c2ef82"), new Guid("33f41895-b601-4aa1-8dc4-8229a9d07008") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { new Guid("51ef7e08-ff07-459b-8c55-c7ebac505103"), new Guid("09097277-2705-40c2-bce5-51dbd1f4c1e6") });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { new Guid("8f6e1c3d-2d3b-4f4a-9f4a-2e5d6c7b8a9b"), new Guid("09097277-5555-40c2-bce5-51dbd1f4c1e6") });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { new Guid("3631e38b-60dd-4d1a-af7f-a26f21c2ef82"), new Guid("33f41895-b601-4aa1-8dc4-8229a9d07008") });

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("09097277-2705-40c2-bce5-51dbd1f4c1e6"));

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("09097277-5555-40c2-bce5-51dbd1f4c1e6"));

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("33f41895-b601-4aa1-8dc4-8229a9d07008"));
        }
    }
}
