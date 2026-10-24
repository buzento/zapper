using Zapper.ApiCommon.Managers;
using Zapper.Infrastructure;
using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
#if DEBUG
using Zapper.ApiCommon.Managers.Cryptography;
using Zapper.Migrator.Seeding;
using Microsoft.Extensions.Options;
#endif

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// optional, because a deployed environment gives each secret as an environment variable, BackendCode.md section 14
builder.Configuration.AddKeyPerFile("/secrets", optional: true);

builder.Services.AddZapperInfrastructure();
builder.Services.AddZapperManagers();

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

#if DEBUG
try
{
    using var scope = host.Services.CreateScope();

    // reading the options runs their validation, so the seed refuses to run without the card key
    var tokenEncryption = scope.ServiceProvider.GetRequiredService<IOptions<TokenEncryptionOptions>>().Value;

    await LocalSeeder.SeedAsync(
        scope.ServiceProvider.GetRequiredService<ZapperDbContext>(),
        scope.ServiceProvider.GetRequiredService<ICryptographyManager>(),
        tokenEncryption);
}
#pragma warning disable CA1031 // Program.cs is not an operation, so BackendCode.md section 7's rule does not reach it
catch (Exception exception)
#pragma warning restore CA1031
{
    logger.LogError(exception, "The local data could not be seeded");

    return 1;
}
#endif

await host.StopAsync();

return 0;
