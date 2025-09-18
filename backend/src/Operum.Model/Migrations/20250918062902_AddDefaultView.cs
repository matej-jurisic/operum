using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultViewId",
                table: "Trackers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trackers_DefaultViewId",
                table: "Trackers",
                column: "DefaultViewId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trackers_Views_DefaultViewId",
                table: "Trackers",
                column: "DefaultViewId",
                principalTable: "Views",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trackers_Views_DefaultViewId",
                table: "Trackers");

            migrationBuilder.DropIndex(
                name: "IX_Trackers_DefaultViewId",
                table: "Trackers");

            migrationBuilder.DropColumn(
                name: "DefaultViewId",
                table: "Trackers");
        }
    }
}
