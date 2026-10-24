using Microsoft.AspNetCore.Authentication;

namespace Zapper.ApiCommon.Security;

public sealed class BasicAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    // empty when the host holds no credential, and then every request is refused
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    // the one permission a caller that proves itself is granted, Security.md section 4
    public string Permission { get; set; } = string.Empty;
}
