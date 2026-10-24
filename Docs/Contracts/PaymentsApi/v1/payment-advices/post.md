- **Fragment:** `post.openapi.json`, beside this file. **The fragment is the
  truth**; this document explains it.
- **Status:** Frozen 2026-10-24
- **Governed by:** `Zapper_Team_Lead_Assement` D-7 and D-8
- **Produced by:** `Zapper.PaymentsApi`, Feature Acme Notifications
- **Consumed by:** Acme Pay, which sends a Payment Advice whenever a transaction
  reaches an outcome

> A **contract**, not an implementation. It freezes **one operation on one route
> of one API**: the surface a caller outside this codebase depends on. Callers
> depend only on this contract, never on a concrete type or another feature's
> internals. A frozen contract changes only by a new **version**, never an
> in-place edit, and because these APIs version by URL a new version is a new
> path and therefore a new file. So `v1` and `v2` are different files that
> coexist, and neither replaces the other. **There is no freeze counter in the
> `Status` line**: a contract's version is the API version in its path, and what
> changed between two freezes of one file is in git.
>
> **Only API surfaces are contracts.** An interface is proved by unit tests and a
> queue message by integration tests, so neither is frozen and neither belongs
> under `Docs/Contracts/`. If a boundary is not exposed by an API, it is not a
> contract.

# Purpose

Acme Pay tells Zapper the outcome of a transaction. This operation receives that
Payment Advice, stores it as received, and uses it to confirm or contradict the
outcome Zapper already holds. It never sets the outcome on its own, because a Payment
Advice carries neither the client message nor the authorised amount.

# Definition

The fragment beside this document covers `POST /v1/payment-advices` and nothing
else. The field names are Acme's own, as Appendix A of the brief fixes them, and each
is mapped to a C# property named for what it is.

```
POST /v1/payment-advices
Authorization: Basic <Payment Advice username and password>
{
  "RequestType": "Payment",
  "MerchantID": "8B67BF88-BB8D-4EB5-ADFC-0F5C97C7ED67",
  "TransactionID": "2e0e1b85-bf91-45a9-b038-282e00036919",
  "TransactionDateTime": "2026-03-01T13:56:36.533",
  "OrderNumber": "MR4385125900",
  "ReconID": "MR747891457",
  "RetrievalReferenceNumber": "MR4385125900",
  "SystemsTraceAuditNumber": "7543",
  "Amount": 1999,
  "CurrencyCode": "ZAR",
  "PaymentService": "CardNotPresent",
  "TransactionStatus": "Success",
  "ResponseSource": "Acquirer",
  "ResponseCode": "00",
  "ResponseDescription": "Approved",
  "AuthCode": "238699",
  "MaskedCardNumber": "541333******1429",
  "CommProtocol": "REST"
}

200, no body
```

# Fields

| Field | Type | Required | User-facing? | Notes |
|---|---|---|---|---|
| `RequestType` | string, 1 to 20 | y | n | `Authorize`, `Capture` or `Payment`; any other value is recorded and changes nothing |
| `MerchantID` | string, 1 to 50 | y | n | Zapper's account at Acme; another value leaves the advice unmatched |
| `TransactionID` | string, 1 to 50 | y | n | Our payment's id; identifies the advice together with `RequestType` |
| `TransactionDateTime` | string or null | n | n | `yyyy-MM-ddTHH:mm:ss.fff`, South African time, no offset |
| `OrderNumber` | string or null, at most 50 | n | n | Must equal the payment's order number where sent |
| `ReconID` | string or null, at most 50 | n | n | Acme's reconciliation identifier |
| `RetrievalReferenceNumber` | string, 1 to 12 | y | n | Must equal the payment's order number |
| `SystemsTraceAuditNumber` | string, 1 to 6 | y | n | Recorded |
| `Amount` | 64-bit integer, at least 1 | y | n | Must equal the payment's amount |
| `CurrencyCode` | string or null, at most 3 | n | n | Must equal the payment's currency where sent |
| `PaymentService` | string, 1 to 30 | y | n | Recorded |
| `TransactionStatus` | string, 1 to 20 | y | n | `Success`, `Failure` or `Other`; any other value is recorded and changes nothing |
| `ResponseSource` | string, 1 to 50 | y | n | Recorded |
| `ResponseCode` | string, 1 to 3 | y | n | `00` is approved |
| `ResponseDescription` | string or null, at most 50 | n | n | Recorded |
| `AuthCode` | string or null, at most 6 | n | n | Recorded |
| `CommProtocol` | string, 1 to 4 | y | n | Recorded |

# Rules

- **CR-1: Basic credentials.** The operation requires the scheme
  `AcmePaymentAdvice`, HTTP Basic, granting `SendPaymentAdvice`. A missing or wrong
  username or password answers 401 `Unauthorized`, and never says which part was
  wrong. Nothing is stored.
- **CR-2: 200 once stored.** Every advice that passes CR-1 and validation answers 200
  with no body: new, a duplicate, unmatched and a contradiction alike. The answer says
  nothing about the payment.
- **CR-3: Fields Acme adds are accepted.** A member this contract does not name is
  ignored, and kept in the stored body. `MaskedCardNumber` is never stored.
- **CR-4: Refusals by the per-field map.** `RequestTypeRequired`, `RequestTypeTooLong`,
  `MerchantIdRequired`, `MerchantIdTooLong`, `TransactionIdRequired`,
  `TransactionIdTooLong`, `TransactionDateTimeMalformed`, `OrderNumberTooLong`,
  `ReconciliationIdTooLong`, `RetrievalReferenceNumberRequired`,
  `RetrievalReferenceNumberTooLong`, `SystemsTraceAuditNumberRequired`,
  `SystemsTraceAuditNumberTooLong`, `AmountBelowMinimum`, `CurrencyCodeTooLong`,
  `PaymentServiceRequired`, `PaymentServiceTooLong`, `TransactionStatusRequired`,
  `TransactionStatusTooLong`, `ResponseSourceRequired`, `ResponseSourceTooLong`,
  `ResponseCodeRequired`, `ResponseCodeTooLong`, `ResponseDescriptionTooLong`,
  `AuthorisationCodeTooLong`, `CommunicationProtocolRequired` and
  `CommunicationProtocolTooLong`, each against the field it names, keyed by Acme's
  name for it. Nothing is stored.
- **CR-5: A 500 is safe to resend.** 500 `ConcurrentChange` means the payment changed
  while the advice was applied, and nothing was stored. Acme's resend applies it.
