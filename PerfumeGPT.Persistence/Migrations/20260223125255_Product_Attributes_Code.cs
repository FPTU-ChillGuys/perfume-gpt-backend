using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class Product_Attributes_Code : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<string>(
				name: "InternalCode",
				table: "Attributes",
				type: "nvarchar(450)",
				nullable: false,
				defaultValue: "");

			// Update InternalCode for existing rows to be unique (e.g., "ATTR_" + Id)
			migrationBuilder.Sql(@"
                UPDATE [Attributes]
                SET [InternalCode] = 'ATTR_' + CAST([Id] AS NVARCHAR(50))
                WHERE [InternalCode] = ''
            ");

			migrationBuilder.CreateIndex(
				name: "IX_Attributes_InternalCode",
				table: "Attributes",
				column: "InternalCode",
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropIndex(
				name: "IX_Attributes_InternalCode",
				table: "Attributes");

			migrationBuilder.DropColumn(
				name: "InternalCode",
				table: "Attributes");
		}
	}
}
