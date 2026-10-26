using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace AcmeStub;

// sends Acme Pay's Payment Advices and Acme Switch's Transaction Statuses to the payments API, proven the way the
// brief's Appendix A fixes: HTTP Basic for an advice, an x-signature over the exact body for a Transaction Status
public sealed class Notifier
{
    // the payments API inside the stack; its server certificate is trusted through SSL_CERT_FILE, set in compose.yml
    private const string PaymentsApiAddress = "https://PaymentsApi:8443";

    private const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    private const int Resends = 3;

    private static readonly TimeSpan ResendGap = TimeSpan.FromSeconds(2);

    private static readonly TimeSpan SouthAfricanOffset = TimeSpan.FromHours(2);

    private readonly HttpClient client = new() { BaseAddress = new Uri(PaymentsApiAddress) };

    private readonly AuthenticationHeaderValue adviceAuthorization;

    private readonly byte[] webhookSecret;

    private readonly TimeProvider time;

    private readonly CancellationToken stopping;

    private readonly ILogger<Notifier> logger;

    public Notifier(IConfiguration configuration, TimeProvider time, IHostApplicationLifetime lifetime, ILogger<Notifier> logger)
    {
        // Appendix A's own names for the three secrets, read from the files compose.yml generates
        var userName = configuration["ACME_ADVICE_USERNAME"];
        var password = configuration["ACME_ADVICE_PASSWORD"];
        var secret = configuration["ACME_WEBHOOK_SECRET"];

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("ACME_ADVICE_USERNAME, ACME_ADVICE_PASSWORD and ACME_WEBHOOK_SECRET are required.");
        }

        adviceAuthorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}")));
        webhookSecret = Encoding.UTF8.GetBytes(secret);
        this.time = time;
        stopping = lifetime.ApplicationStopping;
        this.logger = logger;
    }

    // a Payment Advice, as Acme Pay sends one when a transaction reaches its outcome
    public Notification MakePaymentAdvice(Advice advice) =>
        new(
            "/v1/payment-advices",
            $"Payment Advice for {advice.TransactionId}",
            false,
            new JsonObject
            {
                ["RequestType"] = "Payment",
                ["MerchantID"] = advice.MerchantId,
                ["TransactionID"] = advice.TransactionId,
                ["TransactionDateTime"] = GetSouthAfricanTime(advice.ResolvedAt),
                ["OrderNumber"] = advice.OrderNumber,
                ["ReconID"] = advice.ReconId,
                ["RetrievalReferenceNumber"] = advice.RetrievalReferenceNumber,
                ["SystemsTraceAuditNumber"] = advice.SystemsTraceAuditNumber,
                ["Amount"] = advice.Amount,
                ["CurrencyCode"] = advice.CurrencyCode,
                ["PaymentService"] = advice.PaymentService,
                ["TransactionStatus"] = "Success",
                ["ResponseSource"] = "Acquirer",
                ["ResponseCode"] = advice.Code,
                ["ResponseDescription"] = advice.Description,
                ["AuthCode"] = advice.AuthCode,

                // the stub holds no card number, so this is the masked number of the contract's own example
                ["MaskedCardNumber"] = "541333******1429",
                ["CommProtocol"] = "REST"
            });

    // a reversal or a refund, as Acme Switch publishes one; the stub's are always approved
    public Notification MakeTransactionStatus(string transactionType, string retrievalReferenceNumber, long amount, string currencyCode) =>
        new(
            "/v1/transaction-statuses",
            $"{transactionType} of {retrievalReferenceNumber}",
            true,
            new JsonObject
            {
                ["transactionUUID"] = Guid.NewGuid().ToString(),
                ["transactionType"] = transactionType,
                ["transactionDateTime"] = GetSouthAfricanTime(time.GetUtcNow()),
                ["rrn"] = retrievalReferenceNumber,
                ["transactionAmount"] = amount,
                ["currencyCode"] = currencyCode,
                ["txResultCode"] = "00",
                ["isApproved"] = true
            });

    // sent in the background after a delay, so the answer it follows reaches the payments API first
    public void SendLater(Notification notification, TimeSpan delay, int times) =>
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await Task.Delay(delay, time, stopping);

                    for (var sending = 0; sending < times; sending++)
                    {
                        await SendAsync(notification);
                    }
                }
                catch (OperationCanceledException)
                {
                    // the stub is stopping, and everything it holds is lost anyway
                }
            },
            CancellationToken.None);

    // a notification not answered 200 is sent three more times, 2 seconds apart; true once it is answered 200
    public async Task<bool> SendAsync(Notification notification)
    {
        for (var attempt = 0; attempt <= Resends; attempt++)
        {
            if (attempt > 0)
            {
                await Task.Delay(ResendGap, time, stopping);
            }

            var status = await DeliverAsync(notification);

            if (status == HttpStatusCode.OK)
            {
                return true;
            }

            logger.LogWarning("The {Notification} was answered {Status}", notification.Description, status?.ToString() ?? "with no answer");
        }

        logger.LogWarning("The {Notification} was given up after {Resends} resends", notification.Description, Resends);

        return false;
    }

    private static string GetSouthAfricanTime(DateTimeOffset moment) =>
        moment.ToOffset(SouthAfricanOffset).ToString(TimeFormat, CultureInfo.InvariantCulture);

    private async Task<HttpStatusCode?> DeliverAsync(Notification notification)
    {
        // Acme adds members without notice, so every notification carries one Appendix A does not document, and
        // it changes on each delivery
        var body = Encoding.UTF8.GetBytes(notification.NextDelivery().ToJsonString());

        using var request = new HttpRequestMessage(HttpMethod.Post, notification.Path) { Content = new ByteArrayContent(body) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        if (notification.IsSigned)
        {
            request.Headers.Add("x-signature", Convert.ToBase64String(HMACSHA256.HashData(webhookSecret, body)));
        }
        else
        {
            request.Headers.Authorization = adviceAuthorization;
        }

        try
        {
            using var response = await client.SendAsync(request, stopping);

            logger.LogInformation("The {Notification} was delivered and answered {Status}", notification.Description, response.StatusCode);

            return response.StatusCode;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning("The {Notification} could not be delivered: {Reason}", notification.Description, exception.Message);

            return null;
        }
    }

    public sealed record Advice(
        string MerchantId,
        string TransactionId,
        DateTimeOffset ResolvedAt,
        string OrderNumber,
        string ReconId,
        string RetrievalReferenceNumber,
        string SystemsTraceAuditNumber,
        long Amount,
        string CurrencyCode,
        string PaymentService,
        string Code,
        string Description,
        string AuthCode);

    // one notification, however often it is delivered; only its delivery count changes between deliveries
    public sealed class Notification(string path, string description, bool isSigned, JsonObject body)
    {
        private readonly Lock gate = new();

        private int deliveryCount;

        public string Path { get; } = path;

        public string Description { get; } = description;

        public bool IsSigned { get; } = isSigned;

        public string TransactionUuid => body["transactionUUID"]?.GetValue<string>() ?? string.Empty;

        public JsonObject NextDelivery()
        {
            lock (gate)
            {
                var delivery = (JsonObject)body.DeepClone();
                delivery["DeliveryCount"] = ++deliveryCount;

                return delivery;
            }
        }
    }
}
