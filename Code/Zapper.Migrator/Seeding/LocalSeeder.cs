using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Zapper.ApiCommon.Managers.Cryptography;
using Zapper.Domain.Customer;
using Zapper.Domain.Merchant;
using Zapper.Domain.Payment;
using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Zapper.Migrator.Seeding;

internal static class LocalSeeder
{
    private const string ApprovingCustomerReference = "CUS-APPROVE";

    private const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    private const int SeededPaymentCount = 18;

    // Acme writes its times in South African time, with no offset, and the summary's day is a South African day
    private static readonly TimeSpan SouthAfricanOffset = TimeSpan.FromHours(2);

    // each seeded payment is answered one second after it was recorded, and Acme's time is that answer's time
    private static readonly TimeSpan AnswerDelay = TimeSpan.FromSeconds(1);

    // one card per behaviour of the Acme stub, which chooses its behaviour by the token, D-12 and D-14
    private static readonly (string CustomerReference, string EmailAddress, string Token)[] StoredCards =
    [
        (ApprovingCustomerReference, "approve@example.test", "tok_test_approve"),
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
        TokenEncryptionOptions tokenEncryption,
        TimeProvider time,
        ILogger<LocalSeederLogCategory> logger)
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

        await SeedPaymentsAsync(database, cryptography, time, merchantReferences[0], merchantReferences[1]);

        // read back, so the days logged are the ones the payments are on, whichever run wrote them
        var firstDay = await GetSouthAfricanRecordingDateAsync(database, "SEED-01");
        var secondDay = await GetSouthAfricanRecordingDateAsync(database, "SEED-02");

        logger.LogInformation(
            "The seeded payments are on {FirstDay} and {SecondDay}, the days to put into the daily summary",
            firstDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            secondDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    // eighteen finished payments over the two South African days before the first run, in every final status, at two
    // sub-merchants and in two currencies, so the daily summary shows data on the first run; each is written only where
    // no payment holds its reference, so the payments stay on the days of the first run, D-12
    private static async Task SeedPaymentsAsync(
        ZapperDbContext database,
        ICryptographyManager cryptography,
        TimeProvider time,
        string firstMerchantReference,
        string secondMerchantReference)
    {
        var references = Enumerable.Range(1, SeededPaymentCount).Select(GetSeedReference).ToList();

        var heldReferences = await database.Payments
            .Where(payment => references.Contains(payment.AppPaymentReference))
            .Select(payment => payment.AppPaymentReference)
            .ToListAsync();

        var storedCard = await database.StoredCards
            .SingleAsync(candidate => candidate.CustomerReference == ApprovingCustomerReference);
        var firstSubMerchant = await database.SubMerchants
            .SingleAsync(candidate => candidate.MerchantReference == firstMerchantReference);
        var secondSubMerchant = await database.SubMerchants
            .SingleAsync(candidate => candidate.MerchantReference == secondMerchantReference);
        var zar = await database.CurrencyTypes.SingleAsync(candidate => candidate.Id == Currency.Zar);
        var usd = await database.CurrencyTypes.SingleAsync(candidate => candidate.Id == Currency.Usd);

        var today = DateOnly.FromDateTime((time.GetUtcNow() + SouthAfricanOffset).UtcDateTime);
        var dayOne = today.AddDays(-2);
        var dayTwo = today.AddDays(-1);
        var seedTime = new SeedTimeProvider();

        PaymentEntity Record(int number, SubMerchantEntity subMerchant, CurrencyTypeEntity currency, DateTimeOffset recordedAt, long amount)
        {
            seedTime.SetUtcNow(recordedAt);

            return new PaymentEntity(storedCard, subMerchant, currency, amount, GetSeedReference(number), seedTime);
        }

        PaymentEntity RecordTransactionStatus(
            PaymentEntity payment,
            string transactionType,
            long amount,
            DateTimeOffset processedAt,
            bool isApproved = true)
        {
            seedTime.SetUtcNow(processedAt);
            var currencyCode = payment.Currency.ToString().ToUpperInvariant();

            // Acme Switch names each event with its own identity
            var transactionUuid = Guid.NewGuid().ToString();
            var status = new AcmeTransactionStatus(
                transactionUuid, transactionType, processedAt, payment.OrderNumber, amount, currencyCode, "00", isApproved);
            var body = new JsonObject
            {
                ["transactionUUID"] = transactionUuid,
                ["transactionType"] = transactionType,
                ["transactionDateTime"] = processedAt.ToOffset(SouthAfricanOffset).ToString(TimeFormat, CultureInfo.InvariantCulture),
                ["rrn"] = payment.OrderNumber,
                ["transactionAmount"] = amount,
                ["currencyCode"] = currencyCode,
                ["txResultCode"] = "00",
                ["isApproved"] = isApproved
            };

            // no request carried it, so it has no trace id
            var notification = new NotificationEntity(
                NotificationSource.TransactionStatus,
                transactionUuid,
                null,
                cryptography.GetSha256Hash(status.GetContent()),
                body.ToJsonString(),
                null,
                null,
                string.Empty,
                seedTime);

            payment.RecordTransactionStatus(notification, status, [], seedTime);

            return payment;
        }

        PaymentEntity[] payments =
        [
            Approve(Record(1, firstSubMerchant, zar, At(dayOne, 10, 0), 15000), 15000, seedTime),
            Approve(Record(2, firstSubMerchant, zar, At(dayTwo, 11, 0), 8000), 6000, seedTime),
            Approve(Record(3, secondSubMerchant, usd, At(dayOne, 9, 30), 2500), 2500, seedTime),
            Decline(Record(4, firstSubMerchant, zar, At(dayOne, 12, 0), 4500), "51", seedTime),
            Decline(Record(5, secondSubMerchant, usd, At(dayTwo, 14, 0), 1200), "05", seedTime),
            Fail(Record(6, firstSubMerchant, usd, At(dayOne, 15, 0), 3000), seedTime),
            Fail(Record(7, secondSubMerchant, zar, At(dayTwo, 16, 0), 7000), seedTime),
            RecordTransactionStatus(
                Approve(Record(8, firstSubMerchant, zar, At(dayOne, 13, 0), 20000), 20000, seedTime), "Reversal", 20000, At(dayTwo, 9, 0)),
            RecordTransactionStatus(
                Approve(Record(9, secondSubMerchant, usd, At(dayOne, 17, 0), 5000), 5000, seedTime), "Reversal", 5000, At(dayTwo, 10, 0)),
            RecordTransactionStatus(
                Approve(Record(10, firstSubMerchant, usd, At(dayTwo, 8, 30), 4000), 4000, seedTime), "Refund", 4000, At(dayTwo, 18, 0)),
            RecordTransactionStatus(
                RecordTransactionStatus(
                    Approve(Record(11, secondSubMerchant, zar, At(dayOne, 11, 30), 9000), 9000, seedTime), "Refund", 2000, At(dayOne, 14, 0)),
                "Refund",
                3000,
                At(dayOne, 19, 0)),

            // a refund past the authorised amount makes the payment Unresolved
            RecordTransactionStatus(
                Approve(Record(12, firstSubMerchant, zar, At(dayTwo, 12, 30), 6000), 6000, seedTime), "Refund", 9000, At(dayTwo, 15, 30)),
            TimeOutThenRefuseQuery(Record(13, secondSubMerchant, usd, At(dayOne, 8, 0), 3500), At(dayOne, 8, 5), seedTime),
            RecordTransactionStatus(
                Approve(Record(14, firstSubMerchant, usd, At(dayOne, 16, 0), 2200), 2200, seedTime),
                "Refund",
                1000,
                At(dayOne, 18, 30),
                isApproved: false),
            Approve(Record(15, secondSubMerchant, zar, At(dayTwo, 13, 30), 11000), 11000, seedTime),
            Approve(Record(16, firstSubMerchant, usd, At(dayTwo, 19, 0), 1500), 1500, seedTime),
            Decline(Record(17, secondSubMerchant, zar, At(dayOne, 10, 30), 2600), "51", seedTime),
            Approve(Record(18, secondSubMerchant, usd, At(dayTwo, 17, 30), 4200), 4000, seedTime)
        ];

        // a payment brings its changes, its events and its notifications with it
        database.Payments.AddRange(payments.Where(payment => !heldReferences.Contains(payment.AppPaymentReference)));

        await database.SaveChangesAsync(CancellationToken.None);
    }

    private static string GetSeedReference(int number) =>
        $"SEED-{number.ToString("00", CultureInfo.InvariantCulture)}";

    // a South African time, stored as UTC like every other time
    private static DateTimeOffset At(DateOnly day, int hour, int minute) =>
        new DateTimeOffset(day.Year, day.Month, day.Day, hour, minute, 0, SouthAfricanOffset).ToUniversalTime();

    private static async Task<DateOnly> GetSouthAfricanRecordingDateAsync(ZapperDbContext database, string reference)
    {
        var recordedAt = await database.Payments
            .Where(payment => payment.AppPaymentReference == reference)
            .Select(payment => payment.RecordedAt)
            .SingleAsync();

        return DateOnly.FromDateTime((recordedAt + SouthAfricanOffset).UtcDateTime);
    }

    // the send's answer, in Appendix A's shape, one second after the payment was recorded
    private static PaymentEntity Approve(PaymentEntity payment, long authorisedAmount, SeedTimeProvider time)
    {
        time.SetUtcNow(payment.RecordedAt + AnswerDelay);
        payment.RecordSendAnswer(
            new AcmeAnswer(
                AcmeAnswerType.Success,
                "Acquirer",
                "00",
                "Approved or completed successfully",
                "Approved.",
                "238699",
                authorisedAmount,
                "MR747891457",
                payment.OrderNumber,
                time.GetUtcNow()),
            null,
            time);

        return payment;
    }

    // Appendix A's description and client message for each code the seed declines with
    private static PaymentEntity Decline(PaymentEntity payment, string responseCode, SeedTimeProvider time)
    {
        var (description, clientMessage) = responseCode == "05"
            ? ("Do not honour", "Your bank declined this payment.")
            : ("Insufficient funds", "There was not enough money in the account.");

        time.SetUtcNow(payment.RecordedAt + AnswerDelay);
        payment.RecordSendAnswer(
            new AcmeAnswer(
                AcmeAnswerType.Success,
                "Acquirer",
                responseCode,
                description,
                clientMessage,
                reconciliationId: "MR747891457",
                retrievalReferenceNumber: payment.OrderNumber,
                acmeTransactionAt: time.GetUtcNow()),
            null,
            time);

        return payment;
    }

    private static PaymentEntity Fail(PaymentEntity payment, SeedTimeProvider time)
    {
        time.SetUtcNow(payment.RecordedAt + AnswerDelay);
        payment.RecordSendAnswer(
            new AcmeAnswer(
                AcmeAnswerType.Failure,
                "Acquirer",
                "91",
                "Issuer or switch inoperative",
                "The bank could not be reached. Please try again.",
                reconciliationId: "MR747891457",
                retrievalReferenceNumber: payment.OrderNumber,
                acmeTransactionAt: time.GetUtcNow()),
            null,
            time);

        return payment;
    }

    // the send times out, so the outcome is unknown, then Acme refuses the query, and the payment needs a person
    private static PaymentEntity TimeOutThenRefuseQuery(PaymentEntity payment, DateTimeOffset queriedAt, SeedTimeProvider time)
    {
        time.SetUtcNow(payment.RecordedAt + AnswerDelay);
        payment.RecordSendAnswer(new AcmeAnswer(AcmeAnswerType.TimedOut), null, time);
        time.SetUtcNow(queriedAt);
        payment.RecordQueryAnswer(
            new AcmeAnswer(AcmeAnswerType.Refused, "Application", "E02", "The API key is missing or not valid"),
            null,
            time);

        return payment;
    }
}

#pragma warning disable S2094 // ILogger<T> needs a type, and LocalSeeder is static, BackendCode.md section 15
internal sealed class LocalSeederLogCategory;
#pragma warning restore S2094
