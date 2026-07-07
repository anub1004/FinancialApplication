using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialApplication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleCountColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArticleCount",
                table: "TodayNewsArticles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ArticleCount",
                table: "FinanceNewsArticles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArticleCount",
                table: "TodayNewsArticles");

            migrationBuilder.DropColumn(
                name: "ArticleCount",
                table: "FinanceNewsArticles");
        }
    }
}
