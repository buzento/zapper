using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace Zapper.ApiCommon.Security;

public sealed class KestrelServerConfigureOptions : IConfigureOptions<KestrelServerOptions>
{
    private readonly SecurityOptions security;

    public KestrelServerConfigureOptions(IOptions<SecurityOptions> security) =>
        this.security = security.Value;

    public void Configure(KestrelServerOptions options) =>
        options.Limits.MaxRequestBodySize = security.RequestSizeLimit;
}
