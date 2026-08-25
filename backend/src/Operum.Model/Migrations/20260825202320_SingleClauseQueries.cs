using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Operum.Model.Migrations
{
    /// <inheritdoc />
    public partial class SingleClauseQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A Query used to be a named bag of filters and sorts. It is now a single
            // clause, so the columns are added first, every old clause is expanded into a
            // Query of its own (see the data migration below), and only then do the
            // QueryFilters/QuerySorts tables go away.
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Queries",
                type: "text",
                nullable: false,
                defaultValue: "filter");

            migrationBuilder.AddColumn<string>(
                name: "FieldId",
                table: "Queries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Operator",
                table: "Queries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "Queries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Descending",
                table: "Queries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Queries have no name any more: what one does is read off its field, operator
            // and value.
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Queries");

            // Each clause becomes a Query keeping the clause's own id. Only these new rows
            // carry a field, which is how they are told apart from the rows they came out
            // of when those get cleaned up below.
            migrationBuilder.Sql(@"
                INSERT INTO ""Queries"" (""Id"", ""Kind"", ""TrackerId"", ""FieldId"", ""Operator"", ""Value"", ""Descending"")
                SELECT f.""Id"", 'filter', q.""TrackerId"", f.""FieldId"", f.""Operator"", f.""Value"", false
                FROM ""QueryFilters"" f
                JOIN ""Queries"" q ON q.""Id"" = f.""QueryId"";
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""Queries"" (""Id"", ""Kind"", ""TrackerId"", ""FieldId"", ""Operator"", ""Value"", ""Descending"")
                SELECT s.""Id"", 'sort', q.""TrackerId"", s.""FieldId"", NULL, NULL, s.""Descending""
                FROM ""QuerySorts"" s
                JOIN ""Queries"" q ON q.""Id"" = s.""QueryId"";
            ");

            // Every view that held a query now holds that query's clauses instead. Filters
            // sort ahead of sorts within one old query and the old query order is kept
            // between them, so sort precedence reads exactly as it did before.
            migrationBuilder.Sql(@"
                INSERT INTO ""ViewQueries"" (""Id"", ""ViewId"", ""QueryId"", ""Order"")
                SELECT vq.""Id"" || ':' || c.""Id"", vq.""ViewId"", c.""Id"", vq.""Order"" * 1000 + c.""Idx""
                FROM ""ViewQueries"" vq
                JOIN (
                    SELECT f.""Id"", f.""QueryId"", 0 AS ""Idx"" FROM ""QueryFilters"" f
                    UNION ALL
                    SELECT s.""Id"", s.""QueryId"", 1 + s.""Order"" AS ""Idx"" FROM ""QuerySorts"" s
                ) c ON c.""QueryId"" = vq.""QueryId"";
            ");

            // The old rows are the ones that never got a field; deleting them takes their
            // now-stale ViewQuery links with them (DB cascade).
            migrationBuilder.Sql(@"DELETE FROM ""Queries"" WHERE ""FieldId"" = '';");

            migrationBuilder.Sql(@"
                UPDATE ""ViewQueries"" vq
                SET ""Order"" = r.""NewOrder""
                FROM (
                    SELECT ""Id"", ROW_NUMBER() OVER (PARTITION BY ""ViewId"" ORDER BY ""Order"") - 1 AS ""NewOrder""
                    FROM ""ViewQueries""
                ) r
                WHERE r.""Id"" = vq.""Id"";
            ");

            migrationBuilder.DropTable(
                name: "QueryFilters");

            migrationBuilder.DropTable(
                name: "QuerySorts");

            migrationBuilder.CreateIndex(
                name: "IX_Queries_FieldId",
                table: "Queries",
                column: "FieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_Queries_Fields_FieldId",
                table: "Queries",
                column: "FieldId",
                principalTable: "Fields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Structural only: clauses folded into Queries cannot be put back into tables
            // that no longer know which query they belonged to.
            migrationBuilder.DropForeignKey(
                name: "FK_Queries_Fields_FieldId",
                table: "Queries");

            migrationBuilder.DropIndex(
                name: "IX_Queries_FieldId",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "Descending",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "FieldId",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "Operator",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "Queries");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Queries");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Queries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Queries",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QueryFilters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    QueryId = table.Column<string>(type: "text", nullable: false),
                    Operator = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueryFilters_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueryFilters_Queries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "Queries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuerySorts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    QueryId = table.Column<string>(type: "text", nullable: false),
                    Descending = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuerySorts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuerySorts_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuerySorts_Queries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "Queries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueryFilters_FieldId",
                table: "QueryFilters",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryFilters_QueryId",
                table: "QueryFilters",
                column: "QueryId");

            migrationBuilder.CreateIndex(
                name: "IX_QuerySorts_FieldId",
                table: "QuerySorts",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_QuerySorts_QueryId",
                table: "QuerySorts",
                column: "QueryId");
        }
    }
}
