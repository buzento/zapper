using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Zapper.ApiCommon.Managers.Cryptography;
using Zapper.Domain.Customer;
using Zapper.Domain.Merchant;
using Zapper.Domain.Payment;
using Zapper.Infrastructure.Integrations.Acme;
using Zapper.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Acceptance;

[Collection("Acceptance")]
public sealed class SubmittingPaymentsToAcmeAcceptanceTests : AcceptanceTestBase
{
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);

    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);

    private readonly AcceptanceFixture acceptance;

    private readonly HttpClient client;

    private readonly ZapperDbContext database;

    private readonly IAcmeService acme;

    private readonly FakeTimeProvider time;

    public SubmittingPaymentsToAcmeAcceptanceTests(AcceptanceFixture acceptance)
    {
        this.acceptance = acceptance;
        client = acceptance.GetClient("Zapper.PaymentsApi");
        database = acceptance.Database;
        acme = acceptance.Acme;
        time = acceptance.Time;
    }

    private static string SubMerchantReference => $"ZAP{Discriminator}";

    private static string CustomerReference => $"CUS-2001-{Discriminator}";

    private static string EmailAddress => $"cus-2001-{Discriminator}@example.test";

    private static string CardToken => $"test-token-{Discriminator}";

    [Fact(DisplayName = "Payment approved by the acquirer is shown as approved")]
    [Trait("Rule", "R-1")]
    [Trait("Rule", "R-2")]
    [Trait("Rule", "R-14")]
    public async Task PaymentApprovedByTheAcquirerIsShownAsApproved()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-1");
        var sends = new ConcurrentQueue<(DateTimeOffset At, SubmitPaymentRequest Request)>();
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Approved(), 200))
            .AndDoes(call => sends.Enqueue((time.GetUtcNow(), call.Arg<SubmitPaymentRequest>())));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(10));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Approved");
        payment["responseCode"]!.GetValue<string>().ShouldBe("00");
        payment["clientMessage"]!.GetValue<string>().ShouldBe("Approved.");
        var stored = await database.Payments.SingleAsync(
            candidate => candidate.Id == id, TestContext.Current.CancellationToken);
        var sent = sends.ShouldHaveSingleItem().Request;
        sent.TransactionId.ShouldBe(id);
        sent.OrderNumber.ShouldBe(stored.OrderNumber);
        sent.Amount.ShouldBe(1999);
        sent.CurrencyCode.ShouldBe("ZAR");
        sent.Token.ShouldBe(CardToken);
        sent.CardOnFileScenario.ShouldBe("CIT");
        sent.SubMerchantReference.ShouldBe(SubMerchantReference);
        sent.MerchantCategoryCode.ShouldBe("5812");
        sent.CardAcceptorName.ShouldBe(stored.CardAcceptorName);
        sent.Email.ShouldBe(EmailAddress);
    }

    [Fact(DisplayName = "Payment declined by the acquirer is shown as declined")]
    [Trait("Rule", "R-2")]
    public async Task PaymentDeclinedByTheAcquirerIsShownAsDeclined()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-2");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Declined(), 200));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(10));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Declined");
        payment["responseCode"]!.GetValue<string>().ShouldBe("51");
    }

    [Fact(DisplayName = "Payment the gateway fails is shown as failed")]
    [Trait("Rule", "R-2")]
    public async Task PaymentTheGatewayFailsIsShownAsFailed()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-3");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Failed(), 200));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(10));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Failed");
    }

    [Fact(DisplayName = "Partial authorisation is approved with the authorised amount")]
    [Trait("Rule", "R-2")]
    public async Task PartialAuthorisationIsApprovedWithTheAuthorisedAmount()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-4");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Approved(authorisedAmount: 1500), 200));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(10));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Approved");
        payment["authorisedAmount"]!.GetValue<long>().ShouldBe(1500);
    }

    [Fact(DisplayName = "Approval above the amount sent is approved with the authorised amount")]
    [Trait("Rule", "R-2")]
    public async Task ApprovalAboveTheAmountSentIsApprovedWithTheAuthorisedAmount()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-23");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Approved(authorisedAmount: 2500), 200));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(10));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Approved");
        payment["authorisedAmount"]!.GetValue<long>().ShouldBe(2500);
    }

    [Fact(DisplayName = "Answer with an empty client message is asked about")]
    [Trait("Rule", "R-3")]
    public async Task AnswerWithAnEmptyClientMessageIsAskedAbout()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-24");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(ApprovedWithEmptyClientMessage(), 200));
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(Approved(), 200));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(30));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Approved");
        payment["clientMessage"]!.GetValue<string>().ShouldBe("Approved.");
    }

    [Fact(DisplayName = "Payment whose send times out is approved after a query")]
    [Trait("Rule", "R-3")]
    [Trait("Rule", "R-4")]
    [Trait("Rule", "R-14")]
    public async Task PaymentWhoseSendTimesOutIsApprovedAfterAQuery()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-5");
        var sends = new ConcurrentQueue<DateTimeOffset>();
        var queries = new ConcurrentQueue<DateTimeOffset>();
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendTimedOut())
            .AndDoes(_ => sends.Enqueue(time.GetUtcNow()));
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(Processing(), 200), _ => QueryAnswered(Approved(), 200))
            .AndDoes(_ => queries.Enqueue(time.GetUtcNow()));

        await acceptance.MoveClockUntilAsync(id, Second, TimeSpan.FromSeconds(10), () => Task.FromResult(!sends.IsEmpty));

        (await GetStatusAsync(id)).ShouldBe("Pending");

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(60));

        queries.Count.ShouldBe(2);
        (queries.ElementAt(0) - sends.ElementAt(0)).ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(5));
        (queries.ElementAt(1) - queries.ElementAt(0)).ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(10));
        (await GetStatusAsync(id)).ShouldBe("Approved");
        sends.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Query that finds the payment reversed sets the outcome approved")]
    [Trait("Rule", "R-2")]
    public async Task QueryThatFindsThePaymentReversedSetsTheOutcomeApproved()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-25");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendTimedOut());
        // AcmeService reads Reversed with code 00 as Success, and keeps the status as sent in the body
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(Approved(), 200, """{"PaymentResponse":{"TransactionStatus":"Reversed"}}"""));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(30));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Approved");
        payment["responseCode"]!.GetValue<string>().ShouldBe("00");
    }

    [Fact(DisplayName = "Payment that meets a gateway error is asked about before it is sent again")]
    [Trait("Rule", "R-3")]
    public async Task PaymentThatMeetsAGatewayErrorIsAskedAboutBeforeItIsSentAgain()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-6");
        var sends = new ConcurrentQueue<DateTimeOffset>();
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Degraded(), 503))
            .AndDoes(_ => sends.Enqueue(time.GetUtcNow()));
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(Approved(), 200));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(30));

        (await GetStatusAsync(id)).ShouldBe("Approved");
        sends.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Payment Acme never received is sent again with the same identity")]
    [Trait("Rule", "R-3")]
    [Trait("Rule", "R-5")]
    public async Task PaymentAcmeNeverReceivedIsSentAgainWithTheSameIdentity()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-7");
        var sends = new ConcurrentQueue<SubmitPaymentRequest>();
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendTimedOut(), _ => SendAnswered(Approved(), 200))
            .AndDoes(call => sends.Enqueue(call.Arg<SubmitPaymentRequest>()));
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(NotFound(), 404));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(60));

        (await GetStatusAsync(id)).ShouldBe("Approved");
        sends.Count.ShouldBe(2);
        sends.Select(sent => sent.TransactionId).Distinct().ShouldHaveSingleItem();
        sends.Select(sent => sent.OrderNumber).Distinct().ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "Payment never received after three sends fails as not submitted")]
    [Trait("Rule", "R-5")]
    [Trait("Rule", "R-14")]
    public async Task PaymentNeverReceivedAfterThreeSendsFailsAsNotSubmitted()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-8");
        var sends = new ConcurrentQueue<DateTimeOffset>();
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendTimedOut())
            .AndDoes(_ => sends.Enqueue(time.GetUtcNow()));
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(NotFound(), 404));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(60));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Failed");
        payment["responseCode"].ShouldBeNull();
        payment["clientMessage"].ShouldBeNull();
        sends.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Rate limited payment is sent again only after Retry-After")]
    [Trait("Rule", "R-6")]
    public async Task RateLimitedPaymentIsSentAgainOnlyAfterRetryAfter()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-9");
        var sends = new ConcurrentQueue<DateTimeOffset>();
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(RateLimited(), 429), _ => SendAnswered(Approved(), 200))
            .AndDoes(_ => sends.Enqueue(time.GetUtcNow()));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(30));

        sends.Count.ShouldBe(2);
        (sends.ElementAt(1) - sends.ElementAt(0)).ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(2));
        (await GetStatusAsync(id)).ShouldBe("Approved");
    }

    [Fact(DisplayName = "Payment refused as malformed fails")]
    [Trait("Rule", "R-7")]
    public async Task PaymentRefusedAsMalformedFails()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-10");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Refused("E01", "The body could not be parsed"), 400));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(10));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Failed");
        payment["responseCode"]!.GetValue<string>().ShouldBe("E01");
    }

    [Fact(DisplayName = "Payment refused for a bad API key fails")]
    [Trait("Rule", "R-7")]
    public async Task PaymentRefusedForABadApiKeyFails()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-11");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Refused("E02", "The API key is missing or not valid"), 401));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(10));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Failed");
        payment["responseCode"]!.GetValue<string>().ShouldBe("E02");
    }

    [Fact(DisplayName = "Order number held by another transaction fails the payment")]
    [Trait("Rule", "R-7")]
    public async Task OrderNumberHeldByAnotherTransactionFailsThePayment()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-12");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(OrderNumberHeld(AcmeAnswerType.OrderNumberHeldByAnother), 409));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(10));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Failed");
        payment["responseCode"]!.GetValue<string>().ShouldBe("E09");
    }

    [Fact(DisplayName = "Order number held by the payment itself is asked about")]
    [Trait("Rule", "R-3")]
    public async Task OrderNumberHeldByThePaymentItselfIsAskedAbout()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-13");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(OrderNumberHeld(AcmeAnswerType.OrderNumberHeldByThis), 409));
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(Approved(), 200));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(30));

        (await GetStatusAsync(id)).ShouldBe("Approved");
    }

    [Fact(DisplayName = "Query refused leaves the payment for a person")]
    [Trait("Rule", "R-7")]
    public async Task QueryRefusedLeavesThePaymentForAPerson()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-14");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendTimedOut());
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(Refused("E02", "The API key is missing or not valid"), 401));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(30));

        (await GetStatusAsync(id)).ShouldBe("Unresolved");
    }

    [Fact(DisplayName = "Payment with no final answer after 24 hours needs a person")]
    [Trait("Rule", "R-8")]
    public async Task PaymentWithNoFinalAnswerAfter24HoursNeedsAPerson()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-15");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendTimedOut());
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(Processing(), 200));

        await acceptance.MoveClockAsync(id, Hour, TimeSpan.FromHours(24));

        (await GetStatusAsync(id)).ShouldBe("Unresolved");
    }

    [Fact(DisplayName = "Same payment recorded twice is sent to Acme once")]
    [Trait("Rule", "R-9")]
    public async Task SamePaymentRecordedTwiceIsSentToAcmeOnce()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-16");
        (await RecordPaymentAsync("SUB-16")).ShouldBe(id);
        var sends = new ConcurrentQueue<SubmitPaymentRequest>();
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Approved(), 200))
            .AndDoes(call => sends.Enqueue(call.Arg<SubmitPaymentRequest>()));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(10));

        sends.ShouldHaveSingleItem().TransactionId.ShouldBe(id);
    }

    [Fact(DisplayName = "Call record never holds the card token or the email address")]
    [Trait("Rule", "R-10")]
    public async Task CallRecordNeverHoldsTheCardTokenOrTheEmailAddress()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-17");
        var sends = new ConcurrentQueue<SubmitPaymentRequest>();
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Approved(), 200))
            .AndDoes(call => sends.Enqueue(call.Arg<SubmitPaymentRequest>()));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(10));

        sends.ShouldHaveSingleItem().TransactionId.ShouldBe(id);
        var acmeInteractions = await database.AcmeInteractions
            .Where(acmeInteraction => acmeInteraction.PaymentId == id)
            .ToListAsync(TestContext.Current.CancellationToken);
        var stored = acmeInteractions.ShouldHaveSingleItem();
        stored.Operation.ShouldBe(AcmeOperation.Send);
        stored.HttpStatus.ShouldBe(200);
        // the worker stores the request it built, so its members carry the request type's names
        var request = JsonNode.Parse(stored.Request)!;
        request["Token"]!.GetValue<string>().ShouldBe("[removed]");
        request["Email"]!.GetValue<string>().ShouldBe("[removed]");
        stored.Request.ShouldNotContain(CardToken);
        stored.Request.ShouldNotContain(EmailAddress);
    }

    [Fact(DisplayName = "Each change of a payment is recorded")]
    [Trait("Rule", "R-11")]
    public async Task EachChangeOfAPaymentIsRecorded()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-18");
        var sends = new ConcurrentQueue<DateTimeOffset>();
        var queries = new ConcurrentQueue<DateTimeOffset>();
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendTimedOut())
            .AndDoes(_ => sends.Enqueue(time.GetUtcNow()));
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(Processing(), 200), _ => QueryAnswered(Approved(), 200))
            .AndDoes(_ => queries.Enqueue(time.GetUtcNow()));

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(60));

        sends.Count.ShouldBe(1);
        queries.Count.ShouldBe(2);
        var acmeInteractions = await database.AcmeInteractions
            .Where(acmeInteraction => acmeInteraction.PaymentId == id)
            .OrderBy(acmeInteraction => acmeInteraction.StartedAt)
            .ToListAsync(TestContext.Current.CancellationToken);
        acmeInteractions.Count.ShouldBe(3);
        var changes = await database.PaymentChanges
            .Where(change => change.PaymentId == id)
            .OrderBy(change => change.ChangedAt)
            .ToListAsync(TestContext.Current.CancellationToken);
        changes.Select(change => (change.FromState, change.ToState, change.Source, change.AcmeInteractionId)).ShouldBe(
            [
                (PaymentState.Recorded, PaymentState.OutcomeUnknown, PaymentChangeSource.Send, acmeInteractions[0].Id),
                (PaymentState.OutcomeUnknown, PaymentState.Approved, PaymentChangeSource.Query, acmeInteractions[2].Id)
            ]);
    }

    [Fact(DisplayName = "Payment whose work keeps failing fails when Acme does not hold it")]
    [Trait("Rule", "R-12")]
    public async Task PaymentWhoseWorkKeepsFailingFailsWhenAcmeDoesNotHoldIt()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-19");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("The send failed in a way nobody planned for."));
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(NotFound(), 404));

        await MoveClockUntilFinalAsync(id, Minute, TimeSpan.FromMinutes(60));

        (await GetStatusAsync(id)).ShouldBe("Failed");
    }

    [Fact(DisplayName = "Payment whose work keeps failing takes the answer Acme already holds")]
    [Trait("Rule", "R-12")]
    public async Task PaymentWhoseWorkKeepsFailingTakesTheAnswerAcmeAlreadyHolds()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-20");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("The send failed in a way nobody planned for."));
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(Approved(), 200));

        await MoveClockUntilFinalAsync(id, Minute, TimeSpan.FromMinutes(60));

        (await GetStatusAsync(id)).ShouldBe("Approved");
    }

    [Fact(DisplayName = "Payment whose work keeps failing needs a person when Acme is unclear")]
    [Trait("Rule", "R-12")]
    public async Task PaymentWhoseWorkKeepsFailingNeedsAPersonWhenAcmeIsUnclear()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-21");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("The send failed in a way nobody planned for."));
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => QueryAnswered(Processing(), 200));

        await MoveClockUntilFinalAsync(id, Minute, TimeSpan.FromMinutes(60));

        (await GetStatusAsync(id)).ShouldBe("Unresolved");
    }

    [Fact(DisplayName = "Payment due while the circuit is open is not sent until it closes")]
    [Trait("Rule", "R-13")]
    public async Task PaymentDueWhileTheCircuitIsOpenIsNotSentUntilItCloses()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("SUB-22");
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendRefusedByOpenCircuit(), _ => SendAnswered(Approved(), 200));

        await acceptance.MoveClockAsync(id, Second, TimeSpan.FromSeconds(29));

        (await GetStatusAsync(id)).ShouldBe("Pending");
        (await database.AcmeInteractions.AnyAsync(
                acmeInteraction => acmeInteraction.PaymentId == id, TestContext.Current.CancellationToken))
            .ShouldBeFalse();

        await MoveClockUntilFinalAsync(id, Second, TimeSpan.FromSeconds(10));

        (await GetStatusAsync(id)).ShouldBe("Approved");
    }

    private static SubmitPaymentRequest SentFor(Guid id) =>
        Arg.Is<SubmitPaymentRequest>(request => request.TransactionId == id);

    private static AcmeAnswer Processing() => new(AcmeAnswerType.Processing);

    private static AcmeAnswer NotFound() => new(AcmeAnswerType.NotFound);

    private static AcmeAnswer Degraded() =>
        new(AcmeAnswerType.Degraded, "Application", "E50", "The gateway is degraded.");

    private static AcmeAnswer RateLimited() =>
        new(AcmeAnswerType.RateLimited, "Application", "E29", "Too many requests", retryAfter: TimeSpan.FromSeconds(2));

    private static AcmeAnswer Refused(string responseCode, string responseDescription) =>
        new(AcmeAnswerType.Refused, "Application", responseCode, responseDescription, "We could not process that payment.");

    private static AcmeAnswer OrderNumberHeld(AcmeAnswerType type) =>
        new(type, "Application", "E09", "Retrieval Reference Number already in use", "We could not process that payment.");

    private AcmeAnswer Approved(long authorisedAmount = 1999) =>
        new(
            AcmeAnswerType.Success,
            "Acquirer",
            "00",
            "Approved or completed successfully",
            "Approved.",
            "238699",
            authorisedAmount,
            "MR747891457",
            "MR43851259",
            time.GetUtcNow());

    // AcmeService reads an empty client message as an answer it cannot read
    private AcmeAnswer ApprovedWithEmptyClientMessage() =>
        new(
            AcmeAnswerType.Unreadable,
            "Acquirer",
            "00",
            "Approved or completed successfully",
            string.Empty,
            "238699",
            1999,
            "MR747891457",
            "MR43851259",
            time.GetUtcNow());

    private AcmeAnswer Declined() =>
        new(
            AcmeAnswerType.Success,
            "Acquirer",
            "51",
            "Insufficient funds",
            "There was not enough money in the account.",
            reconciliationId: "MR747891457",
            retrievalReferenceNumber: "MR43851259",
            acmeTransactionAt: time.GetUtcNow());

    private AcmeAnswer Failed() =>
        new(
            AcmeAnswerType.Failure,
            "Acquirer",
            "91",
            "Issuer or switch inoperative",
            "The bank could not be reached. Please try again.",
            reconciliationId: "MR747891457",
            retrievalReferenceNumber: "MR43851259",
            acmeTransactionAt: time.GetUtcNow());

    private SubmitPaymentResponse SendAnswered(AcmeAnswer answer, int httpStatus) =>
        new(answer, AcmeInteractionOutcome.Answered, httpStatus, "{}", time.GetUtcNow(), TimeSpan.FromMilliseconds(400));

    private SubmitPaymentResponse SendTimedOut() =>
        new(new AcmeAnswer(AcmeAnswerType.TimedOut), AcmeInteractionOutcome.TimedOut, null, null, time.GetUtcNow(), TimeSpan.FromSeconds(35));

    private SubmitPaymentResponse SendRefusedByOpenCircuit() =>
        new(new AcmeAnswer(AcmeAnswerType.CircuitOpen, retryAfter: TimeSpan.FromSeconds(30)), null, null, null, time.GetUtcNow(), TimeSpan.Zero);

    private GetTransactionByTransactionIdResponse QueryAnswered(AcmeAnswer answer, int httpStatus, string body = "{}") =>
        new(answer, AcmeInteractionOutcome.Answered, httpStatus, body, time.GetUtcNow(), TimeSpan.FromMilliseconds(400));

    private async Task ArrangeBackgroundAsync()
    {
        database.SubMerchants.Add(new SubMerchantEntity(SubMerchantReference, "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812"));
        database.StoredCards.Add(new StoredCardEntity(
            CustomerReference,
            new CryptographyManager().Encrypt(CardToken, acceptance.TokenKey, Encoding.UTF8.GetBytes(CustomerReference)),
            EmailAddress));

        await database.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<Guid> RecordPaymentAsync(string appPaymentReference)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/payments",
            new
            {
                customerReference = CustomerReference,
                subMerchantReference = SubMerchantReference,
                amount = 1999L,
                currencyCode = "ZAR",
                appPaymentReference = $"{appPaymentReference}-{Discriminator}"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // a 201 from RecordPayment always carries the payment
        var body = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;

        return body["id"]!.GetValue<Guid>();
    }

    private async Task<JsonObject> GetPaymentAsync(Guid id) =>
        // the payment was recorded by this test, so the read always finds it
        (await client.GetFromJsonAsync<JsonObject>($"/v1/payments/{id}", TestContext.Current.CancellationToken))!;

    private async Task<string> GetStatusAsync(Guid id) =>
        (await GetPaymentAsync(id))["status"]!.GetValue<string>();

    private async Task<bool> IsFinalAsync(Guid id) => await GetStatusAsync(id) != "Pending";

    private Task MoveClockUntilFinalAsync(Guid id, TimeSpan step, TimeSpan limit) =>
        acceptance.MoveClockUntilAsync(id, step, limit, () => IsFinalAsync(id));
}
