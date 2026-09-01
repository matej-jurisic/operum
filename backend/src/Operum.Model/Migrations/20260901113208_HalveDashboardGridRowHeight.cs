using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class HalveDashboardGridRowHeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The client's row height goes from 20px to 2px so the vertical step a drag or
            // resize snaps to -- row height plus the 16px margin baked into every widget --
            // halves from 36px to 18px. Nothing about the schema changes: Y/H are still
            // plain grid units, there are just twice as many of them per widget now.
            //
            // Doubling every row coordinate keeps every board pixel-for-pixel where it was:
            // (2 + 16) * 2y == (20 + 16) * y for position, and 2 * 2h + 16 * (2h - 1) ==
            // 20 * h + 16 * (h - 1) for height. The column axis is untouched -- X/W and the
            // mobile grid's 4 columns do not move -- so only Y/H and MobileY/MobileH are
            // scaled, the row-axis counterpart of what IncreaseDashboardGridResolution did.
            //
            // A zero width still means "never placed, the client lays this one out itself",
            // so those rows are left alone rather than doubled into a stale placement.
            migrationBuilder.Sql("""
                UPDATE "DashboardItems"
                SET "Y" = "Y" * 2,
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
                SET "Y" = "Y" / 2,
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
