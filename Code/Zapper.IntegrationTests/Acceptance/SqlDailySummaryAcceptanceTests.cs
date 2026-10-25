using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Zapper.ApiCommon.Managers.Cryptography;
using Zapper.Domain.Customer;
using Zapper.Domain.Merchant;
using Zapper.Domain.Payment;
using Zapper.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Acceptance;

[Collection("Acceptance")]
public sealed class SqlDailySummaryAcceptanceTests : AcceptanceTestBase
{
    private const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    // Acme writes its times in South African time, with no offset, and a summary's day is a South African day
    private static readonly TimeSpan SouthAfricanOffset = TimeSpan.FromHours(2);

    // the one line a person changes, and the only line a test replaces, D-16
    private static readonly Regex DateLine = new("^SELECT DATE '[0-9]{4}-[0-9]{2}-[0-9]{2}' AS \"Day\"$", RegexOptions.Multiline);

    private static readonly string[] ColumnNames =
    [
        "SubMerchantReference", "TradingName", "CurrencyCode", "ApprovedCount", "ApprovedValue", "DeclinedCount",
        "DeclinedValue", "ReversalCount", "ReversalValue", "RefundCount", "RefundValue", "PendingCount", "PendingValue",
        "FailedCount", "FailedValue", "UnresolvedCount", "UnresolvedValue"
    ];

    private readonly AcceptanceFixture acceptance;

    private readonly ZapperDbContext database;

    private int paymentCount;

    private int eventCount;

    public SqlDailySummaryAcceptanceTests(AcceptanceFixture acceptance)
    {
        this.acceptance = acceptance;
        database = acceptance.Database;
    }

    // a sub-merchant reference is at most 15 characters, so the discriminator takes the place of the scenario's digits
    // after the first, which keeps the two references in the scenario's order
    private static string CafeDelSolReference => $"ZA4{Discriminator}";

    private static string BookNookReference => $"ZA5{Discriminator}";

    private static string CustomerReference => $"CUS-4001-{Discriminator}";

    private static string EmailAddress => $"cus-4001-{Discriminator}@example.test";

    private static string CardToken => $"test-token-{Discriminator}";

    [Fact(DisplayName = "Approved payments are counted with the authorised amount")]
    [Trait("Rule", "R-3")]
    public async Task ApprovedPaymentsAreCountedWithTheAuthorisedAmount()
    {
        await ArrangeBackgroundAsync();
        var first = await RecordAsync(CafeDelSolReference, Currency.Zar, 1999, SouthAfricanTime(2, 10, 0));
        Approve(first, 1999, SouthAfricanTime(2, 10, 0));
        var second = await RecordAsync(CafeDelSolReference, Currency.Zar, 1999, SouthAfricanTime(2, 11, 0));
        Approve(second, 1500, SouthAfricanTime(2, 11, 0));
        await SaveAsync(first, second);

        var rows = await RunSummaryAsync(new DateOnly(2040, 3, 2));

        var row = GetRow(rows, CafeDelSolReference, "ZAR");
        (row.ApprovedCount, row.ApprovedValue).ShouldBe((2, "34.99"));
    }

    [Fact(DisplayName = "Declined payments are counted with the requested amount")]
    [Trait("Rule", "R-4")]
    public async Task DeclinedPaymentsAreCountedWithTheRequestedAmount()
    {
        await ArrangeBackgroundAsync();
        var payment = await RecordAsync(CafeDelSolReference, Currency.Zar, 2500, SouthAfricanTime(2, 10, 0));
        Decline(payment, "51", SouthAfricanTime(2, 10, 0));
        await SaveAsync(payment);

        var rows = await RunSummaryAsync(new DateOnly(2040, 3, 2));

        var row = GetRow(rows, CafeDelSolReference, "ZAR");
        (row.DeclinedCount, row.DeclinedValue).ShouldBe((1, "25.00"));
    }

    [Fact(DisplayName = "Day runs from midnight to midnight South African time")]
    [Trait("Rule", "R-1")]
    public async Task DayRunsFromMidnightToMidnightSouthAfricanTime()
    {
        await ArrangeBackgroundAsync();
        var atStart = await RecordAsync(CafeDelSolReference, Currency.Zar, 1000, SouthAfricanTime(2, 0, 0));
        Approve(atStart, 1000, SouthAfricanTime(2, 0, 0));
        var beforeEnd = await RecordAsync(CafeDelSolReference, Currency.Zar, 2000, SouthAfricanTime(2, 23, 59, 59, 999));
        Approve(beforeEnd, 2000, SouthAfricanTime(2, 23, 59, 59, 999));
        var atEnd = await RecordAsync(CafeDelSolReference, Currency.Zar, 4000, SouthAfricanTime(3, 0, 0));
        Approve(atEnd, 4000, SouthAfricanTime(3, 0, 0));
        await SaveAsync(atStart, beforeEnd, atEnd);

        var rows = await RunSummaryAsync(new DateOnly(2040, 3, 2));

        var row = GetRow(rows, CafeDelSolReference, "ZAR");
        (row.ApprovedCount, row.ApprovedValue).ShouldBe((2, "30.00"));
    }

    [Fact(DisplayName = "Reversal on a later day counts on its own day")]
    [Trait("Rule", "R-3")]
    [Trait("Rule", "R-5")]
    public async Task ReversalOnALaterDayCountsOnItsOwnDay()
    {
        await ArrangeBackgroundAsync();
        var payment = await RecordAsync(CafeDelSolReference, Currency.Zar, 1999, SouthAfricanTime(2, 10, 0));
        Approve(payment, 1999, SouthAfricanTime(2, 10, 0));
        RecordTransactionStatus(payment, "Reversal", 1999, SouthAfricanTime(3, 9, 0));
        await SaveAsync(payment);

        var firstDay = await RunSummaryAsync(new DateOnly(2040, 3, 2));
        var secondDay = await RunSummaryAsync(new DateOnly(2040, 3, 3));

        var first = GetRow(firstDay, CafeDelSolReference, "ZAR");
        (first.ApprovedCount, first.ApprovedValue, first.ReversalCount, first.ReversalValue).ShouldBe((1, "19.99", 0, "0.00"));
        var second = GetRow(secondDay, CafeDelSolReference, "ZAR");
        (second.ApprovedCount, second.ApprovedValue, second.ReversalCount, second.ReversalValue).ShouldBe((0, "0.00", 1, "19.99"));
    }

    [Fact(DisplayName = "Several partial refunds are each counted")]
    [Trait("Rule", "R-5")]
    public async Task SeveralPartialRefundsAreEachCounted()
    {
        await ArrangeBackgroundAsync();
        var payment = await RecordAsync(CafeDelSolReference, Currency.Zar, 1999, SouthAfricanTime(2, 10, 0));
        Approve(payment, 1999, SouthAfricanTime(2, 10, 0));
        RecordTransactionStatus(payment, "Refund", 500, SouthAfricanTime(2, 12, 0));
        RecordTransactionStatus(payment, "Refund", 700, SouthAfricanTime(2, 15, 0));
        await SaveAsync(payment);

        var rows = await RunSummaryAsync(new DateOnly(2040, 3, 2));

        var row = GetRow(rows, CafeDelSolReference, "ZAR");
        (row.RefundCount, row.RefundValue).ShouldBe((2, "12.00"));
    }

    [Fact(DisplayName = "Reversal or refund that was not applied is not counted")]
    [Trait("Rule", "R-5")]
    public async Task ReversalOrRefundThatWasNotAppliedIsNotCounted()
    {
        await ArrangeBackgroundAsync();
        var approved = await RecordAsync(CafeDelSolReference, Currency.Zar, 1999, SouthAfricanTime(2, 10, 0));
        Approve(approved, 1999, SouthAfricanTime(2, 10, 0));
        RecordTransactionStatus(approved, "Refund", 500, SouthAfricanTime(2, 12, 0), isApproved: false);
        var notYetAnswered = await RecordAsync(CafeDelSolReference, Currency.Zar, 1000, SouthAfricanTime(2, 13, 0));
        RecordTransactionStatus(notYetAnswered, "Reversal", 1000, SouthAfricanTime(2, 13, 5));
        await SaveAsync(approved, notYetAnswered);

        var rows = await RunSummaryAsync(new DateOnly(2040, 3, 2));

        var row = GetRow(rows, CafeDelSolReference, "ZAR");
        (row.RefundCount, row.RefundValue, row.ReversalCount, row.ReversalValue).ShouldBe((0, "0.00", 0, "0.00"));
    }

    [Fact(DisplayName = "Pending, failed and unresolved payments count on the day they were recorded")]
    [Trait("Rule", "R-6")]
    public async Task PendingFailedAndUnresolvedPaymentsCountOnTheDayTheyWereRecorded()
    {
        await ArrangeBackgroundAsync();
        var notYetAnswered = await RecordAsync(CafeDelSolReference, Currency.Zar, 1000, SouthAfricanTime(2, 9, 0));
        var failed = await RecordAsync(CafeDelSolReference, Currency.Zar, 2000, SouthAfricanTime(2, 23, 0));
        Fail(failed, SouthAfricanTime(3, 0, 30));
        var unresolved = await RecordAsync(CafeDelSolReference, Currency.Zar, 4000, SouthAfricanTime(2, 10, 0));
        TimeOutThenRefuseQuery(unresolved, SouthAfricanTime(2, 10, 0));
        await SaveAsync(notYetAnswered, failed, unresolved);

        var rows = await RunSummaryAsync(new DateOnly(2040, 3, 2));

        var row = GetRow(rows, CafeDelSolReference, "ZAR");
        (row.PendingCount, row.PendingValue, row.FailedCount, row.FailedValue, row.UnresolvedCount, row.UnresolvedValue)
            .ShouldBe((1, "10.00", 1, "20.00", 1, "40.00"));
    }

    [Fact(DisplayName = "Unresolved payment that Acme approved counts as approved and as unresolved")]
    [Trait("Rule", "R-3")]
    [Trait("Rule", "R-6")]
    public async Task UnresolvedPaymentThatAcmeApprovedCountsAsApprovedAndAsUnresolved()
    {
        await ArrangeBackgroundAsync();
        var payment = await RecordAsync(CafeDelSolReference, Currency.Zar, 1999, SouthAfricanTime(2, 10, 0));
        Approve(payment, 1999, SouthAfricanTime(2, 10, 0));
        RecordTransactionStatus(payment, "Refund", 2500, SouthAfricanTime(2, 12, 0));
        await SaveAsync(payment);

        var rows = await RunSummaryAsync(new DateOnly(2040, 3, 2));

        var row = GetRow(rows, CafeDelSolReference, "ZAR");
        (row.ApprovedCount, row.ApprovedValue, row.RefundCount, row.RefundValue, row.UnresolvedCount, row.UnresolvedValue)
            .ShouldBe((1, "19.99", 0, "0.00", 1, "19.99"));
    }

    [Fact(DisplayName = "Each sub-merchant and currency has its own row")]
    [Trait("Rule", "R-2")]
    public async Task EachSubMerchantAndCurrencyHasItsOwnRow()
    {
        await ArrangeBackgroundAsync();
        var inRand = await RecordAsync(CafeDelSolReference, Currency.Zar, 1000, SouthAfricanTime(2, 10, 0));
        Approve(inRand, 1000, SouthAfricanTime(2, 10, 0));
        var inDollars = await RecordAsync(CafeDelSolReference, Currency.Usd, 2000, SouthAfricanTime(2, 10, 0));
        Approve(inDollars, 2000, SouthAfricanTime(2, 10, 0));
        var atBookNook = await RecordAsync(BookNookReference, Currency.Zar, 4000, SouthAfricanTime(2, 10, 0));
        Approve(atBookNook, 4000, SouthAfricanTime(2, 10, 0));
        await SaveAsync(inRand, inDollars, atBookNook);

        var rows = await RunSummaryAsync(new DateOnly(2040, 3, 2));

        GetOwnRows(rows)
            .Select(row => (row.SubMerchantReference, row.TradingName, row.CurrencyCode))
            .ShouldBe(
            [
                (CafeDelSolReference, "Cafe Del Sol", "USD"),
                (CafeDelSolReference, "Cafe Del Sol", "ZAR"),
                (BookNookReference, "Book Nook", "ZAR")
            ]);
    }

    [Fact(DisplayName = "Sub-merchant with nothing on the day has no row")]
    [Trait("Rule", "R-2")]
    public async Task SubMerchantWithNothingOnTheDayHasNoRow()
    {
        await ArrangeBackgroundAsync();
        var dayBefore = await RecordAsync(BookNookReference, Currency.Zar, 1000, SouthAfricanTime(1, 10, 0));
        Approve(dayBefore, 1000, SouthAfricanTime(1, 10, 0));
        var onTheDay = await RecordAsync(CafeDelSolReference, Currency.Zar, 1000, SouthAfricanTime(2, 10, 0));
        Approve(onTheDay, 1000, SouthAfricanTime(2, 10, 0));
        await SaveAsync(dayBefore, onTheDay);

        var rows = await RunSummaryAsync(new DateOnly(2040, 3, 2));

        var references = GetOwnRows(rows).Select(row => row.SubMerchantReference).ToList();
        references.ShouldContain(CafeDelSolReference);
        references.ShouldNotContain(BookNookReference);
    }

    [Fact(DisplayName = "Values show two decimals and empty columns show zero")]
    [Trait("Rule", "R-7")]
    public async Task ValuesShowTwoDecimalsAndEmptyColumnsShowZero()
    {
        await ArrangeBackgroundAsync();
        var payment = await RecordAsync(CafeDelSolReference, Currency.Zar, 100000, SouthAfricanTime(2, 10, 0));
        Approve(payment, 100000, SouthAfricanTime(2, 10, 0));
        await SaveAsync(payment);

        var rows = await RunSummaryAsync(new DateOnly(2040, 3, 2));

        GetRow(rows, CafeDelSolReference, "ZAR").ShouldBe(new SummaryRow(
            CafeDelSolReference, "Cafe Del Sol", "ZAR",
            1, "1000.00",
            0, "0.00",
            0, "0.00",
            0, "0.00",
            0, "0.00",
            0, "0.00",
            0, "0.00"));
    }

    [Fact(DisplayName = "Data reader reads every table and changes none")]
    [Trait("Rule", "R-8")]
    public async Task DataReaderReadsEveryTableAndChangesNone()
    {
        await ArrangeBackgroundAsync();
        var payment = await RecordAsync(CafeDelSolReference, Currency.Zar, 1000, SouthAfricanTime(2, 9, 0));
        await SaveAsync(payment);
        await using var connection = await OpenAsDataReaderAsync();

        var read = () => ExecuteAsync(connection, GetReadOfEveryTable());
        var change = () => ExecuteAsync(
            connection,
            "UPDATE \"Payment\".\"Payment\" SET \"Amount\" = \"Amount\" + 1 WHERE \"Id\" = @id",
            new NpgsqlParameter("id", payment.Id));

        await read.ShouldNotThrowAsync();
        (await change.ShouldThrowAsync<PostgresException>()).SqlState.ShouldBe(PostgresErrorCodes.InsufficientPrivilege);
    }

    // every scenario's day is in March 2040, after any time the acceptance host's clock reaches, so the worker never
    // claims a payment a test makes; every stored time is UTC
    private static DateTimeOffset SouthAfricanTime(int day, int hour, int minute, int second = 0, int millisecond = 0) =>
        new DateTimeOffset(2040, 3, day, hour, minute, second, millisecond, SouthAfricanOffset).ToUniversalTime();

    private static SummaryRow GetRow(List<SummaryRow> rows, string subMerchantReference, string currencyCode) =>
        rows.Where(row => row.SubMerchantReference == subMerchantReference && row.CurrencyCode == currencyCode)
            .ShouldHaveSingleItem();

    // the run shares one database, so a Then reads only the rows of the test's own sub-merchants
    private static List<SummaryRow> GetOwnRows(List<SummaryRow> rows) =>
        [.. rows.Where(row => row.SubMerchantReference == CafeDelSolReference || row.SubMerchantReference == BookNookReference)];

    private async Task ArrangeBackgroundAsync()
    {
        database.SubMerchants.Add(new SubMerchantEntity(CafeDelSolReference, "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812"));
        database.SubMerchants.Add(new SubMerchantEntity(BookNookReference, "Book Nook", "Johannesburg", "GP", "ZA", "5942"));
        database.StoredCards.Add(new StoredCardEntity(
            CustomerReference,
            new CryptographyManager().Encrypt(CardToken, acceptance.TokenKey, Encoding.UTF8.GetBytes(CustomerReference)),
            EmailAddress));

        await database.SaveChangesAsync(CancellationToken.None);
        database.ChangeTracker.Clear();
    }

    // "a payment ... recorded at": made by its own constructor at the stated time, and never added to the run's
    // context until the test has moved it into its state
    private async Task<PaymentEntity> RecordAsync(
        string subMerchantReference,
        Currency currency,
        long amount,
        DateTimeOffset recordedAt)
    {
        var storedCard = await database.StoredCards.SingleAsync(
            candidate => candidate.CustomerReference == CustomerReference, TestContext.Current.CancellationToken);
        var subMerchant = await database.SubMerchants.SingleAsync(
            candidate => candidate.MerchantReference == subMerchantReference, TestContext.Current.CancellationToken);
        var currencyType = await database.CurrencyTypes.SingleAsync(
            candidate => candidate.Id == currency, TestContext.Current.CancellationToken);
        paymentCount++;

        return new PaymentEntity(
            storedCard, subMerchant, currencyType, amount, $"SUM-{paymentCount}-{Discriminator}", new FakeTimeProvider(recordedAt));
    }

    // "that Acme approved for <amount> at <time>": the send's answer, with Acme's time being the time of the answer
    private static void Approve(PaymentEntity payment, long authorisedAmount, DateTimeOffset at) =>
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
                at),
            null,
            new FakeTimeProvider(at));

    private static void Decline(PaymentEntity payment, string responseCode, DateTimeOffset at) =>
        payment.RecordSendAnswer(
            new AcmeAnswer(
                AcmeAnswerType.Success,
                "Acquirer",
                responseCode,
                "Insufficient funds",
                "There was not enough money in the account.",
                reconciliationId: "MR747891457",
                retrievalReferenceNumber: payment.OrderNumber,
                acmeTransactionAt: at),
            null,
            new FakeTimeProvider(at));

    private static void Fail(PaymentEntity payment, DateTimeOffset at) =>
        payment.RecordSendAnswer(
            new AcmeAnswer(
                AcmeAnswerType.Failure,
                "Acquirer",
                "91",
                "Issuer or switch inoperative",
                "The bank could not be reached. Please try again.",
                reconciliationId: "MR747891457",
                retrievalReferenceNumber: payment.OrderNumber,
                acmeTransactionAt: at),
            null,
            new FakeTimeProvider(at));

    // "whose send timed out and whose query Acme refused": the outcome is unknown, then the query is refused
    private static void TimeOutThenRefuseQuery(PaymentEntity payment, DateTimeOffset at)
    {
        var time = new FakeTimeProvider(at);
        payment.RecordSendAnswer(new AcmeAnswer(AcmeAnswerType.TimedOut), null, time);
        payment.RecordQueryAnswer(
            new AcmeAnswer(AcmeAnswerType.Refused, "Application", "E02", "The API key is missing or not valid"),
            null,
            time);
    }

    // "Acme Switch reversed it" or "refunded it": a Transaction Status as Appendix A shapes it, recorded by the payment's
    // own method at the time Acme Switch processed it; each event of one test has its own identity, carrying the
    // discriminator
    private void RecordTransactionStatus(
        PaymentEntity payment,
        string transactionType,
        long amount,
        DateTimeOffset processedAt,
        bool isApproved = true)
    {
        eventCount++;
        var transactionUuid = $"00000000-0000-4000-{0x8000 + eventCount:x4}-{Discriminator}";
        var body = new JsonObject
        {
            ["transactionUUID"] = transactionUuid,
            ["transactionType"] = transactionType,
            ["transactionDateTime"] = processedAt.ToOffset(SouthAfricanOffset).ToString(TimeFormat, CultureInfo.InvariantCulture),
            ["rrn"] = payment.OrderNumber,
            ["transactionAmount"] = amount,
            ["currencyCode"] = "ZAR",
            ["txResultCode"] = "00",
            ["isApproved"] = isApproved
        };
        var status = new AcmeTransactionStatus(
            transactionUuid, transactionType, processedAt, payment.OrderNumber, amount, "ZAR", "00", isApproved);
        var time = new FakeTimeProvider(processedAt);

        // no request carried it, so it has no trace id
        var notification = new NotificationEntity(
            NotificationSource.TransactionStatus,
            transactionUuid,
            null,
            SHA256.HashData(Encoding.UTF8.GetBytes(status.GetContent())),
            body.ToJsonString(),
            null,
            null,
            string.Empty,
            time);

        payment.RecordTransactionStatus(notification, status, [], time);
    }

    // a payment brings its changes, its events and its notifications with it
    private async Task SaveAsync(params PaymentEntity[] payments)
    {
        database.Payments.AddRange(payments);

        await database.SaveChangesAsync(CancellationToken.None);
        database.ChangeTracker.Clear();
    }

    // "the summary runs": the file, with only its date line replaced, run as the data reader, D-16
    private async Task<List<SummaryRow>> RunSummaryAsync(DateOnly day)
    {
        var path = GetSummaryPath();
        File.Exists(path).ShouldBeTrue($"{path} should exist");
        var summary = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        DateLine.Matches(summary).Count.ShouldBe(1);
        var query = DateLine.Replace(
            summary, $"SELECT DATE '{day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}' AS \"Day\"");

        await using var connection = await OpenAsDataReaderAsync();
        await using var command = new NpgsqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ShouldBe(ColumnNames);
        var rows = new List<SummaryRow>();

        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            rows.Add(ReadRow(reader));
        }

        return rows;
    }

    // the test container gives the reader no certificate, so the container's superuser takes the reader's role; the
    // connection is never pooled, so the role never reaches the run's context
    private async Task<NpgsqlConnection> OpenAsDataReaderAsync()
    {
        // the run's context was given its connection string when it was made, so it holds one
        var connectionString = new NpgsqlConnectionStringBuilder(database.Database.GetConnectionString()!) { Pooling = false };
        var connection = new NpgsqlConnection(connectionString.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await ExecuteAsync(connection, "SET ROLE \"ZapperDataReaderRole\"");

        return connection;
    }

    // one statement reading every table the model maps, so a table the reader cannot read fails it
    private string GetReadOfEveryTable() =>
        "SELECT " + string.Join(
            ", ",
            database.Model.GetEntityTypes()
                .Select(entity => $"(SELECT count(*) FROM \"{entity.GetSchema()}\".\"{entity.GetTableName()}\")"));

    private static async Task ExecuteAsync(NpgsqlConnection connection, string statement, params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(statement, connection);
        command.Parameters.AddRange(parameters);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static SummaryRow ReadRow(NpgsqlDataReader reader) =>
        new(
            reader.GetString(reader.GetOrdinal("SubMerchantReference")),
            reader.GetString(reader.GetOrdinal("TradingName")),
            reader.GetString(reader.GetOrdinal("CurrencyCode")),
            GetCount(reader, "ApprovedCount"),
            GetValue(reader, "ApprovedValue"),
            GetCount(reader, "DeclinedCount"),
            GetValue(reader, "DeclinedValue"),
            GetCount(reader, "ReversalCount"),
            GetValue(reader, "ReversalValue"),
            GetCount(reader, "RefundCount"),
            GetValue(reader, "RefundValue"),
            GetCount(reader, "PendingCount"),
            GetValue(reader, "PendingValue"),
            GetCount(reader, "FailedCount"),
            GetValue(reader, "FailedValue"),
            GetCount(reader, "UnresolvedCount"),
            GetValue(reader, "UnresolvedValue"));

    private static long GetCount(NpgsqlDataReader reader, string column) =>
        reader.GetFieldValue<long>(reader.GetOrdinal(column));

    // the value as the database wrote it, so its decimal places are what a person sees, R-7
    private static string GetValue(NpgsqlDataReader reader, string column) =>
        reader.GetFieldValue<decimal>(reader.GetOrdinal(column)).ToString(CultureInfo.InvariantCulture);

    private static string GetSummaryPath()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);

        while (folder is not null && !folder.EnumerateFiles("*.slnx").Any())
        {
            folder = folder.Parent;
        }

        // the test assembly is built inside Code/, which holds the solution file
        return Path.Combine(folder!.Parent!.FullName, "Docs", "Reports", "DailyPaymentSummary.sql");
    }

    private sealed record SummaryRow(
        string SubMerchantReference,
        string TradingName,
        string CurrencyCode,
        long ApprovedCount,
        string ApprovedValue,
        long DeclinedCount,
        string DeclinedValue,
        long ReversalCount,
        string ReversalValue,
        long RefundCount,
        string RefundValue,
        long PendingCount,
        string PendingValue,
        long FailedCount,
        string FailedValue,
        long UnresolvedCount,
        string UnresolvedValue);
}
