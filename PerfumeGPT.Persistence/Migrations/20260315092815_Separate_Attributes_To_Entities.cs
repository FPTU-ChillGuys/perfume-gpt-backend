using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Separate_Attributes_To_Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Carts_CartId",
                table: "CartItems");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropColumn(
                name: "FavoriteNotes",
                table: "CustomerProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredStyle",
                table: "CustomerProfiles");

            migrationBuilder.DropColumn(
                name: "ScentPreference",
                table: "CustomerProfiles");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "ReceiverName",
                table: "Addresses");

            migrationBuilder.RenameColumn(
                name: "CartId",
                table: "CartItems",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                newName: "IX_CartItems_UserId");

            migrationBuilder.AddColumn<int>(
                name: "Longevity",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Sillage",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReleaseYear",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Attributes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "CustomerAttributePreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeValueId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAttributePreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAttributePreferences_AttributeValues_AttributeValueId",
                        column: x => x.AttributeValueId,
                        principalTable: "AttributeValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerAttributePreferences_CustomerProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "CustomerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OlfactoryFamilies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OlfactoryFamilies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScentNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScentNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerFamilyPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerFamilyPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerFamilyPreferences_CustomerProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "CustomerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerFamilyPreferences_OlfactoryFamilies_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "OlfactoryFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductFamilyMaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OlfactoryFamilyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFamilyMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductFamilyMaps_OlfactoryFamilies_OlfactoryFamilyId",
                        column: x => x.OlfactoryFamilyId,
                        principalTable: "OlfactoryFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductFamilyMaps_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerNotePreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerNotePreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerNotePreferences_CustomerProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "CustomerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerNotePreferences_ScentNotes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "ScentNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductNoteMaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScentNoteId = table.Column<int>(type: "int", nullable: false),
                    NoteType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductNoteMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductNoteMaps_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductNoteMaps_ScentNotes_ScentNoteId",
                        column: x => x.ScentNoteId,
                        principalTable: "ScentNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAttributePreferences_AttributeValueId",
                table: "CustomerAttributePreferences",
                column: "AttributeValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAttributePreferences_ProfileId",
                table: "CustomerAttributePreferences",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFamilyPreferences_FamilyId",
                table: "CustomerFamilyPreferences",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFamilyPreferences_ProfileId",
                table: "CustomerFamilyPreferences",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotePreferences_NoteId",
                table: "CustomerNotePreferences",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotePreferences_ProfileId",
                table: "CustomerNotePreferences",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFamilyMaps_OlfactoryFamilyId",
                table: "ProductFamilyMaps",
                column: "OlfactoryFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFamilyMaps_ProductId",
                table: "ProductFamilyMaps",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductNoteMaps_ProductId",
                table: "ProductNoteMaps",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductNoteMaps_ScentNoteId",
                table: "ProductNoteMaps",
                column: "ScentNoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_AspNetUsers_UserId",
                table: "CartItems",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_AspNetUsers_UserId",
                table: "CartItems");

            migrationBuilder.DropTable(
                name: "CustomerAttributePreferences");

            migrationBuilder.DropTable(
                name: "CustomerFamilyPreferences");

            migrationBuilder.DropTable(
                name: "CustomerNotePreferences");

            migrationBuilder.DropTable(
                name: "ProductFamilyMaps");

            migrationBuilder.DropTable(
                name: "ProductNoteMaps");

            migrationBuilder.DropTable(
                name: "OlfactoryFamilies");

            migrationBuilder.DropTable(
                name: "ScentNotes");

            migrationBuilder.DropColumn(
                name: "Longevity",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Sillage",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ReleaseYear",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "CartItems",
                newName: "CartId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItems_UserId",
                table: "CartItems",
                newName: "IX_CartItems_CartId");

            migrationBuilder.AddColumn<string>(
                name: "FavoriteNotes",
                table: "CustomerProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredStyle",
                table: "CustomerProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScentPreference",
                table: "CustomerProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Attributes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Addresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReceiverName",
                table: "Addresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Carts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Carts_CartId",
                table: "CartItems",
                column: "CartId",
                principalTable: "Carts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
