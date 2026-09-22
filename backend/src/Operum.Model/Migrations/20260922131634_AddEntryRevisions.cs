using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddEntryRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntryRevisions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    EntryId = table.Column<string>(type: "text", nullable: false),
                    ChangeType = table.Column<string>(type: "text", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "text", nullable: true),
                    ChangedByUserName = table.Column<string>(type: "text", nullable: false),
                    Snapshot = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntryRevisions_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EntryRevisions_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntryRevisions_ChangedByUserId",
                table: "EntryRevisions",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EntryRevisions_EntryId_ChangedAt",
                table: "EntryRevisions",
                columns: new[] { "EntryId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntryRevisions");
        }
    }
}
