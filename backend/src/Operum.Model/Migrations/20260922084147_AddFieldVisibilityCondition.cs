using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldVisibilityCondition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VisibilityFieldId",
                table: "Fields",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisibilityOperator",
                table: "Fields",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisibilityValue",
                table: "Fields",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fields_VisibilityFieldId",
                table: "Fields",
                column: "VisibilityFieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_Fields_VisibilityFieldId",
                table: "Fields",
                column: "VisibilityFieldId",
                principalTable: "Fields",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fields_Fields_VisibilityFieldId",
                table: "Fields");

            migrationBuilder.DropIndex(
                name: "IX_Fields_VisibilityFieldId",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "VisibilityFieldId",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "VisibilityOperator",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "VisibilityValue",
                table: "Fields");
        }
    }
}
