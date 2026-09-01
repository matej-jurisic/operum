using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class UnifyQueriesAndViewSelector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The old dropdown-over-one-tracker "view" widget and the board-level "filter"
            // widget are both replaced by the "viewSelector" widget, which is configured
            // entirely differently. There is no sensible automatic conversion, so existing
            // placements of either are dropped (their sources cascade).
            migrationBuilder.Sql("DELETE FROM \"DashboardItems\" WHERE \"Type\" IN ('view', 'filter');");

            // ----- DashboardItemSource.LinkedViewWidgetId is gone -----
            migrationBuilder.DropForeignKey(
                name: "FK_DashboardItemSources_DashboardItems_LinkedViewWidgetId",
                table: "DashboardItemSources");

            migrationBuilder.DropIndex(
                name: "IX_DashboardItemSources_LinkedViewWidgetId",
                table: "DashboardItemSources");

            migrationBuilder.DropColumn(
                name: "LinkedViewWidgetId",
                table: "DashboardItemSources");

            // ----- ViewQuery gains the field binding (backfilled from the old field-bound Query) -----
            migrationBuilder.AddColumn<string>(
                name: "FieldId",
                table: "ViewQueries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                "UPDATE \"ViewQueries\" vq SET \"FieldId\" = q.\"FieldId\" FROM \"Queries\" q WHERE vq.\"QueryId\" = q.\"Id\";");

            migrationBuilder.Sql(
                "DELETE FROM \"ViewQueries\" WHERE \"FieldId\" = '' OR \"FieldId\" NOT IN (SELECT \"Id\" FROM \"Fields\");");

            // ----- Query becomes field-agnostic + user-owned -----
            migrationBuilder.DropForeignKey(name: "FK_Queries_Fields_FieldId", table: "Queries");
            migrationBuilder.DropForeignKey(name: "FK_Queries_Trackers_TrackerId", table: "Queries");
            migrationBuilder.DropIndex(name: "IX_Queries_FieldId", table: "Queries");
            migrationBuilder.DropIndex(name: "IX_Queries_TrackerId", table: "Queries");

            migrationBuilder.AddColumn<string>(
                name: "DataType",
                table: "Queries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Queries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                "UPDATE \"Queries\" q SET \"DataType\" = f.\"Type\" FROM \"Fields\" f WHERE q.\"FieldId\" = f.\"Id\";");
            migrationBuilder.Sql(
                "UPDATE \"Queries\" q SET \"OwnerId\" = t.\"OwnerId\" FROM \"Trackers\" t WHERE q.\"TrackerId\" = t.\"Id\";");
            migrationBuilder.Sql(
                "DELETE FROM \"Queries\" WHERE \"OwnerId\" = '' OR \"DataType\" = '';");

            migrationBuilder.DropColumn(name: "FieldId", table: "Queries");
            migrationBuilder.DropColumn(name: "TrackerId", table: "Queries");

            migrationBuilder.CreateIndex(name: "IX_Queries_OwnerId", table: "Queries", column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Queries_AspNetUsers_OwnerId",
                table: "Queries",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(name: "IX_ViewQueries_FieldId", table: "ViewQueries", column: "FieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_ViewQueries_Fields_FieldId",
                table: "ViewQueries",
                column: "FieldId",
                principalTable: "Fields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // ----- DashboardView / DashboardViewQuery -----
            migrationBuilder.CreateTable(
                name: "DashboardViews",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    DashboardId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardViews_Dashboards_DashboardId",
                        column: x => x.DashboardId,
                        principalTable: "Dashboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DashboardViewQueries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DashboardViewId = table.Column<string>(type: "text", nullable: false),
                    QueryId = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardViewQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardViewQueries_DashboardViews_DashboardViewId",
                        column: x => x.DashboardViewId,
                        principalTable: "DashboardViews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DashboardViewQueries_Queries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "Queries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardViewQueries_DashboardViewId",
                table: "DashboardViewQueries",
                column: "DashboardViewId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardViewQueries_QueryId",
                table: "DashboardViewQueries",
                column: "QueryId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardViews_DashboardId",
                table: "DashboardViews",
                column: "DashboardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DashboardViewQueries");
            migrationBuilder.DropTable(name: "DashboardViews");

            migrationBuilder.DropForeignKey(name: "FK_ViewQueries_Fields_FieldId", table: "ViewQueries");
            migrationBuilder.DropIndex(name: "IX_ViewQueries_FieldId", table: "ViewQueries");
            migrationBuilder.DropColumn(name: "FieldId", table: "ViewQueries");

            migrationBuilder.DropForeignKey(name: "FK_Queries_AspNetUsers_OwnerId", table: "Queries");
            migrationBuilder.DropIndex(name: "IX_Queries_OwnerId", table: "Queries");
            migrationBuilder.DropColumn(name: "DataType", table: "Queries");
            migrationBuilder.DropColumn(name: "OwnerId", table: "Queries");

            migrationBuilder.AddColumn<string>(
                name: "FieldId", table: "Queries", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "TrackerId", table: "Queries", type: "text", nullable: false, defaultValue: "");

            migrationBuilder.CreateIndex(name: "IX_Queries_FieldId", table: "Queries", column: "FieldId");
            migrationBuilder.CreateIndex(name: "IX_Queries_TrackerId", table: "Queries", column: "TrackerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Queries_Fields_FieldId", table: "Queries", column: "FieldId",
                principalTable: "Fields", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_Queries_Trackers_TrackerId", table: "Queries", column: "TrackerId",
                principalTable: "Trackers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddColumn<string>(
                name: "LinkedViewWidgetId", table: "DashboardItemSources", type: "text", nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItemSources_LinkedViewWidgetId",
                table: "DashboardItemSources",
                column: "LinkedViewWidgetId");

            migrationBuilder.AddForeignKey(
                name: "FK_DashboardItemSources_DashboardItems_LinkedViewWidgetId",
                table: "DashboardItemSources",
                column: "LinkedViewWidgetId",
                principalTable: "DashboardItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
