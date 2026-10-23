- **Fragment:** `post.openapi.json`, beside this file. **The fragment is the
  truth**; this document explains it.
- **Status:** Frozen 2026-10-23
- **Governed by:** `Zapper_Team_Lead_Assement` D-1 and D-2
- **Produced by:** `Zapper.PaymentsApi`, Feature Recording payments
- **Consumed by:** Zapper's app

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

The app records a customer's card payment at one of Zapper's sub-merchants. The
payment is stored and answered at once, before Acme Pay has been called, so the
app is never held waiting on Acme. The app then follows the payment with
`GET /v1/payments/{id}` until its status is final.

# Definition

The fragment beside this document covers `POST /v1/payments` and nothing else.
A recorded payment answers 201, with the payment and a `Location` header naming
it. A request the validator refuses answers 400 with the per-field map; a request
refused for what is already stored answers 400 with a code and no map.

```
POST /v1/payments
{
  "customerReference": "CUS-1001",
  "subMerchantReference": "ZAP0004182",
  "amount": 1999,
  "currencyCode": "ZAR",
  "appPaymentReference": "APP-1"
}

201, Location: /v1/payments/{id}
{
  "id": "0198f3c1-1a2b-7c3d-8e4f-5a6b7c8d9e0f",
  "appPaymentReference": "APP-1",
  "status": "Pending",
  "amount": 1999,
  "currencyCode": "ZAR",
  "responseCode": null,
  "clientMessage": null,
  "reversedAmount": 0,
  "refundedAmount": 0
}
```

# Fields

| Field | Type | Required | User-facing? | Notes |
|---|---|---|---|---|
| `customerReference` | string, 1 to 50 | y | n | Zapper's reference for the customer, who must hold a stored card |
| `subMerchantReference` | string, 1 to 15 | y | n | Zapper's reference for the sub-merchant, which must be registered |
| `amount` | 64-bit integer | y | n | In the currency's lowest unit, from 1 to the currency's maximum |
| `currencyCode` | string, 3 | y | n | ZAR, USD, EUR or GBP, in any case |
| `appPaymentReference` | string, 1 to 50 | y | n | The app's own reference for this payment; one payment per reference |
| `id` (response) | uuid | y | n | Our id for the payment, used to read it |
| `appPaymentReference` (response) | string | y | n | As sent |
| `status` (response) | string | y | n | One of `Pending`, `Approved`, `Declined`, `Failed`, `Reversed`, `Refunded`, `PartiallyRefunded`, `Unresolved`; always `Pending` here |
| `amount` (response) | 64-bit integer | y | n | As sent |
| `currencyCode` (response) | string, 3 | y | n | Always in capitals |
| `responseCode` (response) | string or null | y | n | Acme's response code; null until Acme answers |
| `clientMessage` (response) | string or null | y | n | Acme's wording that is safe to show the customer; null until Acme answers |
| `reversedAmount` (response) | 64-bit integer | y | n | The total reversed, in the lowest unit |
| `refundedAmount` (response) | 64-bit integer | y | n | The total refunded, in the lowest unit |

> **User-facing? = yes** → the signed-off design must already account for this
> field. The test is not whether the UI renders the value, but whether the field
> implies a **state, action, or piece of information the user should perceive**:
> a status the UI reflects, an id the user selects or routes by, a count, a
> limit, an error the user has to act on. **Plumbing is no**: values the UI never
> shows and the user never acts on, such as correlation ids, timestamps, internal
> metadata and persistence keys. If a user-facing field is not in the design,
> that is a **design iteration**, not a quiet addition: the design is revisited in
> Claude Design and re-signed-off before this contract freezes. **A contract never
> silently introduces a user-facing concept.**

# Rules

- **CR-1: Public.** The operation names no security scheme, `"security": []`, so
  any caller may call it.
- **CR-2: Answered at once.** A recorded payment answers 201 with status `Pending`,
  before Acme has been called. The app reads the payment by its id to follow it.
- **CR-3: One payment per app reference.** A request whose `appPaymentReference` is
  already held with the same customer, sub-merchant, amount and currency answers 201
  with the payment already held, the same body and `Location`. One that differs in
  any of them answers 400 `AppPaymentReferenceReused`. So the app may send the same
  request again safely after any failure.
- **CR-4: Currency.** `currencyCode` is matched without regard to case, and is
  answered in capitals.
- **CR-5: Refusals by the per-field map.** `CustomerReferenceRequired`,
  `CustomerReferenceTooLong`, `SubMerchantReferenceRequired`,
  `SubMerchantReferenceTooLong`, `AmountBelowMinimum`, `CurrencyCodeRequired`,
  `CurrencyCodeTooLong`, `AppPaymentReferenceRequired` and
  `AppPaymentReferenceTooLong`, each against the field it names.
- **CR-6: Refusals by code.** Checked in this order, the first answering:
  `AppPaymentReferenceReused`, `CustomerHasNoStoredCard`,
  `SubMerchantNotRegistered`, `CurrencyCodeNotSupported`, `AmountAboveMaximum`.
  Nothing is recorded for any refusal.
- **CR-7: No personal information in the answer.** The answer never carries the
  customer reference, an email address, a card token or the sub-merchant's
  details.
- **CR-8: A 500 is safe to retry.** A 500 means nothing was recorded, or the
  payment was recorded and CR-3 returns it.
