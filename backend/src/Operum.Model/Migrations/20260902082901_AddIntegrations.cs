using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Integrations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ExternalAccountId = table.Column<string>(type: "text", nullable: true),
                    BaseUrl = table.Column<string>(type: "text", nullable: true),
                    CredentialCiphertext = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Integrations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationTargets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ResourceType = table.Column<string>(type: "text", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    BackfillFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncStatus = table.Column<int>(type: "integer", nullable: false),
                    LastSyncError = table.Column<string>(type: "text", nullable: true),
                    LastCursor = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WebhookToken = table.Column<string>(type: "text", nullable: true),
                    WebhookSecretCiphertext = table.Column<string>(type: "text", nullable: true),
                    IntegrationId = table.Column<string>(type: "text", nullable: false),
                    TrackerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationTargets_Integrations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationTargets_Trackers_TrackerId",
                        column: x => x.TrackerId,
                        principalTable: "Trackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationFieldMappings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SourceKey = table.Column<string>(type: "text", nullable: false),
                    SkipWhenNull = table.Column<bool>(type: "boolean", nullable: false),
                    TargetId = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationFieldMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationFieldMappings_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationFieldMappings_IntegrationTargets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "IntegrationTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationFieldMappings_FieldId",
                table: "IntegrationFieldMappings",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationFieldMappings_TargetId_FieldId",
                table: "IntegrationFieldMappings",
                columns: new[] { "TargetId", "FieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_UserId_Provider_ExternalAccountId",
                table: "Integrations",
                columns: new[] { "UserId", "Provider", "ExternalAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationTargets_IntegrationId_TrackerId_ResourceType",
                table: "IntegrationTargets",
                columns: new[] { "IntegrationId", "TrackerId", "ResourceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationTargets_TrackerId",
                table: "IntegrationTargets",
                column: "TrackerId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationTargets_WebhookToken",
                table: "IntegrationTargets",
                column: "WebhookToken",
                unique: true,
                filter: "\"WebhookToken\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationFieldMappings");

            migrationBuilder.DropTable(
                name: "IntegrationTargets");

            migrationBuilder.DropTable(
                name: "Integrations");
        }
    }
}
