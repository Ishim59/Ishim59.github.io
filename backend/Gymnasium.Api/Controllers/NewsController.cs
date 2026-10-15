using System.ComponentModel.DataAnnotations;
using Gymnasium.Api.Contracts;
using Gymnasium.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gymnasium.Api.Controllers;

[ApiController, Route("api/news")]
public sealed class NewsController(NewsService news) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery, Range(1, 100)] int limit = 50, CancellationToken cancellationToken = default) =>
        Ok((await news.ListAsync(limit, cancellationToken)).Select(NewsResponse.From));

    [HttpGet("{slug}")]
    public async Task<IActionResult> Find(string slug, CancellationToken cancellationToken)
    {
        var article = await news.FindAsync(slug, cancellationToken);
        return article is null ? NotFound() : Ok(NewsResponse.From(article));
    }

    [HttpPost, Authorize(Policy = "Admin"), EnableRateLimiting("admin"), RequestSizeLimit(65536)]
    public async Task<IActionResult> Create(NewsRequest request, CancellationToken cancellationToken)
    {
        if (!await news.CreateAsync(request, cancellationToken)) return Conflict(new { message = "An article with this slug already exists." });
        var article = await news.FindAsync(request.Slug, cancellationToken);
        return CreatedAtAction(nameof(Find), new { slug = request.Slug }, NewsResponse.From(article!));
    }

    [HttpPut("{slug}"), Authorize(Policy = "Admin"), EnableRateLimiting("admin"), RequestSizeLimit(65536)]
    public async Task<IActionResult> Update(string slug, NewsRequest request, CancellationToken cancellationToken)
    {
        if (slug != request.Slug) return BadRequest(new { message = "The URL slug must match the request body." });
        return await news.UpdateAsync(request, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpDelete("{slug}"), Authorize(Policy = "Admin"), EnableRateLimiting("admin")]
    public async Task<IActionResult> Delete(string slug, CancellationToken cancellationToken) =>
        await news.DeleteAsync(slug, cancellationToken) ? NoContent() : NotFound();
}
