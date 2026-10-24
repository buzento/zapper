using Microsoft.AspNetCore.Authentication;

namespace Zapper.ApiCommon.Security;

public sealed class BodySignatureAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    // the request header that carries the Base64 HMAC-SHA256 of the exact body
    public string HeaderName { get; set; } = string.Empty;

    // empty when the host holds no secret, and then every request is refused
    public string Secret { get; set; } = string.Empty;

    // the one permission a caller that proves itself is granted, Security.md section 4
    public string Permission { get; set; } = string.Empty;
}
