using Gymnasium.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Gymnasium.Api.Data;

public sealed class GymnasiumDbContext(DbContextOptions<GymnasiumDbContext> options) : DbContext(options)
{
    public DbSet<NewsArticle> News => Set<NewsArticle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var news = modelBuilder.Entity<NewsArticle>();
        news.ToTable("news_articles");
        news.HasKey(article => article.Slug);
        news.Property(article => article.Slug).HasMaxLength(100);
        news.Property(article => article.Title).HasMaxLength(200);
        news.Property(article => article.Summary).HasMaxLength(500);
        news.Property(article => article.ContentHtml).HasColumnType("text");
        news.HasIndex(article => article.PublishedAt);
    }
}
