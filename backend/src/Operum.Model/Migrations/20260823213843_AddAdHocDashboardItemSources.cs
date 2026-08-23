using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddAdHocDashboardItemSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AnalyticId",
                table: "DashboardItemSources",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

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

            migrationBuilder.CreateTable(
                name: "DashboardItemSourceFields",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    DashboardItemSourceId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItemSourceFields_DashboardItemSourceId",
                table: "DashboardItemSourceFields",
                column: "DashboardItemSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardItemSourceFields_FieldId",
                table: "DashboardItemSourceFields",
                column: "FieldId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardItemSourceFields");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "DashboardItemSources");

            migrationBuilder.DropColumn(
                name: "ResultType",
                table: "DashboardItemSources");

            migrationBuilder.AlterColumn<string>(
                name: "AnalyticId",
                table: "DashboardItemSources",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
