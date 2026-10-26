using Microsoft.Extensions.Options;
using Npgsql;

namespace Zapper.Infrastructure.Persistence;

public sealed class DatabaseValidateOptions : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(options.ConnectionString);

        if (connectionString.SslMode is not SslMode.VerifyFull)
        {
            return ValidateOptionsResult.Fail("ConnectionString must set SSL Mode to VerifyFull.");
        }

        return connectionString.Password is null or { Length: 0 }
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("ConnectionString must carry no password.");
    }
}
