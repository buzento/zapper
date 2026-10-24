using Microsoft.Extensions.Options;

namespace Zapper.Infrastructure.Integrations.Acme;

public sealed class AcmeValidateOptions : IValidateOptions<AcmeOptions>
{
    public ValidateOptionsResult Validate(string? name, AcmeOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        if (!options.BaseAddress.StartsWith("https://", StringComparison.Ordinal))
        {
            builder.AddError("BaseAddress must be https.", nameof(AcmeOptions.BaseAddress));
        }

        if (!Guid.TryParse(options.MerchantId, out _))
        {
            builder.AddError("MerchantId must be a GUID.", nameof(AcmeOptions.MerchantId));
        }

        if (options.RequestsPerSecond < 1)
        {
            builder.AddError("RequestsPerSecond must be at least 1.", nameof(AcmeOptions.RequestsPerSecond));
        }

        if (options.MaxConcurrentCalls < 1)
        {
            builder.AddError("MaxConcurrentCalls must be at least 1.", nameof(AcmeOptions.MaxConcurrentCalls));
        }

        return builder.Build();
    }
}
