using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SystemPage_Media_id : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SystemPageId",
                table: "Media",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Media_EntityType_SystemPageId",
                table: "Media",
                columns: new[] { "EntityType", "SystemPageId" });

            migrationBuilder.CreateIndex(
                name: "IX_Media_SystemPageId",
                table: "Media",
                column: "SystemPageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_SystemPages_SystemPageId",
                table: "Media",
                column: "SystemPageId",
                principalTable: "SystemPages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Media_SystemPages_SystemPageId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_EntityType_SystemPageId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_SystemPageId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "SystemPageId",
                table: "Media");
        }
    }
}
