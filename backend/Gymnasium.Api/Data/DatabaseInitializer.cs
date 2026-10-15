using System.Text.Json;
using Gymnasium.Api.Models;
using Gymnasium.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Gymnasium.Api.Data;

public sealed class DatabaseInitializer(GymnasiumDbContext database, ContentCatalog content)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await database.Database.MigrateAsync(cancellationToken);
        // Seeding is separate from migrations and must be explicitly enabled.
    }

    public async Task SeedFirstRunAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        // The unique marker also serializes concurrent first deployments.
        // Its insert and all news inserts commit together, or are rolled back together.
        var claimed = await database.Database.ExecuteSqlRawAsync(
            "INSERT INTO application_initializations (\"Name\", \"CompletedAt\") " +
            "VALUES ('initial-news', CURRENT_TIMESTAMP) ON CONFLICT (\"Name\") DO NOTHING", cancellationToken);
        if (claimed == 1) await SeedAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(content.Root, "news.json"), cancellationToken);
        var items = JsonSerializer.Deserialize<List<SeedArticle>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
        foreach (var item in items)
        {
            if (await database.News.AnyAsync(article => article.Slug == item.Slug, cancellationToken)) continue;
            var page = content.Pages.Single(page => page.File == item.ContentFile);
            database.News.Add(new NewsArticle
            {
                Slug = item.Slug,
                Title = item.Title,
                Summary = item.Summary,
                ContentHtml = await content.ReadAsync(page, cancellationToken),
                PublishedAt = item.PublishedAt,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
        await database.SaveChangesAsync(cancellationToken);
    }

    private sealed record SeedArticle(string Slug, string Title, string Summary, DateOnly PublishedAt, string ContentFile);
}
