using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zapper.ApiCommon.Security;

public sealed class RateLimiterConfigureOptions : IConfigureOptions<RateLimiterOptions>
{
    // the policy an operation a provider's system calls requires, Security.md section 8
    public const string ProviderPolicy = "Provider";

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly SecurityOptions security;

    private readonly ILogger<RateLimiterConfigureOptions> logger;

    public RateLimiterConfigureOptions(
        IOptions<SecurityOptions> security,
        ILogger<RateLimiterConfigureOptions> logger)
    {
        this.security = security.Value;
        this.logger = logger;
    }

    public void Configure(RateLimiterOptions options)
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(GetPartition);
        options.AddPolicy(ProviderPolicy, GetProviderPartition);
        options.OnRejected = RejectRequestAsync;
    }

    private static string GetAddress(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

    // an operation that names its own policy is counted by that policy alone, so its callers never use up the
    // allowance of every other caller from the same address
    private RateLimitPartition<string> GetPartition(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>() is not null
            ? RateLimitPartition.GetNoLimiter(string.Empty)
            : RateLimitPartition.GetFixedWindowLimiter(GetAddress(context), GetLimiterOptions);

    private RateLimitPartition<string> GetProviderPartition(HttpContext context) =>
        RateLimitPartition.GetFixedWindowLimiter(GetAddress(context), GetProviderLimiterOptions);

    private FixedWindowRateLimiterOptions GetLimiterOptions(string address) =>
        new() { PermitLimit = security.RateLimit, Window = Window, QueueLimit = 0 };

    private FixedWindowRateLimiterOptions GetProviderLimiterOptions(string address) =>
        new() { PermitLimit = security.ProviderRateLimit, Window = Window, QueueLimit = 0 };

    private ValueTask RejectRequestAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        logger.LogWarning("A request was refused for being over its rate limit");

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        return ValueTask.CompletedTask;
    }
}
