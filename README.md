# Zapper

Zapper's app takes a card payment from a customer with a stored card, at one of
Zapper's sub-merchants, through the Acme Pay gateway. This repository holds the
service that records those payments and submits them to Acme, `Zapper.PaymentsApi`,
with its database, a stand-in for Acme, and everything needed to run it on your
own machine.

`SOLUTION.md` explains the design. `CLAUDE.md` and `Claude/` hold the process and
the standards the code is built to.

## What you need

- Docker, with Docker Compose.
- `curl`, to call the API from a terminal.
- `jq`, for the end-to-end script.

## Run the stack

From the repository root:

```bash
docker compose up -d --build --wait
```

This starts, in order:

1. `certificates`, which makes a local certificate authority, a certificate for
   each database role, for the API and for the Acme stub, the card token key, the
   Acme API key, the Payment Advice username and password, the webhook secret, and
   the key of the stub's own routes. It writes the authority's certificate to
   `Code/Certificates/ca.crt`, and a copy of the stub routes' key to
   `Code/Certificates/AcmeStubRoutes.key`, both of which git ignores. It also
   writes the certificate and key of `ZapperDataReaderRole`, the role a person
   signs in as to read the database, to `Code/Certificates/`. Each part is given
   only the keys and secrets it reads.
2. `database`, PostgreSQL 18, which accepts only TLS connections signed in with a
   client certificate.
3. `roles`, which creates the database, one role for each part, and
   `ZapperDataReaderRole`, which reads every table and changes none.
4. `migrator`, which applies the migrations and then seeds the local data below.
5. `AcmeStub`, a stand-in for the Acme Pay gateway, which also sends the API the
   Payment Advices Acme Pay sends and the reversals and refunds Acme Switch sends,
   published at
   `https://localhost:8444` for the end-to-end script. `AcmeStub/README.md` says
   how it answers each card, and what it assumes where the brief leaves a choice.
   When port 8444 is taken, set `ACME_STUB_PORT`, as that README shows.
6. `PaymentsApi`, the API, at `https://localhost:8443`. A worker inside it sends
   each recorded payment to the stub.

Running the command again keeps what is there: nothing is generated twice and
nothing is seeded twice. `docker compose down --volumes` removes everything,
the database and every generated key included.

`certificates` and `roles` keep the scripts they were created with. Where
`compose.yml` has changed since your stack was first started, run them again with
the new scripts:

```bash
docker compose up -d --build --wait --force-recreate certificates roles
```

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

Customers, each holding one stored card. The Acme stub chooses how it answers by
the card:

| Customer reference | What the card does at Acme |
|---|---|
| `CUS-APPROVE` | Approved |
| `CUS-DECLINE-05` | Declined with `05` |
| `CUS-DECLINE-51` | Declined with `51` |
| `CUS-DECLINE-54` | Declined with `54` |
| `CUS-ADVICE-TWICE` | Approved; its Payment Advice is sent twice |
| `CUS-REVERSAL-FIRST` | Reversed before Acme answers, then approved |
| `CUS-LATE-ANSWER` | Answered too late, then approved |
| `CUS-NEVER-RECEIVED` | Never received the first time, then approved |
| `CUS-GATEWAY-ERROR-ONCE` | Answered 503 once, then approved |

Eighteen finished payments, `SEED-01` to `SEED-18`, on the two South African days
before the stack was first started, so the daily summary below shows data at once.
They are at `ZAP1000001` and `ZAP1000002`, in ZAR and USD, and in every final
status: approved, declined, failed, reversed, refunded in full and in part,
unresolved, and a refund Acme Switch did not apply. They are written only once, so
they stay on those two days. The Migrator logs the two days on every run:

```bash
docker compose logs migrator | grep 'seeded payments'
```

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
status is no longer `Pending`.

## Watch a payment reach its final status

The API never calls Acme while it answers the app. A worker inside the API looks
for payments that are due about four times a second, sends each one to Acme, and
records the answer. Record a payment with one of the customers above, then read
it every two seconds:

```bash
while true; do curl --silent --cacert Code/Certificates/ca.crt https://localhost:8443/v1/payments/<id>; echo; sleep 2; done
```

Stop it with Ctrl+C once the status is final. What each card shows:

| Customer reference | What you see |
|---|---|
| `CUS-APPROVE`, `CUS-ADVICE-TWICE` | `Approved`, with response code `00` and the authorised amount, within a second. The Payment Advice that follows confirms it; for `CUS-ADVICE-TWICE` the second copy is a duplicate and changes nothing |
| `CUS-REVERSAL-FIRST` | `Reversed`, with a `reversedAmount` of 1999, within a second: the reversal arrived while the payment was still with Acme, was held, and was applied when Acme's answer approved the payment |
| `CUS-DECLINE-05`, `CUS-DECLINE-51`, `CUS-DECLINE-54` | `Declined`, with that response code and Acme's client message, within a second |
| `CUS-GATEWAY-ERROR-ONCE` | `Pending` for about 5 seconds: Acme answered 503, so the worker asked Acme about it, learned Acme does not hold it, and sent it again. Then `Approved` |
| `CUS-NEVER-RECEIVED` | `Pending` for about 40 seconds: the send timed out after 35 seconds, the worker asked 5 seconds later, Acme did not hold it, and it was sent again. Then `Approved` |
| `CUS-LATE-ANSWER` | `Pending` for about 50 seconds: the send timed out, the first query found it still `Processing`, and the next, 10 seconds later, found it approved. Then `Approved` |

To watch the worker and the stub work, follow their logs in another terminal:

```bash
docker compose logs --follow PaymentsApi AcmeStub
```

Each line the worker writes names the payment by its id, and every line of one
piece of work carries the same trace id. A status means:

- `Approved`: the money was taken, for the authorised amount, which can be less
  than the amount asked for.
- `Declined`: the customer's bank refused it. The client message is safe to show
  the customer.
- `Failed`: no money moved, and the customer may pay again.
- `Reversed`: Acme Switch reversed the whole authorised amount, so no money moved
  in the end.
- `PartiallyRefunded`: some of the authorised amount was refunded, shown in
  `refundedAmount`.
- `Refunded`: the whole authorised amount was refunded.
- `Unresolved`: it is not known whether money moved, or Acme told us something that
  contradicts what we hold. A person settles it with Acme. The app never offers it
  to the customer as a failure to try again.

## Reverse or refund a payment

Acme Switch tells the API when a payment is reversed or refunded, possibly days
later. The stub stands in for it through two routes of its own, which take the key
in `Code/Certificates/AcmeStubRoutes.key`. Each sends the API a signed Transaction
Status, as Acme Switch would, and answers once the API has answered.

Record a payment with `CUS-APPROVE`, wait until it reads `Approved`, and keep its
id:

```bash
id=<the payment's id>
key=$(cat Code/Certificates/AcmeStubRoutes.key)
```

Refund 500 of it:

```bash
curl --cacert Code/Certificates/ca.crt \
  --header "Authorization: Bearer $key" \
  --header 'Content-Type: application/json' \
  --data "{\"TransactionID\":\"$id\",\"Amount\":500}" \
  https://localhost:8444/stub/refunds
```

Reading the payment now shows `PartiallyRefunded` with a `refundedAmount` of 500.
Refund the other 1499 the same way, and it shows `Refunded` with 1999.

Reverse another approved payment, which always reverses the whole amount:

```bash
curl --cacert Code/Certificates/ca.crt \
  --header "Authorization: Bearer $key" \
  --header 'Content-Type: application/json' \
  --data "{\"TransactionID\":\"$id\"}" \
  https://localhost:8444/stub/reversals
```

It shows `Reversed` with a `reversedAmount` of 1999. A reversal of a payment
already refunded, or a refund past the authorised amount, makes it `Unresolved`.
Use the port in `ACME_STUB_PORT` in place of 8444 where you set it.

Every notification is stored as it was received, with what was done with it, in
`Payment.Notification`, and each reversal and refund in `Payment.PaymentEvent`.
The API's log names each notification and its payment by id:

```bash
docker compose logs --follow PaymentsApi AcmeStub
```

## Run the daily summary

The daily summary is one SQL file, `Docs/Reports/DailyPaymentSummary.sql`. For one
South African day, from 00:00 to 24:00 at UTC+02:00, it shows one row for each
sub-merchant and currency with anything on that day: the count and value of
approved payments, declined payments, reversals and refunds, and beside them the
payments still pending, failed or unresolved. Values are in the currency's main
unit, with two decimal places.

Sign in as `ZapperDataReaderRole`, which reads every table and changes none. In
pgAdmin, register a server with:

| Tab | Setting | Value |
|---|---|---|
| Connection | Host name | `localhost` |
| Connection | Port | `5434` |
| Connection | Maintenance database | `ZapperDb` |
| Connection | Username | `ZapperDataReaderRole` |
| Connection | Password | none: the certificate proves the role |
| Parameters | SSL mode | `verify-full` |
| Parameters | Client certificate | `Code/Certificates/ZapperDataReaderRole.crt` |
| Parameters | Client certificate key | `Code/Certificates/ZapperDataReaderRole.key` |
| Parameters | Root certificate | `Code/Certificates/ca.crt` |

Give each file's full path on your machine. Then open the file in the Query Tool,
change only the date on the line `SELECT DATE '2026-01-01' AS "Day"` to one of the
two days the Migrator logged, as `YYYY-MM-DD`, and run it. Any other client that
takes a client certificate works the same way.

An approval or a decline counts on the day of Acme's time, at the authorised amount
or the amount asked for, whatever the payment's status is now. A reversal or a
refund counts on the day Acme Switch processed it, at its own amount, and only once
it is applied. A payment that is now pending, failed or unresolved counts on the
day it was recorded, at the amount asked for. An unresolved payment that Acme
approved counts in both: the money columns say what Acme did, and the unresolved
column warns that a person must settle it before it is paid out.

No seeded payment is pending. To see the pending columns, record a payment with
`CUS-LATE-ANSWER`, which the stub holds for about 45 seconds, and within that time
run the summary for today's South African date.

## Run the end-to-end script

`AcmeStub/end-to-end.sh` walks every case in the brief against the running stack:
through the API, its worker, the real Acme client, the stub and real TLS. It calls
API endpoints only, the API's and the stub's own routes, and never reads the
database. It needs Docker, `curl` and `jq`. From the repository root, start the
stack, then run the script:

```bash
docker compose up -d --build --wait
```

```bash
AcmeStub/end-to-end.sh
```

Where you set `ACME_STUB_PORT` for `docker compose up`, set it for the script too.

The script records one payment for each case, each with one of the seeded
customers above at `ZAP1000001`, for 1999 in ZAR, then reads every payment every
two seconds until it reaches its expected status. For the two cases that need Acme
Switch, it then reverses or refunds the payment through the stub's routes. A case
fails when its payment reaches another final status, or is still not at its
expected status two minutes after it was recorded. Once every case has passed, it
reads each payment once more, so a notification that arrived late cannot change a
status unseen. A run takes under two minutes; the slowest case, the late answer,
needs about 50 seconds. Each run makes its own app references from the time it
started, so it can be run again against the same stack at any time.

While it runs it prints the reference and id of each payment it records. At the end
it prints one line for each case:

```
PASS | Payment with the approving card is approved | expected Approved | read Approved
```

`PASS` or `FAIL`, the scenario title from the plan, the status the case expects,
and the status it read; the declining-cards case reads three payments, one for each
card. Below a failed case, one more line says why, such as a refused request, the
other status read, or a call that got no answer. The script exits with 0 when every
case passed, and 1 otherwise. To look into a failure, read the payment by its id,
as above, and the logs:

```bash
docker compose logs PaymentsApi AcmeStub
```

The pipeline runs the same script, against a stack it starts, on every pull request
and every push to `dev`, `test` and `main`, and prints `docker compose logs` when it
fails.

## Run the tests

From `Code/`, with Docker running:

```bash
dotnet test
```

The integration and acceptance tests start their own PostgreSQL container, so they
do not need the stack above.
