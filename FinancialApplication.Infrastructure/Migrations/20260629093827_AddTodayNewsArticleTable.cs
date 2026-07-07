using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialApplication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTodayNewsArticleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TodayNewsArticles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JsonData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodayNewsArticles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodayNewsArticles_CreatedAt",
                table: "TodayNewsArticles",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TodayNewsArticles");
        }
    }
}
