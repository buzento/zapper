- **Fragment:** `get.openapi.json`, beside this file. **The fragment is the
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

The app follows a payment it recorded with `POST /v1/payments` until the payment
reaches a final status. Recording answers before Acme Pay is called, so this read
is how the app learns Acme's outcome.

# Definition

The fragment beside this document covers `GET /v1/payments/{id}` and nothing
else. A payment that exists answers 200 with its current status. An id that names
no payment, or is not an id at all, answers 404.

```
GET /v1/payments/{id}

200
{
  "id": "0198f3c1-1a2b-7c3d-8e4f-5a6b7c8d9e0f",
  "appPaymentReference": "APP-1",
  "status": "Approved",
  "amount": 1999,
  "currencyCode": "ZAR",
  "responseCode": "00",
  "clientMessage": "Approved.",
  "reversedAmount": 0,
  "refundedAmount": 0
}
```

# Fields

| Field | Type | Required | User-facing? | Notes |
|---|---|---|---|---|
| `id` (route) | uuid | y | n | Our id, from the 201 that recorded the payment |
| `id` | uuid | y | n | Our id for the payment |
| `appPaymentReference` | string | y | n | The app's own reference, as sent |
| `status` | string | y | n | One of `Pending`, `Approved`, `Declined`, `Failed`, `Reversed`, `Refunded`, `PartiallyRefunded`, `Unresolved` |
| `amount` | 64-bit integer | y | n | In the currency's lowest unit |
| `currencyCode` | string, 3 | y | n | Always in capitals |
| `responseCode` | string or null | y | n | Acme's response code; null until Acme answers |
| `clientMessage` | string or null | y | n | Acme's wording that is safe to show the customer; null until Acme answers |
| `reversedAmount` | 64-bit integer | y | n | The total reversed, in the lowest unit |
| `refundedAmount` | 64-bit integer | y | n | The total refunded, in the lowest unit |

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
  any caller holding an id may read the payment.
- **CR-2: No personal information.** The answer never carries the customer
  reference, an email address, a card token, the sub-merchant's details or Acme's
  internal references.
- **CR-3: When to stop asking.** `Pending` means the outcome is not known yet, so
  ask again. Every other status is final and never changes back to `Pending`.
  `Approved` can later become `Reversed`, `Refunded` or `PartiallyRefunded`, which
  the app learns only by reading again. `Unresolved` means a person at Zapper must
  look at the payment.
- **CR-4: How often to ask.** Ask every 2 seconds for the first minute, then double
  the gap each time, to at most 60 seconds. Acme can take 30 seconds to answer, and
  a payment whose outcome Acme left unknown can stay `Pending` for some minutes
  while it is worked out; it becomes final within 24 hours at the latest, as
  `Unresolved` if nothing else.
- **CR-5: Not found.** An id that names no payment and a malformed id answer the
  same 404 `NotFound`.
