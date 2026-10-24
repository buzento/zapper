# Zapper

Zapper's app takes a card payment from a customer with a stored card, at one of
Zapper's sub-merchants, through the Acme Pay gateway. This repository holds the
service that records those payments, `Zapper.PaymentsApi`, with its database and
everything needed to run it on your own machine.

`SOLUTION.md` explains the design. `CLAUDE.md` and `Claude/` hold the process and
the standards the code is built to.

## What you need

- Docker, with Docker Compose.
- `curl`, to call the API from a terminal.

## Run the stack

From the repository root:

```bash
docker compose up -d --build --wait
```

This starts, in order:

1. `certificates`, which makes a local certificate authority, a certificate for
   each database role and for the API, and the card token key. It writes the
   authority's certificate to `Code/Certificates/ca.crt`, which git ignores.
2. `database`, PostgreSQL 18, which accepts only TLS connections signed in with a
   client certificate.
3. `roles`, which creates the database and one role for each part.
4. `migrator`, which applies the migrations and then seeds the local data below.
5. `PaymentsApi`, the API, at `https://localhost:8443`.

Running the command again keeps what is there: nothing is generated twice and
nothing is seeded twice. `docker compose down --volumes` removes everything,
the database and every generated key included.

Every call below trusts the local authority with `--cacert Code/Certificates/ca.crt`.
Check that the API answers:

```bash
curl --cacert Code/Certificates/ca.crt https://localhost:8443/v1/health
```

The API's documentation page is at `https://localhost:8443/scalar/v1`.

## The seeded data

The Migrator's Debug build, which `compose.yml` builds, seeds these synthetic
records. An administrator adds real ones directly; no operation creates them.

Sub-merchants:

| Reference | Trading name | City |
|---|---|---|
| `ZAP1000001` | Harbour Lane Bakery | Cape Town |
| `ZAP1000002` | Karoo Mountain Coffee Roastery | Durban |
| `ZAP1000003` | Fynbos Books | Port Elizabeth North |

Customers, each holding one stored card. The Acme Pay stand-in, added by a later
Feature, chooses how it answers by the card:

| Customer reference | What the card does at Acme |
|---|---|
| `CUS-APPROVE` | Approved |
| `CUS-DECLINE-05` | Declined with `05` |
| `CUS-DECLINE-51` | Declined with `51` |
| `CUS-DECLINE-54` | Declined with `54` |
| `CUS-ADVICE-TWICE` | Approved, and its Payment Advice is sent twice |
| `CUS-REVERSAL-FIRST` | Approved, and its reversal arrives before its Payment Advice |
| `CUS-LATE-ANSWER` | Answered too late, then approved |
| `CUS-NEVER-RECEIVED` | Never received the first time, then approved |
| `CUS-GATEWAY-ERROR-ONCE` | Answered 503 once, then approved |

## Record a payment

The app sends the customer's reference, the sub-merchant's reference, the amount
in the currency's lowest unit, and its own reference for the payment. The
currency may be left out, and ZAR is used. ZAR, USD, EUR and GBP are accepted.

```bash
curl --cacert Code/Certificates/ca.crt \
  --header 'Content-Type: application/json' \
  --data '{"customerReference":"CUS-APPROVE","subMerchantReference":"ZAP1000001","amount":1999,"currencyCode":"ZAR","appPaymentReference":"README-1"}' \
  https://localhost:8443/v1/payments
```

The answer is `201`, at once, with our id for the payment and its status,
`Pending`. Sending the same request again returns the same payment, so the app can
always send it again after a failure. The same app reference with different
details is refused with `400` and the code `AppPaymentReferenceReused`.

## Read a payment

```bash
curl --cacert Code/Certificates/ca.crt https://localhost:8443/v1/payments/<id>
```

`<id>` is the `id` from the answer above. The app reads the payment until its
status is no longer `Pending`. Until the Feature that submits payments to Acme is
built, every payment stays `Pending`.

## Run the tests

From `Code/`, with Docker running:

```bash
dotnet test
```

The integration and acceptance tests start their own PostgreSQL container, so they
do not need the stack above.
