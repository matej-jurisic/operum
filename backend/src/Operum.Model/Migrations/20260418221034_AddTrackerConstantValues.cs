using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackerConstantValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackerConstantValues",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TrackerConstantId = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackerConstantValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackerConstantValues_TrackerConstants_TrackerConstantId",
                        column: x => x.TrackerConstantId,
                        principalTable: "TrackerConstants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackerConstantValueFilters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TrackerConstantValueId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Operator = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackerConstantValueFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackerConstantValueFilters_TrackerConstantValues_TrackerCo~",
                        column: x => x.TrackerConstantValueId,
                        principalTable: "TrackerConstantValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackerConstantValueFilters_TrackerConstantValueId",
                table: "TrackerConstantValueFilters",
                column: "TrackerConstantValueId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerConstantValues_TrackerConstantId",
                table: "TrackerConstantValues",
                column: "TrackerConstantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackerConstantValueFilters");

            migrationBuilder.DropTable(
                name: "TrackerConstantValues");
        }
    }
}
