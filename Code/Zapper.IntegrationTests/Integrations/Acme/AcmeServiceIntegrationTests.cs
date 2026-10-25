using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Zapper.Domain.Enums;
using Zapper.Infrastructure;
using Zapper.Infrastructure.Integrations.Acme;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Integrations.Acme;

public sealed class AcmeServiceIntegrationTests : IDisposable
{
    // Appendix A's example request was made at 13:56:36.533 in South African time
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 11, 56, 36, 533, TimeSpan.Zero);

    private static readonly Guid TransactionId = Guid.Parse("2e0e1b85-bf91-45a9-b038-282e00036919");

    private static readonly Dictionary<string, string?> Settings = new()
    {
        ["Zapper:Acme:BaseAddress"] = "https://sandbox.acmepay.example",
        ["Zapper:Acme:MerchantId"] = "8B67BF88-BB8D-4EB5-ADFC-0F5C97C7ED67",
        ["Zapper:Acme:ApiKey"] = "sk_test_9f3c1a7e2b5d4c8a6e0f",
        ["Zapper:Acme:RequestsPerSecond"] = "20",
        ["Zapper:Acme:MaxConcurrentCalls"] = "50"
    };

    private readonly FakeTimeProvider time = new(Now);

    private readonly RecordingHandler handler = new();

    private readonly IHost host;

    private readonly IAcmeService acme;

    public AcmeServiceIntegrationTests()
    {
        host = BuildHost(replacePrimaryHandler: true);
        acme = host.Services.GetRequiredService<IAcmeService>();
    }

    public void Dispose()
    {
        host.Dispose();
        handler.Dispose();
    }

    [Fact]
    public async Task SubmitPayment_AppendixExample_SendsAppendixRequest()
    {
        handler.AnswerWith(Answer(HttpStatusCode.OK, await ReadExampleAsync("PaymentResponse.json")));

        await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        var sent = handler.Requests.ShouldHaveSingleItem();
        sent.Method.ShouldBe(HttpMethod.Post);
        sent.RequestUri.ShouldBe(new Uri("https://sandbox.acmepay.example/api/payment"));
        sent.Authorization!.Scheme.ShouldBe("Bearer");
        sent.Authorization.Parameter.ShouldBe("sk_test_9f3c1a7e2b5d4c8a6e0f");
        JsonNode.DeepEquals(JsonNode.Parse(sent.Body!), JsonNode.Parse(await ReadExampleAsync("PaymentRequest.json")))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task SubmitPayment_ApprovedAnswer_ReturnsSuccess()
    {
        var body = await ReadExampleAsync("PaymentResponse.json");
        handler.AnswerWith(Answer(HttpStatusCode.OK, body));

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.Outcome.ShouldBe(AcmeInteractionOutcome.Answered);
        response.HttpStatus.ShouldBe(200);
        response.Body.ShouldBe(body);
        response.StartedAt.ShouldBe(Now);
        var answer = response.Answer;
        answer.Type.ShouldBe(AcmeAnswerType.Success);
        answer.ResponseSource.ShouldBe("Acquirer");
        answer.ResponseCode.ShouldBe("00");
        answer.ResponseDescription.ShouldBe("Approved or completed successfully");
        answer.ClientMessage.ShouldBe("Approved.");
        answer.AuthorisationCode.ShouldBe("238699");
        answer.AuthorisedAmount.ShouldBe(1999);
        answer.ReconciliationId.ShouldBe("MR747891457");
        answer.RetrievalReferenceNumber.ShouldBe("MR43851259");
        answer.AcmeTransactionAt.ShouldBe(Now);
    }

    [Fact]
    public async Task SubmitPayment_AnswerWithUnknownMember_ReturnsSuccess()
    {
        handler.AnswerWith(Answer(HttpStatusCode.OK, await ReadExampleAsync("PaymentResponseWithUnknownMember.json")));

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.Answer.Type.ShouldBe(AcmeAnswerType.Success);
        response.Answer.ResponseCode.ShouldBe("00");
    }

    [Fact]
    public async Task SubmitPayment_EmptyClientMessage_ReturnsUnreadable()
    {
        var body = JsonNode.Parse(await ReadExampleAsync("PaymentResponse.json"))!;
        body["PaymentResponse"]!["ResponseDetail"]!["ClientMessage"] = string.Empty;
        handler.AnswerWith(Answer(HttpStatusCode.OK, body.ToJsonString()));

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.Answer.Type.ShouldBe(AcmeAnswerType.Unreadable);
    }

    [Fact]
    public async Task SubmitPayment_ApprovalWithNoAuthorisedAmount_ReturnsUnreadable()
    {
        var body = JsonNode.Parse(await ReadExampleAsync("PaymentResponse.json"))!;
        body["PaymentResponse"]!.AsObject().Remove("AuthAmount");
        handler.AnswerWith(Answer(HttpStatusCode.OK, body.ToJsonString()));

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.Answer.Type.ShouldBe(AcmeAnswerType.Unreadable);
    }

    [Fact]
    public async Task SubmitPayment_AnswerForAnotherTransaction_ReturnsUnreadable()
    {
        var body = JsonNode.Parse(await ReadExampleAsync("PaymentResponse.json"))!;
        body["PaymentResponse"]!["TransactionID"] = "9c4e7f10-2a63-4d8b-91ef-5b027c88a4d1";
        handler.AnswerWith(Answer(HttpStatusCode.OK, body.ToJsonString()));

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.Answer.Type.ShouldBe(AcmeAnswerType.Unreadable);
    }

    [Fact]
    public async Task SubmitPayment_ValueLongerThanAppendixAllows_ReturnsUnreadable()
    {
        var body = JsonNode.Parse(await ReadExampleAsync("PaymentResponse.json"))!;
        body["PaymentResponse"]!["AuthCode"] = "2386991";
        handler.AnswerWith(Answer(HttpStatusCode.OK, body.ToJsonString()));

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.Answer.Type.ShouldBe(AcmeAnswerType.Unreadable);
    }

    [Theory]
    [InlineData("Complete", "00")]
    [InlineData("Reversed", "00")]
    [InlineData("Refunded", "00")]
    [InlineData("Complete", "51")]
    [InlineData("Reversed", "51")]
    [InlineData("Refunded", "51")]
    public async Task SubmitPayment_StatusOtherThanSuccess_IsReadByResponseCode(string status, string responseCode)
    {
        var body = JsonNode.Parse(await ReadExampleAsync("PaymentResponse.json"))!;
        body["PaymentResponse"]!["TransactionStatus"] = status;
        body["PaymentResponse"]!["ResponseDetail"]!["Code"] = responseCode;
        handler.AnswerWith(Answer(HttpStatusCode.OK, body.ToJsonString()));

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.Answer.Type.ShouldBe(AcmeAnswerType.Success);
        response.Answer.ResponseCode.ShouldBe(responseCode);
    }

    [Fact]
    public async Task SubmitPayment_OrderNumberHeldByAnotherTransaction_ReturnsOrderNumberHeldByAnother()
    {
        handler.AnswerWith(Answer(HttpStatusCode.Conflict, await ReadExampleAsync("OrderNumberInUse.json")));

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.HttpStatus.ShouldBe(409);
        response.Answer.Type.ShouldBe(AcmeAnswerType.OrderNumberHeldByAnother);
        response.Answer.ResponseCode.ShouldBe("E09");
    }

    [Fact]
    public async Task SubmitPayment_OrderNumberHeldByThisTransaction_ReturnsOrderNumberHeldByThis()
    {
        var body = JsonNode.Parse(await ReadExampleAsync("OrderNumberInUse.json"))!;
        body["PaymentResponse"]!["TransactionID"] = TransactionId.ToString();
        handler.AnswerWith(Answer(HttpStatusCode.Conflict, body.ToJsonString()));

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.Answer.Type.ShouldBe(AcmeAnswerType.OrderNumberHeldByThis);
    }

    [Fact]
    public async Task SubmitPayment_RateLimited_ReturnsRateLimitedWithRetryAfter()
    {
        var answer = Answer(HttpStatusCode.TooManyRequests, await ReadExampleAsync("RateLimited.json"));
        answer.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(2));
        handler.AnswerWith(answer);

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.HttpStatus.ShouldBe(429);
        response.Answer.Type.ShouldBe(AcmeAnswerType.RateLimited);
        response.Answer.ResponseCode.ShouldBe("E29");
        response.Answer.RetryAfter.ShouldBe(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task SubmitPayment_GatewayDegraded_ReturnsDegraded()
    {
        handler.AnswerWith(Answer(HttpStatusCode.ServiceUnavailable, await ReadExampleAsync("Degraded.json")));

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.HttpStatus.ShouldBe(503);
        response.Answer.Type.ShouldBe(AcmeAnswerType.Degraded);
        response.Answer.ResponseCode.ShouldBe("E50");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Malformed.json", "E01")]
    [InlineData(HttpStatusCode.Unauthorized, "ApiKeyNotValid.json", "E02")]
    public async Task SubmitPayment_Refused_ReturnsRefusedWithCode(HttpStatusCode status, string example, string responseCode)
    {
        handler.AnswerWith(Answer(status, await ReadExampleAsync(example)));

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.Answer.Type.ShouldBe(AcmeAnswerType.Refused);
        response.Answer.ResponseCode.ShouldBe(responseCode);
        response.Answer.ClientMessage.ShouldBe("We could not process that payment.");
    }

    [Fact]
    public async Task GetTransactionByTransactionId_AppendixExample_SendsAppendixQuery()
    {
        handler.AnswerWith(Answer(HttpStatusCode.OK, await ReadExampleAsync("QueryResponseProcessing.json")));

        var response = await acme.GetTransactionByTransactionIdAsync(TransactionId, TestContext.Current.CancellationToken);

        var sent = handler.Requests.ShouldHaveSingleItem();
        sent.Method.ShouldBe(HttpMethod.Get);
        sent.RequestUri.ShouldBe(new Uri(
            "https://sandbox.acmepay.example/apirttq/query/Merchant/8B67BF88-BB8D-4EB5-ADFC-0F5C97C7ED67?TransactionID=2e0e1b85-bf91-45a9-b038-282e00036919"));
        sent.Authorization!.Parameter.ShouldBe("sk_test_9f3c1a7e2b5d4c8a6e0f");
        response.Answer.Type.ShouldBe(AcmeAnswerType.Processing);
    }

    [Fact]
    public async Task GetTransactionByTransactionId_NotFound_ReturnsNotFound()
    {
        handler.AnswerWith(new HttpResponseMessage(HttpStatusCode.NotFound));

        var response = await acme.GetTransactionByTransactionIdAsync(TransactionId, TestContext.Current.CancellationToken);

        response.HttpStatus.ShouldBe(404);
        response.Answer.Type.ShouldBe(AcmeAnswerType.NotFound);
    }

    [Fact]
    public async Task SubmitPayment_NeverAnswered_ReturnsTimedOutAfterTimeLimit()
    {
        var call = acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);
        await handler.RequestReceived;

        time.Advance(TimeSpan.FromSeconds(35));

        var response = await call;
        response.Answer.Type.ShouldBe(AcmeAnswerType.TimedOut);
        response.Outcome.ShouldBe(AcmeInteractionOutcome.TimedOut);
        response.HttpStatus.ShouldBeNull();
        response.Duration.ShouldBe(TimeSpan.FromSeconds(35));
    }

    [Fact]
    public async Task SubmitPayment_AcmeKeepsFailing_ReturnsCircuitOpenWithoutCalling()
    {
        foreach (var _ in Enumerable.Range(0, 10))
        {
            handler.AnswerWith(Answer(HttpStatusCode.ServiceUnavailable, await ReadExampleAsync("Degraded.json")));
            await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);
        }

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.Answer.Type.ShouldBe(AcmeAnswerType.CircuitOpen);
        response.Answer.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
        response.Outcome.ShouldBeNull();
        handler.Requests.Count.ShouldBe(10);
    }

    [Fact]
    public async Task SubmitPayment_Redirect_ReturnsUnreadableWithoutFollowingIt()
    {
        var answer = new HttpResponseMessage(HttpStatusCode.Found);
        answer.Headers.Location = new Uri("https://elsewhere.example/api/payment");
        handler.AnswerWith(answer);

        var response = await acme.SubmitPaymentAsync(ExampleRequest(), TestContext.Current.CancellationToken);

        response.Answer.Type.ShouldBe(AcmeAnswerType.Unreadable);
        handler.Requests.ShouldHaveSingleItem();
    }

    [Fact]
    public void AcmeClient_ProductionRegistration_FollowsNoRedirectAndLimitsConnections()
    {
        using var productionHost = BuildHost(replacePrimaryHandler: false);
        using var client = productionHost.Services.GetRequiredService<IHttpMessageHandlerFactory>()
            .CreateHandler(nameof(IAcmeService));

        var primary = GetPrimaryHandler(client).ShouldBeOfType<SocketsHttpHandler>();

        primary.AllowAutoRedirect.ShouldBeFalse();
        primary.MaxConnectionsPerServer.ShouldBe(50);
        primary.PooledConnectionIdleTimeout.ShouldBe(TimeSpan.FromSeconds(50));
    }

    private static SubmitPaymentRequest ExampleRequest() =>
        new(
            Guid.Parse("a5f1c204-9e77-4a3e-8f21-0b6d1c4e5a90"),
            TransactionId,
            "MR43851259",
            1999,
            "ZAR",
            "CardNotPresent",
            "4f2a8c9e1b7d3560",
            "CIT",
            "ZAP0004182",
            "5812",
            "ZAP*CAFE DEL SOL       CAPE TOWN    WCZA",
            "alice@example.com");

    private static HttpResponseMessage Answer(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static Task<string> ReadExampleAsync(string example) =>
        File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Integrations", "Acme", "Examples", example),
            TestContext.Current.CancellationToken);

    private static HttpMessageHandler GetPrimaryHandler(HttpMessageHandler handler) =>
        handler is DelegatingHandler delegating ? GetPrimaryHandler(delegating.InnerHandler!) : handler;

    // the production registration, with only the primary handler replaced where the test answers for Acme
    private IHost BuildHost(bool replacePrimaryHandler)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration.AddInMemoryCollection(Settings);
        builder.Services.AddSingleton<TimeProvider>(time);
        builder.Services.AddZapperInfrastructure();

        if (replacePrimaryHandler)
        {
            builder.Services.AddHttpClient<IAcmeService, AcmeService>()
                .ConfigurePrimaryHttpMessageHandler(() => handler);
        }

        return builder.Build();
    }
}
