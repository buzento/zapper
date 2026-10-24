using Zapper.Infrastructure.Integrations;
using Zapper.Infrastructure.Logging;
using Zapper.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Zapper.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddZapperInfrastructure(this IServiceCollection services) =>
        services.AddZapperPersistence()
                .AddZapperLogging()
                .AddZapperIntegration();
}
