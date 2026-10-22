using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zapper.ApiCommon.Security;

public sealed class RateLimiterConfigureOptions : IConfigureOptions<RateLimiterOptions>
{
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
        options.OnRejected = RejectRequestAsync;
    }

    private RateLimitPartition<string> GetPartition(HttpContext context) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            GetLimiterOptions);

    private FixedWindowRateLimiterOptions GetLimiterOptions(string address) =>
        new() { PermitLimit = security.RateLimit, Window = Window, QueueLimit = 0 };

    private ValueTask RejectRequestAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        logger.LogWarning("A request was refused for being over its rate limit");

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        return ValueTask.CompletedTask;
    }
}
