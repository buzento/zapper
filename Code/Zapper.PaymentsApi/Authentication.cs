using Zapper.ApiCommon.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Zapper.PaymentsApi;

// Acme proves itself the way Appendix A fixes, through ApiCommon's generic handlers given Acme's values, D-7
public static class Authentication
{
    public const string PaymentAdviceScheme = "AcmePaymentAdvice";

    public const string TransactionStatusScheme = "AcmeTransactionStatus";

    public const string SendPaymentAdvice = "SendPaymentAdvice";

    public const string SendTransactionStatus = "SendTransactionStatus";

    private const string SignatureHeaderName = "x-signature";

    public static IServiceCollection AddAcmeAuthentication(this IServiceCollection services)
    {
        // without ValidateOnStart, so the startup check reads it, and a host with notifications off needs no secret
        services.AddOptions<AcmeNotificationOptions>()
                .BindConfiguration("Zapper:AcmeNotification")
                .ValidateDataAnnotations();

        services.AddSingleton<IValidateOptions<AcmeNotificationOptions>, AcmeNotificationValidateOptions>();

        services.AddAuthentication()
                .AddScheme<BasicAuthenticationSchemeOptions, BasicAuthenticationHandler>(PaymentAdviceScheme, null)
                .AddScheme<BodySignatureAuthenticationSchemeOptions, BodySignatureAuthenticationHandler>(
                    TransactionStatusScheme,
                    null);

        services.AddOptions<BasicAuthenticationSchemeOptions>(PaymentAdviceScheme)
                .Configure<IOptions<AcmeNotificationOptions>>(SetPaymentAdviceCredentials);

        services.AddOptions<BodySignatureAuthenticationSchemeOptions>(TransactionStatusScheme)
                .Configure<IOptions<AcmeNotificationOptions>>(SetTransactionStatusSecret);

        services.AddAuthorizationBuilder()
                .AddPolicy(SendPaymentAdvice, GrantedBy(PaymentAdviceScheme, SendPaymentAdvice))
                .AddPolicy(SendTransactionStatus, GrantedBy(TransactionStatusScheme, SendTransactionStatus));

        return services;
    }

    // with notifications off, the handler is given no credential and refuses every request
    private static void SetPaymentAdviceCredentials(
        BasicAuthenticationSchemeOptions scheme,
        IOptions<AcmeNotificationOptions> notification)
    {
        scheme.Permission = SendPaymentAdvice;

        if (notification.Value.IsEnabled is true)
        {
            scheme.UserName = notification.Value.PaymentAdviceUserName;
            scheme.Password = notification.Value.PaymentAdvicePassword;
        }
    }

    private static void SetTransactionStatusSecret(
        BodySignatureAuthenticationSchemeOptions scheme,
        IOptions<AcmeNotificationOptions> notification)
    {
        scheme.HeaderName = SignatureHeaderName;
        scheme.Permission = SendTransactionStatus;

        if (notification.Value.IsEnabled is true)
        {
            scheme.Secret = notification.Value.WebhookSecret;
        }
    }

    private static AuthorizationPolicy GrantedBy(string scheme, string permission) =>
        new AuthorizationPolicyBuilder(scheme)
            .RequireClaim(PermissionClaim.Type, permission)
            .Build();
}
