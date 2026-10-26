using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AcmeStub;

// a stand-in for Acme Pay's payment and query routes, and for the notifications Acme Pay and Acme Switch send, as
// the brief's Appendix A describes them; everything it holds is in memory and is lost when it restarts
public sealed class Gateway
{
    private const int RequestsPerSecond = 20;

    private const int RetrievalReferenceLength = 12;

    private const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    // Acme's default when a payment names no currency
    private const string DefaultCurrencyCode = "ZAR";

    // Appendix A gives this client message on its one error example, so every error carries it
    private const string ErrorClientMessage = "We could not process that payment.";

    private static readonly TimeSpan SouthAfricanOffset = TimeSpan.FromHours(2);

    // longer than the 35 seconds the payments API waits, so the API times out
    private static readonly TimeSpan Hold = TimeSpan.FromSeconds(40);

    private static readonly TimeSpan LateAnswerResolvesAfter = TimeSpan.FromSeconds(45);

    private static readonly TimeSpan ReplayLifetime = TimeSpan.FromHours(24);

    private static readonly TimeSpan AdviceDelay = TimeSpan.FromSeconds(1);

    private static readonly string[] CardServices = ["CardNotPresent", "CardNotPresentRecurring", "GooglePay", "ApplePay"];

    // Appendix A's table of commonly seen codes
    private static readonly Dictionary<string, (string Description, string ClientMessage)> Codes = new()
    {
        ["00"] = ("Approved or completed successfully", "Approved."),
        ["05"] = ("Do not honour", "Your bank declined this payment."),
        ["51"] = ("Insufficient funds", "There was not enough money in the account."),
        ["54"] = ("Expired card", "That card has expired.")
    };

    // one card per behaviour, by the tokens the Migrator's Debug build seeds
    private static readonly Dictionary<string, Behaviour> Cards = new()
    {
        ["tok_test_approve"] = Behaviour.Approve,
        ["tok_test_decline_05"] = Behaviour.Decline05,
        ["tok_test_decline_51"] = Behaviour.Decline51,
        ["tok_test_decline_54"] = Behaviour.Decline54,
        ["tok_test_advice_twice"] = Behaviour.AdviceTwice,
        ["tok_test_reversal_first"] = Behaviour.ReversalFirst,
        ["tok_test_late_answer"] = Behaviour.LateAnswer,
        ["tok_test_never_received"] = Behaviour.NeverReceived,
        ["tok_test_gateway_error_once"] = Behaviour.GatewayErrorOnce
    };

    private readonly Lock gate = new();

    private readonly Dictionary<string, Transaction> transactions = [];

    // each Retrieval Reference Number and the TransactionID that holds it
    private readonly Dictionary<string, string> retrievalReferences = [];

    private readonly HashSet<string> firstSendsDropped = [];

    private readonly byte[] expectedAuthorization;

    private readonly byte[] expectedRoutesAuthorization;

    private readonly string merchantId;

    private readonly TimeProvider time;

    private readonly Notifier notifier;

    private readonly ILogger<Gateway> logger;

    private long rateWindow;

    private int rateWindowCount;

    public Gateway(IConfiguration configuration, TimeProvider time, Notifier notifier, ILogger<Gateway> logger)
    {
        var apiKey = configuration["AcmeStub:ApiKey"];
        var routesKey = configuration["AcmeStub:RoutesKey"];
        var configuredMerchantId = configuration["AcmeStub:MerchantId"];

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(routesKey) || !Guid.TryParse(configuredMerchantId, out _))
        {
            throw new InvalidOperationException("AcmeStub:ApiKey, AcmeStub:RoutesKey and AcmeStub:MerchantId, a GUID, are required.");
        }

        expectedAuthorization = Encoding.UTF8.GetBytes($"Bearer {apiKey}");
        expectedRoutesAuthorization = Encoding.UTF8.GetBytes($"Bearer {routesKey}");
        merchantId = configuredMerchantId;
        this.time = time;
        this.notifier = notifier;
        this.logger = logger;
    }

    private enum Behaviour
    {
        Approve,
        Decline05,
        Decline51,
        Decline54,
        AdviceTwice,
        ReversalFirst,
        LateAnswer,
        NeverReceived,
        GatewayErrorOnce,
        NotOnTheList
    }

    public async Task<IResult> SubmitPaymentAsync(HttpContext context)
    {
        var body = await ReadBodyAsync(context.Request);
        var paymentRequest = body?["PaymentRequest"] as JsonObject;
        var transactionId = GetText(paymentRequest, "TransactionID");
        var orderNumber = GetText(paymentRequest, "OrderNumber");

        if (!IsAuthorised(context.Request))
        {
            return Error(StatusCodes.Status401Unauthorized, "E02", "The API key is missing or not valid", transactionId, orderNumber);
        }

        if (IsOverRateLimit())
        {
            return RateLimited(context, transactionId, orderNumber);
        }

        if (body?["MessageHeader"]?["MessageID"] is null || !TryReadPayment(paymentRequest, out var payment))
        {
            return Error(StatusCodes.Status400BadRequest, "E01", "The body could not be parsed", transactionId, orderNumber);
        }

        var behaviour = Cards.GetValueOrDefault(payment.Token, Behaviour.NotOnTheList);
        Transaction? transaction;

        lock (gate)
        {
            if (FindTransaction(payment.TransactionId) is { } held)
            {
                // Acme replays the outcome it stored for a repeated TransactionID, or answers Processing while in progress
                return Answer(held);
            }

            var retrievalReference = payment.OrderNumber[..Math.Min(RetrievalReferenceLength, payment.OrderNumber.Length)];

            if (retrievalReferences.TryGetValue(retrievalReference, out var holder) && holder != payment.TransactionId)
            {
                return Error(StatusCodes.Status409Conflict, "E09", "Retrieval Reference Number already in use", holder, transactions[holder].OrderNumber);
            }

            if (behaviour == Behaviour.GatewayErrorOnce && firstSendsDropped.Add(payment.TransactionId))
            {
                logger.LogInformation("Payment {TransactionId} meets a degraded gateway once, and is not kept", payment.TransactionId);

                return Error(StatusCodes.Status503ServiceUnavailable, "E50", "The gateway is degraded", payment.TransactionId, payment.OrderNumber);
            }

            // the first send of this card is lost, so nothing is kept and a query finds nothing
            transaction = behaviour == Behaviour.NeverReceived && firstSendsDropped.Add(payment.TransactionId)
                ? null
                : Keep(payment, retrievalReference, behaviour);
        }

        if (transaction is null)
        {
            logger.LogInformation("Payment {TransactionId} is held and never received, so a query finds nothing", payment.TransactionId);

            await HoldAsync(context);
            context.Abort();

            return Results.Empty;
        }

        if (behaviour == Behaviour.ReversalFirst)
        {
            // the reversal reaches the payments API before the answer that sets the outcome
            logger.LogInformation("Payment {TransactionId} is reversed before it is answered", payment.TransactionId);

            await notifier.SendAsync(
                notifier.MakeTransactionStatus("Reversal", transaction.RetrievalReferenceNumber, transaction.Amount, transaction.CurrencyCode));
        }

        // every kept outcome sends a Payment Advice about 1 second after the stub answers, or after a late answer
        // resolves; the card whose advice is sent twice sends the same advice twice
        notifier.SendLater(
            notifier.MakePaymentAdvice(GetAdvice(transaction)),
            (transaction.ResolvesAt > time.GetUtcNow() ? transaction.ResolvesAt - time.GetUtcNow() : TimeSpan.Zero) + AdviceDelay,
            behaviour == Behaviour.AdviceTwice ? 2 : 1);

        if (behaviour == Behaviour.LateAnswer)
        {
            logger.LogInformation("Payment {TransactionId} is held, and approved {Seconds} seconds after it arrived", payment.TransactionId, LateAnswerResolvesAfter.TotalSeconds);

            if (!await HoldAsync(context))
            {
                return Results.Empty;
            }
        }

        return Answer(transaction);
    }

    public IResult QueryTransaction(HttpContext context, string merchantIdInPath)
    {
        var transactionId = context.Request.Query["TransactionID"].ToString();
        var orderNumber = context.Request.Query["OrderNumber"].ToString();

        if (!IsAuthorised(context.Request))
        {
            return Error(StatusCodes.Status401Unauthorized, "E02", "The API key is missing or not valid", transactionId, orderNumber);
        }

        if (IsOverRateLimit())
        {
            return RateLimited(context, transactionId, orderNumber);
        }

        lock (gate)
        {
            var transaction = string.Equals(merchantIdInPath, merchantId, StringComparison.OrdinalIgnoreCase)
                ? FindTransaction(transactionId) ?? FindTransactionByOrderNumber(orderNumber)
                : null;

            return transaction is null
                ? Results.Text(GetEnvelope(null), "application/json", statusCode: StatusCodes.Status404NotFound)
                : Answer(transaction);
        }
    }

    // a stub-only route: Acme Switch reverses the whole of a payment the stub holds
    public Task<IResult> SendReversalAsync(HttpContext context) => SendTransactionStatusAsync(context, "Reversal");

    // a stub-only route: Acme Switch refunds part or all of a payment the stub holds
    public Task<IResult> SendRefundAsync(HttpContext context) => SendTransactionStatusAsync(context, "Refund");

    private static async Task<JsonNode?> ReadBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body);
        var text = await reader.ReadToEndAsync();

        try
        {
            return JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IResult StubAnswer(int statusCode, string name, string value) =>
        Results.Text(new JsonObject { [name] = value }.ToJsonString(), "application/json", statusCode: statusCode);

    private static bool TryReadPayment(JsonObject? paymentRequest, out Payment payment)
    {
        payment = default!;

        var merchant = GetText(paymentRequest, "MerchantID");
        var transactionId = GetText(paymentRequest, "TransactionID");
        var orderNumber = GetText(paymentRequest, "OrderNumber");
        var service = GetText(paymentRequest, "PaymentService");
        var currencyCode = GetText(paymentRequest, "CurrencyCode");
        var token = GetText(paymentRequest?["Card"] as JsonObject, "Token");
        var hasAmount = paymentRequest?["Amount"] is JsonValue amountValue && amountValue.TryGetValue<long>(out _);

        if (string.IsNullOrEmpty(merchant)
            || string.IsNullOrEmpty(transactionId)
            || string.IsNullOrEmpty(orderNumber)
            || !orderNumber.All(char.IsAsciiLetterOrDigit)
            || !hasAmount
            || !CardServices.Contains(service)
            || string.IsNullOrEmpty(token))
        {
            return false;
        }

        payment = new Payment(
            transactionId,
            orderNumber,
            paymentRequest!["Amount"]!.GetValue<long>(),
            string.IsNullOrEmpty(currencyCode) ? DefaultCurrencyCode : currencyCode.ToUpperInvariant(),
            service!,
            token);

        return true;
    }

    private static string? GetText(JsonObject? container, string name) =>
        container?[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static string GetDigits(int count) =>
        string.Concat(Enumerable.Range(0, count).Select(_ => RandomNumberGenerator.GetInt32(10).ToString(CultureInfo.InvariantCulture)));

    private bool IsAuthorised(HttpRequest request) =>
        CryptographicOperations.FixedTimeEquals(
            expectedAuthorization,
            Encoding.UTF8.GetBytes(request.Headers.Authorization.ToString()));

    private bool IsRoutesAuthorised(HttpRequest request) =>
        CryptographicOperations.FixedTimeEquals(
            expectedRoutesAuthorization,
            Encoding.UTF8.GetBytes(request.Headers.Authorization.ToString()));

    private async Task<IResult> SendTransactionStatusAsync(HttpContext context, string transactionType)
    {
        if (!IsRoutesAuthorised(context.Request))
        {
            return StubAnswer(StatusCodes.Status401Unauthorized, "error", "The key of the stub routes is missing or not valid");
        }

        var body = await ReadBodyAsync(context.Request) as JsonObject;
        var transactionId = GetText(body, "TransactionID");
        Transaction? transaction;

        lock (gate)
        {
            transaction = transactionId is not null ? transactions.GetValueOrDefault(transactionId) : null;
        }

        if (transaction is null)
        {
            return StubAnswer(StatusCodes.Status404NotFound, "error", "The stub holds no payment with that TransactionID");
        }

        // a reversal is the whole amount; a refund names its own
        var amount = transactionType == "Reversal"
            ? transaction.Amount
            : body?["Amount"] is JsonValue amountValue && amountValue.TryGetValue<long>(out var refundAmount) ? refundAmount : 0;

        if (amount < 1)
        {
            return StubAnswer(StatusCodes.Status400BadRequest, "error", "A refund needs an Amount of at least 1");
        }

        var notification = notifier.MakeTransactionStatus(transactionType, transaction.RetrievalReferenceNumber, amount, transaction.CurrencyCode);

        return await notifier.SendAsync(notification)
            ? StubAnswer(StatusCodes.Status200OK, "transactionUUID", notification.TransactionUuid)
            : StubAnswer(StatusCodes.Status502BadGateway, "transactionUUID", notification.TransactionUuid);
    }

    private bool IsOverRateLimit()
    {
        var second = time.GetUtcNow().ToUnixTimeSeconds();

        lock (gate)
        {
            if (second != rateWindow)
            {
                rateWindow = second;
                rateWindowCount = 0;
            }

            return ++rateWindowCount > RequestsPerSecond;
        }
    }

    private async Task<bool> HoldAsync(HttpContext context)
    {
        try
        {
            await Task.Delay(Hold, time, context.RequestAborted);

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private Transaction Keep(Payment payment, string retrievalReference, Behaviour behaviour)
    {
        if (behaviour == Behaviour.NotOnTheList)
        {
            logger.LogWarning("Payment {TransactionId} carries a token not on the stub's list, so it is declined with 05", payment.TransactionId);
        }

        var now = time.GetUtcNow();
        var code = behaviour switch
        {
            Behaviour.Decline05 or Behaviour.NotOnTheList => "05",
            Behaviour.Decline51 => "51",
            Behaviour.Decline54 => "54",
            _ => "00"
        };

        var transaction = new Transaction(
            payment.TransactionId,
            payment.OrderNumber,
            retrievalReference,
            payment.Amount,
            payment.CurrencyCode,
            payment.PaymentService,
            code,
            code == "00" ? GetDigits(6) : string.Empty,
            $"MR{GetDigits(9)}",
            GetDigits(6),
            now,
            behaviour == Behaviour.LateAnswer ? now + LateAnswerResolvesAfter : now);

        transactions[payment.TransactionId] = transaction;
        retrievalReferences[retrievalReference] = payment.TransactionId;

        return transaction;
    }

    private Transaction? FindTransaction(string? transactionId) =>
        !string.IsNullOrEmpty(transactionId)
        && transactions.TryGetValue(transactionId, out var transaction)
        && transaction.ArrivedAt + ReplayLifetime > time.GetUtcNow()
            ? transaction
            : null;

    private Transaction? FindTransactionByOrderNumber(string? orderNumber) =>
        string.IsNullOrEmpty(orderNumber)
            ? null
            : FindTransaction(transactions.Values.FirstOrDefault(transaction => transaction.OrderNumber == orderNumber)?.TransactionId);

    private Notifier.Advice GetAdvice(Transaction transaction) =>
        new(
            merchantId,
            transaction.TransactionId,
            transaction.ResolvesAt,
            transaction.OrderNumber,
            transaction.ReconId,
            transaction.RetrievalReferenceNumber,
            transaction.SystemsTraceAuditNumber,
            transaction.Amount,
            transaction.CurrencyCode,
            transaction.PaymentService,
            transaction.Code,
            Codes[transaction.Code].Description,
            transaction.AuthCode);

    private IResult Answer(Transaction transaction)
    {
        if (time.GetUtcNow() < transaction.ResolvesAt)
        {
            return Results.Text(
                GetEnvelope(new JsonObject
                {
                    ["TransactionID"] = transaction.TransactionId,
                    ["OrderNumber"] = transaction.OrderNumber,
                    ["RetrievalReferenceNumber"] = transaction.RetrievalReferenceNumber,
                    ["AuthCode"] = string.Empty,
                    ["TransactionStatus"] = "Processing"
                }),
                "application/json");
        }

        var (description, clientMessage) = Codes[transaction.Code];
        var paymentResponse = new JsonObject
        {
            ["TransactionID"] = transaction.TransactionId,
            ["TransactionDateTime"] = transaction.ResolvesAt.ToOffset(SouthAfricanOffset).ToString(TimeFormat, CultureInfo.InvariantCulture),
            ["OrderNumber"] = transaction.OrderNumber,
            ["ReconID"] = transaction.ReconId,
            ["RetrievalReferenceNumber"] = transaction.RetrievalReferenceNumber,
            ["AuthCode"] = transaction.AuthCode,
            ["TransactionStatus"] = "Success",
            ["ResponseDetail"] = new JsonObject
            {
                ["Source"] = "Acquirer",
                ["Code"] = transaction.Code,
                ["Description"] = description,
                ["ClientMessage"] = clientMessage
            }
        };

        // partial authorisation is not simulated, so an approval authorises the amount sent
        if (transaction.Code == "00")
        {
            paymentResponse["AuthAmount"] = transaction.Amount;
        }

        logger.LogInformation("Payment {TransactionId} is answered with response code {Code}", transaction.TransactionId, transaction.Code);

        return Results.Text(GetEnvelope(paymentResponse), "application/json");
    }

    private IResult RateLimited(HttpContext context, string? transactionId, string? orderNumber)
    {
        context.Response.Headers.RetryAfter = "1";

        return Error(StatusCodes.Status429TooManyRequests, "E29", "Too many requests", transactionId, orderNumber);
    }

    private IResult Error(int statusCode, string code, string description, string? transactionId, string? orderNumber) =>
        Results.Text(
            GetEnvelope(new JsonObject
            {
                ["TransactionID"] = transactionId,
                ["OrderNumber"] = orderNumber,
                ["TransactionStatus"] = "Failure",
                ["ResponseDetail"] = new JsonObject
                {
                    ["Source"] = "Application",
                    ["Code"] = code,
                    ["Description"] = description,
                    ["ClientMessage"] = ErrorClientMessage
                }
            }),
            "application/json",
            statusCode: statusCode);

    private string GetEnvelope(JsonObject? paymentResponse)
    {
        var envelope = new JsonObject
        {
            ["MessageHeader"] = new JsonObject
            {
                ["MessageDateTime"] = time.GetUtcNow().ToOffset(SouthAfricanOffset).ToString(TimeFormat, CultureInfo.InvariantCulture),
                ["MessageID"] = Guid.NewGuid().ToString(),
                ["Channel"] = "App"
            }
        };

        if (paymentResponse is not null)
        {
            envelope["PaymentResponse"] = paymentResponse;
        }

        // Acme adds members without notice, so every answer carries one Appendix A does not document
        envelope["GatewayRegion"] = "local";

        return envelope.ToJsonString();
    }

    private sealed record Payment(string TransactionId, string OrderNumber, long Amount, string CurrencyCode, string PaymentService, string Token);

    private sealed record Transaction(
        string TransactionId,
        string OrderNumber,
        string RetrievalReferenceNumber,
        long Amount,
        string CurrencyCode,
        string PaymentService,
        string Code,
        string AuthCode,
        string ReconId,
        string SystemsTraceAuditNumber,
        DateTimeOffset ArrivedAt,
        DateTimeOffset ResolvesAt);
}
