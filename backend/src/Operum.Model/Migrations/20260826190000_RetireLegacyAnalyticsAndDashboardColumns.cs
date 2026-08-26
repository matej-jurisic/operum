using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class RetireLegacyAnalyticsAndDashboardColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DashboardItemSources_Trackers_TrackerId",
                table: "DashboardItemSources");

            migrationBuilder.DropTable(
                name: "AnalyticFields");

            migrationBuilder.DropTable(
                name: "DashboardItemSourceFields");

            migrationBuilder.DropTable(
                name: "Analytics");

            migrationBuilder.DropIndex(
                name: "IX_DashboardItemSources_TrackerId",
                table: "DashboardItemSources");

            migrationBuilder.DropColumn(
                name: "TrackerId",
                table: "DashboardItemSources");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "MatchedValuesOnly",
                table: "DashboardItems");

            migrationBuilder.DropColumn(
                name: "ResultType",
                table: "DashboardItems");

            migrationBuilder.AlterColumn<string>(
                name: "WidgetSourceId",
                table: "DashboardItemSources",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WidgetSourceId",
                table: "DashboardItemSources",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "TrackerId",
                table: "DashboardItemSources",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "DashboardItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "MatchedValuesOnly",
                table: "DashboardItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ResultType",
                table: "DashboardItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Analytics",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TrackerId = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: true),
                    ResultType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Analytics_Trackers_TrackerId",
                        column: x => x.TrackerId,
                        principalTable: "Trackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DashboardItemSourceFields",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DashboardItemSourceId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardItemSourceFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardItemSourceFields_DashboardItemSources_DashboardIte~",
                        column: x => x.DashboardItemSourceId,
                        principalTable: "DashboardItemSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DashboardItemSourceFields_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalyticFields",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AnalyticId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticFields_Analytics_AnalyticId",
                        column: x => x.AnalyticId,
                        principalTable: "Analytics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnalyticFields_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItemSources_TrackerId",
                table: "DashboardItemSources",
                column: "TrackerId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticFields_AnalyticId",
                table: "AnalyticFields",
                column: "AnalyticId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticFields_FieldId",
                table: "AnalyticFields",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Analytics_TrackerId",
                table: "Analytics",
                column: "TrackerId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItemSourceFields_DashboardItemSourceId",
                table: "DashboardItemSourceFields",
                column: "DashboardItemSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItemSourceFields_FieldId",
                table: "DashboardItemSourceFields",
                column: "FieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_DashboardItemSources_Trackers_TrackerId",
                table: "DashboardItemSources",
                column: "TrackerId",
                principalTable: "Trackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
