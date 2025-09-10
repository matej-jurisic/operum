using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddViewFilterToView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ViewFilters_FieldId",
                table: "ViewFilters",
                column: "FieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_ViewFilters_Fields_FieldId",
                table: "ViewFilters",
                column: "FieldId",
                principalTable: "Fields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ViewFilters_Fields_FieldId",
                table: "ViewFilters");

            migrationBuilder.DropIndex(
                name: "IX_ViewFilters_FieldId",
                table: "ViewFilters");
        }
    }
}
