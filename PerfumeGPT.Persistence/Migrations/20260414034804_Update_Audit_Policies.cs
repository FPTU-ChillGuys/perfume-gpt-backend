using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class Update_Audit_Policies : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.RenameColumn(
				name: "LastUpdated",
				table: "SystemPolicies",
				newName: "CreatedAt");

			migrationBuilder.AddColumn<DateTime>(
				name: "UpdatedAt",
				table: "SystemPolicies",
				type: "datetime2",
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "UpdatedBy",
				table: "SystemPolicies",
				type: "nvarchar(max)",
				nullable: true);

			migrationBuilder.UpdateData(
				table: "SystemPolicies",
				keyColumn: "PolicyCode",
				keyValue: "SHIPPING_RETURN",
				columns: new[] { "CreatedAt", "UpdatedAt", "UpdatedBy" },
				values: new object[] { DateTime.UtcNow, null, null });

			migrationBuilder.UpdateData(
				table: "SystemPolicies",
				keyColumn: "PolicyCode",
				keyValue: "USAGE_STORAGE",
				columns: new[] { "CreatedAt", "UpdatedAt", "UpdatedBy" },
				values: new object[] { DateTime.UtcNow, null, null });
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "UpdatedAt",
				table: "SystemPolicies");

			migrationBuilder.DropColumn(
				name: "UpdatedBy",
				table: "SystemPolicies");

			migrationBuilder.RenameColumn(
				name: "CreatedAt",
				table: "SystemPolicies",
				newName: "LastUpdated");

			migrationBuilder.UpdateData(
				table: "SystemPolicies",
				keyColumn: "PolicyCode",
				keyValue: "SHIPPING_RETURN",
				column: "LastUpdated",
				value: new DateTime(2026, 4, 13, 10, 0, 0, 0, DateTimeKind.Utc));

			migrationBuilder.UpdateData(
				table: "SystemPolicies",
				keyColumn: "PolicyCode",
				keyValue: "USAGE_STORAGE",
				column: "LastUpdated",
				value: new DateTime(2026, 4, 13, 10, 0, 0, 0, DateTimeKind.Utc));
		}
	}
}
