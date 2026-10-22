using Zapper.Infrastructure;
using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddZapperInfrastructure();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    await host.StartAsync();
}
#pragma warning disable CA1031 // Program.cs is not an operation, so BackendCode.md section 7's rule does not reach it
catch (Exception)
#pragma warning restore CA1031
{
    return 1;
}

logger.LogInformation("{Application} has started", builder.Environment.ApplicationName);

try
{
    using var scope = host.Services.CreateScope();

    var database = scope.ServiceProvider.GetRequiredService<ZapperDbContext>();

    await database.Database.MigrateAsync();
}
#pragma warning disable CA1031 // Program.cs is not an operation, so BackendCode.md section 7's rule does not reach it
catch (Exception exception)
#pragma warning restore CA1031
{
    logger.LogError(exception, "The migrations could not be applied");

    return 1;
}

await host.StopAsync();

return 0;
