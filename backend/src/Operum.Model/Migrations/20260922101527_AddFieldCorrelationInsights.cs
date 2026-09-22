using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldCorrelationInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FieldCorrelationInsights",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TrackerAId = table.Column<string>(type: "text", nullable: false),
                    TrackerBId = table.Column<string>(type: "text", nullable: false),
                    MatchFieldAId = table.Column<string>(type: "text", nullable: false),
                    MatchFieldBId = table.Column<string>(type: "text", nullable: false),
                    ValueFieldAId = table.Column<string>(type: "text", nullable: false),
                    ValueFieldBId = table.Column<string>(type: "text", nullable: false),
                    Coefficient = table.Column<double>(type: "double precision", nullable: false),
                    SampleSize = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Dismissed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldCorrelationInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldCorrelationInsights_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldCorrelationInsights_Fields_MatchFieldAId",
                        column: x => x.MatchFieldAId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldCorrelationInsights_Fields_MatchFieldBId",
                        column: x => x.MatchFieldBId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldCorrelationInsights_Fields_ValueFieldAId",
                        column: x => x.ValueFieldAId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldCorrelationInsights_Fields_ValueFieldBId",
                        column: x => x.ValueFieldBId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldCorrelationInsights_Trackers_TrackerAId",
                        column: x => x.TrackerAId,
                        principalTable: "Trackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldCorrelationInsights_Trackers_TrackerBId",
                        column: x => x.TrackerBId,
                        principalTable: "Trackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldCorrelationInsights_MatchFieldAId",
                table: "FieldCorrelationInsights",
                column: "MatchFieldAId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldCorrelationInsights_MatchFieldBId",
                table: "FieldCorrelationInsights",
                column: "MatchFieldBId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldCorrelationInsights_TrackerAId",
                table: "FieldCorrelationInsights",
                column: "TrackerAId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldCorrelationInsights_TrackerBId",
                table: "FieldCorrelationInsights",
                column: "TrackerBId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldCorrelationInsights_UserId_ValueFieldAId_ValueFieldBId",
                table: "FieldCorrelationInsights",
                columns: new[] { "UserId", "ValueFieldAId", "ValueFieldBId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldCorrelationInsights_ValueFieldAId",
                table: "FieldCorrelationInsights",
                column: "ValueFieldAId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldCorrelationInsights_ValueFieldBId",
                table: "FieldCorrelationInsights",
                column: "ValueFieldBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FieldCorrelationInsights");
        }
    }
}
