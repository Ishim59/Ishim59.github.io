namespace Gymnasium.Api.Models;

public sealed class NewsArticle
{
    public required string Slug { get; set; }
    public required string Title { get; set; }
    public required string Summary { get; set; }
    public required string ContentHtml { get; set; }
    public DateOnly PublishedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
