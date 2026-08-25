using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardItemMobileLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MobileH",
                table: "DashboardItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MobileW",
                table: "DashboardItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MobileX",
                table: "DashboardItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MobileY",
                table: "DashboardItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Until now a phone folded the wide placement down to a single column on the fly
            // and could not save what it rendered. Seed the narrow grid with exactly what
            // those users were already looking at, so a board that is never arranged on a
            // phone stays where it was: every widget full width, stacked in the order the
            // board reads on a desktop, each keeping its own height.
            //
            // The sizes below are the grid's dimensions as of this migration rather than
            // whatever DashboardGrid holds today: a migration has to keep producing the same
            // rows however those constants are retuned later.
            //
            // Heights are settled first, floor included, so the stack is built from the same
            // numbers it writes. Summing the raw height instead would give a widget shorter
            // than the floor a top edge that the widget above it still covers.
            //
            // The running total is over the widgets *before* this one in that order, which is
            // where a stack of full-width cards puts its top edge. COALESCE covers the first
            // widget on each board, which has no preceding rows to sum.
            migrationBuilder.Sql("""
                WITH sized AS (
                    SELECT "Id", "DashboardId", "X", "Y",
                           GREATEST("H", 2) AS "Height"
                    FROM "DashboardItems"
                ),
                stacked AS (
                    SELECT "Id", "Height",
                           COALESCE(
                               SUM("Height") OVER (
                                   PARTITION BY "DashboardId"
                                   ORDER BY "Y", "X", "Id"
                                   ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING
                               ), 0) AS "Top"
                    FROM sized
                )
                UPDATE "DashboardItems" AS d
                SET "MobileX" = 0,
                    "MobileW" = 4,
                    "MobileH" = s."Height",
                    "MobileY" = s."Top"
                FROM stacked AS s
                WHERE s."Id" = d."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MobileH",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "MobileW",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "MobileX",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "MobileY",
                table: "DashboardItems");
        }
    }
}
