using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnalyticTypeId",
                table: "Analytics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Analytics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AnalyticTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Analytics_AnalyticTypeId",
                table: "Analytics",
                column: "AnalyticTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Analytics_AnalyticTypes_AnalyticTypeId",
                table: "Analytics",
                column: "AnalyticTypeId",
                principalTable: "AnalyticTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Analytics_AnalyticTypes_AnalyticTypeId",
                table: "Analytics");

            migrationBuilder.DropTable(
                name: "AnalyticTypes");

            migrationBuilder.DropIndex(
                name: "IX_Analytics_AnalyticTypeId",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "AnalyticTypeId",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Analytics");
        }
    }
}
