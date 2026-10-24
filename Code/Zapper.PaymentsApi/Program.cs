using Zapper.PaymentsApi;
using Zapper.ApiCommon;
using Zapper.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// optional, because a deployed environment gives each secret as an environment variable, BackendCode.md section 14
builder.Configuration.AddKeyPerFile("/secrets", optional: true);

builder.Services.AddZapperInfrastructure();
builder.Services.AddZapperApiCommon(1);
builder.Services.AddHostedService<StartupHostedService>();
builder.Services.AddWorkers();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseZapperApiCommon();
app.MapEndpoints();

try
{
    await app.StartAsync();
}
#pragma warning disable CA1031 // Program.cs is not an operation, so BackendCode.md section 7's rule does not reach it
catch (Exception)
#pragma warning restore CA1031
{
    return 1;
}

logger.LogInformation("{Application} has started", app.Environment.ApplicationName);

await app.WaitForShutdownAsync();

return 0;
