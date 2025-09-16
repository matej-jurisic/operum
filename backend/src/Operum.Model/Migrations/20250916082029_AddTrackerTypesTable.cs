using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackerTypesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trackers_TrackerType_TrackerTypeId",
                table: "Trackers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrackerType",
                table: "TrackerType");

            migrationBuilder.RenameTable(
                name: "TrackerType",
                newName: "TrackerTypes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrackerTypes",
                table: "TrackerTypes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Trackers_TrackerTypes_TrackerTypeId",
                table: "Trackers",
                column: "TrackerTypeId",
                principalTable: "TrackerTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trackers_TrackerTypes_TrackerTypeId",
                table: "Trackers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrackerTypes",
                table: "TrackerTypes");

            migrationBuilder.RenameTable(
                name: "TrackerTypes",
                newName: "TrackerType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrackerType",
                table: "TrackerType",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Trackers_TrackerType_TrackerTypeId",
                table: "Trackers",
                column: "TrackerTypeId",
                principalTable: "TrackerType",
                principalColumn: "Id");
        }
    }
}
