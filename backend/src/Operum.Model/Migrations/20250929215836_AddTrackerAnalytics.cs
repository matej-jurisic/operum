using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackerAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackerAnalytics",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TrackerId = table.Column<string>(type: "text", nullable: false),
                    AnalyticId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackerAnalytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackerAnalytics_Analytics_AnalyticId",
                        column: x => x.AnalyticId,
                        principalTable: "Analytics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrackerAnalytics_Trackers_TrackerId",
                        column: x => x.TrackerId,
                        principalTable: "Trackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackerAnalyticDataTypesField",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TrackerAnalyticId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackerAnalyticDataTypesField", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackerAnalyticDataTypesField_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrackerAnalyticDataTypesField_TrackerAnalytics_TrackerAnaly~",
                        column: x => x.TrackerAnalyticId,
                        principalTable: "TrackerAnalytics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackerAnalyticDataTypesField_FieldId",
                table: "TrackerAnalyticDataTypesField",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerAnalyticDataTypesField_TrackerAnalyticId",
                table: "TrackerAnalyticDataTypesField",
                column: "TrackerAnalyticId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerAnalytics_AnalyticId",
                table: "TrackerAnalytics",
                column: "AnalyticId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerAnalytics_TrackerId",
                table: "TrackerAnalytics",
                column: "TrackerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackerAnalyticDataTypesField");

            migrationBuilder.DropTable(
                name: "TrackerAnalytics");
        }
    }
}
