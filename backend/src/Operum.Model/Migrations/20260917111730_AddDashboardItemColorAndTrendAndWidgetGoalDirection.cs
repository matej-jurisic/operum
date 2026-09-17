using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardItemColorAndTrendAndWidgetGoalDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoalDirection",
                table: "Widgets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "DashboardItems",
                type: "text",
                nullable: true);

            // Every existing placement should read as "trend on" once this ships, matching
            // the C# default (DashboardItem.ShowTrend = true) -- scaffolding leaves this at
            // the CLR default (false) instead, which would silently hide the trend on every
            // widget that already existed before this migration.
            migrationBuilder.AddColumn<bool>(
                name: "ShowTrend",
                table: "DashboardItems",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoalDirection",
                table: "Widgets");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "ShowTrend",
                table: "DashboardItems");
        }
    }
}
