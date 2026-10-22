using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Zapper.Infrastructure.Logging;

public static class LoggingRegistration
{
    public static IServiceCollection AddZapperLogging(this IServiceCollection services) =>
        services.AddSerilog(LoggingPipeline.Configure);
}
