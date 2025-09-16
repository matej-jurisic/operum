using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackerTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTemplate",
                table: "Trackers");

            migrationBuilder.AddColumn<int>(
                name: "TrackerTypeId",
                table: "Trackers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrackerType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackerType", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trackers_TrackerTypeId",
                table: "Trackers",
                column: "TrackerTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trackers_TrackerType_TrackerTypeId",
                table: "Trackers",
                column: "TrackerTypeId",
                principalTable: "TrackerType",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trackers_TrackerType_TrackerTypeId",
                table: "Trackers");

            migrationBuilder.DropTable(
                name: "TrackerType");

            migrationBuilder.DropIndex(
                name: "IX_Trackers_TrackerTypeId",
                table: "Trackers");

            migrationBuilder.DropColumn(
                name: "TrackerTypeId",
                table: "Trackers");

            migrationBuilder.AddColumn<bool>(
                name: "IsTemplate",
                table: "Trackers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
