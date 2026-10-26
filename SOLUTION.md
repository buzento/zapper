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

Where the brief and its Appendix A leave a choice open, the solution assumes:

**About Acme**

- Appendix A does not say what Acme does when a `TransactionID` is sent again while
  the first request is still in progress. So the worker always asks Acme by the
  transaction query first, and sends again only when the query answers 404, with
  the same `TransactionID` and `OrderNumber`.
- `Complete`, `Reversed` and `Refunded` on a send or query answer are read like
  `Success`, by the response code, because Appendix A puts the outcome in the
  response code. A reversal or a refund is applied only from Acme Switch's
  notification, which alone carries the event's amount and identity.
- Acme always sends a client message with a response code, and always sends
  `TransactionDateTime` on a final answer. An answer without a client message is
  asked about by query.
- `Authorize` and `Capture` Payment Advices are recorded and do not set the outcome.
- Acme's times carry no offset, and are South African time.
- Acme accepts every currency on Zapper's list: ZAR, USD, EUR and GBP. Production
  would confirm this with Acme.

**About Zapper's data**

- Sub-merchants and stored cards exist already. They are seeded locally, and an
  administrator changes them directly; no operation adds, changes or removes one.
- A customer holds one stored card, enforced by a unique index. In production the
  request would carry the stored card's id, checked as belonging to the customer.
- A payment in a foreign currency is a merchant pricing in it. Nothing is
  converted: each currency is recorded and settled on its own. A currency cannot be
  withdrawn while payments use it.
- A day in the daily summary is a South African day, from 00:00 to 24:00 at
  UTC+02:00.

**About the app**

- Both of the app's operations are public, as the brief allows, so the rate limit
  for each network address is the only guard on recording a payment.

**About the stub**

- The stub keeps its state in memory, and loses it on a restart. Its delays are its
  own: 40 seconds to make our API time out, 45 seconds before a late answer is
  approved, so a timed-out payment resolves a few seconds later by query. Reversals
  and refunds always settle approved, and partial authorisation is not simulated.
  `AcmeStub/README.md` lists every choice it makes.

## Design and key trade-offs

**The app is answered at once, and polls.** Recording a payment answers `201` as
soon as the payment is stored, before Acme is called, and the app reads the payment
until its status is final. Acme can take 30 seconds and may time out, so holding the
app's request open would tie a connection to Acme's slowest answer and give the app a
timeout with no answer. The cost is that the app polls. In production the status is
pushed through SignalR with a backplane, with polling kept as the fallback.

**A worker inside the API calls Acme, never the app's request.** It finds due
payments by polling the database about every 250 milliseconds and claims them with
`FOR UPDATE SKIP LOCKED` and a 60-second lease, so no two workers send one payment
and a crashed worker's payments come back. Normally an event bus or a message queue
would carry this work, for resilience and replay; the assignment does not call for
one, so polling the database suits this structure. A worker project of its own was
rejected, because one image can run as two services instead.

**A payment's identity at Acme never changes, and Acme is asked before anything is
sent again.** The payment's id is its `TransactionID` and its `OrderNumber` is made
once. After a timeout, a 500 to 504 or a broken connection the outcome is unknown,
so Acme is asked first, 5 seconds later and then at doubling gaps up to 5 minutes. A
new `TransactionID` per attempt was rejected, because it could charge the customer
twice. The cost is a slower recovery: a payment Acme never received waits for a
query before it is sent again.

**Limits that keep Acme's rate limit, per instance.** Each instance works on at most
50 payments at once, keeps to its share of Acme's 20 requests a second, limits each
call to 35 seconds, and opens a circuit breaker when most recent calls fail. A limit
reached refuses nothing: payments stay due. A limiter shared through Redis is the
exact alternative to a share worked out at deployment.

**Each operation saves once, and a unique index decides a duplicate.** A save that
could break a unique index reports `Saved`, `AlreadyExists` or `Conflict`. So a
payment recorded twice returns the one already held, and a notification delivered
twice answers 200 and changes nothing. The payment row carries PostgreSQL's `xmin` as
its concurrency token, so a worker and a notification that change it at the same
moment cannot both win. `INSERT ... ON CONFLICT` and `SELECT ... FOR UPDATE` were
rejected: the first needs statements outside the entities, and the second holds a
lock for as long as the work takes.

**A payment's state is worked out from every fact, whatever their order.** Every
notification is stored as received before anything is decided. The outcome is set
once, by the worker's first final answer from Acme. A Payment Advice confirms or
contradicts it, but never sets it, because it carries neither the client message nor
the authorised amount. A reversal or a refund that arrives before the outcome is held
and applied once the payment is approved. Anything that contradicts what is held
makes the payment `Unresolved`, for a person to settle with Acme. The trade-off is
deliberate: the solution never guesses about money, so a contradiction costs a
person's time rather than a wrong status.

**The read answer carries no personal information.** Because both app operations are
public, anyone holding an id can read a payment, so the answer holds only the status,
amounts, Acme's code and client message, and the references. In production the app
signs in, and a payment is read only by its own customer.

**The card token is encrypted by one manager that the API and the Migrator share**,
with AES-256-GCM from .NET, the stored card's customer reference bound as associated
data, and a version byte so a new key or algorithm is a new version. A value
converter was rejected, because it cannot bind a value to its row. Key rotation is
not built.

**Acme proves itself the way Appendix A fixes.** A Payment Advice carries HTTP Basic,
and a Transaction Status an HMAC-SHA256 signature of its exact body; both are checked
in constant time by generic handlers. There is no replay protection, which is
harmless because a repeat is a duplicate.

**The daily summary is a SQL file, not an endpoint.** An endpoint would be public, and
settlement is back office work, so a person runs `Docs/Reports/DailyPaymentSummary.sql`
in a database client, signed in as a data reader that reads every table and changes
none.

**Tests never call Acme or its stub, and a script proves the whole path.** The Acme
client is tested against Appendix A's own examples, and the acceptance tests stand in
for Acme. Testing the client against the stub was rejected, because it proves only
that two readings of Appendix A agree. The end-to-end script runs every case in the
brief through real TLS, the worker, the real client and the stub, and CI runs it on
every pull request.

## Data model

PostgreSQL 18, with one schema for each area. Every id is a version 7 UUID made by
the code, every amount a whole number of the currency's lowest unit, and every time
is stored in UTC.

| Table | What it holds |
|---|---|
| `Merchant.SubMerchant` | A sub-merchant: Zapper's reference for it, its trading name, city, region, country and merchant category code |
| `Customer.StoredCard` | A customer's stored card: the customer reference, the card token stored encrypted, and the email address |
| `Payment.Payment` | One payment: the app's reference, the stored card, the sub-merchant, the currency, the amount, its identity at Acme, the statement name and category code it was sent with, its state, when it was recorded and is next due for work, its attempt and failure counts, and Acme's outcome, authorised amount and time, and its reversed and refunded totals |
| `Payment.AcmeInteraction` | Every call to Acme: a send or a query, how it ended, the request with the card token and email address removed, the HTTP status, the answer exactly as received, when it started, how long it took, and its trace id |
| `Payment.PaymentChange` | Every change of a payment's state, and each answer that confirms it: the state before and after, its source, the call or notification it came from, and when |
| `Payment.Notification` | Every notification as received, a Payment Advice without its masked card number: when it arrived, its trace id, the payment it matched or none, and what was done with it |
| `Payment.PaymentEvent` | Each reversal and refund: its amount and currency, when Acme Switch processed it, and whether it is held, applied, not applied or a contradiction |
| `Error.ErrorStatus` and `Error.ErrorMessage` | The text of every failure the API answers, by status and by code |

The payment states, the currencies, Acme's operations and call outcomes, the change
sources, the notification sources and outcomes, and the event types and states are
reference tables named for their enum plus `Type`, so each row's id is its enum
member.

**What keeps it correct.** Unique indexes guard the app's reference, the
`OrderNumber`, a sub-merchant's reference, a stored card's customer reference, and a
notification's identity with a hash of its content. The payment row's `xmin` is its
concurrency token. Indexes on the payment's due time, Acme's time and the time it was
recorded, and on an event's processing time, keep the worker's claim and a day's
summary cheap over years of payments.

**Who may change what.** Each part connects as its own database role with a client
certificate and no password. The API's role reads stored cards and sub-merchants and
never changes them; it may insert call records, changes and notifications and never
change them; it may update an event, because a held event becomes applied. The
Migrator's role owns the schema. `ZapperDataReaderRole` reads every table and changes
none.

**How long it is kept.** Payments, their changes, call records, notifications and
events are kept for 5 years after the payment, under the FIC Act. A stored card is
kept while the customer keeps it, and de-identified within one day of its removal.
The job that enforces those periods is not built.

**How the schema would evolve.**

- Once the app signs in, each payment belongs to the signed-in user's customer, and
  is read only by that customer.
- More than one card per customer: the unique index on the customer reference goes,
  and the request carries the stored card's id.
- A currency stopped for new payments: a flag on its row in `Payment.CurrencyType`.
  A currency with other than two decimal places: its decimal places on the same row.
- Once volume or audit needs it, a settled summary stored for each closed day, so a
  day already paid out is read as it was settled.
- The retention job, which de-identifies a stored card and the records past their
  period.

## AWS deployment approach

This is the approach, not something built: nothing is deployed to AWS in this
assignment.

**How it runs.** One container image of `Zapper.PaymentsApi` runs as two ECS services.
One answers the app and Acme's notifications, behind a load balancer, with the worker
off, and scales on requests. The other does background
work, with the worker on, and takes no traffic. PostgreSQL runs on RDS, encrypted at
rest, where the internet cannot reach it, with a read replica for the daily
summary and for investigation. The Migrator's Release image, which holds no seed
code, runs as a one-off ECS task before each deployment, and the deployment stops
unless it exits with 0; an API refuses to start against a database that is behind
it.

**Configuration and secrets.** Settings that differ by environment are environment
variables of the task. Secrets live in Secrets Manager, encrypted by KMS, given by
ECS as environment variables to only the task roles that read them, with every read
and change recorded by CloudTrail. The service that answers requests holds the Payment
Advice credentials and the webhook secret, and never Acme's API key or the card key;
the background service holds those two, and never the notification secrets. Each
service connects to RDS as its own role with a client certificate, over TLS that
checks the server. Every vault secret is replaced within 12 months, so card key
rotation must be built before the first production key is that old; envelope
encryption through KMS is the fuller answer. Where Acme publishes its addresses, a
firewall rule lets through only those to the notification operations.

**Scaling.** The service that answers requests scales on requests. The background
service scales on the number of payments due and not claimed. Each instance's share of
Acme's 20 requests a second is a setting worked out from the number of instances at
deployment, and a 429 with `Retry-After` is the backstop; a limiter shared through
Redis is the exact alternative.

**Graceful shutdown.** On the stop signal the worker stops claiming and finishes what
it has claimed, each call bounded by its 35 seconds. The host's shutdown timeout is 45
seconds, and ECS is given a stop timeout of 60 seconds. The load balancer's
deregistration delay drains the service that answers requests. Correctness never
depends on a graceful stop: a killed task's payments come back when their lease
lapses.

**The signals, and what each one tells.** Every log line goes to CloudWatch Logs in a
format read field by field, and every line of one request or one piece of work carries
its trace id, so one payment's story is found by its id or its trace id. CloudWatch
metric filters and alarms are built on:

- **The health check**, `GET /v1/health`, which the load balancer and ECS use to
  replace a task that stops answering.
- **Each Acme call's duration and outcome**, held in `Payment.AcmeInteraction` and in
  the worker's lines: a rise in timeouts or broken connections means Acme is slow or
  down.
- **429s**, from Acme and from our own rate limit: a rise means a share is set wrong,
  or a caller sends too much.
- **The circuit's state**: while it is open no call reaches Acme and payments wait.
- **The payments due and not claimed, and the age of the oldest**, from the worker's
  line every 30 seconds: the count scales the background service, and the age raises
  an alarm when work falls behind.
- **The `Unresolved` count**: each one needs a person to settle it with Acme.
- **Unmatched and contradicting notifications**, logged at `Error`: Acme told us
  something we cannot place or that disagrees with what we hold.
- **401s on the notification operations**: a wrong credential, or someone other than
  Acme trying.

OpenTelemetry metrics and traces are the fuller answer to metric filters on log lines.

## AI workflow and tools

The AI tools work from what the repository holds:

- **`CLAUDE.md`**, at the root: the rules every piece of work follows, where each
  decision is kept, the words the process uses, such as Feature, plan, decision and
  contract, and where each kind of file lives.
- **`Claude/Skills/`**: one folder for each skill, the steps for one kind of work.
  `create-solution` creates the solution once, `create-project` adds an API,
  `create-plan` plans a piece of work with Francois, and `feature-execution` builds one
  Feature of an approved plan, increment by increment. No other skill builds or changes
  the repository.
- **`Claude/Standards/`**: how the work is built. `BackendCode.md` for the C# code,
  `Testing.md` for the tests, `Security.md` for what must be true of security,
  `Branching.md` for branches, commits and pull requests, and `Versions.md` for the one
  approved version of each program and package. `FrontendCode.md` is kept for review;
  Zapper has no frontend.
- **`Claude/Templates/`**: the shape of a plan and of a contract's document, which
  `create-plan` fills in.

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

### Acme Notifications

**Two notifications, each proven the way Appendix A fixes.** Acme Pay sends a
Payment Advice to `POST /v1/payment-advices` with HTTP Basic: the username and
password are checked together, by comparing the SHA-256 of each in constant time,
and a failure never says which part was wrong. Acme Switch sends a reversal or a
refund to `POST /v1/transaction-statuses`, with `x-signature`, the Base64
HMAC-SHA256 of the exact body keyed with the webhook secret: the body is read within
the request size limit, the signature is compared in constant time, and the body is
rewound for binding. Anything else answers 401 and stores nothing. The handlers are
generic, in ApiCommon, and given Acme's values by the payments API. There is no
replay protection, which is harmless because a repeat is a duplicate.

**Applied inside the request, and the one that can be lost.** A notification is
checked, stored and applied in one save, and answered 200 with no body, whatever it
said: new, a duplicate, unmatched or a contradiction. Its body is stored as
received, the Payment Advice without `MaskedCardNumber`, with the time it arrived,
its trace id, the payment it matched or none, and what was done with it. A save
that meets a change by the worker answers 500 `ConcurrentChange`, and Acme's resend
applies it. A lost Payment Advice costs nothing, because the worker's own answer
sets the outcome. A notification whose every delivery fails through a fault of ours
is lost; a person finds it on Acme's settlement file.

**Comparing mapped fields, and a removed field.** A Transaction Status is identified
by its `transactionUUID`, and a Payment Advice by its `TransactionID` with its
`RequestType`. Only the fields this Feature maps are compared, through a SHA-256
content hash held in a unique index: the same identity and the same fields is a
duplicate, caught by the unique index and changing nothing, and the same identity
with other fields keeps the first, stores the second as a contradiction, and makes
the payment `Unresolved`. So a field Acme adds, or one that changes on each
delivery, never makes a delivery different, and is kept in the stored body. A field
Appendix A requires that is missing, or a value too long, answers 400 and stores
nothing: a removed field is a change by Acme, settled with Acme. Acme's lists are
read as text, so a value Acme adds is recorded rather than refused. Acme's times
carry no offset, so they are checked as text, read as South African time and stored
in UTC.

**A Payment Advice never sets the outcome**, because it carries neither the client
message nor the authorised amount. The worker's answer from Acme sets it. An advice
that says the same outcome is a confirmation, recorded as a change from the state to
itself; one that says otherwise, or whose amount, currency, order number or
Retrieval Reference Number differs from the payment's, makes the payment
`Unresolved`. `Authorize` and `Capture` advices, an advice of status `Other`, and a
value Appendix A does not name are recorded and change nothing; that `Authorize`
and `Capture` do not set the outcome is an assumption.

**The authorised amount is authoritative.** It is what was taken from the card, so
it is what reversals and refunds are measured against and what is settled. An
approval for more than was sent is approved, and logged at `Error`, because the
customer is owed a refund.

**Reversals and refunds.** A reversal applies to an approved payment for exactly
its authorised amount, and makes it `Reversed`. Refunds may be partial and several:
below the authorised amount the payment is `PartiallyRefunded`, and at exactly that
amount `Refunded`. A second reversal, a reversal after a refund, a reversal for
another amount, a refund that would pass the authorised amount, an event in another
currency, and an event for a declined or failed payment are each kept, not applied,
and make the payment `Unresolved`. An event Acme Switch did not apply, with
`isApproved` false or a `txResultCode` other than `00`, is recorded and never
applied. A `transactionType` other than `Reversal` or `Refund` is recorded and
changes nothing.

**Held until the worker's answer, and the worker's lease.** A Payment Advice, a
reversal or a refund that arrives before the outcome is known is held. When the
worker's next answer sets the outcome, the held advices are compared and the held
events applied, in the order Acme Switch processed them; if the payment was declined
or failed, each event is a contradiction. A notification never changes when the
worker next calls Acme, because that time is also the worker's lease: moving it would
let a second worker send the payment while the first call is still in progress. A
notification kept for a payment always writes the payment's row, so its `xmin`
changes even when nothing on the payment did. A worker whose call was in flight then
meets a conflict at its save, reloads the payment with the held notification, and
asks the Domain again; without it, the worker saved its answer without seeing the
reversal, and nothing applied it later. The stub's reversal-first card found this on
the running stack (ruled by Francois).

**The merchant account check.** A Payment Advice matches the payment whose id is its
`TransactionID`, and only where its `MerchantID` is Zapper's account at Acme. A
Transaction Status matches the payment whose order number is its Retrieval Reference
Number. A notification that matches no payment, or names another account, is stored
unmatched, answered 200, logged at `Error`, never matched later, and changes nothing.

**The switch that keeps the secrets off the background service.**
`Zapper:AcmeNotification:IsEnabled` is true on the service that answers requests and
false on the one that does background work, so only the first holds the Payment
Advice username and password and the webhook secret. With it off, both operations
answer 401. Every host must set it. The merchant id is read from its own setting
rather than from the Acme client's, whose API key the service that answers requests
must not hold, so `compose.yml` writes the same value into both. Locally one
container does both jobs, with both on.

**The provider rate limit, and Acme's addresses in production.** The two
notification operations have a rate limit of their own, 2,400 requests a minute per
address, twice the Payment Advices our own sending can cause at 20 payments a
second; the general limit leaves them alone. The better production answer is a
firewall rule that lets through only Acme's published addresses, where Acme
publishes them.

**The data model this Feature adds.** `Payment.Notification` holds every notification
as described above, and `Payment.PaymentEvent` each reversal and refund: its amount,
its currency as Acme sent it, when Acme Switch processed it, whether it is held,
applied, not applied or a contradiction, and when it was applied. A change of a
payment now also names the notification it came from. The notification sources and
outcomes, the event types and the event states are reference tables, and the
payment states gain `Reversed`, `Refunded` and `PartiallyRefunded`. The API's role
may insert notifications and never change them, and may insert and update events,
because a held event becomes applied; it deletes neither. Notifications and events
are personal information, kept for 5 years with the payment, and an unmatched
notification for 5 years after it arrived.

**The stub's assumptions.** `AcmeStub/README.md` lists them in full. In short: a
Payment Advice follows every outcome the stub keeps, about 1 second after it
answers, or after a late answer is approved; a send it does not keep sends none; the
advice-twice card sends the same advice twice; the reversal-first card sends its
reversal before it answers the send; every notification carries `DeliveryCount`, a
member Appendix A does not document, which changes on each delivery; a notification
not answered 200 is sent three more times, 2 seconds apart; reversals and refunds,
sent through the stub's own `/stub/` routes, always settle approved; and a
notification waiting to be sent is lost when the stub restarts.

**Facts for the AI workflow section.** The Feature was planned by `create-plan` and
built by `feature-execution` in seven increments, each committed as
`Acme_Notifications_<n>/7` and checked before the next began: failing acceptance
tests first, then the schema and the Domain, Acme's proof, the Payment Advice
operation, the Transaction Status operation, the stub with `compose.yml`, and this
documentation. The plan gave the twenty-nine scenarios and every name; the
standards gave how each part is written. Where the code met something the plan did
not settle, the run stopped and put it to Francois, and his ruling was written into
the plan before the work carried on. Five rulings came from this Feature's run: the
acceptance tests gained their database reads in increment 2, once the tables
existed; the delivery count is the member `DeliveryCount`; Appendix A's worked
example is signed with the webhook secret the brief publishes; a notification kept
for a payment writes the payment's row; and the stub's routes take their key as a
bearer key, with a refund's amount as `Amount`.

**Facts for the validation section.** Each of the twenty-nine scenarios is an
acceptance test that runs the whole API with the Acme client substituted, sends each
notification with the run's own credentials or signature, and reads the payment
through the API and the notifications, events and changes through the database.
Each operation's integration tests cover every result, every validation code, a
wrong username, a wrong password, a missing header, a wrong signature, a body changed
by one byte, the other notification's proof, notifications switched off, and
Appendix A's worked example accepted; the cryptography manager's tests reproduce the
worked example's signature. The 401 sweep covers both operations. Three problems
were found and fixed in the run: PostgreSQL refused a time read at UTC+02:00, so
each time is now stored in UTC; a test meant to change the body by one byte wrote
the value already there, so it proved nothing until it was corrected; and the race
between a held notification and the worker's call, which no test had placed, was
found by running the stub's reversal-first card against the stack, then proven by a
worker test that sends a reversal through the API during the call, failing before
the fix and passing after. `docker compose up` then took the seeded cards through a
confirmed advice, a duplicate advice, an out-of-order reversal, two refunds and a
reversal.

### SQL Daily Summary

**The summary is one `SELECT`, run in a database client.** It is
`Docs/Reports/DailyPaymentSummary.sql`, with the date written once at its top. A
person opens it in pgAdmin or any other client, signed in as the data reader below,
changes the date and runs it. It holds no transaction, no `psql` command and no
variable, so it runs unchanged in any client. It is not an endpoint: the app's
endpoints are public in this assignment, a date key and an unpaged list fit the API
standards badly, and settlement is back office work. In production it runs on a read
replica.

**Facts, not status.** An approval and a decline count once, on the day of Acme's
`TransactionDateTime`, whatever the payment's status is now: an approval at the
authorised amount, which is what was taken from the card, and a decline at the
amount asked for. A reversal and a refund count on the day Acme Switch processed
them, at their own amount, and only once applied; one held, not applied or a
contradiction does not count. An approval and its reversal each count on their own
day. So a day's money columns do not change once the day has passed, except for an
event applied late. Beside them, Pending, Failed and Unresolved count by the day the
payment was recorded, at the amount asked for. `Unresolved` is a warning: an
unresolved payment Acme approved counts as approved and as unresolved, and the
person settling holds it back by hand. Rows are grouped by sub-merchant, by Zapper's
reference and trading name, and by currency, and only a group with something on the
day has a row.

**Acme's time is the only time for an approval or a decline.** Acme always sends
`TransactionDateTime` on a final answer, which is an assumption. An approval stored
without it would count on no day, with no warning; a person finds one by querying
for a missing Acme time.

**A day is a South African day**, from 00:00 to 24:00 at UTC+02:00, so it starts at
22:00 UTC the day before. The file works that out from the date and never writes
`AT TIME ZONE '+02'`, which PostgreSQL reads as two hours west of UTC.

**Two decimal places.** A value is the amount in the lowest unit divided by 100,
with exactly two decimal places, and an empty column shows `0` and `0.00`. Every
currency on Zapper's list has two; a currency with another number would need its
decimal places in `Payment.CurrencyType`.

**Acme's outcome is read from the change history.** The outcome is set once and
every change of a payment is recorded, so a payment counts as approved or declined
when one of its changes moved it into that state, even where a reversal, a refund or
a contradiction has moved it on since.

**Late facts and snapshots.** A held reversal or refund applied after its day was
summarised counts on its processing day, so that day is run again. A payment that
becomes `Unresolved` after its day was summarised leaves that day's money columns as
they were and changes its recording day's unresolved count. `Pending` is a snapshot:
a new run counts the payment under its final status.

**Three indexes make a day cheap to find in 5 years of payments:**
`IX_Payment_AcmeTransactionAt`, `IX_Payment_RecordedAt` and
`IX_PaymentEvent_AcmeProcessedAt`, each on its one column. The grouping runs over one
day's rows only, so no index leads with the sub-merchant. A covering index was
rejected, because the payment row is updated often and such an index would rarely be
read alone.

**The data reader role.** A person reading the database, for the summary or to
investigate a payment, signs in as `ZapperDataReaderRole`, never with the Migrator's
or an API's credential. `compose.yml` creates it with `LOGIN NOSUPERUSER NOCREATEDB
NOCREATEROLE`, beside the other roles, and issues and renews its client certificate
as it does theirs. A migration grants it `USAGE` on every schema and `SELECT` on every
table, the `Customer` schema included, and sets default privileges, as the role that
runs the migrations, so every table and schema a later migration creates is readable
by it too, with no grant of its own. It never gains `INSERT`, `UPDATE`, `DELETE` or a
schema change. Its connection carries the email address and the encrypted card
token, so it is at sensitive data; it never gets the card key. In production it is a
group role with no login, on a read replica: each person signs in as themselves,
with an IAM database account and a second factor, as a member of it, and who may read
the `Customer` schema is limited by who is made a member.

**The seeded payments.** The Migrator's Debug build seeds eighteen finished payments
on the two South African days before the stack's first run, at two sub-merchants and
in two currencies, in every final status, so the summary shows data on the first run.
Each is moved only by its own domain methods, with a clock the seed sets before each
step. None is `Pending`, because the worker makes a payment recorded on an earlier
day `Unresolved` at once; `Pending` is shown by a payment recorded live. Each has a
fixed app reference and is written only where no payment holds that reference, so the
seeded payments stay on the days of the first run. The Migrator logs those two days
on every run. The seed reads the current time through the same injected
`TimeProvider` an API does, which `AddZapperManagers` now registers (ruled by
Francois on 2026-10-25).

**How the schema would evolve.** Once volume or audit needs it, a settled summary is
stored for each closed day, so a day already paid out is read as it was settled
rather than worked out again.

**Facts for the AI workflow section.** The Feature was planned by `create-plan` and
built by `feature-execution` in five increments, each committed as
`SQL_Daily_Summary_<n>/5` and checked before the next began: failing acceptance tests
first, then the data reader role, the summary with its indexes, the seed, and this
documentation. The plan gave the twelve scenarios, the file's columns and every
name; the standards gave how each part is written. One ruling came from this run: the
seed needed the current time, which only an API registered, so `AddZapperManagers`
registers `TimeProvider` and `BackendCode.md` section 9 says so.

**Facts for the validation section.** Each of the twelve scenarios is an acceptance
test that arranges its payments through the entities and their own domain methods,
then runs the file itself, with only its date line replaced, signed in as the data
reader. Each test also checks that the file holds exactly one date line and returns
the seventeen columns in order. `DataReaderRoleIntegrationTests` proves that the
reader reads every table the model maps, that an insert, an update and a delete on a
payment are each refused, and that a table created after the migrations, by the role
that ran them, is readable. The naming test covers the three indexes and the
migration test their `Down`. On the running stack, the reader signed in with its
certificate over TLS, read the payments and was refused a change; `docker compose up`
run twice left eighteen seeded payments; and the file run for each logged day showed
the plan's figures for both sub-merchants in both currencies. Two problems were found
in the run: the seed first stored South African times with their offset, which
PostgreSQL refused, so each is now stored in UTC; and on a stack started earlier,
`certificates` and `roles` kept their old scripts, so the role was made only once they
were recreated, which the README now says.

### End to End validation checks

**One script proves the real path.** `AcmeStub/end-to-end.sh` walks ten cases against
the stack `docker compose up` starts: the brief's eight, and two more that prove a
payment is asked about before it is sent again, the card Acme never received and the
card that meets a 503 once. Each case is named by its scenario title in the plan. The
path it proves is the one the C# tests cannot: the API, the worker, the real Acme
client, the stub and real TLS together. It is a `bash` script with `curl` and `jq`
only, outside the standards with the stub it drives, and it adds no code to the
solution.

**API endpoints only.** The script is for Zapper's reviewers to run against the API,
so it calls our API's operations and the stub's own routes, and never reads the
database (ruled by Francois). What it proves is that each case ends in the right
status, read the way the app reads it. The records behind a status, which the read
answer does not show, are proven by the acceptance tests of the Feature that built
them: that a payment recorded twice is sent once, that a payment Acme never received
or met a 503 is asked about and sent again with the same identity, that a duplicate
Payment Advice changes nothing, and that a reversal arriving first is applied once the
payment is approved.

**Record first, then wait.** Every payment is recorded one after another, then all are
read together, so the slow cases wait at the same time and a run takes under two
minutes rather than several.

**The read at the end.** A Payment Advice arrives about a second after the worker's
answer, so a case can pass before its advice arrives. Once every case has passed, each
payment is read once more, and a status that changed fails its case.

**Within our own rate limit.** The script reads only the payments still waiting, every
2 seconds, so its load on the API stays well under the 300 requests a minute each
address is allowed. A 429 is not a failure: it waits for `Retry-After`, or 5 seconds,
and asks again within the case's 2 minutes.

**CI runs it.** Four steps in the pipeline, after the tests: start the stack, run the
script, print `docker compose logs` when anything failed, and stop the stack with its
volumes whatever happened. They run on every pull request and every push to `dev`,
`test` and `main`, with the runner's own `curl` and `jq`.

**Facts for the AI workflow section.** The Feature was planned by `create-plan` and
built by `feature-execution` in three increments, each committed as
`End_To_End_Validation_Checks_<n>/3`: the script with every case written and run, the
CI steps, and this documentation. Two rulings came from this run: the declining-cards
case records three payments, and each needs a reference of its own, so each ends in
the response code its card expects; and the AI workflow section gets only the list of
what `CLAUDE.md` and `Claude/` hold from this run, with the rest left to Francois.

**Facts for the validation section.** The script's first run against the stack passed
all ten cases in 52 seconds, because the four Features before it had built everything
it checks. A run with the stub's port wrong failed the reversal and refund cases, each
with the reason, passed the other eight, and exited with 1. The script runs under the
`bash` macOS ships, version 3.2, as well as the runner's.
