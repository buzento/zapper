using System.ComponentModel.DataAnnotations;

namespace Zapper.PaymentsApi;

public sealed class AcmeNotificationOptions
{
    // true on the service that answers requests and false on the one that does background work, so only the first
    // holds the three secrets
    [Required]
    public bool? IsEnabled { get; init; }

    // Zapper's account at Acme, read here rather than from AcmeOptions, whose API key this service must not hold
    public string MerchantId { get; init; } = string.Empty;

    public string PaymentAdviceUserName { get; init; } = string.Empty;

    public string PaymentAdvicePassword { get; init; } = string.Empty;

    public string WebhookSecret { get; init; } = string.Empty;
}
