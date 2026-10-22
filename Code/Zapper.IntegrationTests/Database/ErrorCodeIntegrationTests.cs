using System.Reflection;
using Zapper.ApiCommon.Errors;
using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Database;

public sealed class ErrorCodeIntegrationTests : IClassFixture<ClonedDatabaseFixture>
{
    private static readonly List<string> ErrorMessageCodes = typeof(ErrorMessageCode)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral)
        .Select(field => field.GetRawConstantValue())
        .OfType<string>()
        .ToList();

    private readonly ZapperDbContext database;

    public ErrorCodeIntegrationTests(ClonedDatabaseFixture clonedDatabase) =>
        database = clonedDatabase.Database;

    [Fact]
    public async Task ErrorMessageCode_EveryConstant_NamesRow()
    {
        var statusCodes = await database.ErrorStatuses.Select(status => status.Code).ToListAsync(TestContext.Current.CancellationToken);
        var messageCodes = await database.ErrorMessages.Select(message => message.Code).ToListAsync(TestContext.Current.CancellationToken);

        var offenders = ErrorMessageCodes
            .Where(code => !statusCodes.Contains(code) && !messageCodes.Contains(code))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public async Task ErrorMessageCode_EveryConstant_NamesRowInOneTableOnly()
    {
        var statusCodes = await database.ErrorStatuses.Select(status => status.Code).ToListAsync(TestContext.Current.CancellationToken);
        var messageCodes = await database.ErrorMessages.Select(message => message.Code).ToListAsync(TestContext.Current.CancellationToken);

        var offenders = ErrorMessageCodes
            .Where(code => statusCodes.Contains(code) && messageCodes.Contains(code))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public async Task ErrorTables_EveryRow_HasConstant()
    {
        var statusCodes = await database.ErrorStatuses.Select(status => status.Code).ToListAsync(TestContext.Current.CancellationToken);
        var messageCodes = await database.ErrorMessages.Select(message => message.Code).ToListAsync(TestContext.Current.CancellationToken);

        var offenders = statusCodes.Concat(messageCodes)
            .Where(code => !ErrorMessageCodes.Contains(code))
            .ToList();

        offenders.ShouldBeEmpty();
    }
}
