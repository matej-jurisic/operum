using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class FixTrackerAnalyticsRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Purpose",
                table: "TrackerAnalyticDataTypesField",
                newName: "AnalyticRequiredDataTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerAnalyticDataTypesField_AnalyticRequiredDataTypeId",
                table: "TrackerAnalyticDataTypesField",
                column: "AnalyticRequiredDataTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrackerAnalyticDataTypesField_AnalyticRequiredDataTypes_Ana~",
                table: "TrackerAnalyticDataTypesField",
                column: "AnalyticRequiredDataTypeId",
                principalTable: "AnalyticRequiredDataTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrackerAnalyticDataTypesField_AnalyticRequiredDataTypes_Ana~",
                table: "TrackerAnalyticDataTypesField");

            migrationBuilder.DropIndex(
                name: "IX_TrackerAnalyticDataTypesField_AnalyticRequiredDataTypeId",
                table: "TrackerAnalyticDataTypesField");

            migrationBuilder.RenameColumn(
                name: "AnalyticRequiredDataTypeId",
                table: "TrackerAnalyticDataTypesField",
                newName: "Purpose");
        }
    }
}
