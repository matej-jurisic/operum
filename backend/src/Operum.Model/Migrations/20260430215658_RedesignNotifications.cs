using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class RedesignNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationConditionFields");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "NotificationConditions");

            migrationBuilder.DropColumn(
                name: "Operator",
                table: "NotificationConditions");

            migrationBuilder.DropColumn(
                name: "ResultType",
                table: "NotificationConditions");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "NotificationConditions");

            migrationBuilder.RenameColumn(
                name: "LastTriggeredAt",
                table: "TrackerNotifications",
                newName: "LastFiredAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEvaluatedAt",
                table: "TrackerNotifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnalyticCode",
                table: "NotificationConditions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnalyticResultType",
                table: "NotificationConditions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValueMode",
                table: "NotificationConditions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationConditionFilters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: true),
                    Operator = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                    ConditionId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationConditionFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationConditionFilters_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NotificationConditionFilters_NotificationConditions_Conditi~",
                        column: x => x.ConditionId,
                        principalTable: "NotificationConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationConditionPurposeFields",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    ConditionId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationConditionPurposeFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationConditionPurposeFields_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationConditionPurposeFields_NotificationConditions_C~",
                        column: x => x.ConditionId,
                        principalTable: "NotificationConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    TimeOfDay = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    IntervalDays = table.Column<int>(type: "integer", nullable: true),
                    SkipWeekendsDay = table.Column<bool>(type: "boolean", nullable: true),
                    IntervalWeeks = table.Column<int>(type: "integer", nullable: true),
                    DaysOfWeekMask = table.Column<int>(type: "integer", nullable: true),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    LastDayOfMonth = table.Column<bool>(type: "boolean", nullable: true),
                    SkipWeekendsMonth = table.Column<bool>(type: "boolean", nullable: true),
                    NotificationId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationEvents_TrackerNotifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "TrackerNotifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTriggeredEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotificationId = table.Column<string>(type: "text", nullable: false),
                    EntryId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTriggeredEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationTriggeredEntries_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationTriggeredEntries_TrackerNotifications_Notificat~",
                        column: x => x.NotificationId,
                        principalTable: "TrackerNotifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConditionFilters_ConditionId",
                table: "NotificationConditionFilters",
                column: "ConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConditionFilters_FieldId",
                table: "NotificationConditionFilters",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConditionPurposeFields_ConditionId",
                table: "NotificationConditionPurposeFields",
                column: "ConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConditionPurposeFields_FieldId",
                table: "NotificationConditionPurposeFields",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationEvents_NotificationId",
                table: "NotificationEvents",
                column: "NotificationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTriggeredEntries_EntryId",
                table: "NotificationTriggeredEntries",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTriggeredEntries_NotificationId_EntryId",
                table: "NotificationTriggeredEntries",
                columns: new[] { "NotificationId", "EntryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationConditionFilters");

            migrationBuilder.DropTable(
                name: "NotificationConditionPurposeFields");

            migrationBuilder.DropTable(
                name: "NotificationEvents");

            migrationBuilder.DropTable(
                name: "NotificationTriggeredEntries");

            migrationBuilder.DropColumn(
                name: "LastEvaluatedAt",
                table: "TrackerNotifications");

            migrationBuilder.DropColumn(
                name: "AnalyticCode",
                table: "NotificationConditions");

            migrationBuilder.DropColumn(
                name: "AnalyticResultType",
                table: "NotificationConditions");

            migrationBuilder.DropColumn(
                name: "ValueMode",
                table: "NotificationConditions");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "LastFiredAt",
                table: "TrackerNotifications",
                newName: "LastTriggeredAt");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "NotificationConditions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Operator",
                table: "NotificationConditions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultType",
                table: "NotificationConditions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "NotificationConditions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "NotificationConditionFields",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ConditionId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false)
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
        }
    }
}
