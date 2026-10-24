using System.Text;
using Zapper.ApiCommon.Managers.Cryptography;
using Zapper.Domain.Customer;
using Zapper.Domain.Merchant;
using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Zapper.Migrator.Seeding;

public static class LocalSeeder
{
    // one card per behaviour of the Acme stub, which chooses its behaviour by the token, D-12 and D-14
    private static readonly (string CustomerReference, string EmailAddress, string Token)[] StoredCards =
    [
        ("CUS-APPROVE", "approve@example.test", "tok_test_approve"),
        ("CUS-DECLINE-05", "decline-05@example.test", "tok_test_decline_05"),
        ("CUS-DECLINE-51", "decline-51@example.test", "tok_test_decline_51"),
        ("CUS-DECLINE-54", "decline-54@example.test", "tok_test_decline_54"),
        ("CUS-ADVICE-TWICE", "advice-twice@example.test", "tok_test_advice_twice"),
        ("CUS-REVERSAL-FIRST", "reversal-first@example.test", "tok_test_reversal_first"),
        ("CUS-LATE-ANSWER", "late-answer@example.test", "tok_test_late_answer"),
        ("CUS-NEVER-RECEIVED", "never-received@example.test", "tok_test_never_received"),
        ("CUS-GATEWAY-ERROR-ONCE", "gateway-error-once@example.test", "tok_test_gateway_error_once")
    ];

    public static async Task SeedAsync(
        ZapperDbContext database,
        ICryptographyManager cryptography,
        TokenEncryptionOptions tokenEncryption)
    {
        // the second has a trading name past 23 characters and the third a city past 13, so both are cut, D-12
        SubMerchantEntity[] subMerchants =
        [
            new("ZAP1000001", "Harbour Lane Bakery", "Cape Town", "WC", "ZA", "5462"),
            new("ZAP1000002", "Karoo Mountain Coffee Roastery", "Durban", "KZN", "ZA", "5814"),
            new("ZAP1000003", "Fynbos Books", "Port Elizabeth North", "EC", "ZA", "5942")
        ];

        var key = Convert.FromBase64String(tokenEncryption.Key);
        var merchantReferences = subMerchants.Select(subMerchant => subMerchant.MerchantReference).ToList();
        var customerReferences = StoredCards.Select(storedCard => storedCard.CustomerReference).ToList();

        var heldMerchantReferences = await database.SubMerchants
            .Where(subMerchant => merchantReferences.Contains(subMerchant.MerchantReference))
            .Select(subMerchant => subMerchant.MerchantReference)
            .ToListAsync();

        var heldCustomerReferences = await database.StoredCards
            .Where(storedCard => customerReferences.Contains(storedCard.CustomerReference))
            .Select(storedCard => storedCard.CustomerReference)
            .ToListAsync();

        database.SubMerchants.AddRange(subMerchants
            .Where(subMerchant => !heldMerchantReferences.Contains(subMerchant.MerchantReference)));

        database.StoredCards.AddRange(StoredCards
            .Where(storedCard => !heldCustomerReferences.Contains(storedCard.CustomerReference))
            .Select(storedCard => new StoredCardEntity(
                storedCard.CustomerReference,
                cryptography.Encrypt(storedCard.Token, key, Encoding.UTF8.GetBytes(storedCard.CustomerReference)),
                storedCard.EmailAddress)));

        await database.SaveChangesAsync(CancellationToken.None);
    }
}
