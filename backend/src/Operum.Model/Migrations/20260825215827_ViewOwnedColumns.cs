using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class ViewOwnedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ViewColumns_FieldId",
                table: "ViewColumns",
                column: "FieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_ViewColumns_Fields_FieldId",
                table: "ViewColumns",
                column: "FieldId",
                principalTable: "Fields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ViewColumns_Fields_FieldId",
                table: "ViewColumns");

            migrationBuilder.DropIndex(
                name: "IX_ViewColumns_FieldId",
                table: "ViewColumns");
        }
    }
}
