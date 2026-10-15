using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymnasium.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialNews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "news_articles",
                columns: table => new
                {
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentHtml = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_news_articles", x => x.Slug);
                });

            migrationBuilder.CreateIndex(
                name: "IX_news_articles_PublishedAt",
                table: "news_articles",
                column: "PublishedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "news_articles");
        }
    }
}
