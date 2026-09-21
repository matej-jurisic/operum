using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDashboardItemKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DashboardItems_DashboardId_Key",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "DashboardItems");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItems_DashboardId",
                table: "DashboardItems",
                column: "DashboardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DashboardItems_DashboardId",
                table: "DashboardItems");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "DashboardItems",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItems_DashboardId_Key",
                table: "DashboardItems",
                columns: new[] { "DashboardId", "Key" },
                unique: true,
                filter: "\"Key\" IS NOT NULL");
        }
    }
}
