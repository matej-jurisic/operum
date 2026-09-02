using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationWritePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FieldValues_EntryId",
                table: "FieldValues");

            migrationBuilder.DropIndex(
                name: "IX_FieldValues_FieldId",
                table: "FieldValues");

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Entries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Entries",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldValues_EntryId_FieldId",
                table: "FieldValues",
                columns: new[] { "EntryId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldValues_FieldId_BooleanValue",
                table: "FieldValues",
                columns: new[] { "FieldId", "BooleanValue" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldValues_FieldId_DateTimeValue",
                table: "FieldValues",
                columns: new[] { "FieldId", "DateTimeValue" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldValues_FieldId_NumberValue",
                table: "FieldValues",
                columns: new[] { "FieldId", "NumberValue" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldValues_FieldId_StringValue",
                table: "FieldValues",
                columns: new[] { "FieldId", "StringValue" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldValues_FieldId_TimeSpanValue",
                table: "FieldValues",
                columns: new[] { "FieldId", "TimeSpanValue" });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_TrackerId_Source_ExternalId",
                table: "Entries",
                columns: new[] { "TrackerId", "Source", "ExternalId" },
                unique: true,
                filter: "\"Source\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FieldValues_EntryId_FieldId",
                table: "FieldValues");

            migrationBuilder.DropIndex(
                name: "IX_FieldValues_FieldId_BooleanValue",
                table: "FieldValues");

            migrationBuilder.DropIndex(
                name: "IX_FieldValues_FieldId_DateTimeValue",
                table: "FieldValues");

            migrationBuilder.DropIndex(
                name: "IX_FieldValues_FieldId_NumberValue",
                table: "FieldValues");

            migrationBuilder.DropIndex(
                name: "IX_FieldValues_FieldId_StringValue",
                table: "FieldValues");

            migrationBuilder.DropIndex(
                name: "IX_FieldValues_FieldId_TimeSpanValue",
                table: "FieldValues");

            migrationBuilder.DropIndex(
                name: "IX_Entries_TrackerId_Source_ExternalId",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Entries");

            migrationBuilder.CreateIndex(
                name: "IX_FieldValues_EntryId",
                table: "FieldValues",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldValues_FieldId",
                table: "FieldValues",
                column: "FieldId");
        }
    }
}
