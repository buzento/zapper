using System.ComponentModel.DataAnnotations;

namespace Zapper.Infrastructure.Integrations.Acme;

public sealed class AcmeOptions
{
    [Required]
    public string BaseAddress { get; init; } = string.Empty;

    [Required]
    public string MerchantId { get; init; } = string.Empty;

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public int RequestsPerSecond { get; init; }

    [Required]
    public int MaxConcurrentCalls { get; init; }
}
