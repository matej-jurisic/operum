using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardItemParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentItemId",
                table: "DashboardItems",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItems_ParentItemId",
                table: "DashboardItems",
                column: "ParentItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_DashboardItems_DashboardItems_ParentItemId",
                table: "DashboardItems",
                column: "ParentItemId",
                principalTable: "DashboardItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DashboardItems_DashboardItems_ParentItemId",
                table: "DashboardItems");

            migrationBuilder.DropIndex(
                name: "IX_DashboardItems_ParentItemId",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "ParentItemId",
                table: "DashboardItems");
        }
    }
}
