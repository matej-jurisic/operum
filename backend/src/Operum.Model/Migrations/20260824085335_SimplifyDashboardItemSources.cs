using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyDashboardItemSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "DashboardItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultType",
                table: "DashboardItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            // The definition moves from each source up to the item they belong to. An item
            // whose sources disagreed (which the old shape allowed, with a warning) adopts
            // its first source's definition; the others then no longer calculate and are
            // skipped when the dashboard renders.
            migrationBuilder.Sql("""
                UPDATE "DashboardItems" i
                SET "ResultType" = COALESCE(s."ResultType", a."ResultType", ''),
                    "Code" = COALESCE(s."Code", a."Code", '')
                FROM "DashboardItemSources" s
                LEFT JOIN "Analytics" a ON a."Id" = s."AnalyticId"
                WHERE s."DashboardItemId" = i."Id"
                  AND s."Order" = (
                      SELECT MIN(s2."Order")
                      FROM "DashboardItemSources" s2
                      WHERE s2."DashboardItemId" = i."Id"
                  );
                """);

            // Sources that pointed at a saved analytic kept their field mapping on that
            // analytic. Copy it onto the source itself, which is the only place it lives now.
            migrationBuilder.Sql("""
                INSERT INTO "DashboardItemSourceFields" ("Id", "Purpose", "DashboardItemSourceId", "FieldId")
                SELECT md5(random()::text || clock_timestamp()::text || af."Id")::uuid::text,
                       af."Purpose", s."Id", af."FieldId"
                FROM "DashboardItemSources" s
                JOIN "AnalyticFields" af ON af."AnalyticId" = s."AnalyticId"
                WHERE s."AnalyticId" IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_DashboardItemSources_Analytics_AnalyticId",
                table: "DashboardItemSources");

            migrationBuilder.DropIndex(
                name: "IX_DashboardItemSources_AnalyticId",
                table: "DashboardItemSources");

            migrationBuilder.DropColumn(
                name: "AnalyticId",
                table: "DashboardItemSources");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "DashboardItemSources");

            migrationBuilder.DropColumn(
                name: "ResultType",
                table: "DashboardItemSources");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "ResultType",
                table: "DashboardItems");

            migrationBuilder.AddColumn<string>(
                name: "AnalyticId",
                table: "DashboardItemSources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "DashboardItemSources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultType",
                table: "DashboardItemSources",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItemSources_AnalyticId",
                table: "DashboardItemSources",
                column: "AnalyticId");

            migrationBuilder.AddForeignKey(
                name: "FK_DashboardItemSources_Analytics_AnalyticId",
                table: "DashboardItemSources",
                column: "AnalyticId",
                principalTable: "Analytics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
