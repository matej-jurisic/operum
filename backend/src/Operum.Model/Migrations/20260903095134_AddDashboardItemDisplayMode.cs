using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardItemDisplayMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The two expandable flags become one per-grid mode: 0 Full, 1 Expandable,
            // 2 Hidden. Add the new columns, carry the old value across (true -> Expandable),
            // then drop the old ones.
            migrationBuilder.AddColumn<int>(
                name: "DisplayMode",
                table: "DashboardItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MobileDisplayMode",
                table: "DashboardItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "DashboardItems"
                SET "DisplayMode" = CASE WHEN "Expandable" THEN 1 ELSE 0 END,
                    "MobileDisplayMode" = CASE WHEN "MobileExpandable" THEN 1 ELSE 0 END;
                """);

            migrationBuilder.DropColumn(
                name: "Expandable",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "MobileExpandable",
                table: "DashboardItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Expandable",
                table: "DashboardItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MobileExpandable",
                table: "DashboardItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Anything not drawn inline (Expandable or Hidden) collapses back to the single
            // expandable flag; Hidden has no equivalent in the old shape.
            migrationBuilder.Sql("""
                UPDATE "DashboardItems"
                SET "Expandable" = "DisplayMode" <> 0,
                    "MobileExpandable" = "MobileDisplayMode" <> 0;
                """);

            migrationBuilder.DropColumn(
                name: "DisplayMode",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "MobileDisplayMode",
                table: "DashboardItems");
        }
    }
}
