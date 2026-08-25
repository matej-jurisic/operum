using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class QueriesAndSingleActiveView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The new tables are created before ViewFilters/ViewSorts are dropped, so their
            // rows can be copied across (see the data migration below) instead of lost.
            migrationBuilder.CreateTable(
                name: "Queries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TrackerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Queries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Queries_Trackers_TrackerId",
                        column: x => x.TrackerId,
                        principalTable: "Trackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QueryFilters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    QueryId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Operator = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueryFilters_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueryFilters_Queries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "Queries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuerySorts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    QueryId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Descending = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuerySorts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuerySorts_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuerySorts_Queries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "Queries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ViewQueries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ViewId = table.Column<string>(type: "text", nullable: false),
                    QueryId = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViewQueries_Queries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "Queries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ViewQueries_Views_ViewId",
                        column: x => x.ViewId,
                        principalTable: "Views",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Queries_TrackerId",
                table: "Queries",
                column: "TrackerId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryFilters_FieldId",
                table: "QueryFilters",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryFilters_QueryId",
                table: "QueryFilters",
                column: "QueryId");

            migrationBuilder.CreateIndex(
                name: "IX_QuerySorts_FieldId",
                table: "QuerySorts",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_QuerySorts_QueryId",
                table: "QuerySorts",
                column: "QueryId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewQueries_QueryId",
                table: "ViewQueries",
                column: "QueryId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewQueries_ViewId",
                table: "ViewQueries",
                column: "ViewId");

            // Data migration: give every existing View exactly one Query carrying its old
            // filters/sorts, so nothing already saved is lost. The new Query reuses its
            // View's own id — two different tables, so no key collision — which gives a
            // ready-made 1:1 mapping to link them through ViewQueries without an extra join.
            migrationBuilder.Sql("""
                INSERT INTO "Queries" ("Id", "Name", "Description", "TrackerId")
                SELECT v."Id", v."Name", v."Description", v."TrackerId"
                FROM "Views" v;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "ViewQueries" ("Id", "ViewId", "QueryId", "Order")
                SELECT md5(random()::text || clock_timestamp()::text || v."Id")::uuid::text, v."Id", v."Id", 0
                FROM "Views" v;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "QueryFilters" ("Id", "QueryId", "FieldId", "Operator", "Value")
                SELECT vf."Id", vf."ViewId", vf."FieldId", vf."Operator", vf."Value"
                FROM "ViewFilters" vf;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "QuerySorts" ("Id", "QueryId", "FieldId", "Order", "Descending")
                SELECT vs."Id", vs."ViewId", vs."FieldId", vs."Order", vs."Descending"
                FROM "ViewSorts" vs;
                """);

            migrationBuilder.DropTable(
                name: "ViewFilters");

            migrationBuilder.DropTable(
                name: "ViewSorts");

            // Every view is now a single active selection instead of a combinable list, so
            // each of these becomes one nullable id. Rename first, then collapse whatever the
            // old list-shaped value held down to its first entry.
            migrationBuilder.RenameColumn(
                name: "DefaultViewIds",
                table: "Trackers",
                newName: "DefaultViewId");

            migrationBuilder.Sql("""
                UPDATE "Trackers" SET "DefaultViewId" = ("DefaultViewId"::jsonb ->> 0)
                WHERE "DefaultViewId" IS NOT NULL;
                """);

            migrationBuilder.RenameColumn(
                name: "ViewIds",
                table: "TrackerNotifications",
                newName: "ViewId");

            migrationBuilder.Sql("""
                UPDATE "TrackerNotifications" SET "ViewId" = ("ViewId"::jsonb ->> 0)
                WHERE "ViewId" IS NOT NULL;
                """);

            // DashboardItemSources stored its list comma-joined rather than as JSON.
            migrationBuilder.RenameColumn(
                name: "ViewIds",
                table: "DashboardItemSources",
                newName: "ViewId");

            migrationBuilder.Sql("""
                UPDATE "DashboardItemSources" SET "ViewId" = NULLIF(split_part("ViewId", ',', 1), '')
                WHERE "ViewId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueryFilters");

            migrationBuilder.DropTable(
                name: "QuerySorts");

            migrationBuilder.DropTable(
                name: "ViewQueries");

            migrationBuilder.DropTable(
                name: "Queries");

            migrationBuilder.RenameColumn(
                name: "DefaultViewId",
                table: "Trackers",
                newName: "DefaultViewIds");

            migrationBuilder.RenameColumn(
                name: "ViewId",
                table: "TrackerNotifications",
                newName: "ViewIds");

            migrationBuilder.RenameColumn(
                name: "ViewId",
                table: "DashboardItemSources",
                newName: "ViewIds");

            migrationBuilder.CreateTable(
                name: "ViewFilters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    ViewId = table.Column<string>(type: "text", nullable: false),
                    Operator = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViewFilters_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ViewFilters_Views_ViewId",
                        column: x => x.ViewId,
                        principalTable: "Views",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ViewSorts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    ViewId = table.Column<string>(type: "text", nullable: false),
                    Descending = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewSorts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViewSorts_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ViewSorts_Views_ViewId",
                        column: x => x.ViewId,
                        principalTable: "Views",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ViewFilters_FieldId",
                table: "ViewFilters",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewFilters_ViewId",
                table: "ViewFilters",
                column: "ViewId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewSorts_FieldId",
                table: "ViewSorts",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewSorts_ViewId",
                table: "ViewSorts",
                column: "ViewId");
        }
    }
}
