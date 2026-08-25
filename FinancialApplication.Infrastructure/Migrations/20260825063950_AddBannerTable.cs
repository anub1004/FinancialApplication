using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialApplication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Banners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompressedImage = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "image/jpeg"),
                    OriginalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SourcePageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OriginalSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CompressedSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banners", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Banners_OriginalUrl",
                table: "Banners",
                column: "OriginalUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Banners");
        }
    }
}
