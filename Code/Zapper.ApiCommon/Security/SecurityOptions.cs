using System.ComponentModel.DataAnnotations;

namespace Zapper.ApiCommon.Security;

public sealed class SecurityOptions
{
    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];

    [Range(1, int.MaxValue)]
    public int RequestSizeLimit { get; init; }

    [Range(1, int.MaxValue)]
    public int RateLimit { get; init; }

    // each provider's system is counted by its address at this limit instead, Security.md section 8
    [Range(1, int.MaxValue)]
    public int ProviderRateLimit { get; init; }
}
