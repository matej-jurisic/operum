using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class RemoveViewSelectorWidget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The "viewSelector" widget is merged into "filter", which gains optional
            // presets pulling from the same DashboardViews (filters AND sorts, resolved via
            // its own new PresetLinks -- see FilterWidgetConfigDto). There is no sensible
            // automatic conversion, so existing placements are dropped (their sources
            // cascade) -- same treatment as 'view'/'filter' got in
            // UnifyQueriesAndViewSelector. No column changes are needed: PresetIds,
            // SelectedPresetId and PresetLinks live inside the existing JSON Config column,
            // same as every other Filter/ViewSelector field always has.
            migrationBuilder.Sql("DELETE FROM \"DashboardItems\" WHERE \"Type\" = 'viewSelector';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deleted rows are unrecoverable -- UnifyQueriesAndViewSelector's Down didn't
            // attempt to restore its deleted 'view'/'filter' rows either.
        }
    }
}
