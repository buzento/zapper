using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Zapper.ApiCommon.Managers.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zapper.ApiCommon.Security;

// a signature over the exact body, for a provider's system calling an endpoint registered with it; it runs before
// binding, and leaves the body buffered and rewound for it, Security.md section 4
public sealed class BodySignatureAuthenticationHandler : AuthenticationHandler<BodySignatureAuthenticationSchemeOptions>
{
    private const int SignatureSize = 32;

    private readonly ICryptographyManager cryptographyManager;

    public BodySignatureAuthenticationHandler(
        IOptionsMonitor<BodySignatureAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ICryptographyManager cryptographyManager)
        : base(options, logger, encoder) =>
        this.cryptographyManager = cryptographyManager;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Options.Secret.Length == 0)
        {
            return AuthenticateResult.Fail("This host holds no secret for the scheme.");
        }

        // one byte more than the signature, so a value that decodes to more than 32 bytes does not fit and is refused
        var sent = new byte[SignatureSize + 1];

        if (!Convert.TryFromBase64String(Request.Headers[Options.HeaderName].ToString(), sent, out var written)
            || written != SignatureSize)
        {
            return AuthenticateResult.Fail("The signature is missing or malformed.");
        }

        // read within the request size limit Kestrel enforces, BackendCode.md section 9
        Request.EnableBuffering();

        using var body = new MemoryStream();

        await Request.Body.CopyToAsync(body, Context.RequestAborted);
        Request.Body.Position = 0;

        var expected = cryptographyManager.GetHmacSha256Signature(
            body.GetBuffer().AsSpan(0, (int)body.Length),
            Encoding.UTF8.GetBytes(Options.Secret));

        if (!CryptographicOperations.FixedTimeEquals(expected, sent.AsSpan(0, written)))
        {
            return AuthenticateResult.Fail("The signature is wrong.");
        }

        var identity = new ClaimsIdentity([new Claim(PermissionClaim.Type, Options.Permission)], Scheme.Name);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Logger.LogWarning("A request was refused by the scheme {Scheme}", Scheme.Name);

        return base.HandleChallengeAsync(properties);
    }
}
