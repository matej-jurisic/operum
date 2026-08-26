using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddWidgetLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WidgetSourceId",
                table: "DashboardItemSources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntriesWidgetId",
                table: "DashboardItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WidgetId",
                table: "DashboardItems",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EntriesWidgets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TrackerId = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntriesWidgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntriesWidgets_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntriesWidgets_Trackers_TrackerId",
                        column: x => x.TrackerId,
                        principalTable: "Trackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Widgets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ResultType = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    MatchedValuesOnly = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Widgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Widgets_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WidgetSources",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    WidgetId = table.Column<string>(type: "text", nullable: false),
                    TrackerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WidgetSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WidgetSources_Trackers_TrackerId",
                        column: x => x.TrackerId,
                        principalTable: "Trackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WidgetSources_Widgets_WidgetId",
                        column: x => x.WidgetId,
                        principalTable: "Widgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WidgetSourceFields",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    WidgetSourceId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WidgetSourceFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WidgetSourceFields_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WidgetSourceFields_WidgetSources_WidgetSourceId",
                        column: x => x.WidgetSourceId,
                        principalTable: "WidgetSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItemSources_WidgetSourceId",
                table: "DashboardItemSources",
                column: "WidgetSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItems_EntriesWidgetId",
                table: "DashboardItems",
                column: "EntriesWidgetId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItems_WidgetId",
                table: "DashboardItems",
                column: "WidgetId");

            migrationBuilder.CreateIndex(
                name: "IX_EntriesWidgets_OwnerId",
                table: "EntriesWidgets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_EntriesWidgets_TrackerId",
                table: "EntriesWidgets",
                column: "TrackerId");

            migrationBuilder.CreateIndex(
                name: "IX_Widgets_OwnerId",
                table: "Widgets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetSourceFields_FieldId",
                table: "WidgetSourceFields",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetSourceFields_WidgetSourceId",
                table: "WidgetSourceFields",
                column: "WidgetSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetSources_TrackerId",
                table: "WidgetSources",
                column: "TrackerId");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetSources_WidgetId",
                table: "WidgetSources",
                column: "WidgetId");

            migrationBuilder.AddForeignKey(
                name: "FK_DashboardItems_EntriesWidgets_EntriesWidgetId",
                table: "DashboardItems",
                column: "EntriesWidgetId",
                principalTable: "EntriesWidgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DashboardItems_Widgets_WidgetId",
                table: "DashboardItems",
                column: "WidgetId",
                principalTable: "Widgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DashboardItemSources_WidgetSources_WidgetSourceId",
                table: "DashboardItemSources",
                column: "WidgetSourceId",
                principalTable: "WidgetSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DashboardItems_EntriesWidgets_EntriesWidgetId",
                table: "DashboardItems");

            migrationBuilder.DropForeignKey(
                name: "FK_DashboardItems_Widgets_WidgetId",
                table: "DashboardItems");

            migrationBuilder.DropForeignKey(
                name: "FK_DashboardItemSources_WidgetSources_WidgetSourceId",
                table: "DashboardItemSources");

            migrationBuilder.DropTable(
                name: "EntriesWidgets");

            migrationBuilder.DropTable(
                name: "WidgetSourceFields");

            migrationBuilder.DropTable(
                name: "WidgetSources");

            migrationBuilder.DropTable(
                name: "Widgets");

            migrationBuilder.DropIndex(
                name: "IX_DashboardItemSources_WidgetSourceId",
                table: "DashboardItemSources");

            migrationBuilder.DropIndex(
                name: "IX_DashboardItems_EntriesWidgetId",
                table: "DashboardItems");

            migrationBuilder.DropIndex(
                name: "IX_DashboardItems_WidgetId",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "WidgetSourceId",
                table: "DashboardItemSources");

            migrationBuilder.DropColumn(
                name: "EntriesWidgetId",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "WidgetId",
                table: "DashboardItems");
        }
    }
}
