using Gymnasium.Api.Data;
using Gymnasium.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gymnasium.Api.Repositories;

public sealed class NewsRepository(GymnasiumDbContext database) : INewsRepository
{
    public async Task<IReadOnlyList<NewsArticle>> ListAsync(int limit, CancellationToken cancellationToken) =>
        await database.News.AsNoTracking().OrderByDescending(article => article.PublishedAt)
            .ThenBy(article => article.Slug).Take(limit).ToListAsync(cancellationToken);

    public Task<NewsArticle?> FindAsync(string slug, CancellationToken cancellationToken) =>
        database.News.AsNoTracking().SingleOrDefaultAsync(article => article.Slug == slug, cancellationToken);

    public async Task<bool> CreateAsync(NewsArticle article, CancellationToken cancellationToken)
    {
        database.News.Add(article);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            database.Entry(article).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> UpdateAsync(NewsArticle article, CancellationToken cancellationToken) =>
        await database.News.Where(existing => existing.Slug == article.Slug)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(existing => existing.Title, article.Title)
                .SetProperty(existing => existing.Summary, article.Summary)
                .SetProperty(existing => existing.ContentHtml, article.ContentHtml)
                .SetProperty(existing => existing.PublishedAt, article.PublishedAt)
                .SetProperty(existing => existing.UpdatedAt, article.UpdatedAt), cancellationToken) > 0;

    public async Task<bool> DeleteAsync(string slug, CancellationToken cancellationToken) =>
        await database.News.Where(article => article.Slug == slug).ExecuteDeleteAsync(cancellationToken) > 0;
}
