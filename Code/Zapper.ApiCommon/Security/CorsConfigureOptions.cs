using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace Zapper.ApiCommon.Security;

public sealed class CorsConfigureOptions : IConfigureOptions<CorsOptions>
{
    private readonly SecurityOptions security;

    public CorsConfigureOptions(IOptions<SecurityOptions> security) =>
        this.security = security.Value;

    public void Configure(CorsOptions options) => options.AddDefaultPolicy(ConfigurePolicy);

    private void ConfigurePolicy(CorsPolicyBuilder policy) =>
        policy.WithOrigins([.. security.AllowedOrigins])
              .AllowAnyHeader()
              .AllowAnyMethod();
}
