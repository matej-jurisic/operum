using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardItemLinkedViewWidget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkedViewWidgetId",
                table: "DashboardItemSources",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItemSources_LinkedViewWidgetId",
                table: "DashboardItemSources",
                column: "LinkedViewWidgetId");

            migrationBuilder.AddForeignKey(
                name: "FK_DashboardItemSources_DashboardItems_LinkedViewWidgetId",
                table: "DashboardItemSources",
                column: "LinkedViewWidgetId",
                principalTable: "DashboardItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DashboardItemSources_DashboardItems_LinkedViewWidgetId",
                table: "DashboardItemSources");

            migrationBuilder.DropIndex(
                name: "IX_DashboardItemSources_LinkedViewWidgetId",
                table: "DashboardItemSources");

            migrationBuilder.DropColumn(
                name: "LinkedViewWidgetId",
                table: "DashboardItemSources");
        }
    }
}
