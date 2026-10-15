using System.Text.Json;

namespace Gymnasium.Api.Services;

public sealed record PageInfo(string Slug, string Title, string Url, string File, string LegacyUrl);

/// <summary>Reads only files listed in content/pages.json, never arbitrary URL paths.</summary>
public sealed class ContentCatalog
{
    public string Root { get; }
    public IReadOnlyList<PageInfo> Pages { get; }

    public ContentCatalog(IHostEnvironment environment, IConfiguration configuration)
    {
        Root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuration["Site:ContentRoot"] ?? "../../content"));
        var json = File.ReadAllText(Path.Combine(Root, "pages.json"));
        Pages = JsonSerializer.Deserialize<List<PageInfo>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("content/pages.json is invalid.");
    }

    public Task<string> ReadAsync(PageInfo page, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(Root, "pages") + Path.DirectorySeparatorChar;
        var filename = Path.GetFullPath(Path.Combine(directory, page.File));
        if (!filename.StartsWith(directory, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Content file must stay inside content/pages.");
        return File.ReadAllTextAsync(filename, cancellationToken);
    }
}
