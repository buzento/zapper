using System.ComponentModel.DataAnnotations;

namespace Zapper.ApiCommon.Security;

public sealed class SecurityOptions
{
    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];

    [Range(1, int.MaxValue)]
    public int RequestSizeLimit { get; init; }

    [Range(1, int.MaxValue)]
    public int RateLimit { get; init; }
}
