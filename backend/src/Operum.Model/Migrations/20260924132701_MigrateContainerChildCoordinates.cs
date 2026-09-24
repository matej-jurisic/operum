using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class MigrateContainerChildCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A Container no longer owns a nested sub-grid: its children now carry ordinary
            // board-relative X/Y like any other top-level item, instead of being relative to
            // the container's own origin. Re-baseline every existing container's children the
            // same way DashboardService.RemoveDashboardItem already did on delete (proven,
            // already-shipped behavior) -- just without also clearing ParentItemId, since the
            // grouping itself is kept, only the coordinate space changes.
            migrationBuilder.Sql("""
                UPDATE "DashboardItems" AS child
                SET "Y" = child."Y" + parent."Y",
                    "X" = LEAST(child."X", GREATEST(0, 24 - child."W"))
                FROM "DashboardItems" AS parent
                WHERE child."ParentItemId" = parent."Id"
                  AND parent."Type" = 'container';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort inverse: re-baseline back onto the container's own origin. Not
            // perfectly reversible if a child's X was clamped in Up (the original
            // container-relative X is lost in that case), same caveat as any lossy migration.
            migrationBuilder.Sql("""
                UPDATE "DashboardItems" AS child
                SET "Y" = GREATEST(child."Y" - parent."Y", 0),
                    "X" = LEAST(child."X", GREATEST(0, 24 - child."W"))
                FROM "DashboardItems" AS parent
                WHERE child."ParentItemId" = parent."Id"
                  AND parent."Type" = 'container';
                """);
        }
    }
}
