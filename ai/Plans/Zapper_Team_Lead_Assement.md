- **Status:** Approved 2026-10-23
- **Planned on branch:** `Feature/Zapper_Team_Lead_Assement`. Each Feature is built on a
  branch of its own, `Feature/<Feature_Title>`, by `feature-execution`.

> A **plan**, not an implementation. It states what must be true and how it is
> built, for every Feature in it, so that `feature-execution` invents nothing.
> The standards in `Claude/Standards/` read the parts of a Feature's section
> that they name: its rules, its scenarios, how it is built and its
> implementation order. Every Feature is **tech-only**: none has a user
> interface.

# Outcome

Zapper's app can take a card payment from a customer with a stored card, at one of
Zapper's sub-merchants, through the Acme Pay gateway the brief describes, without
holding the app's request open while Acme works. Every payment reaches a final
state that the app can read, whatever Acme does: answers slowly, times out, answers
twice, sends notifications out of order, reverses or refunds the payment later.
Everything needed to investigate a payment is kept, and Zapper can run a daily
summary per sub-merchant and currency for merchant settlement. A stand-in for Acme,
and a script that walks every case in the brief against the running stack, show
that it works end to end. This is the deliverable of the Zapper Team Lead
technical assessment.

# Boundaries

- **In scope:** recording a payment and reading its status; submitting it to Acme
  in the background, with the timeouts, retries, queries and limits Acme's
  documentation calls for; receiving Acme's Payment Advice and Transaction Status
  notifications; the record of every interaction and notification; the daily
  summary as a SQL query; a stub of Acme; an end-to-end script run in CI; the README
  and SOLUTION.md, with the AWS approach described in SOLUTION.md.
- **Out of scope:** signing the app or its users in (both app operations are public,
  by D-2); adding, changing or removing a stored card or a sub-merchant (they are
  seeded, and an administrator changes them directly); converting between
  currencies; the job that enforces retention periods; key rotation (D-11);
  deploying to AWS; any user interface.
- **Depends on:** `Zapper.PaymentsApi`, which `create-project` added, and the
  solution `create-solution` created.

# Decisions

> A decision is here when it affects more than one Feature, or more than this
> plan. A decision that affects one Feature only is written in that Feature's
> "How it is built". A decision a standard already makes is never repeated here,
> and never re-argued. A decision that replaces one in an earlier plan names it,
> as `<Plan_Title>` D-<n>.

- **D-1: The app's request is answered at once, and the app polls.** Recording a
  payment answers 201 as soon as the payment is stored, with our id and its
  status, before Acme has been called. The app then reads the payment by our id
  until its status is final. A create request repeated with the same app payment
  reference returns the payment already held, so an app that lost the 201 still
  gets our id. Acme can take 30 seconds and may time out, so holding the app's
  request open would tie a connection to Acme's slowest answer and give the app a
  timeout with no answer. Considered and rejected: holding the request open;
  SignalR in this assignment. SOLUTION.md describes the production approach: push
  through SignalR with a backplane for the platforms that support it, polling kept
  as the fallback, and the push channel signed in once app authentication exists.
  **Affects:** Recording payments, Submitting Payments to Acme, End to End
  validation checks.

- **D-2: The app's two operations are public, and the read answer carries no
  personal information.** The brief allows the app's endpoints to be public, so
  both are `.AllowAnonymous()` with `"security": []`. Because anyone holding an id
  can read the payment, the read answer holds only our id, the app's payment
  reference, the status, the amount and currency, Acme's response code and its
  client message, and the reversed and refunded totals: never the customer
  reference, the email address, the card token, the sub-merchant's details or
  Acme's internal references. The rate limit per network address is the only guard
  on recording. Cost: anyone can record a payment for any customer reference they
  know. SOLUTION.md describes production: the app signs in with a token issued to
  the Zapper user, each payment belongs to that user's customer, reading another
  customer's payment answers 404, and recording takes the customer from the token.
  **Affects:** Recording payments, End to End validation checks, and every later
  plan that adds app sign-in.

- **D-3: A payment's identity at Acme is fixed for its whole life, and Acme is
  asked before anything is sent again.** Each payment's `TransactionID` is the
  payment's own id, and its `OrderNumber` is made once, when the payment is
  recorded. No retry and no recovery makes new ones. After a timeout, a 500 to
  504, or a connection broken after sending, the outcome is unknown, and Acme's
  transaction query is asked first: a final answer is recorded; `Processing` means
  ask again later; a 404 means Acme never received it, and only then is it sent
  again, with the same `TransactionID` and `OrderNumber`. A 429 is sent again with
  the same `TransactionID` after its `Retry-After`. A 400 `E01` or a 401 `E02` fails
  the payment, logged at `Error`. A 409 `E09` naming another `TransactionID` means
  our `OrderNumber` was not unique: the payment fails and an alert is raised. The
  query spacing is a Domain rule: the first query 5 seconds after the timeout;
  while `Processing`, the gap doubles from 5 seconds to at most 5 minutes; after 3
  sends that each end in a 404 the payment fails as not submitted; and with no
  final answer after 24 hours the payment becomes `Unresolved`, logged at `Error`.
  The time limit on a call to Acme is 35 seconds, above Acme's documented 30.
  Assumption for SOLUTION.md: Appendix A does not say what Acme does when a
  `TransactionID` is sent again while the first request is still in progress, so we
  always ask first and send again only on a 404; the stub replays a stored outcome
  and answers `Processing` for a request still in progress. Considered and
  rejected: a new `TransactionID` per attempt, which could charge the customer
  twice. **Affects:** Recording payments, Submitting Payments to Acme, Acme
  Notifications.

- **D-4: Background work runs in workers inside `Zapper.PaymentsApi`.** A worker
  submits due payments to Acme, as `BackendCode.md` section 23 (A7) states. In
  production one image runs as two ECS services: one that answers the app, with
  every worker off, scaled on requests; and one that does background work, with
  every worker on, scaled on the number of payments due and not claimed, with an
  alert on the age of the oldest. Locally and in tests every worker is on. The
  polling interval is about 250 milliseconds, a batch is small, and the lease is
  60 seconds. The host's shutdown timeout is 45 seconds, `compose.yml` gives the
  API a stop grace period of 60 seconds, and ECS a stop timeout of 60 seconds. On
  AWS a CloudWatch metric filter turns each worker's 30-second line into metrics.
  SOLUTION.md says that normally an event bus or a message queue would carry work
  to the worker, for resilience and replay; the assignment does not call for it,
  so polling the database suits this structure, with `LISTEN` and `NOTIFY` a later
  optimisation; that OpenTelemetry metrics are the fuller answer; that a load
  balancer's deregistration delay drains the app-facing service; and that a crash
  between Acme's answer and the save loses that one interaction record, which the
  next query recovers. Considered and rejected: a worker project of its own, and a
  project per worker, which multiply projects and deploy the same way the API
  does. **Standard changes:** A6 and A7, ruled by Francois on 2026-10-23.
  **Affects:** Recording payments, Submitting Payments to Acme, End to End
  validation checks.

- **D-5: Only the background service calls Acme, within limits that keep Acme's
  rate limit.** Each worker instance limits the payments it works on at once to a
  setting sized generously, about 50, since the calls are asynchronous, and claims
  only into free places. Each instance limits its requests a second to its share of
  Acme's 20, a setting worked out from the number of instances at deployment; a 429
  with `Retry-After` is the backstop. The Acme HTTP client's connection limit equals
  the limit on work at once, and an idle connection is retired after less than 60
  seconds. The client retries nothing itself: it holds the 35-second limit, the rate
  limiter, and a circuit breaker that opens when most recent calls fail with 500 to
  504 or a timeout, during which due payments are moved forward. It uses
  `Microsoft.Extensions.Http.Resilience`, whose `Versions.md` row is written in the
  increment that adds it. A limit reached refuses nothing: payments stay due, and
  whichever instance has room claims them. SOLUTION.md names a limiter shared
  through Redis as the exact alternative. **Affects:** Submitting Payments to
  Acme, End to End validation checks.

- **D-6: Each operation and each piece of work saves once, and a save reports
  what happened.** Every operation in this plan saves once: recording a payment,
  each notification, and each piece of a worker's work. A record that must exist
  once is guarded by a unique index, and the save that could break it reports
  `Saved`, `AlreadyExists` or `Conflict`, as `BackendCode.md` section 10 (A3)
  states. Recording a payment reads and returns the payment already held; a
  duplicate notification answers 200. The payment row carries `xmin` as its
  concurrency token from Submitting Payments to Acme on, the first Feature that
  changes a payment. Considered and rejected: `INSERT ... ON CONFLICT DO NOTHING`;
  `SELECT ... FOR UPDATE`; retrying inside an operation, which would be a second
  save. **Standard change:** A3, ruled by Francois on 2026-10-23. **Affects:**
  Recording payments, Submitting Payments to Acme, Acme Notifications.

- **D-7: Acme proves itself the way Appendix A fixes, through generic handlers in
  ApiCommon.** Our service gives Acme its bearer API key. Acme gives our Payment
  Advice endpoint HTTP Basic: the username and password are checked together, by
  comparing the SHA-256 of each with `CryptographicOperations.FixedTimeEquals`,
  and a failure never says which part was wrong. Acme Switch signs our Transaction
  Status endpoint's body with `x-signature`: the exact body is read within the size
  limit, its HMAC-SHA256 with the webhook secret is compared in constant time with
  the header decoded from Base64, and the body is rewound for binding;
  authentication runs before binding. Each scheme grants one permission, and each
  operation requires it with `.RequireAuthorization(...)`. A failure answers 401
  `Unauthorized`, logged at `Warning`. The handlers are generic, in ApiCommon: a
  Basic handler and a body-signature handler, given their credentials as
  arguments, which the payments API registers with Acme's values.
  `OperationSecurityTransformer` stops throwing and writes `http` `basic`, and an
  `apiKey` in the header `x-signature`, with the permission; `UseAuthentication`
  runs before `UseAuthorization`; the contract tests compare both. Tests use
  Appendix A's worked example, and refuse a wrong password, a wrong signature, a
  missing header, and a body changed by one byte. `Docs/CryptographicInventory.md`
  gains the webhook key and the local authority's certificates. SOLUTION.md: no
  replay protection, which is harmless because a repeat is a duplicate; production
  adds a firewall rule for Acme's published addresses where Acme publishes them.
  **Standard change:** A9, ruled by Francois on 2026-10-23. **Affects:** Acme
  Notifications, End to End validation checks.

- **D-8: A payment's state is worked out from every fact received, whatever their
  order.** Every notification is stored as it was received, before anything is
  decided: its exact body, the time it arrived, its trace id, and the payment it
  matched or none. A duplicate is a notification whose identity is already held,
  found through `SaveOutcome.AlreadyExists`, and answers 200 with nothing changed:
  a Transaction Status notification's identity is its `transactionUUID`; a Payment
  Advice notification's is its `TransactionID` with its `RequestType`. The same
  identity with different content keeps the first, stores the second as a
  contradiction, logs at `Error`, and makes the payment `Unresolved`. The outcome
  is set once, by the first final answer from any source: the worker's call, a
  query, or a Payment Advice. Later answers that agree confirm it, recorded as
  changes; a later answer that disagrees is never applied, and makes the payment
  `Unresolved`, logged at `Error`. `Success` with response code `00` is approved;
  `Success` with any other code is declined; `Failure` is failed. `Authorize` and
  `Capture` advices are recorded and do not set the outcome (an assumption for
  SOLUTION.md). Reversals and refunds are events of the payment, applied by rules:
  kept and applied once the payment is approved; against a payment not approved, a
  contradiction; refunds may be partial and several, and one that would pass the
  amount is kept, not applied, and makes the payment `Unresolved`; a reversal is
  the full amount, a second with the same `transactionUUID` is a duplicate, and any
  other second one is a contradiction; one with `isApproved` false or a
  `txResultCode` other than `00` is recorded and never applied. A reversal or refund
  whose currency differs from its payment's is a contradiction. A notification
  matching no payment is kept, answered 200, and logged at `Error`. Every change is
  recorded as a change of the payment: the state before and after, its source, the
  notification or call it came from, and when. The app sees eight statuses:
  `Pending` (not final), `Approved`, `Declined`, `Failed`, `Reversed`, `Refunded`,
  `PartiallyRefunded` and `Unresolved` (needs a person, and final for the app).
  The finer internal states, such as submitting and outcome unknown, all show as
  `Pending`. **Affects:** Recording payments, Submitting Payments to Acme, Acme
  Notifications, SQL Daily Summary.

- **D-9: The data, its schemas, and what each record keeps.** Three schemas:
  `Merchant`, `Customer` and `Payment`. `Merchant.SubMerchant` and
  `Customer.StoredCard` are created in Recording payments. `Payment.Payment` holds
  the payment, its identity at Acme, its internal state, Acme's outcome, its
  reversed and refunded totals, the time it is next due for work, its attempt and
  unexpected-failure counts, and its times; it keeps a copy of the
  `CardAcceptorName` and the merchant category code it was sent with. Each Feature
  adds the columns it first writes. `Payment.PaymentChange`, `Payment.PaymentEvent`,
  `Payment.AcmeInteraction` and `Payment.Notification` hold the changes, the
  reversals and refunds, every call to Acme (request sent, response or none, HTTP
  status, duration, attempt, trace id) and every notification. States, sources,
  event types and interaction outcomes are reference tables named for their enum
  plus `Type`. The interaction record stores the request with the token and the
  email address replaced by a fixed marker, and names the stored card by its id.
  A Payment Advice body is stored with `MaskedCardNumber` removed; a Transaction
  Status body is stored exactly. No log line carries a token, an email address or a
  card number. Payments, their changes, interaction records and notifications are
  kept for 5 years after the payment, under the FIC Act; a stored card is kept
  while the customer keeps it, and de-identified within one day of its removal. The
  job that enforces those periods is not built, and SOLUTION.md names it. The
  email address is protected by RDS encryption at rest and TLS, not encrypted in
  its column (SOLUTION.md). Assumption for SOLUTION.md: a customer holds one stored
  card, enforced by a unique index; in production the request would carry the
  stored card's id, checked as belonging to the customer. **Affects:** Recording
  payments, Submitting Payments to Acme, Acme Notifications, SQL Daily Summary.

- **D-10: Payments may be in any currency on Zapper's list, and the list is a
  reference table.** The brief groups the daily summary by currency, and Appendix A
  makes `CurrencyCode` optional with ZAR as Acme's default, so Zapper's own list is
  `Payment.CurrencyType`, holding each currency's ISO code, its symbol and its
  maximum amount in the lowest unit: ZAR (R, 100 000 000), USD ($, 5 000 000), EUR
  (€, 5 000 000) and GBP (£, 4 000 000). The app always sends the currency; it is
  compared without regard to case, and always stored, sent to Acme and answered in
  capitals. There is no conversion: each currency is recorded and settled on its
  own. Assumptions for SOLUTION.md: Acme accepts every code on our list, which
  production would confirm with Acme; a South African merchant normally charges in
  rand and the cardholder's bank converts, so a foreign currency here is a merchant
  pricing in it; a currency cannot be withdrawn, because its row cannot be deleted
  while payments use it, and stopping new payments in it would need a flag on the
  row, added when asked for. The ZAR-only ruling made earlier in this planning
  session was withdrawn by Francois on 2026-10-23. **Affects:** Recording payments,
  Submitting Payments to Acme, Acme Notifications, SQL Daily Summary.

- **D-11: The card token is encrypted by a manager shared by the API and the
  Migrator.** The token is sensitive data, stored only encrypted, with AES-256-GCM
  from the .NET base class library, through `CryptographyManager` in ApiCommon.
  `Encrypt` always uses the current algorithm and writes its one-byte version first,
  today always 1; `Decrypt` reads the version and chooses the algorithm, so a new
  algorithm or a new key is a new version inside the manager, with no caller
  changed and every stored value still readable. The stored card's customer
  reference is the associated data, so a token moved to another card fails to
  decrypt. The key is 32
  random bytes in Base64. Locally the `compose.yml` init service generates it with
  `openssl rand` into the secrets volume, mounted read-only into only the hosts
  that use it, and the key-per-file source reads it; tests generate a fresh key for
  each run. In production it is a Secrets Manager secret, itself encrypted by KMS,
  injected by ECS as an environment variable of the same name, readable only by
  the task roles that use it, with every read and change recorded by CloudTrail.
  Rotation is not built (ruled by Francois): one key, one setting. SOLUTION.md says
  that rotation must be built before the first production key is 12 months old,
  because `Security.md` section 3 replaces every vault secret within 12 months, and
  that envelope encryption through KMS is the fuller answer. Considered and
  rejected: a value converter, which every host building the model could use and
  which cannot bind a value to its row; giving the Migrator the key in every build.
  **Standard changes:** A1, A2 and A5.2, ruled by Francois on 2026-10-23.
  **Affects:** Recording payments, Submitting Payments to Acme.

- **D-12: Local data is seeded by the Migrator's Debug build.** The seed writes
  through the entities, and so through `CryptographyManager`, which is why it is
  .NET and not SQL. It uses the Migrator's role, which owns the tables, so the API's
  role stays read-only on stored cards and sub-merchants. It seeds two or three
  sub-merchants, one with a trading name past 23 characters and one with a city past
  13; one customer with a stored card for each behaviour of the Acme stub, with
  email addresses at `example.test` and tokens matching the stub's list, which the
  end-to-end script checks; and, from SQL Daily Summary on, finished payments on the
  previous two days, in every status, across two sub-merchants and two currencies,
  so the summary shows data on the first run. Considered and rejected by Francois: a
  seed tool project, and an exception recorded against `Security.md` section 13.
  **Standard change:** A4, ruled by Francois on 2026-10-23. **Affects:** Recording
  payments, SQL Daily Summary, End to End validation checks.

- **D-13: Local connections use TLS, and every local secret is generated.** The
  `compose.yml` certificate script issues a server certificate for the stub's host
  name. Our API and the stub trust the local authority through `SSL_CERT_FILE` and
  `SSL_CERT_DIR`, set in `compose.yml` to the public roots plus `ca.crt`, so no code
  touches the certificate check; the first increment that calls the stub proves a
  call fails without the variable and works with it. Five secrets are generated with
  `openssl rand` by the init service on the first `docker compose up`: the card
  key, the Acme API key prefixed `sk_test_`, the Payment Advice username and
  password, and the webhook secret. The merchant id is configuration, not a secret.
  Considered and rejected: fixed values in `compose.yml`; an uncommitted `.env`;
  plain HTTP locally. **Standard change:** A5.3, ruled by Francois on 2026-10-23.
  **Affects:** Recording payments, Submitting Payments to Acme, Acme Notifications,
  End to End validation checks.

- **D-14: The Acme stub stands outside the standards and follows the brief.** It
  lives at `/AcmeStub/`, outside `Code/` and the solution: one ASP.NET Core minimal
  API on .NET 10 with no package beyond the framework, its state in memory (cleared
  on restart, an assumption in SOLUTION.md), its own `Dockerfile` and `compose.yml`
  service, and a README saying all of this. Only two repository rules reach it: no
  committed secret, and no em dash. The card token chooses its behaviour, one seeded
  card per behaviour: approve; decline with `05`, `51` and `54`; Payment Advice sent
  twice; reversal before the Payment Advice; a late answer causing a timeout, then
  approved; never received, so the query answers 404; and 503 once, then approved. A
  token not on its list is declined with `05` and a log line. Reversals and refunds
  are triggered through stub-only routes under `/stub/`, which need the stub's API
  key. Every response and notification carries one member Appendix A does not
  document; a notification not answered 200 is sent three more times; 401 `E02`,
  400 `E01`, 409 `E09` and 429 with `Retry-After` are as Appendix A. Assumptions for
  SOLUTION.md: the stub's delays; a timed-out payment resolving a few seconds later;
  reversals and refunds always settling approved; partial authorisation not
  simulated. **Standard change:** A10, ruled by Francois on 2026-10-23. **Affects:**
  Submitting Payments to Acme, Acme Notifications, End to End validation checks.

- **D-15: Tests never call Acme or its stub, and a script proves the whole path.**
  The Acme client is tested through its production registration with a recording
  handler, against Appendix A's own examples copied once into test files: a 200,
  the error envelope, a 409 naming another `TransactionID`, a 429 with
  `Retry-After`, and a response with a member it does not know. Requests are
  checked against Appendix A's example: PascalCase, the amount in the lowest unit,
  `TransactionDateTime` in SAST with no offset and to the millisecond, and the
  bearer header. Acceptance tests substitute our Acme interface and send Acme's
  notifications themselves, with the right credentials or signature. The real path,
  through the API, the worker, the real client, the stub and real TLS, is a script
  in `/AcmeStub/` using only `curl` and `jq`, run against the `docker compose`
  stack: it reads the generated secrets, trusts `ca.crt` with `--cacert`, walks
  every case in the brief with the seeded cards and the stub's routes, polls each
  payment until its status is final, and fails when any case does not end in its
  expected status. CI runs it (ruled by Francois): a step brings the stack up and
  runs the script on every pull request and every push to `dev`, `test` and `main`.
  The README says how to run it, and Francois's video uses it. Considered and
  rejected: testing the client against the stub, which proves only that two readings
  of Appendix A agree; a Postman collection, rejected by Francois because the
  generated secrets and the waiting are simpler in a script. **Standard changes:**
  A8.1 to A8.4, ruled by Francois on 2026-10-23. **Affects:** Submitting Payments to
  Acme, Acme Notifications, End to End validation checks.

- **D-16: The daily summary is a SQL file.** An endpoint would be public by D-2, and a
  date key and an unpaged list fit the standards badly, while settlement is back
  office work. One file, `Docs/Reports/DailyPaymentSummary.sql`, takes the date as a
  `psql` variable; the README shows running it with `docker compose exec database
  psql`; SOLUTION.md says production runs it on a read replica under a
  `SELECT`-only reporting role. Its rules are SQL Daily Summary's: a date is a South
  African calendar day, 00:00 to 24:00 SAST (UTC+2); approvals and declines count
  by Acme's `TransactionDateTime`, reversals and refunds by their own
  `transactionDateTime`; the approved value is `AuthAmount`, the declined value the
  requested amount, reversal and refund values the event's amount; only applied
  events count; an approval and its reversal each count on their own day; rows are
  grouped by sub-merchant (Zapper's reference and trading name) and currency. Beside
  the brief's columns, Francois asked for Pending, Failed and Unresolved, each by the
  time we recorded the payment, each with a count and a value. Its scenarios are
  proven by database tests that run that exact file over seeded data across two days
  and two sub-merchants, which `Testing.md` section 7 allows because no contract
  reads the data. **Affects:** SQL Daily Summary, End to End validation checks.

- **D-17: How the Features after Recording payments are delivered.** End to End
  validation checks' first increment is the end-to-end script with every case
  written and failing, since its code is outside the standards (ruled by Francois).
  Each Feature writes its own part of README.md and SOLUTION.md in its last
  increment. SOLUTION.md's sections on the AI workflow and on validation are in
  Francois's own words, from a list of facts each Feature supplies. Session exports
  go to `ai/`, and the brief asks for the sessions that build the Acme client, so
  Submitting Payments to Acme exports them. **Affects:** every Feature in this plan.

# Features

In the order they are built. A Feature is built only after every Feature above
it, on its own branch and with its own pull request, and counts as built once
that pull request is merged. Every Feature title, once converted, differs from
the plan's and from every other Feature's in every plan, because
`feature-execution` finds a Feature's pull request by its branch name.

| # | Feature | Planned |
|---|---|---|
| 1 | Recording payments | 2026-10-23 |
| 2 | Submitting Payments to Acme | Not yet |
| 3 | Acme Notifications | Not yet |
| 4 | SQL Daily Summary | Not yet |
| 5 | End to End validation checks | Not yet |

# Feature: Recording payments

## What it delivers

The app can record a payment for a customer with a stored card, at a registered
sub-merchant, in a currency on Zapper's list, and read its status by our id. A
payment is stored as `Pending` and due for submission to Acme, with its identity at
Acme and its statement name fixed when it is recorded. A repeated request returns
the payment already held. The schemas, the reference tables, the card token's
encryption and the local seed of sub-merchants and stored cards are in place.
Payments stay `Pending` until Submitting Payments to Acme, which depends on all of
it.

## Rules

> What must always be true, in domain language, never as tables, columns or
> keys. Each rule has an id, `R-<n>`, numbered within this Feature. Every rule is
> covered by at least one scenario. The standards that read "the plan states"
> read it here: who may see each record, who may read and change each field,
> every limit, the order of a process's steps, how long each category of
> personal information is kept, and how it is corrected and de-identified.

- **R-1: Recording a payment.** The app records a payment by giving the customer's
  reference, the sub-merchant's reference, the amount, the currency and its own
  reference for the payment. The payment is recorded as `Pending` and is due for
  submission to Acme at once. In this Feature it stays `Pending`.
- **R-2: The amount.** The amount is a whole number of the currency's lowest unit,
  at least 1 and at most that currency's maximum: R1 000 000 for ZAR, $50 000 for
  USD, €50 000 for EUR and £40 000 for GBP. An amount outside that range is refused,
  and nothing is recorded.
- **R-3: The currency.** The currency must be one on Zapper's list: ZAR, USD, EUR or
  GBP. Its code is compared without regard to case, and is always stored, sent to
  Acme and answered in capitals. A currency not on the list is refused, and nothing
  is recorded. No amount is converted between currencies.
- **R-4: Who and where.** The customer must hold a stored card, and the sub-merchant
  must be registered. Otherwise the payment is refused, and nothing is recorded.
  The refusals are checked in this order: the app's reference already used (R-5),
  then the stored card, the sub-merchant, the currency, and the amount's maximum,
  and the first refusal is the answer.
- **R-5: One payment per app reference.** A request whose app reference is already
  held, with the same customer, sub-merchant, amount and currency, returns the
  payment already held, answered exactly as when it was recorded. One that differs
  in any of them is refused. Two identical requests at the same moment record one
  payment, and both receive it.
- **R-6: Identity at Acme.** The payment's own id is its `TransactionID` at Acme.
  Its `OrderNumber` is 12 random capital letters and digits, unique across every
  payment. Both are made when the payment is recorded and never change.
- **R-7: Statement name.** The payment keeps the statement name the cardholder will
  see, and the sub-merchant's category code, as they were when it was recorded. The
  statement name is 40 characters, in FNB's layout: every part in capitals, keeping
  only A to Z, 0 to 9 and spaces; positions 1 to 23 are `ZAP*` and the trading name,
  cut to fit and padded with spaces, so the asterisk is at position 4; positions 24
  to 36 are the city, cut to 13 characters and padded; positions 37 and 38 are the
  first two characters of the region code; positions 39 and 40 are the country
  code.
- **R-8: Reading a payment.** Anyone holding a payment's id may read it, by D-2. The
  answer holds only our id, the app's reference, the status, the amount, the
  currency, Acme's response code and client message (empty until Acme answers), and
  the reversed and refunded totals. It never holds personal information. An id that
  names no payment, or is not an id at all, is not found.
- **R-9: Status shown.** The status is one of the eight in D-8. In this Feature it
  is always `Pending`, which means the app should ask again; every other status is
  final.
- **R-10: The card token.** The API never receives a card token from the app: the
  token comes from the customer's stored card. It is stored only encrypted, bound to
  its stored card, and never appears in an answer or a log line.

## Failure modes

> Every way this Feature can fail, what the caller receives, and what recovery
> looks like. A Feature with only a happy path is not planned.

| What goes wrong | What the caller receives | Recovery |
|---|---|---|
| No body, or a body that is not JSON | 400 `BadRequest` | The app fixes the request |
| A field missing, or too long, or the amount below 1 | 400 with the per-field map: `CustomerReferenceRequired`, `CustomerReferenceTooLong`, `SubMerchantReferenceRequired`, `SubMerchantReferenceTooLong`, `AmountBelowMinimum`, `CurrencyCodeRequired`, `CurrencyCodeTooLong`, `AppPaymentReferenceRequired`, `AppPaymentReferenceTooLong` | The app fixes the field the map names |
| The app reference is held, with different details | 400 `AppPaymentReferenceReused` | The app uses a new reference for a new payment |
| The customer is unknown, or holds no stored card | 400 `CustomerHasNoStoredCard` | The customer adds a card first, outside this API |
| The sub-merchant is not registered | 400 `SubMerchantNotRegistered` | Zapper registers the sub-merchant first |
| The currency is not on the list | 400 `CurrencyCodeNotSupported` | The app sends a currency on the list |
| The amount is above the currency's maximum | 400 `AmountAboveMaximum` | The app splits or refuses the payment |
| Two identical requests at the same moment | 201 to both, with the same payment | None needed |
| A new `OrderNumber` collides with a stored one | 500 `Internal`, logged at `Error`; nothing stored | The app sends the same request again, which makes a new number |
| The database cannot be reached | 500 `Internal` | The app sends the same request again; R-5 makes it safe |
| The id names no payment, or is not an id | 404 `NotFound` | None; the app checks the id it holds |
| Too many requests from one address, or a body over 1 MB | 429 `TooManyRequests` or 413 `RequestTooLarge`, from the shared machinery | The app waits and tries again, or sends less |

## Scenarios

> One Gherkin `Feature:` per plan Feature. `Testing.md` section 7 builds the
> acceptance test class from the `Feature:` line and each test from a scenario
> title, so every title begins with a letter. Each scenario is tagged with every
> rule id it proves.

```gherkin
Feature: Recording payments

  Background:
    Given the sub-merchant "ZAP0004182" is registered, trading as "Cafe Del Sol" in "Cape Town", region "WC", country "ZA", category "5812"
    And the customer "CUS-1001" holds a stored card

  @R-1 @R-3 @R-6
  Scenario: App records a payment for a customer with a stored card
    When the app records a payment of 1999 "ZAR" for customer "CUS-1001" at sub-merchant "ZAP0004182" with its reference "APP-1"
    Then the answer is 201 with a payment whose status is "Pending", amount 1999 and currency "ZAR"
    And the answer's location names that payment
    And the stored payment's TransactionID is its id
    And its OrderNumber is 12 capital letters and digits
    And it is due for submission at the moment it was recorded

  @R-8 @R-9
  Scenario: App reads a payment it recorded
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-1001" at sub-merchant "ZAP0004182" with its reference "APP-2"
    When the app reads that payment by its id
    Then the answer is 200 with status "Pending", amount 1999, currency "ZAR", reference "APP-2", no response code, no client message, and reversed and refunded totals of 0

  @R-2
  Scenario: Payment of less than one cent is refused
    When the app records a payment of 0 "ZAR" for customer "CUS-1001" at sub-merchant "ZAP0004182" with its reference "APP-3"
    Then the answer is 400 with "AmountBelowMinimum" against "amount"
    And no payment with the reference "APP-3" is recorded

  @R-2
  Scenario: Payment of more than one million rand is refused
    When the app records a payment of 100000001 "ZAR" for customer "CUS-1001" at sub-merchant "ZAP0004182" with its reference "APP-4"
    Then the answer is 400 with the code "AmountAboveMaximum"
    And no payment with the reference "APP-4" is recorded

  @R-3
  Scenario: Payment in a currency that is not supported is refused
    When the app records a payment of 1999 "JPY" for customer "CUS-1001" at sub-merchant "ZAP0004182" with its reference "APP-5"
    Then the answer is 400 with the code "CurrencyCodeNotSupported"
    And no payment with the reference "APP-5" is recorded

  @R-3
  Scenario: Currency code in lower case is accepted
    When the app records a payment of 1999 "usd" for customer "CUS-1001" at sub-merchant "ZAP0004182" with its reference "APP-6"
    Then the answer is 201 with a payment whose currency is "USD"

  @R-4
  Scenario: Payment for a customer without a stored card is refused
    When the app records a payment of 1999 "ZAR" for customer "CUS-9999" at sub-merchant "ZAP0004182" with its reference "APP-7"
    Then the answer is 400 with the code "CustomerHasNoStoredCard"
    And no payment with the reference "APP-7" is recorded

  @R-4
  Scenario: Payment at an unregistered sub-merchant is refused
    When the app records a payment of 1999 "ZAR" for customer "CUS-1001" at sub-merchant "ZAP9999999" with its reference "APP-8"
    Then the answer is 400 with the code "SubMerchantNotRegistered"
    And no payment with the reference "APP-8" is recorded

  @R-5
  Scenario: Same payment recorded twice returns the first payment
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-1001" at sub-merchant "ZAP0004182" with its reference "APP-9"
    When the app records the same payment again
    Then the answer is 201 with the same id, body and location as the first
    And one payment with the reference "APP-9" is recorded

  @R-5
  Scenario: Reused app reference with a different amount is refused
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-1001" at sub-merchant "ZAP0004182" with its reference "APP-10"
    When the app records a payment of 2500 "ZAR" for customer "CUS-1001" at sub-merchant "ZAP0004182" with its reference "APP-10"
    Then the answer is 400 with the code "AppPaymentReferenceReused"
    And the payment with the reference "APP-10" still has amount 1999

  @R-5
  Scenario: Two identical requests at the same moment record one payment
    When the app sends two identical requests at the same moment to record 1999 "ZAR" for customer "CUS-1001" at sub-merchant "ZAP0004182" with its reference "APP-11"
    Then both answers are 201 with the same id
    And one payment with the reference "APP-11" is recorded

  @R-7
  Scenario: Card acceptor name follows the FNB layout
    When the app records a payment of 1999 "ZAR" for customer "CUS-1001" at sub-merchant "ZAP0004182" with its reference "APP-12"
    Then the stored payment's card acceptor name is "ZAP*CAFE DEL SOL       CAPE TOWN    WCZA"
    And its merchant category code is "5812"

  @R-8
  Scenario: Reading an unknown payment answers not found
    When the app reads a payment by an id that names no payment
    Then the answer is 404 with the code "NotFound"

  @R-8 @R-10
  Scenario: Payment answer carries no personal information
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-1001" at sub-merchant "ZAP0004182" with its reference "APP-13"
    When the app reads that payment by its id
    Then the answer holds exactly the members id, appPaymentReference, status, amount, currencyCode, responseCode, clientMessage, reversedAmount and refundedAmount
    And no member holds the customer reference, the email address, the card token or the sub-merchant's details
```

The references and values are carried as each test's discriminator requires,
by `Testing.md` section 7. The card acceptor name, the `TransactionID`, the
`OrderNumber` and the due time are read through `database`, because no frozen
contract shows them.

## How it is built

- **API:** `Zapper.PaymentsApi`. Two operations in `Features/Payments/`:
  `RecordPayment.cs` (`POST /v1/payments`) and `GetPayment.cs`
  (`GET /v1/payments/{id}`), both `.AllowAnonymous()`, grouped by
  `Features/Payments/PaymentEndpoints.cs` with the tag `payments`; `Endpoints.cs`
  gains one line. `RecordPayment` validates with `RecordPaymentValidator`, which
  checks each text field is present and at most its length (`customerReference`
  50, `subMerchantReference` 15, `currencyCode` 3, `appPaymentReference` 50) and the
  amount is at least 1. Its handler, in this order: loads the payment held under
  the app reference, if any; loads the stored card by customer reference, the
  sub-merchant by its reference, and the currency by the code upper-cased with
  `ToUpperInvariant`; where a payment is held, `CompareRequest` answers 201 with it
  or `AppPaymentReferenceReused`; otherwise answers `CustomerHasNoStoredCard`,
  `SubMerchantNotRegistered` or `CurrencyCodeNotSupported` for a lookup that found
  nothing, then `CanRecordPayment` answers `AmountAboveMaximum`; creates the payment
  and saves once with `SaveChangesWithOutcomeAsync`; on `AlreadyExists` reads the
  payment by app reference again and compares as above; where that read finds
  nothing, the collision was on `OrderNumber`, and it answers 500 `Internal` and
  logs at `Error`. It answers 201 with `TypedResults.Created` and the location
  `/v1/payments/{id}`, and declares `.Produces<RecordPaymentResponse>(201)`,
  `.ProducesValidationProblem()` and `.ProducesProblem(500)`. `GetPayment` projects
  the payment in the query and declares `.Produces<GetPaymentResponse>(200)` and
  `.ProducesProblem(404)`. Each response's `status` is a string, from
  `GetStatus().ToString()`, because a contract never exposes a domain enum; its
  `currencyCode` is the currency row's `Code`.
- **Frontend:** None. This Feature is tech-only.
- **Domain:**
  - `Zapper.Domain/Merchant/SubMerchantEntity.cs`, with `GetCardAcceptorName()`,
    which builds R-7's layout; `ZAP` is a constant there.
  - `Zapper.Domain/Customer/StoredCardEntity.cs`, holding `EncryptedToken`.
  - `Zapper.Domain/Payment/CurrencyTypeEntity.cs`, with the enum `Currency` (`Zar`,
    `Usd`, `Eur`, `Gbp`) and `CanRecordPayment(long amount)`, which returns
    `RecordPaymentResult.AmountAboveMaximum` or `RecordPaymentResult.Allowed`.
  - `Zapper.Domain/Payment/PaymentStateTypeEntity.cs`, with the enum `PaymentState`,
    whose one member here is `Recorded`.
  - `Zapper.Domain/Payment/PaymentEntity.cs`. Its constructor takes the stored card,
    the sub-merchant, the currency, the amount, the app reference and
    `TimeProvider`; it makes the id with `Guid.CreateVersion7()`, makes the
    `OrderNumber` with `RandomNumberGenerator.GetString` over A to Z and 0 to 9,
    copies the card acceptor name and the category code, and sets the state to
    `Recorded`, `RecordedAt` to now, `NextActionAt` to the same moment, and both
    totals to 0. `CompareRequest(storedCard, subMerchant, currency, amount)`
    returns `RecordPaymentResult.AlreadyRecorded` or
    `RecordPaymentResult.AppPaymentReferenceReused`. `GetStatus()` returns
    `PaymentStatus`, the eight values of D-8, and here always `Pending`.
  - `RecordPaymentResult` is in `Zapper.Domain/Payment/RecordPaymentResult.cs`,
    because two classes return it, and holds `Allowed`, `AlreadyRecorded`,
    `AppPaymentReferenceReused` and `AmountAboveMaximum`. `PaymentStatus` is in
    `Zapper.Domain/Payment/PaymentStatus.cs`.
  - The token is encrypted before the stored card is made, with the customer
    reference as the associated data, and the constructor takes the result; the
    card's id cannot be used, because it does not exist until the constructor runs.
- **Data model:** every text column is `character varying` with the length shown;
  every `Id` of an ordinary table is a `uuid` made by its entity and configured
  `ValueGeneratedNever`.
  - `Merchant.SubMerchant`: `Id` (`PK_SubMerchant`); `MerchantReference` 15
    (`UX_SubMerchant_MerchantReference`); `TradingName` 100; `City` 100; `Region`
    50; `CountryCode` 2; `MerchantCategoryCode` 4.
  - `Customer.StoredCard`: `Id` (`PK_StoredCard`); `CustomerReference` 50
    (`UX_StoredCard_CustomerReference`, which enforces one card per customer);
    `EncryptedToken` `bytea`, with no unique index, since two encryptions of one
    token differ; `EmailAddress` 254.
  - `Payment.CurrencyType`, a reference table: `Id` `integer` (`PK_CurrencyType`);
    `Name` 64; `Code` 3 (`UX_CurrencyType_Code`); `Symbol` 5; `MaximumAmount`
    `bigint`. Rows by `HasData`: 1 `Zar` `ZAR` `R` 100000000; 2 `Usd` `USD` `$`
    5000000; 3 `Eur` `EUR` `€` 5000000; 4 `Gbp` `GBP` `£` 4000000.
  - `Payment.PaymentStateType`, a reference table: `Id` `integer`
    (`PK_PaymentStateType`); `Name` 64. One row: 1 `Recorded`.
  - `Payment.Payment`: `Id` (`PK_Payment`); `AppPaymentReference` 50
    (`UX_Payment_AppPaymentReference`); `StoredCardId` (`FK_Payment_StoredCardId`,
    `IX_Payment_StoredCardId`); `SubMerchantId` (`FK_Payment_SubMerchantId`,
    `IX_Payment_SubMerchantId`); `CurrencyTypeId` `integer`
    (`FK_Payment_CurrencyTypeId`, `IX_Payment_CurrencyTypeId`); `Amount` `bigint`;
    `OrderNumber` 12 (`UX_Payment_OrderNumber`); `CardAcceptorName` 40, its padding
    kept; `MerchantCategoryCode` 4; `PaymentStateTypeId` `integer`
    (`FK_Payment_PaymentStateTypeId`, `IX_Payment_PaymentStateTypeId`); `RecordedAt`
    `timestamp with time zone`; `NextActionAt` `timestamp with time zone`,
    nullable; `ResponseCode` 3, nullable; `ClientMessage` 100, nullable;
    `ReversedAmount` `bigint`; `RefundedAmount` `bigint`. The response columns'
    lengths are Appendix A's maximums. No check constraint: the validator and the
    entities already guarantee the amount and the currency.
  - Left to the Feature that first writes them: the changes, events, interaction
    records and notifications; Acme's outcome columns; the attempt and
    unexpected-failure counts; the due-time index; and `xmin`.
  - The error message table gains one row for each of the fourteen codes, with
    this detail: `CustomerReferenceRequired` "A customer reference is required.";
    `CustomerReferenceTooLong` "A customer reference is at most 50 characters.";
    `SubMerchantReferenceRequired` "A sub-merchant reference is required.";
    `SubMerchantReferenceTooLong` "A sub-merchant reference is at most 15
    characters."; `AmountBelowMinimum` "The amount must be at least 1 in the
    currency's lowest unit."; `CurrencyCodeRequired` "A currency code is
    required."; `CurrencyCodeTooLong` "A currency code is 3 letters.";
    `AppPaymentReferenceRequired` "A payment reference is required.";
    `AppPaymentReferenceTooLong` "A payment reference is at most 50 characters.";
    `AppPaymentReferenceReused` "That payment reference is already used for a
    different payment."; `CustomerHasNoStoredCard` "That customer has no stored
    card."; `SubMerchantNotRegistered` "That sub-merchant is not registered.";
    `CurrencyCodeNotSupported` "That currency is not supported.";
    `AmountAboveMaximum` "The amount is above the maximum for its currency."
- **Protection level of each field:** `Merchant.SubMerchant` holds data about a
  business and is at no level. `Customer.StoredCard`: `CustomerReference` and
  `EmailAddress` are personal information; the token is sensitive data, as bank
  details, and `EncryptedToken` is the one column stored encrypted, by D-11.
  `Payment.Payment` is personal information, because it records what an
  identifiable customer bought, where and when, linked through `StoredCardId`; the
  read answer leaves out every link to the customer, which is why D-2 can make it
  public. The reference tables are at no level. Every response already carries
  `Cache-Control: no-store`. The `OrderNumber`, about 62 bits, is a reference and
  not a value that must not be guessed, and the payment id is not a secret, since
  the read answer identifies no one. Retention is D-9's: a payment 5 years after
  it, a stored card while the customer keeps it and de-identified within one day of
  its removal. No operation in this plan corrects, restricts, produces or
  de-identifies personal information: an administrator changes the database
  directly, and SOLUTION.md names those operations as production work.
- **Database privileges:** `ZapperPaymentsApiRole` is granted `USAGE` on the
  schemas `Merchant`, `Customer` and `Payment`; `SELECT` on `SubMerchant`,
  `StoredCard`, `CurrencyType` and `PaymentStateType`; and `SELECT` and `INSERT` on
  `Payment`. `UPDATE` on `Payment` comes with Submitting Payments to Acme. No delete
  and no schema change. The Migrator's role owns the tables and runs the seed.
- **Addresses it calls:** None. Nothing in this Feature leaves the process except
  the database connection.
- **Slow or costly operations:** None. Recording is a few indexed reads and one
  insert, and reading is one indexed read.
- **Infrastructure inventory:** The "Highest protection level" of the part
  `Zapper.PaymentsApi` and of its connection to `ZapperDb` becomes "Sensitive data",
  since both carry the encrypted card token. No part or connection is added.
- **Names the standards leave to planning:** the schemas `Merchant`, `Customer` and
  `Payment`; the entities, enums and methods under Domain above;
  `Zapper.ApiCommon/Managers/Cryptography/CryptographyManager.cs` and
  `ICryptographyManager.cs`, with `Encrypt(string plainText, ReadOnlySpan<byte>
  key, ReadOnlySpan<byte> associatedData)` and `Decrypt(ReadOnlySpan<byte>
  cipherText, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData)`; the
  stored form, one version byte (1), a 12-byte nonce, the ciphertext and a 16-byte
  tag; `Zapper.ApiCommon/Managers/Cryptography/TokenEncryptionOptions.cs`, binding
  `Zapper:TokenEncryption`, whose one member `Key` is `[Required]`, with
  `TokenEncryptionValidateOptions` checking it decodes to exactly 32 bytes, bound
  in `ManagerRegistration` without `ValidateOnStart` (A5.2), and supplied locally
  by the file `Zapper__TokenEncryption__Key`; `Zapper.Migrator/Seeding/LocalSeeder.cs`
  with `SeedAsync`.
- **Decisions for this Feature only:**
  - The read answer's four Acme fields exist from this Feature, so the read
    projects every member from the row and never changes shape; the due time
    exists from this Feature, so no payment recorded before Submitting Payments to
    Acme needs it filled in later. Everything else is added by the Feature that
    first writes it (`BackendCode.md` section 20).
  - The currency code is matched after `ToUpperInvariant`, never `ToUpper`, which
    follows the server's culture.
  - No currency, stored card or sub-merchant is ever deleted; a foreign key refuses
    it.
  - The `OrderNumber` collision answers 500 rather than retrying inside the
    request, which would be a second save. At 100 million payments a collision has
    about two chances in a hundred billion per payment.
  - The seed in this Feature writes the sub-merchants and stored cards of D-12;
    finished payments are seeded from SQL Daily Summary on. The API does not need
    the card key in this Feature, because nothing it does encrypts or decrypts; it
    gains the key-per-file source and reads the key in its startup check in
    Submitting Payments to Acme.
- **Tests:**
  - C# unit: `SubMerchantTests` (every part of R-7's layout, cutting, padding,
    capitals and dropped characters); `PaymentTests` (what the constructor sets,
    each answer of `CompareRequest`, `GetStatus`); `CurrencyTypeTests`
    (`CanRecordPayment` at the maximum and one above it); `CryptographyManagerTests`
    (a value round-trips, starts with version 1 and encrypts differently each time;
    decrypting fails on one changed byte, wrong associated data, a wrong key and an
    unknown version).
  - API contract: `PaymentsApiV1ContractTests` covers the two new fragments.
  - Database integration: `RecordPaymentIntegrationTests` (every code, the replay,
    the reuse, two identical requests at once recording one); `GetPaymentIntegrationTests`
    (found, unknown, malformed); `SubMerchantPersistenceIntegrationTests`,
    `StoredCardPersistenceIntegrationTests` and `PaymentPersistenceIntegrationTests`
    (each unique index and foreign key, and `SaveOutcome.AlreadyExists`). The
    migration, naming, reference table and error code tests cover the new tables,
    rows and codes as they stand.
  - Acceptance: `RecordingPaymentsAcceptanceTests`, one test per scenario. Stored
    cards and sub-merchants are arranged through `database`, because no operation
    creates them; the stored card's token is encrypted with the manager and a key
    made for the run.
  - Authorization: no sweep case, because both operations are public.
  - Load: none, because neither operation is slow or costly.
  - Not tested: the `OrderNumber` collision path, because a test cannot choose the
    random number; the seed, which End to End validation checks proves against the
    running stack.

## Contracts

> One row per operation this Feature adds. Each is two files, frozen when this
> plan was approved. They wait under `ai/Plans/<Plan_Title>/Contracts/`, and
> `feature-execution` moves them to the same path under `Docs/Contracts/` in
> this Feature's first increment. The table gives the path they are moved to.
> An operation that changes a frozen contract is a new version, so a new path.

| Operation | Contract |
|---|---|
| `POST /v1/payments` | `Docs/Contracts/PaymentsApi/v1/payments/post.md` |
| `GET /v1/payments/{id}` | `Docs/Contracts/PaymentsApi/v1/payments/{id}/get.md` |

## Implementation order

> Numbered, in the order they are built. `feature-execution` commits each as
> `<Feature_Title>_<n>/<total>`, per `Branching.md` section 4. Increment 1 is
> always the failing acceptance tests. Each increment says what it delivers and
> what makes it reviewable on its own.

1. **Failing acceptance tests.** One per scenario above, fourteen, failing. The two
   contracts move from `ai/Plans/Zapper_Team_Lead_Assement/Contracts/` to
   `Docs/Contracts/`, so their contract tests fail too until the operations exist.
2. **Cryptography.** `CryptographyManager` and its interface, `ManagerRegistration`
   called by `AddZapperApiCommon`, and `TokenEncryptionOptions` with its validator,
   bound without `ValidateOnStart`; `CryptographyManagerTests`; the cryptographic
   inventory's row for the card token key (AES-256-GCM; used only by
   `CryptographyManager`; protects only `Customer.StoredCard.EncryptedToken`).
   Reviewable on its own: it touches no table, and its unit tests prove the whole
   stored form.
3. **Schema and Domain.** The five entities and their configurations, the two
   reference tables with their rows, `SaveChangesWithOutcomeAsync` and `SaveOutcome`
   with `Saved` and `AlreadyExists`, and one migration holding the grants; the
   Domain unit tests and the three persistence test classes. Reviewable on its own:
   the migration, naming and reference table tests check it as well.
4. **Seed.** The Migrator references ApiCommon, calls `AddZapperManagers`, adds the
   key-per-file source (its `Versions.md` row with it), and runs on the
   `aspnet:10.0.13-noble-chiseled` image; its `Dockerfile` takes the build
   configuration, Release by default, and `compose.yml` builds it as Debug; the
   init service generates the card key into the secrets volume, mounted read-only
   into the Migrator only; `LocalSeeder` writes the sub-merchants and stored cards
   of D-12. Reviewable on its own: `docker compose up` run twice leaves the seeded
   rows once.
5. **`RecordPayment`.** The validator, the handler, the endpoint group, the fourteen
   codes in `ErrorMessageCode` with their message rows in a migration,
   `RecordPaymentIntegrationTests`, and the infrastructure inventory change.
   Reviewable on its own: the create contract test and every acceptance test that
   only records a payment pass.
6. **`GetPayment`.** The handler and `GetPaymentIntegrationTests`. Reviewable on its
   own: every acceptance test and every contract test passes.
7. **README and SOLUTION.md.** The README says how to run the stack and record and
   read a payment with the seeded data. SOLUTION.md gains this Feature's facts: the
   data model and the currency table, the encryption and its 12-month replacement,
   one stored card per customer, the public operations and production sign-in,
   polling now and SignalR in production, and what is not built for retention and
   correction.
