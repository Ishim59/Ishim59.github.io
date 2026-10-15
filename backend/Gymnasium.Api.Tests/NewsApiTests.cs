using Gymnasium.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Gymnasium.Api.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Gymnasium.Api.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_POSTGRES")))
            Skip = "Set TEST_POSTGRES to a dedicated PostgreSQL test database.";
    }
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public string AdminKey { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var repository = FindRepository();
        builder.UseContentRoot(Path.Combine(repository, "backend/Gymnasium.Api"));
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = Environment.GetEnvironmentVariable("TEST_POSTGRES"),
            ["Admin:ApiKey"] = AdminKey,
            ["Database:ApplyMigrations"] = "true",
            ["Site:ContentRoot"] = Path.Combine(repository, "content"),
            ["Site:StaticRoot"] = Path.Combine(repository, "dist")
        }));
    }

    private static string FindRepository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "package.json"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}

public sealed class NewsApiTests
{
    [PostgresFact]
    public async Task WritesRequireAuthentication()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/news", Article("unauthorized"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [PostgresFact]
    public async Task NewsPersistsAcrossApplicationRestartAndEscapesHtml()
    {
        var slug = "integration-" + Guid.NewGuid().ToString("N");
        await using (var factory = new ApiFactory())
        {
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Admin-Key", factory.AdminKey);
            var created = await client.PostAsJsonAsync("/api/news", Article(slug));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/news", Article(slug))).StatusCode);
        }
        await using var restartedFactory = new ApiFactory();
        using var restartedClient = restartedFactory.CreateClient();
        restartedClient.DefaultRequestHeaders.Add("X-Admin-Key", restartedFactory.AdminKey);
        try
        {
            var saved = await restartedClient.GetFromJsonAsync<NewsResponse>("/api/news/" + slug);
            Assert.NotNull(saved);
            Assert.Contains("&lt;script&gt;", saved.ContentHtml);
            Assert.DoesNotContain("<script>", saved.ContentHtml);
            var updated = Article(slug) with { Title = "__NEWS_CONTENT__" };
            Assert.Equal(HttpStatusCode.NoContent, (await restartedClient.PutAsJsonAsync("/api/news/" + slug, updated)).StatusCode);
            Assert.Equal("__NEWS_CONTENT__", (await restartedClient.GetFromJsonAsync<NewsResponse>("/api/news/" + slug))!.Title);
            var rendered = await restartedClient.GetStringAsync("/news/" + slug + "/");
            Assert.Contains("<h1>__NEWS_CONTENT__</h1>", rendered);
            Assert.DoesNotContain("<script>", rendered);
        }
        finally
        {
            await restartedClient.DeleteAsync("/api/news/" + slug);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await restartedClient.GetAsync("/api/news/" + slug)).StatusCode);
    }

    [PostgresFact]
    public async Task InvalidSlugsAndDatesAreRejected()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Key", factory.AdminKey);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/news", Article("../invalid"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/news", Article("date-test") with { PublishedAt = "0001-01-01" })).StatusCode);
    }

    [PostgresFact]
    public async Task CatalogAndDatabaseReadinessWork()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health/ready")).StatusCode);
        var page = await client.GetStringAsync("/api/pages/documents");
        Assert.Contains("documents", page);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/pages/not-a-page")).StatusCode);
    }

    [PostgresFact]
    public async Task FirstRunSeedNeverRestoresDeletedNews()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        var database = scope.ServiceProvider.GetRequiredService<GymnasiumDbContext>();
        await initializer.SeedFirstRunAsync();
        var original = await database.News.AsNoTracking().SingleAsync(article => article.Slug == "pushkin-card");
        try
        {
            await database.News.Where(article => article.Slug == original.Slug).ExecuteDeleteAsync();
            await initializer.SeedFirstRunAsync();
            Assert.False(await database.News.AnyAsync(article => article.Slug == original.Slug));
        }
        finally
        {
            database.News.Add(original);
            await database.SaveChangesAsync();
        }
    }

    private static ArticleRequest Article(string slug) => new(slug, "Test article", "Test summary", "<script>alert('unsafe')</script>", "2026-10-15");
    private sealed record ArticleRequest(string Slug, string Title, string Summary, string Content, string PublishedAt);
}
