using Gymnasium.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gymnasium.Api.Controllers;

[ApiController, Route("api/pages")]
public sealed class PagesController(ContentCatalog catalog) : ControllerBase
{
    [HttpGet]
    public IActionResult List([FromQuery] string? query = null) => Ok(catalog.Pages
        .Where(page => string.IsNullOrWhiteSpace(query) || page.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
        .Select(page => new { page.Slug, page.Title, page.Url }));

    [HttpGet("{**slug}")]
    public async Task<IActionResult> Find(string slug, CancellationToken cancellationToken)
    {
        var page = catalog.Pages.FirstOrDefault(page => page.Slug == slug);
        if (page is null) return NotFound();
        return Ok(new { page.Slug, page.Title, page.Url, contentHtml = await catalog.ReadAsync(page, cancellationToken) });
    }
}
