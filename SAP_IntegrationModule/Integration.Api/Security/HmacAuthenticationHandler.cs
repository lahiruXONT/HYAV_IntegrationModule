using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Integration.Api.Security;

public sealed class HmacAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;

    public HmacAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        IConfiguration configuration,
        TimeProvider timeProvider
    )
        : base(options, logger, encoder)
    {
        _configuration = configuration;
        _timeProvider = timeProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (
            !Request.Headers.TryGetValue("X-Api-Key", out var apiKey)
            || !Request.Headers.TryGetValue("X-Timestamp", out var timestamp)
            || !Request.Headers.TryGetValue("X-Signature", out var signature)
        )
        {
            return AuthenticateResult.Fail("Missing HMAC headers");
        }

        var configApiKey = _configuration["HmacAuth:ApiKey"];
        var secret = _configuration["HmacAuth:Secret"];

        if (apiKey != configApiKey)
            return AuthenticateResult.Fail("Invalid API key");

        if (!long.TryParse(timestamp, out var requestTime))
            return AuthenticateResult.Fail("Invalid timestamp");

        var now = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        if (Math.Abs(now - requestTime) > 300)
            return AuthenticateResult.Fail("Request expired");

        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var payload = $"{Request.Method}\n{Request.Path}\n{timestamp}\n{body}";

        var computedSignature = ComputeHmac(payload, secret);

        if (
            !CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(signature!),
                Convert.FromBase64String(computedSignature)
            )
        )
        {
            return AuthenticateResult.Fail("Invalid signature");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, apiKey!),
            new Claim(ClaimTypes.Name, apiKey!),
            new Claim(ClaimTypes.System, apiKey!),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private static string ComputeHmac(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
