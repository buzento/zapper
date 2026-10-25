using Zapper.ApiCommon.Managers.Cryptography;
using Zapper.ApiCommon.Managers.ErrorMessage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Zapper.ApiCommon.Managers;

public static class ManagerRegistration
{
    public static IServiceCollection AddZapperManagers(this IServiceCollection services)
    {
        // here rather than beside the shared machinery, so a host that serves no API reads the clock as an API does
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IErrorMessageManager, ErrorMessageManager>();
        services.AddSingleton<ICryptographyManager, CryptographyManager>();

        // without ValidateOnStart, so a host that never uses the key never needs it, BackendCode.md section 14
        services.AddOptions<TokenEncryptionOptions>()
                .BindConfiguration("Zapper:TokenEncryption")
                .ValidateDataAnnotations();

        services.AddSingleton<IValidateOptions<TokenEncryptionOptions>, TokenEncryptionValidateOptions>();

        return services;
    }
}
