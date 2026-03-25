using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeGPT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Customer_PreferenceNote_Type : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AspNetUsers_ModeratedByStaffId",
                table: "Reviews");

            migrationBuilder.RenameColumn(
                name: "ModerationReason",
                table: "Reviews",
                newName: "StaffFeedbackComment");

            migrationBuilder.RenameColumn(
                name: "ModeratedByStaffId",
                table: "Reviews",
                newName: "StaffFeedbackByStaffId");

            migrationBuilder.RenameColumn(
                name: "ModeratedAt",
                table: "Reviews",
                newName: "StaffFeedbackAt");

            migrationBuilder.RenameIndex(
                name: "IX_Reviews_ModeratedByStaffId",
                table: "Reviews",
                newName: "IX_Reviews_StaffFeedbackByStaffId");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "NoteType",
                table: "CustomerNotePreferences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AspNetUsers_StaffFeedbackByStaffId",
                table: "Reviews",
                column: "StaffFeedbackByStaffId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AspNetUsers_StaffFeedbackByStaffId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "NoteType",
                table: "CustomerNotePreferences");

            migrationBuilder.RenameColumn(
                name: "StaffFeedbackComment",
                table: "Reviews",
                newName: "ModerationReason");

            migrationBuilder.RenameColumn(
                name: "StaffFeedbackByStaffId",
                table: "Reviews",
                newName: "ModeratedByStaffId");

            migrationBuilder.RenameColumn(
                name: "StaffFeedbackAt",
                table: "Reviews",
                newName: "ModeratedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Reviews_StaffFeedbackByStaffId",
                table: "Reviews",
                newName: "IX_Reviews_ModeratedByStaffId");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AspNetUsers_ModeratedByStaffId",
                table: "Reviews",
                column: "ModeratedByStaffId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
