using System.Net;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Zapper.Infrastructure.Integrations.Acme;

namespace Zapper.Infrastructure.Integrations;

public static class IntegrationRegistration
{
    // an answer is a few kilobytes; Acme adds members without notice, so the limit leaves room
    private const int LargestAnswerBytes = 64 * 1024;

    // below the 60 seconds after which Acme closes an idle connection
    private static readonly TimeSpan IdleConnectionLifetime = TimeSpan.FromSeconds(50);

    public static IServiceCollection AddZapperIntegration(this IServiceCollection services)
    {
        // a host with the worker off never resolves the client, so it never needs Acme's key, D-4
        services.AddOptions<AcmeOptions>()
                .BindConfiguration("Zapper:Acme")
                .ValidateDataAnnotations();

        services.AddSingleton<IValidateOptions<AcmeOptions>, AcmeValidateOptions>();

        services.AddHttpClient<IAcmeService, AcmeService>(ConfigureAcmeClient)
                .ConfigurePrimaryHttpMessageHandler(GetAcmePrimaryHandler)
                .AddResilienceHandler("Acme", ConfigureAcmeResilience);

        return services;
    }

    private static void ConfigureAcmeClient(IServiceProvider provider, HttpClient client)
    {
        var acme = provider.GetRequiredService<IOptions<AcmeOptions>>().Value;

        client.BaseAddress = new Uri(acme.BaseAddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acme.ApiKey);
        client.MaxResponseContentBufferSize = LargestAnswerBytes;
    }

    private static HttpMessageHandler GetAcmePrimaryHandler(IServiceProvider provider) =>
        new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            MaxConnectionsPerServer = provider.GetRequiredService<IOptions<AcmeOptions>>().Value.MaxConcurrentCalls,
            PooledConnectionIdleTimeout = IdleConnectionLifetime
        };

    // outermost first; nothing is retried here, because the worker asks Acme before anything is sent again, D-5
    private static void ConfigureAcmeResilience(
        ResiliencePipelineBuilder<HttpResponseMessage> pipeline,
        ResilienceHandlerContext context)
    {
        var acme = context.ServiceProvider.GetRequiredService<IOptions<AcmeOptions>>().Value;

        pipeline.AddRateLimiter(new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                {
                    TokenLimit = acme.RequestsPerSecond,
                    TokensPerPeriod = acme.RequestsPerSecond,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    QueueLimit = acme.MaxConcurrentCalls,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                }))
                .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = 10,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = AcmeService.BreakDuration,
                    ShouldHandle = IsAcmeFailing
                })
                .AddTimeout(AcmeService.TimeLimit);
    }

    private static ValueTask<bool> IsAcmeFailing(CircuitBreakerPredicateArguments<HttpResponseMessage> arguments) =>
        ValueTask.FromResult(
            arguments.Outcome.Exception is TimeoutRejectedException
            || arguments.Outcome.Result is
                { StatusCode: >= HttpStatusCode.InternalServerError and <= HttpStatusCode.GatewayTimeout });
}
