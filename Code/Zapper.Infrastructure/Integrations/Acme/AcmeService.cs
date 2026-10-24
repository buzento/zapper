using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Zapper.Domain.Payment;

namespace Zapper.Infrastructure.Integrations.Acme;

public sealed class AcmeService : IAcmeService
{
    // above the 30 seconds Appendix A gives an unresponsive issuer, D-3
    internal static readonly TimeSpan TimeLimit = TimeSpan.FromSeconds(35);

    internal static readonly TimeSpan BreakDuration = TimeSpan.FromSeconds(30);

    private const string Channel = "App";

    private const string ApprovedResponseCode = "00";

    // Appendix A's times carry no offset and are South African Standard Time
    private const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    private static readonly TimeSpan SouthAfricanOffset = TimeSpan.FromHours(2);

    // Appendix A's longest value for each text member read from an answer
    private static readonly Dictionary<string, int> PaymentResponseLengths = new()
    {
        ["TransactionID"] = 50,
        ["OrderNumber"] = 50,
        ["ReconID"] = 50,
        ["RetrievalReferenceNumber"] = 12,
        ["AuthCode"] = 6,
        ["TransactionStatus"] = 20
    };

    private static readonly Dictionary<string, int> ResponseDetailLengths = new()
    {
        ["Source"] = 50,
        ["Code"] = 3,
        ["Description"] = 50,
        ["ClientMessage"] = 100
    };

    private static readonly AcmeAnswer Unreadable = new(AcmeAnswerType.Unreadable);

    private readonly HttpClient client;

    private readonly AcmeOptions acme;

    private readonly TimeProvider time;

    private readonly ILogger<AcmeService> logger;

    public AcmeService(HttpClient client, IOptions<AcmeOptions> acme, TimeProvider time, ILogger<AcmeService> logger)
    {
        this.client = client;
        this.acme = acme.Value;
        this.time = time;
        this.logger = logger;
    }

    public async Task<SubmitPaymentResponse> SubmitPaymentAsync(
        SubmitPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["MessageHeader"] = new JsonObject
            {
                ["MessageDateTime"] = time.GetUtcNow().ToOffset(SouthAfricanOffset).ToString(TimeFormat, CultureInfo.InvariantCulture),
                ["MessageID"] = request.MessageId.ToString(),
                ["Channel"] = Channel
            },
            ["PaymentRequest"] = new JsonObject
            {
                ["MerchantID"] = acme.MerchantId,
                ["TransactionID"] = request.TransactionId.ToString(),
                ["OrderNumber"] = request.OrderNumber,
                ["Amount"] = request.Amount,
                ["CurrencyCode"] = request.CurrencyCode,
                ["PaymentService"] = request.PaymentService,
                ["Card"] = new JsonObject { ["Token"] = request.Token },
                ["CardOnFileScenario"] = request.CardOnFileScenario,
                ["SubMerchantID"] = request.SubMerchantReference,
                ["MerchantCategoryCode"] = request.MerchantCategoryCode,
                ["CardAcceptorName"] = request.CardAcceptorName,
                ["Email"] = request.Email
            }
        };
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/payment") { Content = JsonContent.Create(body) };

        var (answer, outcome, httpStatus, answerBody, startedAt, duration) =
            await CallAsync(message, request.TransactionId, cancellationToken);

        return new SubmitPaymentResponse(answer, outcome, httpStatus, answerBody, startedAt, duration);
    }

    public async Task<GetTransactionByTransactionIdResponse> GetTransactionByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Get,
            $"/apirttq/query/Merchant/{Uri.EscapeDataString(acme.MerchantId)}?TransactionID={transactionId}");

        var (answer, outcome, httpStatus, answerBody, startedAt, duration) =
            await CallAsync(message, transactionId, cancellationToken);

        return new GetTransactionByTransactionIdResponse(answer, outcome, httpStatus, answerBody, startedAt, duration);
    }

    private static AcmeAnswer ReadAnswer(HttpResponseMessage response, string body, Guid transactionId) =>
        (int)response.StatusCode switch
        {
            200 => ReadPaymentResponse(body, transactionId),
            400 or 401 => ReadError(AcmeAnswerType.Refused, body),
            404 => new AcmeAnswer(AcmeAnswerType.NotFound),
            409 => ReadOrderNumberHeld(body, transactionId),
            429 => ReadError(AcmeAnswerType.RateLimited, body, response.Headers.RetryAfter?.Delta),
            >= 500 and <= 504 => ReadError(AcmeAnswerType.Degraded, body),
            _ => Unreadable
        };

    private static AcmeAnswer ReadPaymentResponse(string body, Guid transactionId)
    {
        var payment = GetObject(ParseObject(body), "PaymentResponse");
        var detail = GetObject(payment, "ResponseDetail");

        if (payment is null || !IsReadable(payment, PaymentResponseLengths) || !IsReadable(detail, ResponseDetailLengths)
            || !IsTransaction(payment, transactionId))
        {
            return Unreadable;
        }

        // Appendix A puts the outcome in the response code, so Complete, Reversed and Refunded read like Success
        return GetText(payment, "TransactionStatus") switch
        {
            "Processing" => new AcmeAnswer(AcmeAnswerType.Processing),
            "Success" or "Complete" or "Reversed" or "Refunded" => ReadOutcome(AcmeAnswerType.Success, payment, detail),
            "Failure" => ReadOutcome(AcmeAnswerType.Failure, payment, detail),
            _ => Unreadable
        };
    }

    // an answer is final only with a response code and a client message, and an approval also with its amount
    private static AcmeAnswer ReadOutcome(AcmeAnswerType type, JsonObject payment, JsonObject? detail)
    {
        var responseCode = GetText(detail, "Code");
        var clientMessage = GetText(detail, "ClientMessage");

        if (!TryGetAmount(payment, "AuthAmount", out var authorisedAmount)
            || !TryGetTime(payment, "TransactionDateTime", out var acmeTransactionAt)
            || responseCode is null
            || string.IsNullOrEmpty(clientMessage)
            || (type == AcmeAnswerType.Success && responseCode == ApprovedResponseCode && authorisedAmount is null))
        {
            return Unreadable;
        }

        return new AcmeAnswer(
            type,
            GetText(detail, "Source"),
            responseCode,
            GetText(detail, "Description"),
            clientMessage,
            GetText(payment, "AuthCode"),
            authorisedAmount,
            GetText(payment, "ReconID"),
            GetText(payment, "RetrievalReferenceNumber"),
            acmeTransactionAt);
    }

    // on a 409 Appendix A names the transaction that holds the order number, which may be this one
    private static AcmeAnswer ReadOrderNumberHeld(string body, Guid transactionId)
    {
        var payment = GetObject(ParseObject(body), "PaymentResponse");
        var detail = GetObject(payment, "ResponseDetail");

        if (payment is null || !IsReadable(payment, PaymentResponseLengths) || !IsReadable(detail, ResponseDetailLengths)
            || !Guid.TryParse(GetText(payment, "TransactionID"), out var holder))
        {
            return Unreadable;
        }

        return GetError(
            holder == transactionId ? AcmeAnswerType.OrderNumberHeldByThis : AcmeAnswerType.OrderNumberHeldByAnother,
            detail,
            null);
    }

    // an error carries the reason in ResponseDetail; one that does not still has its meaning in its status
    private static AcmeAnswer ReadError(AcmeAnswerType type, string body, TimeSpan? retryAfter = null)
    {
        var detail = GetObject(GetObject(ParseObject(body), "PaymentResponse"), "ResponseDetail");

        return GetError(type, IsReadable(detail, ResponseDetailLengths) ? detail : null, retryAfter);
    }

    private static AcmeAnswer GetError(AcmeAnswerType type, JsonObject? detail, TimeSpan? retryAfter) =>
        new(
            type,
            GetText(detail, "Source"),
            GetText(detail, "Code"),
            GetText(detail, "Description"),
            GetText(detail, "ClientMessage"),
            retryAfter: retryAfter);

    private static JsonObject? ParseObject(string body)
    {
        try
        {
            return JsonNode.Parse(body) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonObject? GetObject(JsonObject? owner, string member) => owner?[member] as JsonObject;

    private static bool IsTransaction(JsonObject payment, Guid transactionId) =>
        Guid.TryParse(GetText(payment, "TransactionID"), out var answered) && answered == transactionId;

    // a member is absent, or text no longer than Appendix A allows; a member Acme adds is never looked at
    private static bool IsReadable(JsonObject? owner, Dictionary<string, int> lengths) =>
        owner is null
        || lengths.All(member => owner[member.Key] is null
            || (owner[member.Key]!.GetValueKind() == JsonValueKind.String
                && owner[member.Key]!.GetValue<string>().Length <= member.Value));

    // IsReadable has checked that the member is absent or text
    private static string? GetText(JsonObject? owner, string member) => owner?[member]?.GetValue<string>();

    private static bool TryGetAmount(JsonObject owner, string member, out long? amount)
    {
        amount = null;
        var node = owner[member];

        if (node is null)
        {
            return true;
        }

        if (node.GetValueKind() != JsonValueKind.Number || !node.AsValue().TryGetValue<long>(out var value))
        {
            return false;
        }

        amount = value;

        return true;
    }

    private static bool TryGetTime(JsonObject owner, string member, out DateTimeOffset? at)
    {
        at = null;
        var node = owner[member];

        if (node is null)
        {
            return true;
        }

        if (node.GetValueKind() != JsonValueKind.String
            || !DateTimeOffset.TryParseExact(
                node.GetValue<string>(),
                TimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var local))
        {
            return false;
        }

        at = new DateTimeOffset(local.Ticks, SouthAfricanOffset).ToUniversalTime();

        return true;
    }

    private async Task<(AcmeAnswer Answer, AcmeInteractionOutcome? Outcome, int? HttpStatus, string? Body, DateTimeOffset StartedAt, TimeSpan Duration)>
        CallAsync(HttpRequestMessage message, Guid transactionId, CancellationToken cancellationToken)
    {
        var startedAt = time.GetUtcNow();
        var started = time.GetTimestamp();

        try
        {
            using var response = await client.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var answer = ReadAnswer(response, body, transactionId);

            LogStatusReadAsSuccess(answer, body, transactionId);

            return (answer, AcmeInteractionOutcome.Answered, (int)response.StatusCode, body, startedAt, time.GetElapsedTime(started));
        }
        catch (TimeoutRejectedException exception)
        {
            logger.LogWarning(
                exception,
                "The call to Acme for payment {PaymentId} timed out after {TimeLimit}",
                transactionId,
                TimeLimit);

            return (new AcmeAnswer(AcmeAnswerType.TimedOut), AcmeInteractionOutcome.TimedOut, null, null, startedAt, time.GetElapsedTime(started));
        }
        catch (BrokenCircuitException)
        {
            return (new AcmeAnswer(AcmeAnswerType.CircuitOpen, retryAfter: BreakDuration), null, null, null, startedAt, TimeSpan.Zero);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "The call to Acme for payment {PaymentId} broke before an answer", transactionId);

            return (new AcmeAnswer(AcmeAnswerType.ConnectionFailed), AcmeInteractionOutcome.ConnectionFailed, null, null, startedAt, time.GetElapsedTime(started));
        }
    }

    // a status other than Success is read by its response code, and kept in the call record, R-2
    private void LogStatusReadAsSuccess(AcmeAnswer answer, string body, Guid transactionId)
    {
        if (answer.Type != AcmeAnswerType.Success)
        {
            return;
        }

        // a Success answer was read from this body, so its status is text
        var status = GetText(GetObject(ParseObject(body), "PaymentResponse"), "TransactionStatus");

        if (status != "Success")
        {
            logger.LogWarning(
                "Acme answered payment {PaymentId} with the status {TransactionStatus}, read by its response code",
                transactionId,
                status);
        }
    }
}
