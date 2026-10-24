- **Fragment:** `post.openapi.json`, beside this file. **The fragment is the
  truth**; this document explains it.
- **Status:** Frozen 2026-10-24
- **Governed by:** `Zapper_Team_Lead_Assement` D-7 and D-8
- **Produced by:** `Zapper.PaymentsApi`, Feature Acme Notifications
- **Consumed by:** Acme Switch, which publishes reversals and refunds

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

Acme Switch tells Zapper that a payment was reversed or refunded, possibly days after
it was approved and possibly before Zapper knows the outcome. This operation receives
that Transaction Status, stores it exactly as received, and applies it by the rules of
D-8.

# Definition

The fragment beside this document covers `POST /v1/transaction-statuses` and nothing
else. The field names are Acme Switch's own, as Appendix A fixes them, and each is
mapped to a C# property named for what it is. The signature is the Base64
HMAC-SHA256 of the exact body bytes, keyed with the webhook secret.

```
POST /v1/transaction-statuses
x-signature: KURAJlPrV3BWvWzsGBgr0EzClUSfu3itG62dM5Lqy/Y=
{"transactionUUID":"7f3a9c12-4b8e-4d21-9f0a-6c5e2d81b4a7","transactionType":"Reversal","transactionDateTime":"2026-03-04T08:02:11.417","rrn":"MR43851259","transactionAmount":1999,"currencyCode":"ZAR","txResultCode":"00","isApproved":true}

200, no body
```

# Fields

| Field | Type | Required | User-facing? | Notes |
|---|---|---|---|---|
| `transactionUUID` | string, 1 to 50 | y | n | Acme Switch's identifier for the event; identifies the notification |
| `transactionType` | string, 1 to 20 | y | n | `Reversal` or `Refund`; any other value is recorded and changes nothing |
| `transactionDateTime` | string | y | n | `yyyy-MM-ddTHH:mm:ss.fff`, South African time, no offset |
| `rrn` | string, 1 to 12 | y | n | The Retrieval Reference Number, which is our payment's order number |
| `transactionAmount` | 64-bit integer, at least 1 | y | n | The event's own amount |
| `currencyCode` | string, 1 to 3 | y | n | Must equal the payment's currency |
| `txResultCode` | string, 1 to 3 | y | n | `00` when the switch applied the event |
| `isApproved` | boolean | y | n | Whether the switch applied the event; nullable in the schema so that a missing value is refused by the validator rather than read as false |

# Rules

- **CR-1: Signature.** The operation requires the scheme `AcmeTransactionStatus`, the
  `x-signature` header, granting `SendTransactionStatus`. A missing header, a wrong
  signature, or a body changed by one byte answers 401 `Unauthorized`. The signature
  is checked before the body is bound. Nothing is stored.
- **CR-2: 200 once stored.** Every proven notification that passes validation answers
  200 with no body: new, a duplicate, unmatched and a contradiction alike. The answer
  says nothing about the payment.
- **CR-3: Fields Acme adds are accepted.** A member this contract does not name is
  ignored, and the body is stored exactly as received.
- **CR-4: Refusals by the per-field map.** `TransactionUuidRequired`,
  `TransactionUuidTooLong`, `TransactionTypeRequired`, `TransactionTypeTooLong`,
  `TransactionDateTimeRequired`, `TransactionDateTimeMalformed`,
  `RetrievalReferenceNumberRequired`, `RetrievalReferenceNumberTooLong`,
  `AmountBelowMinimum`, `CurrencyCodeRequired`, `CurrencyCodeTooLong`,
  `TransactionResultCodeRequired`, `TransactionResultCodeTooLong` and
  `IsApprovedRequired`, each against the field it names, keyed by Acme's name for it.
  Nothing is stored.
- **CR-5: A 500 is safe to resend.** 500 `ConcurrentChange` means the payment changed
  while the notification was applied, and nothing was stored. Acme Switch's resend
  applies it.
