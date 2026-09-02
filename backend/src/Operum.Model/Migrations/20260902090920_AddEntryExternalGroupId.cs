using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddEntryExternalGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalGroupId",
                table: "Entries",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entries_TrackerId_Source_ExternalGroupId",
                table: "Entries",
                columns: new[] { "TrackerId", "Source", "ExternalGroupId" },
                filter: "\"ExternalGroupId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Entries_TrackerId_Source_ExternalGroupId",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "ExternalGroupId",
                table: "Entries");
        }
    }
}
