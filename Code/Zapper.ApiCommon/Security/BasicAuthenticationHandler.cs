using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Zapper.ApiCommon.Managers.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Zapper.ApiCommon.Security;

// HTTP Basic for a provider's system calling an endpoint registered with it; the username and password are checked
// together, so a refusal never says which part was wrong, Security.md section 4
public sealed class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationSchemeOptions>
{
    private const string BasicPrefix = "Basic ";

    private readonly ICryptographyManager cryptographyManager;

    public BasicAuthenticationHandler(
        IOptionsMonitor<BasicAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ICryptographyManager cryptographyManager)
        : base(options, logger, encoder) =>
        this.cryptographyManager = cryptographyManager;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Options.UserName.Length == 0 || Options.Password.Length == 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("This host holds no credential for the scheme."));
        }

        var header = Request.Headers[HeaderNames.Authorization].ToString();

        if (!header.StartsWith(BasicPrefix, StringComparison.Ordinal)
            || !TryReadCredentials(header[BasicPrefix.Length..], out var userName, out var password))
        {
            return Task.FromResult(AuthenticateResult.Fail("The credentials are missing or malformed."));
        }

        // both are compared every time, so the time taken does not say which part was wrong
        var userNameMatches = CryptographicOperations.FixedTimeEquals(
            cryptographyManager.GetSha256Hash(userName),
            cryptographyManager.GetSha256Hash(Options.UserName));
        var passwordMatches = CryptographicOperations.FixedTimeEquals(
            cryptographyManager.GetSha256Hash(password),
            cryptographyManager.GetSha256Hash(Options.Password));

        if (!(userNameMatches & passwordMatches))
        {
            return Task.FromResult(AuthenticateResult.Fail("The credentials are wrong."));
        }

        // the operation stores the body as received, so it reads it again after binding, BackendCode.md section 9
        Request.EnableBuffering();

        var identity = new ClaimsIdentity([new Claim(PermissionClaim.Type, Options.Permission)], Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Logger.LogWarning("A request was refused by the scheme {Scheme}", Scheme.Name);

        return base.HandleChallengeAsync(properties);
    }

    private static bool TryReadCredentials(string encoded, out string userName, out string password)
    {
        userName = string.Empty;
        password = string.Empty;

        var decoded = new byte[encoded.Length];

        if (!Convert.TryFromBase64String(encoded, decoded, out var written))
        {
            return false;
        }

        var credentials = Encoding.UTF8.GetString(decoded, 0, written);
        var separator = credentials.IndexOf(':', StringComparison.Ordinal);

        if (separator < 0)
        {
            return false;
        }

        userName = credentials[..separator];
        password = credentials[(separator + 1)..];

        return true;
    }
}
