using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Refactor_ImportDate_ImportTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImportDate",
                table: "ImportTickets",
                newName: "ExpectedArrivalDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualImportDate",
                table: "ImportTickets",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualImportDate",
                table: "ImportTickets");

            migrationBuilder.RenameColumn(
                name: "ExpectedArrivalDate",
                table: "ImportTickets",
                newName: "ImportDate");
        }
    }
}
