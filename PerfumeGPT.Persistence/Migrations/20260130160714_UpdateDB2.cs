using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDB2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<SqlVector<float>>(
                name: "Embedding",
                table: "Products",
                type: "vector(1024)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "Products");
        }
    }
}
