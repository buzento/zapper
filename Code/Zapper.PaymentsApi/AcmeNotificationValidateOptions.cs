using Microsoft.Extensions.Options;

namespace Zapper.PaymentsApi;

public sealed class AcmeNotificationValidateOptions : IValidateOptions<AcmeNotificationOptions>
{
    public ValidateOptionsResult Validate(string? name, AcmeNotificationOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        if (options.IsEnabled is not true)
        {
            return builder.Build();
        }

        if (!Guid.TryParse(options.MerchantId, out _))
        {
            builder.AddError("MerchantId must be a GUID.", nameof(AcmeNotificationOptions.MerchantId));
        }

        if (options.PaymentAdviceUserName.Length == 0)
        {
            builder.AddError(
                "PaymentAdviceUserName is required.",
                nameof(AcmeNotificationOptions.PaymentAdviceUserName));
        }

        if (options.PaymentAdvicePassword.Length == 0)
        {
            builder.AddError(
                "PaymentAdvicePassword is required.",
                nameof(AcmeNotificationOptions.PaymentAdvicePassword));
        }

        if (options.WebhookSecret.Length == 0)
        {
            builder.AddError("WebhookSecret is required.", nameof(AcmeNotificationOptions.WebhookSecret));
        }

        return builder.Build();
    }
}
