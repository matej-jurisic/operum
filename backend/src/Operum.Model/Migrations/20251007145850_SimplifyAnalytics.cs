using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Analytics_AnalyticTypes_AnalyticTypeId",
                table: "Analytics");

            migrationBuilder.DropTable(
                name: "AnalyticTypes");

            migrationBuilder.DropTable(
                name: "TrackerAnalyticDataTypesField");

            migrationBuilder.DropTable(
                name: "AnalyticRequiredDataTypes");

            migrationBuilder.DropTable(
                name: "TrackerAnalytics");

            migrationBuilder.DropIndex(
                name: "IX_Analytics_AnalyticTypeId",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "AnalyticTypeId",
                table: "Analytics");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Analytics",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Analytics",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackerId",
                table: "Analytics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AnalyticFields",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    AnalyticId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticFields_Analytics_AnalyticId",
                        column: x => x.AnalyticId,
                        principalTable: "Analytics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnalyticFields_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Analytics_TrackerId",
                table: "Analytics",
                column: "TrackerId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticFields_AnalyticId",
                table: "AnalyticFields",
                column: "AnalyticId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticFields_FieldId",
                table: "AnalyticFields",
                column: "FieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_Analytics_Trackers_TrackerId",
                table: "Analytics",
                column: "TrackerId",
                principalTable: "Trackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Analytics_Trackers_TrackerId",
                table: "Analytics");

            migrationBuilder.DropTable(
                name: "AnalyticFields");

            migrationBuilder.DropIndex(
                name: "IX_Analytics_TrackerId",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "TrackerId",
                table: "Analytics");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Analytics",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "AnalyticTypeId",
                table: "Analytics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AnalyticRequiredDataTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AnalyticId = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticRequiredDataTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticRequiredDataTypes_Analytics_AnalyticId",
                        column: x => x.AnalyticId,
                        principalTable: "Analytics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalyticTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackerAnalytics",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AnalyticId = table.Column<string>(type: "text", nullable: false),
                    TrackerId = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackerAnalytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackerAnalytics_Analytics_AnalyticId",
                        column: x => x.AnalyticId,
                        principalTable: "Analytics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrackerAnalytics_Trackers_TrackerId",
                        column: x => x.TrackerId,
                        principalTable: "Trackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackerAnalyticDataTypesField",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AnalyticRequiredDataTypeId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    TrackerAnalyticId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackerAnalyticDataTypesField", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackerAnalyticDataTypesField_AnalyticRequiredDataTypes_Ana~",
                        column: x => x.AnalyticRequiredDataTypeId,
                        principalTable: "AnalyticRequiredDataTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrackerAnalyticDataTypesField_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrackerAnalyticDataTypesField_TrackerAnalytics_TrackerAnaly~",
                        column: x => x.TrackerAnalyticId,
                        principalTable: "TrackerAnalytics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Analytics_AnalyticTypeId",
                table: "Analytics",
                column: "AnalyticTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticRequiredDataTypes_AnalyticId",
                table: "AnalyticRequiredDataTypes",
                column: "AnalyticId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerAnalyticDataTypesField_AnalyticRequiredDataTypeId",
                table: "TrackerAnalyticDataTypesField",
                column: "AnalyticRequiredDataTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerAnalyticDataTypesField_FieldId",
                table: "TrackerAnalyticDataTypesField",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerAnalyticDataTypesField_TrackerAnalyticId",
                table: "TrackerAnalyticDataTypesField",
                column: "TrackerAnalyticId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerAnalytics_AnalyticId",
                table: "TrackerAnalytics",
                column: "AnalyticId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerAnalytics_TrackerId",
                table: "TrackerAnalytics",
                column: "TrackerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Analytics_AnalyticTypes_AnalyticTypeId",
                table: "Analytics",
                column: "AnalyticTypeId",
                principalTable: "AnalyticTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
