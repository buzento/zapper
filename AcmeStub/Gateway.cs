using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AcmeStub;

// a stand-in for Acme Pay's payment and query routes, as the brief's Appendix A describes them;
// everything it holds is in memory and is lost when it restarts
public sealed class Gateway
{
    private const int RequestsPerSecond = 20;

    private const int RetrievalReferenceLength = 12;

    private const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    // Appendix A gives this client message on its one error example, so every error carries it
    private const string ErrorClientMessage = "We could not process that payment.";

    private static readonly TimeSpan SouthAfricanOffset = TimeSpan.FromHours(2);

    // longer than the 35 seconds the payments API waits, so the API times out
    private static readonly TimeSpan Hold = TimeSpan.FromSeconds(40);

    private static readonly TimeSpan LateAnswerResolvesAfter = TimeSpan.FromSeconds(45);

    private static readonly TimeSpan ReplayLifetime = TimeSpan.FromHours(24);

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
        ["tok_test_advice_twice"] = Behaviour.Approve,
        ["tok_test_reversal_first"] = Behaviour.Approve,
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

    private readonly string merchantId;

    private readonly TimeProvider time;

    private readonly ILogger<Gateway> logger;

    private long rateWindow;

    private int rateWindowCount;

    public Gateway(IConfiguration configuration, TimeProvider time, ILogger<Gateway> logger)
    {
        var apiKey = configuration["AcmeStub:ApiKey"];
        var configuredMerchantId = configuration["AcmeStub:MerchantId"];

        if (string.IsNullOrWhiteSpace(apiKey) || !Guid.TryParse(configuredMerchantId, out _))
        {
            throw new InvalidOperationException("AcmeStub:ApiKey and AcmeStub:MerchantId, a GUID, are required.");
        }

        expectedAuthorization = Encoding.UTF8.GetBytes($"Bearer {apiKey}");
        merchantId = configuredMerchantId;
        this.time = time;
        this.logger = logger;
    }

    private enum Behaviour
    {
        Approve,
        Decline05,
        Decline51,
        Decline54,
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

    private static bool TryReadPayment(JsonObject? paymentRequest, out Payment payment)
    {
        payment = default!;

        var merchant = GetText(paymentRequest, "MerchantID");
        var transactionId = GetText(paymentRequest, "TransactionID");
        var orderNumber = GetText(paymentRequest, "OrderNumber");
        var service = GetText(paymentRequest, "PaymentService");
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

        payment = new Payment(transactionId, orderNumber, paymentRequest!["Amount"]!.GetValue<long>(), token);

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
            code,
            code == "00" ? GetDigits(6) : string.Empty,
            $"MR{GetDigits(9)}",
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

    private sealed record Payment(string TransactionId, string OrderNumber, long Amount, string Token);

    private sealed record Transaction(
        string TransactionId,
        string OrderNumber,
        string RetrievalReferenceNumber,
        long Amount,
        string Code,
        string AuthCode,
        string ReconId,
        DateTimeOffset ArrivedAt,
        DateTimeOffset ResolvesAt);
}
