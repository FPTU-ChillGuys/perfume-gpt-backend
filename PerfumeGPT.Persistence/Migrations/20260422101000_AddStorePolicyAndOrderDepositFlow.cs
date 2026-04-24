using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
	public partial class AddStorePolicyAndOrderDepositFlow : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "StorePolicies",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
					RequiredDepositPercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
					DepositTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
					IsDepositRequiredForCOD = table.Column<bool>(type: "bit", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_StorePolicies", x => x.Id);
				});

			migrationBuilder.AddColumn<decimal>(
				name: "PaidAmount",
				table: "Orders",
				type: "decimal(18,2)",
				nullable: false,
				defaultValue: 0m);

			migrationBuilder.AddColumn<decimal>(
				name: "RequiredDepositAmount",
				table: "Orders",
				type: "decimal(18,2)",
				nullable: false,
				defaultValue: 0m);

			migrationBuilder.InsertData(
				table: "StorePolicies",
				columns: new[] { "Id", "DepositTimeoutMinutes", "IsDepositRequiredForCOD", "RequiredDepositPercentage" },
				values: new object[] { new Guid("f6c2a71d-a76c-43cf-8f1f-315766251001"), 15, true, 20m });

			migrationBuilder.Sql("UPDATE Orders SET PaidAmount = CASE WHEN PaymentStatus = 'Paid' THEN TotalAmount ELSE 0 END;");

			migrationBuilder.Sql("UPDATE Orders SET RequiredDepositAmount = 0 WHERE RequiredDepositAmount IS NULL;");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "StorePolicies");

			migrationBuilder.DropColumn(
				name: "PaidAmount",
				table: "Orders");

			migrationBuilder.DropColumn(
				name: "RequiredDepositAmount",
				table: "Orders");
		}
	}
}