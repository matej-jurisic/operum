using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackerNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackerNotifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CooldownMinutes = table.Column<int>(type: "integer", nullable: false),
                    LastTriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsTriggered = table.Column<bool>(type: "boolean", nullable: false),
                    ViewIds = table.Column<string>(type: "text", nullable: true),
                    TrackerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackerNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackerNotifications_Trackers_TrackerId",
                        column: x => x.TrackerId,
                        principalTable: "Trackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationConditions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    ResultType = table.Column<string>(type: "text", nullable: false),
                    Operator = table.Column<string>(type: "text", nullable: false),
                    Threshold = table.Column<double>(type: "double precision", nullable: false),
                    NotificationId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationConditions_TrackerNotifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "TrackerNotifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationConditionFields",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    ConditionId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationConditionFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationConditionFields_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationConditionFields_NotificationConditions_Conditio~",
                        column: x => x.ConditionId,
                        principalTable: "NotificationConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConditionFields_ConditionId",
                table: "NotificationConditionFields",
                column: "ConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConditionFields_FieldId",
                table: "NotificationConditionFields",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConditions_NotificationId",
                table: "NotificationConditions",
                column: "NotificationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackerNotifications_TrackerId",
                table: "TrackerNotifications",
                column: "TrackerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationConditionFields");

            migrationBuilder.DropTable(
                name: "NotificationConditions");

            migrationBuilder.DropTable(
                name: "TrackerNotifications");
        }
    }
}
