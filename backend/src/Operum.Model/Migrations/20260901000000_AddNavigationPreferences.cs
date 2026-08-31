using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Dashboards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPage",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            // Give existing dashboards a stable initial order (by name) so the sidebar
            // list is not arbitrary before the user first reorders it.
            migrationBuilder.Sql(@"
                WITH ordered AS (
                    SELECT ""Id"", ROW_NUMBER() OVER (PARTITION BY ""UserId"" ORDER BY ""Name"") - 1 AS rn
                    FROM ""Dashboards""
                )
                UPDATE ""Dashboards"" d SET ""Order"" = ordered.rn
                FROM ordered WHERE ordered.""Id"" = d.""Id"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "Dashboards");

            migrationBuilder.DropColumn(
                name: "DefaultPage",
                table: "AspNetUsers");
        }
    }
}
