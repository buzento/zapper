# Solution

## Decisions that differ from the brief

### .NET 10 instead of .NET 8

The brief asks for C# and .NET 8. This solution is built on .NET 10 with C# 14,
and every project targets `net10.0`. The reasons:

- **.NET 8 leaves support on 10 November 2026**, a few weeks after this
  assignment. A payments service started today on .NET 8 would need a framework
  upgrade before its first production release, and would stop receiving
  security fixes if that upgrade slipped. .NET 10 is the current long-term
  support release and is supported until November 2028.
- **The engineering standards this solution is built to fix .NET 10.** They are
  in `Claude/Standards/`, and the AI tools read them on every task. They fix the
  framework, the language version, the SDK version in `Code/global.json`, the
  XML solution format (`.slnx`), and every package version against .NET 10.
  Moving to .NET 8 would mean rewriting those standards for an older framework,
  and the point of the assignment is to show how the team would work, not a
  one-off exception to it.
- **Nothing in the brief depends on .NET 8.** The service, its container image
  and the AWS approach below are the same on .NET 10. The container image is
  built from Microsoft's .NET 10 images, which run anywhere a .NET 8 image does.

If Zapper's platform requires .NET 8, the change starts with the target
framework, the SDK version and the package versions in
`Claude/Standards/Versions.md`.

## Assumptions

To be written.

## Design and key trade-offs

To be written.

## Data model

To be written.

## AWS deployment approach

To be written.

## AI workflow and tools

To be written.

## How AI-generated work was validated

To be written.

## Facts recorded by each Feature

Each Feature records here the facts that the sections above are written from.
End to End validation checks writes "Assumptions", "Design and key trade-offs",
"Data model" and "AWS deployment approach" in full from them, by the plan's D-17.

### Recording payments

**The app is answered at once, and polls.** Recording a payment answers `201` as
soon as the payment is stored, with our id and the status `Pending`, before Acme
has been called. The app then reads the payment by our id until its status is
final. A request repeated with the same app payment reference returns the payment
already held, so an app that lost the `201` still gets our id. Acme can take 30
seconds and may time out, so holding the app's request open would tie a
connection to Acme's slowest answer. Considered and rejected: holding the request
open, and SignalR in this assignment. In production the status is pushed through
SignalR with a backplane on the platforms that support it, polling stays as the
fallback, and the push channel is signed in once the app signs in.

**Both operations are public.** The brief allows the app's endpoints to be
public, so `POST /v1/payments` and `GET /v1/payments/{id}` need no sign-in.
Because anyone holding an id can read a payment, the answer holds only our id,
the app's reference, the status, the amount and currency, the authorised amount,
Acme's response code and client message, and the reversed and refunded totals:
never the customer reference, the email address, the card token or the
sub-merchant's details. The rate limit per network address is the only guard on
recording, so anyone can record a payment for a customer reference they know. In
production the app signs in with a token issued to the Zapper user, each payment
belongs to that user's customer, reading another customer's payment answers
`404`, and recording takes the customer from the token.

**The data model so far.** Three schemas: `Merchant`, `Customer` and `Payment`.

| Table | What it holds |
|---|---|
| `Merchant.SubMerchant` | A sub-merchant: Zapper's reference for it, its trading name, city, region, country and merchant category code |
| `Customer.StoredCard` | A customer's stored card: the customer reference, the card token stored encrypted, and the email address |
| `Payment.CurrencyType` | Zapper's list of currencies, a reference table: each currency's ISO code, its symbol and its maximum amount in the lowest unit |
| `Payment.PaymentStateType` | The payment's internal states, a reference table |
| `Payment.Payment` | One payment: the app's reference, the stored card, the sub-merchant, the currency, the amount, the `OrderNumber` sent to Acme, the statement name and category code as they were when it was recorded, its state, when it was recorded and when it is next due for work, and Acme's outcome and totals once known |

Every id is a version 7 UUID made by the code, and every amount is a whole number
of the currency's lowest unit. The payment's own id is its `TransactionID` at Acme,
and its `OrderNumber` is 12 random capital letters and digits; both are made once,
when the payment is recorded, and never change. The statement name follows FNB's
40-character layout for a payment facilitator, `ZAP*` and the trading name, then
the city, the region and the country. The app's reference, the `OrderNumber`, a
sub-merchant's reference and a stored card's customer reference are each unique.
Later Features add the tables and columns they first write.

**The currency list.** The brief groups the daily summary by currency, and Acme
makes the currency optional with ZAR as its default, so Zapper keeps its own list:

| Code | Symbol | Maximum, in the lowest unit |
|---|---|---|
| ZAR | R | 100 000 000 (R1 000 000) |
| USD | $ | 5 000 000 ($50 000) |
| EUR | € | 5 000 000 (€50 000) |
| GBP | £ | 4 000 000 (£40 000) |

A payment with no currency is in ZAR. A currency is matched without regard to
case and always stored, sent and answered in capitals. Nothing is converted: each
currency is recorded and settled on its own. Assumptions: Acme accepts every code
on the list, which production would confirm with Acme; a South African merchant
normally charges in rand and the cardholder's bank converts, so a foreign currency
here is a merchant pricing in it; a currency cannot be withdrawn, because its row
cannot be deleted while payments use it, and stopping new payments in it would
need a flag on the row, added when asked for.

**The card token is encrypted.** The token is stored only encrypted, with
AES-256-GCM from the .NET base class library, through `CryptographyManager`. The
stored value starts with a one-byte version, today always 1, so a new algorithm or
a new key becomes a new version with every stored value still readable. The stored
card's customer reference is bound to the token as associated data, so a token
moved to another card cannot be decrypted. The key is 32 random bytes. Locally the
`compose.yml` init service generates it with `openssl rand` into a volume mounted
read-only into the hosts that use it, and tests make a fresh key for each run. In
production it is a Secrets Manager secret, encrypted by KMS, given by ECS as an
environment variable to the task roles that use it, with every read and change
recorded by CloudTrail. Rotation is not built: there is one key. It must be built
before the first production key is 12 months old, because `Security.md` section 3
replaces every vault secret within 12 months. Envelope encryption through KMS is
the fuller answer. The email address is protected by RDS encryption at rest and by
TLS, not encrypted in its column.

**One stored card per customer.** An assumption, enforced by a unique index on the
customer reference. In production the request would carry the stored card's id,
checked as belonging to the customer.

**What is not built for retention and correction.** Payments are kept for 5 years
after the payment, under the FIC Act. A stored card is kept while the customer
keeps it, and de-identified within one day of its removal. The job that enforces
those periods is not built. No operation corrects, restricts, produces or
de-identifies personal information: an administrator changes the database
directly, and in production each of those is an operation of its own.

### Submitting Payments to Acme

**A worker sends each payment, never the app's request.** A worker inside
`Zapper.PaymentsApi` looks for due payments about every 250 milliseconds. It
claims up to 10 at a time with one SQL statement, `FOR UPDATE SKIP LOCKED`, which
moves each claimed payment's due time 60 seconds on, so no two workers work on one
payment and a crashed worker's payments come back when that lease lapses. Each
payment is worked in its own scope and its own trace id, and saved once after each
call to Acme. A save that finds the payment changed by another writer reloads it,
asks the Domain again, and saves again, up to three times. Normally an event bus or
a message queue would carry work to the worker, for resilience and replay; the
assignment does not call for one, so polling the database suits this structure,
with PostgreSQL's `LISTEN` and `NOTIFY` a later optimisation. Considered and
rejected: a worker project of its own, and a project per worker.

**The limits on calls to Acme.** Each instance works on at most 50 payments at once
and claims only into free places. Its HTTP client holds the same number of
connections, retires an idle one after 50 seconds, below Acme's 60, and follows no
redirect. A token bucket keeps each instance at its share of Acme's 20 requests a
second, a share worked out from the number of instances at deployment, and a call
waits for a token rather than being refused. Each call is limited to 35 seconds,
above the 30 Appendix A gives an unresponsive issuer. A circuit breaker opens for
30 seconds when half of at least 10 calls in 30 seconds end in 500 to 504 or a
timeout; while it is open no call is made and no call is recorded, and due
payments wait. The client retries nothing itself. A limit reached refuses nothing:
payments stay due, and whichever instance has room claims them. A limiter shared
through Redis is the exact alternative to the per-instance share.

**Reading Acme's answers, and asking first.** An answer is final only with a
response code and a client message that is not empty, and on an approval also the
authorised amount. `Success` with code `00` is approved, `Success` with any other
code declined, and `Failure` failed. Decisions are made on the response code, never
on the client message. A timeout, a broken connection, a 500 to 504, an answer that
cannot be read, `Processing`, or a 409 naming this payment's own `TransactionID`
means the outcome is unknown: Acme is asked by the transaction query 5 seconds
later, and the gap doubles while the answer stays unknown, up to 5 minutes. A
payment is sent again only after the query answers 404, always with the same
`TransactionID` and `OrderNumber`, because a new one could charge the customer
twice. After 3 sends that each ended in a 404 the payment is `Failed` as not
submitted. A 429 is sent again after `Retry-After`. A 400 `E01`, a 401 `E02` or a
409 naming another `TransactionID` fails the payment, logged at `Error`; the same
400 or 401 on a query makes it `Unresolved`, because the send before it may have
reached the bank.

**The 24-hour limit.** A payment with no final answer 24 hours after it was
recorded is `Unresolved`, logged at `Error`, for a person to settle with Acme.

**Partial authorisation.** The authorised amount is what was taken from the card,
so it is what is recorded. An approval for less than was sent is approved and
logged at `Information`. One for more is approved and logged at `Error`, because the
customer is owed a refund. A decline that carries an authorised amount above 0 is
recorded as sent, stays `Declined`, and is logged at `Error`.

**Failures nobody planned for.** A piece of work that fails in a way the code does
not expect is tried again after 1, 5, then 15 minutes. After five such failures in
a row Acme is asked once: a 404 makes the payment `Failed`, a final answer is
applied, and anything else, or a sixth failure, makes it `Unresolved`. Each is
logged at `Error`.

**What each final status means.** `Approved`: the money was taken, for the
authorised amount. `Declined`: the customer's bank refused it, and the client
message is safe to show. `Failed`: no money moved, and the customer may pay again.
`Unresolved`: it is not known whether money moved, and a person settles it with
Acme; the app never shows it as a failure to try again, because trying again could
charge the customer twice. `Recorded` and `OutcomeUnknown` are internal states, and
the app sees both as `Pending`.

**Every call and every change is recorded.** `Payment.AcmeInteraction` holds each
call to Acme: whether it was a send or a query, how it ended (answered, timed out
or connection failed), the message id, the attempt, the request as built with the
card token and the email address replaced by `[removed]`, the stored card by its
id, the HTTP status, the answer exactly as received, when it started, how long it
took, and the trace id. `Payment.PaymentChange` holds each change of a payment's
state, and each final answer that confirms it, with the state before and after,
its source (a send, a query or the worker), the call it came from, and when. An
answer that leaves the payment as it was, such as `Processing`, leaves a call
record and no change. The operations, outcomes and sources are reference tables.
Both records are kept for 5 years with the payment, and written by the API's
role, which may insert them and never update or delete them. The payment row carries PostgreSQL's `xmin` as its concurrency token.
No log line holds a token, an email address or a card number; the worker's lines
name a payment by its id only.

**Graceful shutdown.** On the stop signal the worker stops claiming at once and
finishes the work it has claimed, each call bounded by its 35 seconds. The host's
shutdown timeout is 45 seconds, `compose.yml` gives the API 60 seconds to stop,
and ECS would be given the same. Correctness never depends on a graceful stop: a
killed host's payments come back when their lease lapses, and a crash between
Acme's answer and the save loses only that one call record, which the next query
recovers.

**Operating it.** Every 30 seconds the worker logs how many payments are due and
not claimed, and the age of the oldest. On AWS a CloudWatch metric filter turns
that line into metrics; the background service scales on the count and alerts on
the age. OpenTelemetry metrics are the fuller answer. In production one image runs
as two ECS services: one that answers the app, with every worker off, scaled on
requests; and one that does background work, with every worker on. The service
with the worker off never holds Acme's API key or the card key: the startup check
reads both only when the worker is on, and the worker takes the Acme client from
the scope of each piece of work, so a host with it off never builds the client. A
load balancer's deregistration delay drains the app-facing service.

**Local TLS.** The `compose.yml` certificate script issues the stub's server
certificate from the local authority. The API trusts that authority through
`SSL_CERT_FILE`, set to the authority's certificate, and `SSL_CERT_DIR`, the image's
public roots, so no code touches the certificate check. Run with `SSL_CERT_FILE`
pointed at the public roots alone, the API's call to the stub fails with
`PartialChain`; with the authority, the same payment is approved.

**The stub's assumptions.** `AcmeStub/README.md` lists them in full. In short: its
state is in memory and lost on restart; it replays a stored outcome for a repeated
`TransactionID` for 24 hours and answers `Processing` while a request is in
progress; it holds each 12-character Retrieval Reference Number once; it allows 20
requests in each calendar second and answers the next with 429 and `Retry-After:
1`; a late answer is held 40 seconds and approved 45 seconds after it arrived, so
the payments API times out and resolves the payment a few seconds later by query;
every error carries the client message "We could not process that payment.";
every answer carries one member Appendix A does not document; partial
authorisation is not simulated.

**Assumptions about Acme.** Appendix A does not say what Acme does when a
`TransactionID` is sent again while the first request is still in progress, so
the worker always asks first and sends again only on a 404. `Complete`,
`Reversed` and `Refunded` on a send or query answer are read like `Success`, by
the response code, because Appendix A puts the outcome in the response code; the
status as sent is kept in the call record and logged at `Warning`, and a reversal
or a refund itself is applied only from Acme Switch's notification, which alone
carries the event's amount and identity. Acme always sends a client message with a
response code (ruled by Francois); an answer without one is asked about by query.

**Facts for the AI workflow section.** The Feature was planned by `create-plan`
and built by `feature-execution` in six increments, each committed as
`Submitting_Payments_To_Acme_<n>/6` and checked before the next began: failing
acceptance tests first, then the schema and the Domain, the Acme client, the
worker, the stub with `compose.yml`, and this documentation. The plan gave the
twenty-five scenarios and every name; the standards gave how each part is written.
Where the code met something the plan or a standard did not settle, the run
stopped and put it to Francois, and his ruling was written into the file it
changed before the work carried on. Four rulings came from this Feature's run:
three acceptance tests gained their database reads in increment 2, once the tables
existed; a worker takes the Acme client from the scope of each piece of work, so a
service with the worker off never needs Acme's key; `Workers.cs` became
`WorkerRegistration.cs`, because C# refuses a class with the name of the `Workers/`
folder's namespace; and the database data source is built once per host, which
fixed a connection leak in the code the `create-solution` skill had prescribed.

**Facts for the validation section.** Each of the twenty-five scenarios is an
acceptance test that runs the whole API on a clock the test moves, with the Acme
client substituted, and reads the outcome through the API and the call and change
records through the database. The Acme client is tested through its production
registration against Appendix A's own examples, including a call that never
answers and the circuit opening. The worker is tested one polling pass at a time
against a real PostgreSQL, including two claims at the same moment, a lapsed
lease, a conflicting save, the free places, and five and six unexpected failures.
The whole suite was run three times in a row to show no test depends on timing.
Two problems were found only by running things for real, and both were fixed in
the run: the acceptance fixture missed a claim whose work finished within its
10-millisecond check, and the stack ran out of database connections within half a
minute of the worker starting, because each context built its own connection pool.
After the fix, `docker compose up` took every seeded card to its expected status
through the API, the worker, the real client, real TLS and the stub. The package
`Microsoft.Extensions.Http.Resilience` is at 10.10.0, not 10.11.0, because 10.11.0
needs Microsoft.Extensions packages newer than the solution's pins.
