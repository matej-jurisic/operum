using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOwnerIdFromEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entries_AspNetUsers_OwnerId",
                table: "Entries");

            migrationBuilder.DropIndex(
                name: "IX_Entries_OwnerId",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Entries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Entries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Entries_OwnerId",
                table: "Entries",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Entries_AspNetUsers_OwnerId",
                table: "Entries",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
