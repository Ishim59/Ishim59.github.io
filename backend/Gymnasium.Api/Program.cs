using Gymnasium.Api.Authentication;
using Gymnasium.Api.Data;
using Gymnasium.Api.Repositories;
using Gymnasium.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<GymnasiumDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Configure ConnectionStrings__Postgres.")));
builder.Services.AddSingleton<ContentCatalog>();
builder.Services.AddScoped<INewsRepository, NewsRepository>();
builder.Services.AddScoped<NewsService>();
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddAuthentication(AdminApiKeyHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, AdminApiKeyHandler>(AdminApiKeyHandler.SchemeName, _ => { });
builder.Services.AddAuthorization(options => options.AddPolicy("Admin", policy => policy.RequireAuthenticatedUser()));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Content-Type", "X-Admin-Key");
}));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("admin", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

if (args.Contains("--migrate") || args.Contains("--seed") || builder.Configuration.GetValue<bool>("Database:ApplyMigrations")
    || builder.Configuration.GetValue<bool>("Database:SeedOnFirstRun"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
    if (builder.Configuration.GetValue<bool>("Database:SeedOnFirstRun")) await initializer.SeedFirstRunAsync();
    if (args.Contains("--seed")) await initializer.SeedAsync();
    if (args.Contains("--migrate") || args.Contains("--seed")) return;
}

var webRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath,
    builder.Configuration["Site:StaticRoot"] ?? "../../dist"));
if (Directory.Exists(webRoot))
{
    var files = new PhysicalFileProvider(webRoot);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = files });
    app.MapGet("/news/{slug}/", async (string slug, NewsService news, CancellationToken cancellationToken) =>
    {
        var article = await news.FindAsync(slug, cancellationToken);
        if (article is null) return Results.NotFound();
        var templatePath = Path.Combine(webRoot, "news/article-template.html");
        if (!File.Exists(templatePath)) return Results.Problem("Run npm run build:server first.", statusCode: 503);
        var template = await File.ReadAllTextAsync(templatePath, cancellationToken);
        var html = System.Text.RegularExpressions.Regex.Replace(template, "__NEWS_(TITLE|CONTENT)__",
            match => match.Groups[1].Value == "TITLE" ? System.Net.WebUtility.HtmlEncode(article.Title) : article.ContentHtml);
        return Results.Content(html, "text/html; charset=utf-8");
    });
}

await app.RunAsync();

public partial class Program;

