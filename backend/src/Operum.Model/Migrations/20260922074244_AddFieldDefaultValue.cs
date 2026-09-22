using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldDefaultValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultValue",
                table: "Fields",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultValueConstantId",
                table: "Fields",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fields_DefaultValueConstantId",
                table: "Fields",
                column: "DefaultValueConstantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_TrackerConstants_DefaultValueConstantId",
                table: "Fields",
                column: "DefaultValueConstantId",
                principalTable: "TrackerConstants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fields_TrackerConstants_DefaultValueConstantId",
                table: "Fields");

            migrationBuilder.DropIndex(
                name: "IX_Fields_DefaultValueConstantId",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "DefaultValue",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "DefaultValueConstantId",
                table: "Fields");
        }
    }
}
