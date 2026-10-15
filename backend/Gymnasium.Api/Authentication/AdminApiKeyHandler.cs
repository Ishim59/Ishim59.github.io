using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Gymnasium.Api.Authentication;

/// <summary>API writes are disabled unless an administrator supplies a strong key outside Git.</summary>
public sealed class AdminApiKeyHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "AdminApiKey";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expected = configuration["Admin:ApiKey"];
        if (string.IsNullOrWhiteSpace(expected) || expected.Length < 32)
            return Task.FromResult(AuthenticateResult.NoResult());
        var supplied = Request.Headers["X-Admin-Key"].ToString();
        if (string.IsNullOrEmpty(supplied) || supplied.Length > 512)
            return Task.FromResult(AuthenticateResult.NoResult());
        var matches = CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(supplied)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
        if (!matches) return Task.FromResult(AuthenticateResult.Fail("Invalid administrator key."));
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "administrator")], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
