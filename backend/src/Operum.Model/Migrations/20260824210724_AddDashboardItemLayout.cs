using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardItemLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Config",
                table: "DashboardItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "H",
                table: "DashboardItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "DashboardItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "W",
                table: "DashboardItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "X",
                table: "DashboardItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Y",
                table: "DashboardItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Boards that existed before the grid were rendered as a masonry of equal cards
            // in Order, so lay their items out two to a row in that same order and size them
            // the way a freshly added widget of that chart type would be. Rows are spaced
            // wide enough for the tallest of them; the grid compacts the gaps away on the
            // first render.
            migrationBuilder.Sql("""
                UPDATE "DashboardItems"
                SET "Type" = 'analytic',
                    "W" = CASE "ResultType" WHEN 'Single Value' THEN 3 WHEN 'Donut Chart' THEN 4 ELSE 6 END,
                    "H" = CASE "ResultType" WHEN 'Single Value' THEN 2 WHEN 'Calendar' THEN 8 ELSE 6 END,
                    "X" = CASE WHEN "Order" % 2 = 0 THEN 0 ELSE 6 END,
                    "Y" = ("Order" / 2) * 8;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Config",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "H",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "W",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "X",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "Y",
                table: "DashboardItems");
        }
    }
}
