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
        // Seed only through an explicit command, never each time the application starts.
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
