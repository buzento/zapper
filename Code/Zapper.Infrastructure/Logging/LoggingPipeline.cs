using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Zapper.Infrastructure.Logging;

public static class LoggingPipeline
{
    private const string OutputTemplate =
        "{Timestamp:u} | {Level:u3} | Application={Application} | " +
        "Source={SourceContext} | TraceId={TraceId} | " +
        "{Message:lj}{NewLine}{Exception}";

    public static void Configure(IServiceProvider provider, LoggerConfiguration pipeline) =>
        pipeline.ReadFrom.Configuration(provider.GetRequiredService<IConfiguration>())
                .Enrich.WithProperty(
                    "Application",
                    provider.GetRequiredService<IHostEnvironment>().ApplicationName)
                .WriteTo.Console(outputTemplate: OutputTemplate);
}
