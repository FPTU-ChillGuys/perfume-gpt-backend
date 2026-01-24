using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVerifiedByIdToImportTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ImportTickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ImportTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ImportTickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedById",
                table: "ImportTickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportTickets_VerifiedById",
                table: "ImportTickets",
                column: "VerifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportTickets_AspNetUsers_VerifiedById",
                table: "ImportTickets",
                column: "VerifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportTickets_AspNetUsers_VerifiedById",
                table: "ImportTickets");

            migrationBuilder.DropIndex(
                name: "IX_ImportTickets_VerifiedById",
                table: "ImportTickets");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ImportTickets");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ImportTickets");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ImportTickets");

            migrationBuilder.DropColumn(
                name: "VerifiedById",
                table: "ImportTickets");
        }
    }
}
