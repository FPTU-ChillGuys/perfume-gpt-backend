using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRejectQuantityAndNoteToImportDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "ImportDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectQuantity",
                table: "ImportDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("09097277-2705-40c2-bce5-51dbd1f4c1e6"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKHinnJYz3sNmgoyw1lyOSf143VtvFvyCDcYcupcT7XK7Hf+J3UFoVZMKadVq3YmOA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("09097277-5555-40c2-bce5-51dbd1f4c1e6"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKHinnJYz3sNmgoyw1lyOSf143VtvFvyCDcYcupcT7XK7Hf+J3UFoVZMKadVq3YmOA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("33f41895-b601-4aa1-8dc4-8229a9d07008"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKHinnJYz3sNmgoyw1lyOSf143VtvFvyCDcYcupcT7XK7Hf+J3UFoVZMKadVq3YmOA==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "ImportDetails");

            migrationBuilder.DropColumn(
                name: "RejectQuantity",
                table: "ImportDetails");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("09097277-2705-40c2-bce5-51dbd1f4c1e6"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJNw8/gF2YQZvKjKHqB0v7FmXqKhWU5nf0K9w8xYzJ5L3qW8dN2mR1pQ4vT7sA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("09097277-5555-40c2-bce5-51dbd1f4c1e6"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJNw8/gF2YQZvKjKHqB0v7FmXqKhWU5nf0K9w8xYzJ5L3qW8dN2mR1pQ4vT7sA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("33f41895-b601-4aa1-8dc4-8229a9d07008"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJNw8/gF2YQZvKjKHqB0v7FmXqKhWU5nf0K9w8xYzJ5L3qW8dN2mR1pQ4vT7sA==");
        }
    }
}
