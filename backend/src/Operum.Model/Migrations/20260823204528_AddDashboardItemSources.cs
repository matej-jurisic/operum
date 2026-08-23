using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardItemSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the new table first — the old DashboardItems.AnalyticId/TrackerId/ViewIds
            // columns are still around at this point so we can copy their data below before
            // dropping them. Postgres 13+ has gen_random_uuid() built in (no extension needed).
            migrationBuilder.CreateTable(
                name: "DashboardItemSources",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    ViewIds = table.Column<string>(type: "text", nullable: true),
                    DashboardItemId = table.Column<string>(type: "text", nullable: false),
                    AnalyticId = table.Column<string>(type: "text", nullable: false),
                    TrackerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardItemSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardItemSources_Analytics_AnalyticId",
                        column: x => x.AnalyticId,
                        principalTable: "Analytics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DashboardItemSources_DashboardItems_DashboardItemId",
                        column: x => x.DashboardItemId,
                        principalTable: "DashboardItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DashboardItemSources_Trackers_TrackerId",
                        column: x => x.TrackerId,
                        principalTable: "Trackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Every existing DashboardItem was single-tracker — wrap each one's
            // AnalyticId/TrackerId/ViewIds into its single Source (Order = 0).
            migrationBuilder.Sql(
                "INSERT INTO \"DashboardItemSources\" (\"Id\", \"Order\", \"Label\", \"ViewIds\", \"DashboardItemId\", \"AnalyticId\", \"TrackerId\") " +
                "SELECT gen_random_uuid()::text, 0, NULL, \"ViewIds\", \"Id\", \"AnalyticId\", \"TrackerId\" FROM \"DashboardItems\";");

            migrationBuilder.DropForeignKey(
                name: "FK_DashboardItems_Analytics_AnalyticId",
                table: "DashboardItems");

            migrationBuilder.DropForeignKey(
                name: "FK_DashboardItems_Trackers_TrackerId",
                table: "DashboardItems");

            migrationBuilder.DropIndex(
                name: "IX_DashboardItems_AnalyticId",
                table: "DashboardItems");

            migrationBuilder.DropIndex(
                name: "IX_DashboardItems_TrackerId",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "AnalyticId",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "TrackerId",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "ViewIds",
                table: "DashboardItems");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItemSources_AnalyticId",
                table: "DashboardItemSources",
                column: "AnalyticId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItemSources_DashboardItemId",
                table: "DashboardItemSources",
                column: "DashboardItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItemSources_TrackerId",
                table: "DashboardItemSources",
                column: "TrackerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalyticId",
                table: "DashboardItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TrackerId",
                table: "DashboardItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ViewIds",
                table: "DashboardItems",
                type: "text",
                nullable: true);

            // Best-effort: an item with more than one Source can only keep one on the way
            // back down, so we take the lowest-Order source per item and drop the rest.
            migrationBuilder.Sql(
                "UPDATE \"DashboardItems\" di SET \"AnalyticId\" = s.\"AnalyticId\", \"TrackerId\" = s.\"TrackerId\", \"ViewIds\" = s.\"ViewIds\" " +
                "FROM (SELECT DISTINCT ON (\"DashboardItemId\") \"DashboardItemId\", \"AnalyticId\", \"TrackerId\", \"ViewIds\" " +
                "FROM \"DashboardItemSources\" ORDER BY \"DashboardItemId\", \"Order\") s " +
                "WHERE di.\"Id\" = s.\"DashboardItemId\";");

            migrationBuilder.DropTable(
                name: "DashboardItemSources");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItems_AnalyticId",
                table: "DashboardItems",
                column: "AnalyticId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItems_TrackerId",
                table: "DashboardItems",
                column: "TrackerId");

            migrationBuilder.AddForeignKey(
                name: "FK_DashboardItems_Analytics_AnalyticId",
                table: "DashboardItems",
                column: "AnalyticId",
                principalTable: "Analytics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DashboardItems_Trackers_TrackerId",
                table: "DashboardItems",
                column: "TrackerId",
                principalTable: "Trackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
