using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    public partial class RefactorBackend : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename the table instead of dropping it
            migrationBuilder.RenameTable(
                name: "ApplicationUserTrackers",
                newName: "UserTrackers");

            // Rename indexes to match the new table name
            migrationBuilder.RenameIndex(
                name: "IX_ApplicationUserTrackers_TrackerId",
                table: "UserTrackers",
                newName: "IX_UserTrackers_TrackerId");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationUserTrackers_ApplicationUserId",
                table: "UserTrackers",
                newName: "IX_UserTrackers_ApplicationUserId");

            // (Optional) If you renamed foreign key names, drop and re-add them
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUserTrackers_AspNetUsers_ApplicationUserId",
                table: "UserTrackers");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUserTrackers_Trackers_TrackerId",
                table: "UserTrackers");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTrackers_AspNetUsers_ApplicationUserId",
                table: "UserTrackers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserTrackers_Trackers_TrackerId",
                table: "UserTrackers",
                column: "TrackerId",
                principalTable: "Trackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse everything in Down()
            migrationBuilder.DropForeignKey(
                name: "FK_UserTrackers_AspNetUsers_ApplicationUserId",
                table: "UserTrackers");

            migrationBuilder.DropForeignKey(
                name: "FK_UserTrackers_Trackers_TrackerId",
                table: "UserTrackers");

            migrationBuilder.RenameTable(
                name: "UserTrackers",
                newName: "ApplicationUserTrackers");

            migrationBuilder.RenameIndex(
                name: "IX_UserTrackers_TrackerId",
                table: "ApplicationUserTrackers",
                newName: "IX_ApplicationUserTrackers_TrackerId");

            migrationBuilder.RenameIndex(
                name: "IX_UserTrackers_ApplicationUserId",
                table: "ApplicationUserTrackers",
                newName: "IX_ApplicationUserTrackers_ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUserTrackers_AspNetUsers_ApplicationUserId",
                table: "ApplicationUserTrackers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUserTrackers_Trackers_TrackerId",
                table: "ApplicationUserTrackers",
                column: "TrackerId",
                principalTable: "Trackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
