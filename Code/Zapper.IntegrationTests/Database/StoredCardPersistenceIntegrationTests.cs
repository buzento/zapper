using Zapper.Domain.Entities.Customer;
using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Database;

public sealed class StoredCardPersistenceIntegrationTests : IClassFixture<ClonedDatabaseFixture>
{
    private readonly ZapperDbContext database;

    public StoredCardPersistenceIntegrationTests(ClonedDatabaseFixture clonedDatabase)
    {
        database = clonedDatabase.Database;

        // the class shares one context, so what a refused save left tracked never reaches the next test
        database.ChangeTracker.Clear();
    }

    [Fact]
    public async Task SaveStoredCard_NewStoredCard_RoundTripsEveryColumn()
    {
        var storedCard = new StoredCardEntity("CUS-0101", [1, 2, 3, 255], "cus-0101@example.test");
        database.StoredCards.Add(storedCard);

        await database.SaveChangesAsync(CancellationToken.None);

        var stored = await database.StoredCards.SingleAsync(
            candidate => candidate.Id == storedCard.Id, TestContext.Current.CancellationToken);
        stored.CustomerReference.ShouldBe("CUS-0101");
        stored.EncryptedToken.ShouldBe([1, 2, 3, 255]);
        stored.EmailAddress.ShouldBe("cus-0101@example.test");
    }

    [Fact]
    public async Task SaveChangesWithOutcome_CustomerAlreadyHoldsStoredCard_ReturnsAlreadyExists()
    {
        database.StoredCards.Add(new StoredCardEntity("CUS-0102", [1], "cus-0102@example.test"));
        await database.SaveChangesAsync(CancellationToken.None);
        database.StoredCards.Add(new StoredCardEntity("CUS-0102", [2], "other@example.test"));

        var outcome = await database.SaveChangesWithOutcomeAsync(CancellationToken.None);

        outcome.ShouldBe(SaveOutcome.AlreadyExists);
    }
}
