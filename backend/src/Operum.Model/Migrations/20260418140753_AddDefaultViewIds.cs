using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultViewIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trackers_Views_DefaultViewId",
                table: "Trackers");

            migrationBuilder.DropIndex(
                name: "IX_Trackers_DefaultViewId",
                table: "Trackers");

            migrationBuilder.RenameColumn(
                name: "DefaultViewId",
                table: "Trackers",
                newName: "DefaultViewIds");

            migrationBuilder.Sql(
                "UPDATE \"Trackers\" SET \"DefaultViewIds\" = '[\"' || \"DefaultViewIds\" || '\"]' WHERE \"DefaultViewIds\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DefaultViewIds",
                table: "Trackers",
                newName: "DefaultViewId");

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
    }
}
