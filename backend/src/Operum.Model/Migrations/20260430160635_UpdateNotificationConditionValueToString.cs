using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotificationConditionValueToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Threshold",
                table: "NotificationConditions");

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "NotificationConditions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Value",
                table: "NotificationConditions");

            migrationBuilder.AddColumn<double>(
                name: "Threshold",
                table: "NotificationConditions",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
