using Gymnasium.Api.Models;

namespace Gymnasium.Api.Repositories;

public interface INewsRepository
{
    Task<IReadOnlyList<NewsArticle>> ListAsync(int limit, CancellationToken cancellationToken);
    Task<NewsArticle?> FindAsync(string slug, CancellationToken cancellationToken);
    Task<bool> CreateAsync(NewsArticle article, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(NewsArticle article, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string slug, CancellationToken cancellationToken);
}
