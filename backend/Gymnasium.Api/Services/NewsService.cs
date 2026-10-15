using System.Net;
using Gymnasium.Api.Contracts;
using Gymnasium.Api.Models;
using Gymnasium.Api.Repositories;

namespace Gymnasium.Api.Services;

public sealed class NewsService(INewsRepository repository)
{
    public Task<IReadOnlyList<NewsArticle>> ListAsync(int limit, CancellationToken cancellationToken) => repository.ListAsync(limit, cancellationToken);
    public Task<NewsArticle?> FindAsync(string slug, CancellationToken cancellationToken) => repository.FindAsync(slug, cancellationToken);
    public Task<bool> CreateAsync(NewsRequest request, CancellationToken cancellationToken) => repository.CreateAsync(ToArticle(request), cancellationToken);
    public Task<bool> UpdateAsync(NewsRequest request, CancellationToken cancellationToken) => repository.UpdateAsync(ToArticle(request), cancellationToken);
    public Task<bool> DeleteAsync(string slug, CancellationToken cancellationToken) => repository.DeleteAsync(slug, cancellationToken);

    private static NewsArticle ToArticle(NewsRequest request) => new()
    {
        Slug = request.Slug,
        Title = request.Title.Trim(),
        Summary = request.Summary.Trim(),
        ContentHtml = "<p>" + WebUtility.HtmlEncode(request.Content.Trim()).Replace("\r\n", "\n").Replace("\n", "<br>") + "</p>",
        PublishedAt = request.PublishedAt,
        UpdatedAt = DateTimeOffset.UtcNow
    };
}
