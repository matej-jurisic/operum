using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardItemYAxisFromZero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "YAxisFromZero",
                table: "DashboardItems",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YAxisFromZero",
                table: "DashboardItems");
        }
    }
}
