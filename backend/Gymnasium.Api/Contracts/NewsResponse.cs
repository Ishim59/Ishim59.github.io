using Gymnasium.Api.Models;

namespace Gymnasium.Api.Contracts;

public sealed record NewsResponse(string Slug, string Title, string Summary, string ContentHtml,
    DateOnly PublishedAt, DateTimeOffset UpdatedAt)
{
    public static NewsResponse From(NewsArticle article) => new(article.Slug, article.Title,
        article.Summary, article.ContentHtml, article.PublishedAt, article.UpdatedAt);
}
