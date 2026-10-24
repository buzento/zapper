using Zapper.Domain.Merchant;
using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Database;

public sealed class SubMerchantPersistenceIntegrationTests : IClassFixture<ClonedDatabaseFixture>
{
    private readonly ZapperDbContext database;

    public SubMerchantPersistenceIntegrationTests(ClonedDatabaseFixture clonedDatabase)
    {
        database = clonedDatabase.Database;

        // the class shares one context, so what a refused save left tracked never reaches the next test
        database.ChangeTracker.Clear();
    }

    [Fact]
    public async Task SaveSubMerchant_NewSubMerchant_RoundTripsEveryColumn()
    {
        var subMerchant = new SubMerchantEntity("ZAP0000101", "Cafe Del Sol", "Cape Town", "Western Cape", "ZA", "5812");
        database.SubMerchants.Add(subMerchant);

        await database.SaveChangesAsync(CancellationToken.None);

        var stored = await database.SubMerchants.SingleAsync(
            candidate => candidate.Id == subMerchant.Id, TestContext.Current.CancellationToken);
        stored.MerchantReference.ShouldBe("ZAP0000101");
        stored.TradingName.ShouldBe("Cafe Del Sol");
        stored.City.ShouldBe("Cape Town");
        stored.Region.ShouldBe("Western Cape");
        stored.CountryCode.ShouldBe("ZA");
        stored.MerchantCategoryCode.ShouldBe("5812");
    }

    [Fact]
    public async Task SaveChangesWithOutcome_MerchantReferenceAlreadyHeld_ReturnsAlreadyExists()
    {
        database.SubMerchants.Add(new SubMerchantEntity("ZAP0000102", "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812"));
        await database.SaveChangesAsync(CancellationToken.None);
        database.SubMerchants.Add(new SubMerchantEntity("ZAP0000102", "Other Cafe", "Durban", "KZN", "ZA", "5812"));

        var outcome = await database.SaveChangesWithOutcomeAsync(CancellationToken.None);

        outcome.ShouldBe(SaveOutcome.AlreadyExists);
    }
}
