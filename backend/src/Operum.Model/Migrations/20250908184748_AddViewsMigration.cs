using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddViewsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Visible",
                table: "Fields",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Views",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TrackerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Views", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Views_Trackers_TrackerId",
                        column: x => x.TrackerId,
                        principalTable: "Trackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ViewColumns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ViewId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViewColumns_Views_ViewId",
                        column: x => x.ViewId,
                        principalTable: "Views",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ViewFilters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ViewId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Operator = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViewFilters_Views_ViewId",
                        column: x => x.ViewId,
                        principalTable: "Views",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ViewGroups",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ViewId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViewGroups_Views_ViewId",
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
                    ViewId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Descending = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "IX_ViewColumns_ViewId",
                table: "ViewColumns",
                column: "ViewId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewFilters_ViewId",
                table: "ViewFilters",
                column: "ViewId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewGroups_ViewId",
                table: "ViewGroups",
                column: "ViewId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewSorts_FieldId",
                table: "ViewSorts",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewSorts_ViewId",
                table: "ViewSorts",
                column: "ViewId");

            migrationBuilder.CreateIndex(
                name: "IX_Views_TrackerId",
                table: "Views",
                column: "TrackerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ViewColumns");

            migrationBuilder.DropTable(
                name: "ViewFilters");

            migrationBuilder.DropTable(
                name: "ViewGroups");

            migrationBuilder.DropTable(
                name: "ViewSorts");

            migrationBuilder.DropTable(
                name: "Views");

            migrationBuilder.DropColumn(
                name: "Visible",
                table: "Fields");
        }
    }
}
