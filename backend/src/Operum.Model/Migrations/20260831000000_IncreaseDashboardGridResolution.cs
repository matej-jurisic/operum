using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseDashboardGridResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The wide grid goes from 12 columns to 24, and the client's row from 40px to
            // 20px, so a drag or resize now snaps to half the step it used to on either
            // axis. Nothing about the schema changes -- X/Y/W/H are still plain grid units --
            // but every one of them now counts in a cell half the size, so a stored
            // placement left alone would render at half its width and half its height.
            //
            // Doubling every coordinate keeps every board exactly where it was: twice the
            // units at half the size is the same place. The narrow grid keeps its 4 columns,
            // so its X/W are untouched; only its Y/H follow the shared row height down.
            //
            // A zero width still means "never placed, the client lays this one out itself",
            // so those rows are left at zero rather than doubled into a stale placement.
            migrationBuilder.Sql("""
                UPDATE "DashboardItems"
                SET "X" = "X" * 2,
                    "Y" = "Y" * 2,
                    "W" = "W" * 2,
                    "H" = "H" * 2
                WHERE "W" > 0;
                """);

            migrationBuilder.Sql("""
                UPDATE "DashboardItems"
                SET "MobileY" = "MobileY" * 2,
                    "MobileH" = "MobileH" * 2
                WHERE "MobileW" > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "DashboardItems"
                SET "X" = "X" / 2,
                    "Y" = "Y" / 2,
                    "W" = GREATEST("W" / 2, 1),
                    "H" = GREATEST("H" / 2, 1)
                WHERE "W" > 0;
                """);

            migrationBuilder.Sql("""
                UPDATE "DashboardItems"
                SET "MobileY" = "MobileY" / 2,
                    "MobileH" = GREATEST("MobileH" / 2, 1)
                WHERE "MobileW" > 0;
                """);
        }
    }
}
