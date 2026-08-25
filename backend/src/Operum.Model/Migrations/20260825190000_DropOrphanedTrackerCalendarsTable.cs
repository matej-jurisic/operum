using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    // "TrackerCalendars" (with a FieldId-referencing "LabelFieldId" column) predates this
    // migration history: it was created by whatever shipped the original calendar-view
    // feature, before that was replaced by the generic Calendar-type analytic builder, and
    // was never dropped when the code moved on. EF never modeled it, so its FK to "Fields"
    // was left with Postgres's default ON DELETE NO ACTION — every attempt to delete a
    // field it happened to reference threw an unhandled 23503 instead of the field's own
    // (properly cascaded) delete logic ever running. Dropping the orphaned table removes
    // the constraint along with it.
    public partial class DropOrphanedTrackerCalendarsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "TrackerCalendars";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: the table was never part of this model, so its original
            // column list, defaults and indexes aren't known here to recreate it.
        }
    }
}
