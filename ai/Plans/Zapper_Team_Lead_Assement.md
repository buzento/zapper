- **Status:** Approved 2026-10-24
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
  summary as a SQL query, and the data reader role that runs it; a stub of Acme; an
  end-to-end script run in CI; the README and SOLUTION.md, with the AWS approach
  described in SOLUTION.md.
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
  reference, the status, the amount and currency, the authorised amount, Acme's
  response code and its client message, and the reversed and refunded totals: never
  the customer
  reference, the email address, the card token, the sub-merchant's details or
  Acme's internal references. The rate limit per network address is the only guard
  on recording. Cost: anyone can record a payment for any customer reference they
  know. SOLUTION.md describes production: the app signs in with a token issued to
  the Zapper user, each payment belongs to that user's customer, reading another
  customer's payment answers 404, and recording takes the customer from the token.
  The authorised amount was added to both waiting contracts by Francois on
  2026-10-24. **Affects:** Recording payments, End to End validation checks, and
  every later plan that adds app sign-in.

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
  is set once, by the first final answer of the worker's call or a query; an answer
  is final only with a response code and a client message that is not empty, and
  an approval also with the authorised amount. A Payment Advice never sets the
  outcome, because it carries neither the client message nor the authorised amount:
  it confirms or contradicts an outcome already set, and one that arrives first is kept and compared when the worker's next answer sets the outcome. Later answers that agree confirm it, recorded as changes;
  a later answer that disagrees is never applied, and makes the payment
  `Unresolved`, logged at `Error`. The authorised amount is authoritative: it is
  what was taken from the card, and it is recorded and settled whether it is below
  or above the amount sent. `Success` with response code `00` is approved;
  `Success` with any other code is declined; `Failure` is failed. `Authorize` and
  `Capture` advices are recorded and do not set the outcome (an assumption for
  SOLUTION.md). Reversals and refunds are events of the payment, applied by rules:
  kept and applied once the payment is approved; against a payment not approved, a
  contradiction; refunds may be partial and several, and one that would pass the
  authorised amount, counting any reversal, is kept, not applied, and makes the
  payment `Unresolved`; a reversal is the full authorised amount, a second with the
  same `transactionUUID` is a duplicate, and any
  other second one is a contradiction; one with `isApproved` false or a
  `txResultCode` other than `00` is recorded and never applied. A reversal or refund
  whose currency differs from its payment's is a contradiction. A notification
  matching no payment is kept, answered 200, and logged at `Error`. Every change is
  recorded as a change of the payment: the state before and after, its source, the
  notification or call it came from, and when. The app sees eight statuses:
  `Pending` (not final), `Approved`, `Declined`, `Failed`, `Reversed`, `Refunded`,
  `PartiallyRefunded` and `Unresolved` (needs a person, and final for the app).
  The finer internal states, such as submitting and outcome unknown, all show as
  `Pending`. Changed by Francois on 2026-10-24: a Payment Advice no longer sets the outcome, and the authorised amount is authoritative. Changed by Francois on 2026-10-24: a notification never changes when the worker next calls Acme, because that time is also the worker's lease; `Complete`, `Reversed` and `Refunded` on an answer are read like `Success`, by the response code, as Appendix A states. **Affects:** Recording payments, Submitting Payments to Acme, Acme
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
  (€, 5 000 000) and GBP (£, 4 000 000). The app may leave the currency out, and ZAR is used, as Acme does when `CurrencyCode` is omitted. A currency sent is compared without regard to case. It is always stored, sent to Acme and answered in capitals. There is no conversion: each currency is recorded and settled on its
  own. Assumptions for SOLUTION.md: Acme accepts every code on our list, which
  production would confirm with Acme; a South African merchant normally charges in
  rand and the cardholder's bank converts, so a foreign currency here is a merchant
  pricing in it; a currency cannot be withdrawn, because its row cannot be deleted
  while payments use it, and stopping new payments in it would need a flag on the
  row, added when asked for. The ZAR-only ruling made earlier in this planning session was withdrawn by Francois on 2026-10-23. Changed by Francois on 2026-10-24: the currency is optional and defaults to ZAR, as he had asked, because the brief's request names only the customer, the sub-merchant, the amount and the app's reference. **Affects:** Recording payments,
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
  two days before the first run, in every final status, across two sub-merchants
  and two currencies, so the summary shows data on the first run. No seeded payment
  is `Pending`: a payment recorded on an earlier day is made `Unresolved` by the
  worker at once (Submitting Payments to Acme R-8), so `Pending` is shown by a
  payment recorded live. Each seeded payment has a fixed app reference, and the
  seed writes it only where no payment holds that reference, so the seeded payments
  stay on the two days of the first run. Considered and rejected by Francois: a
  seed tool project, and an exception recorded against `Security.md` section 13.
  Changed by Francois on 2026-10-24: every final status rather than every status,
  and fixed references. **Standard change:** A4, ruled by Francois on 2026-10-23.
  **Affects:** Recording payments, SQL Daily Summary, End to End validation checks.

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
  are triggered through stub-only routes under `/stub/`, which need the stub routes' own key. The init service generates it with `openssl rand -hex 32` into the secrets volume, mounted read-only into `AcmeStub` only, and writes a copy to `Code/Certificates/AcmeStubRoutes.key`, which git ignores, for the end-to-end script. The stub publishes `127.0.0.1:${ACME_STUB_PORT:-8444}:8443`, so its port comes from the environment with 8444 as the default, and its server certificate names `AcmeStub`, `localhost`, `127.0.0.1` and `::1`. The README says to set `ACME_STUB_PORT` for both `docker compose up` and the script when 8444 is taken. Every answer of the stub that carries a response code carries its client message, from Appendix A's table of codes. Changed by Francois on 2026-10-24. Every response and notification carries one member Appendix A does not
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
  stack: it reads the stub routes' key from `Code/Certificates/AcmeStubRoutes.key`, trusts `ca.crt` with `--cacert`, walks
  every case in the brief with the seeded cards and the stub's routes, polls each
  payment until its status is final, and fails when any case does not end in its
  expected status. It calls API endpoints only, our API's and the stub's routes, and
  never reads the database, because it is for Zapper's reviewers to run against the
  API (ruled by Francois on 2026-10-24); the stored records behind each case are
  proven by the acceptance tests. CI runs it (ruled by Francois): a step brings the stack up and
  runs the script on every pull request and every push to `dev`, `test` and `main`.
  The README says how to run it, and Francois's video uses it. Considered and
  rejected: testing the client against the stub, which proves only that two readings
  of Appendix A agree; a Postman collection, rejected by Francois because the
  generated secrets and the waiting are simpler in a script. **Standard changes:**
  A8.1 to A8.4, ruled by Francois on 2026-10-23. **Affects:** Submitting Payments to
  Acme, Acme Notifications, End to End validation checks.

- **D-16: The daily summary is a SQL file, run in a database client.** An endpoint
  would be public by D-2, and a date key and an unpaged list fit the standards
  badly, while settlement is back office work. One file,
  `Docs/Reports/DailyPaymentSummary.sql`, is one plain `SELECT` with the date
  written once at its top. A person opens it in pgAdmin, or any other client, signed
  in as the data reader of D-18, sets the date, reads it through and runs it, and
  the client shows the rows as usual. The README shows connecting pgAdmin and
  running the file; SOLUTION.md says production runs it on a read replica. Its rules
  are SQL Daily Summary's: a date is a South African calendar day, 00:00 to 24:00
  SAST (UTC+2); approvals and declines count by Acme's `TransactionDateTime`,
  reversals and refunds by their own `transactionDateTime`; the approved value is
  the authorised amount, the declined value the requested amount, reversal and
  refund values the event's amount; only applied events count; an approval and its
  reversal each count on their own day; rows are grouped by sub-merchant (Zapper's
  reference and trading name) and currency. Beside the brief's columns, Francois
  asked for Pending, Failed and Unresolved, each by the time we recorded the
  payment, each with a count and a value. Its scenarios are proven by acceptance
  tests that run that exact file, with only its date line replaced, as the data
  reader, over data arranged through the entities; no standard changes for it
  (ruled by Francois on 2026-10-24). Changed by Francois on 2026-10-24: the date is
  written in the file rather than given as a `psql` variable, so the file runs in
  pgAdmin; production's `SELECT`-only reporting role is the data reader of D-18.
  Changed by Francois on 2026-10-24: the end-to-end script calls API endpoints only,
  so it does not run the file. **Affects:** SQL Daily Summary.

- **D-17: How the Features after Recording payments are delivered.** End to End
  validation checks' first increment is the end-to-end script with every case
  written and run against the stack, since its code is outside the standards (ruled
  by Francois): a case fails only where it needs that Feature's own work, and the
  increment's report lists the cases already passing because of the Features before
  it. Changed by Francois on 2026-10-24, because every behaviour the script checks is
  built by then.
  Each Feature writes its own part of README.md and SOLUTION.md in its last
  increment. SOLUTION.md's sections on the AI workflow and on validation are in
  Francois's own words, from a list of facts each Feature supplies. End to End validation checks' last increment writes SOLUTION.md's "Assumptions", "Design and key trade-offs", "Data model" and "AWS deployment approach" in full, from the facts each Feature recorded, against the brief's lists. The AI workflow section names `CLAUDE.md`, `Claude/Skills/`, `Claude/Standards/` and `Claude/Templates/`, and what each holds. Changed by Francois on 2026-10-24. Session exports
  go to `ai/`, and the brief asks for the sessions that build the Acme client, so
  Submitting Payments to Acme exports them. **Affects:** every Feature in this plan.

- **D-18: A data reader role reads every table and changes none.** A person reading
  the database, for the daily summary or to investigate a payment, signs in as
  `ZapperDataReaderRole`, never with the Migrator's or an API's credential. Locally
  `compose.yml` creates it with `LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE` and
  issues its client certificate, beside the Migrator's, into `Code/Certificates/`,
  which pgAdmin is given. It is granted `USAGE` on every schema and `SELECT` on
  every table, the `Customer` schema included (ruled by Francois), by a migration;
  and default privileges, set once by the role that runs the migrations, give it
  `SELECT` on every table and `USAGE` on every schema created later, so no later
  plan or migration grants it anything. It never gains `INSERT`, `UPDATE`,
  `DELETE` or a schema change. Its connection carries the email address and the
  encrypted card token, so it is at "Sensitive data"; it never gets the card key.
  SOLUTION.md says that in production it is a group role with no login on a read
  replica: each person signs in as themselves, with an IAM database account and a
  second factor (`Security.md` section 11), as a member of it, and reading the
  `Customer` schema is limited by who is made a member. Considered and rejected:
  naming the role in every table-creating migration, which would need a standard
  change and is easy to forget; no reporting role, which leaves a person only a
  service's credential. **Affects:** SQL Daily Summary, every later plan that adds
  a table.

# Features

In the order they are built. A Feature is built only after every Feature above
it, on its own branch and with its own pull request, and counts as built once
that pull request is merged. Every Feature title, once converted, differs from
the plan's and from every other Feature's in every plan, because
`feature-execution` finds a Feature's pull request by its branch name.

| # | Feature | Planned |
|---|---|---|
| 1 | Recording payments | 2026-10-24 |
| 2 | Submitting Payments to Acme | 2026-10-24 |
| 3 | Acme Notifications | 2026-10-24 |
| 4 | SQL Daily Summary | 2026-10-24 |
| 5 | End to End validation checks | 2026-10-24 |

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
  reference, the sub-merchant's reference, the amount and its own reference for the payment, and may give the currency. The payment is recorded as `Pending` and is due for
  submission to Acme at once. In this Feature it stays `Pending`.
- **R-2: The amount.** The amount is a whole number of the currency's lowest unit,
  at least 1 and at most that currency's maximum: R1 000 000 for ZAR, $50 000 for
  USD, €50 000 for EUR and £40 000 for GBP. An amount outside that range is refused,
  and nothing is recorded.
- **R-3: The currency.** A payment with no currency is in ZAR. The currency must be one on Zapper's list: ZAR, USD, EUR or
  GBP. Its code is compared without regard to case, and is always stored, sent to
  Acme and answered in capitals. A currency not on the list is refused, and nothing
  is recorded. No amount is converted between currencies.
- **R-4: Who and where.** The customer must hold a stored card, and the sub-merchant
  must be registered. Otherwise the payment is refused, and nothing is recorded.
  The refusals are checked in this order: the app's reference already used (R-5),
  then the stored card, the sub-merchant, the currency, and the amount's maximum,
  and the first refusal is the answer.
- **R-5: One payment per app reference.** A request whose app reference is already
  held, with the same customer, sub-merchant, amount and currency, a missing currency being ZAR, returns the
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
  currency, the authorised amount, Acme's response code and client message (empty
  until Acme answers), and
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
| A field missing, or too long, or the amount below 1 | 400 with the per-field map: `CustomerReferenceRequired`, `CustomerReferenceTooLong`, `SubMerchantReferenceRequired`, `SubMerchantReferenceTooLong`, `AmountBelowMinimum`, `CurrencyCodeTooLong`, `AppPaymentReferenceRequired`, `AppPaymentReferenceTooLong` | The app fixes the field the map names |
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
    Then the answer is 200 with status "Pending", amount 1999, currency "ZAR", reference "APP-2", no authorised amount, no response code, no client message, and reversed and refunded totals of 0

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

  @R-3
  Scenario: Payment without a currency is recorded in rand
    When the app records a payment of 1999 with no currency for customer "CUS-1001" at sub-merchant "ZAP0004182" with its reference "APP-14"
    Then the answer is 201 with a payment whose currency is "ZAR"

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
    Then the answer holds exactly the members id, appPaymentReference, status, amount, currencyCode, authorisedAmount, responseCode, clientMessage, reversedAmount and refundedAmount
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
  50, `subMerchantReference` 15, `currencyCode` at most 3 where sent, `appPaymentReference` 50) and the
  amount is at least 1. Its handler, in this order: loads the payment held under
  the app reference, if any; loads the stored card by customer reference, the
  sub-merchant by its reference, and the currency by the code upper-cased with `ToUpperInvariant`, or `ZAR` where none is sent (the request's `CurrencyCode` is `string?` with the default `null`); where a payment is held, `CompareRequest` answers 201 with it
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
  `currencyCode` is the currency row's `Code`; its `authorisedAmount` is the
  payment's `AuthorisedAmount`.
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
    `AuthorisedAmount` `bigint`, nullable; `ReversedAmount` `bigint`;
    `RefundedAmount` `bigint`. The response columns'
    lengths are Appendix A's maximums. No check constraint: the validator and the
    entities already guarantee the amount and the currency.
  - Left to the Feature that first writes them: the changes, events, interaction
    records and notifications; Acme's outcome columns; the attempt and
    unexpected-failure counts; the due-time index; and `xmin`.
  - The error message table gains one row for each of the thirteen codes, with
    this detail: `CustomerReferenceRequired` "A customer reference is required.";
    `CustomerReferenceTooLong` "A customer reference is at most 50 characters.";
    `SubMerchantReferenceRequired` "A sub-merchant reference is required.";
    `SubMerchantReferenceTooLong` "A sub-merchant reference is at most 15
    characters."; `AmountBelowMinimum` "The amount must be at least 1 in the
    currency's lowest unit.";  `CurrencyCodeTooLong` "A currency code is 3 letters.";
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
  - The read answer's five Acme fields (the authorised amount, the response code,
    the client message and the two totals) exist from this Feature, so the read
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
  - Increment 1 writes each acceptance test with its When and its Then on the
    API's answer, because the entities that arrange the Background, and the reads
    through `database`, do not exist before increment 3. Increment 3 adds the
    Background's arrangement and the reads through `database` to those tests, and
    the closing report names each test it changes. Ruled by Francois on 2026-10-24.
  - The values the seed writes for the sub-merchants and the stored cards are
    synthetic, chosen in increment 4 and written into this section then, and the
    closing report lists them. Ruled by Francois on 2026-10-24.
  - The seed's values, chosen in increment 4 by that ruling. Sub-merchants, as
    reference, trading name, city, region, country and category code:
    `ZAP1000001` "Harbour Lane Bakery", "Cape Town", `WC`, `ZA`, `5462`;
    `ZAP1000002` "Karoo Mountain Coffee Roastery", "Durban", `KZN`, `ZA`, `5814`,
    the trading name past 23 characters; `ZAP1000003` "Fynbos Books", "Port
    Elizabeth North", `EC`, `ZA`, `5942`, the city past 13. Stored cards, as
    customer reference, email address and token, one per behaviour of the stub in
    D-14's order: `CUS-APPROVE`, `approve@example.test`, `tok_test_approve`;
    `CUS-DECLINE-05`, `decline-05@example.test`, `tok_test_decline_05`;
    `CUS-DECLINE-51`, `decline-51@example.test`, `tok_test_decline_51`;
    `CUS-DECLINE-54`, `decline-54@example.test`, `tok_test_decline_54`;
    `CUS-ADVICE-TWICE`, `advice-twice@example.test`, `tok_test_advice_twice`;
    `CUS-REVERSAL-FIRST`, `reversal-first@example.test`, `tok_test_reversal_first`;
    `CUS-LATE-ANSWER`, `late-answer@example.test`, `tok_test_late_answer`;
    `CUS-NEVER-RECEIVED`, `never-received@example.test`, `tok_test_never_received`;
    `CUS-GATEWAY-ERROR-ONCE`, `gateway-error-once@example.test`,
    `tok_test_gateway_error_once`. The key-per-file source reads `/secrets`, where
    `compose.yml` mounts the secrets volume.
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

1. **Failing acceptance tests.** One per scenario above, fifteen, failing. The two
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
5. **`RecordPayment`.** The validator, the handler, the endpoint group, the thirteen codes in `ErrorMessageCode` with their message rows in a migration,
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

# Feature: Submitting Payments to Acme

## What it delivers

A recorded payment is sent to Acme by a worker inside `Zapper.PaymentsApi`, never
during the app's request, and reaches a final state the app reads with
`GET /v1/payments/{id}`: `Approved`, `Declined`, `Failed` or `Unresolved`. Every
answer Acme can give is read as D-3 states: an unknown outcome is asked about
before anything is sent again, a payment is sent again only after Acme says it
never received it, and a payment that cannot be settled in 24 hours waits for a
person. Every call to Acme and every change of a payment is recorded. The Acme
client, the worker, and the stub's payment and query routes are in place, so
`docker compose up` takes each seeded card to its expected status. Acme
Notifications depends on all of it.

## Rules

- **R-1: Sending.** A payment that is due is sent to Acme by the background
  service, never during the app's request. The send carries the payment's id as
  `TransactionID`, its `OrderNumber`, the amount, the currency in capitals,
  `CardNotPresent`, the stored card's token, `CIT`, the sub-merchant's reference,
  the category code and statement name kept when the payment was recorded, and the
  customer's email address, with a new `MessageID`, the `MessageDateTime` in South
  African time to the millisecond with no offset, and the channel `App`.
- **R-2: A final answer.** An answer is final only with a response code and a client message that is not empty, and on an approval also the authorised amount. By Appendix A the outcome is in the response code, so `Success`, `Complete`, `Reversed` and `Refunded` are read alike: code `00` is approved, any other code declined. A status other than `Success` is logged at `Warning` and kept in the call record. A reversal or refund itself is applied only from Acme Switch's notification, which alone carries the event's amount and identity.
  `Success` with response code `00` makes the payment `Approved`; `Success` with any
  other code makes it `Declined`; `Failure` makes it `Failed`. Decisions are made on
  the response code, never on the client message. Acme's response code, client
  message, description, source, authorisation code, authorised amount,
  reconciliation id, retrieval reference number and transaction time are kept. The
  authorised amount is authoritative, being what was taken from the card: an
  approval below the amount sent is approved and logged at `Information`; one above
  it is approved and logged at `Error`, because the customer is owed a refund. A
  decline or a failure with no authorised amount records 0; a decline carrying one
  above 0 is recorded as sent, stays `Declined`, and is logged at `Error`. Changed by
  Francois on 2026-10-24.
- **R-3: Outcome unknown.** A timeout, a broken connection, a 500 to 504, an answer
  that cannot be read (which includes a 200 with no response code, an empty client
  message, or an approval with no authorised amount), any other transaction status,
  or a 409 naming this payment's
  own `TransactionID` means the outcome is unknown. Acme is asked by query 5 seconds
  later, and nothing is sent again before a query answers that Acme does not hold
  the payment.
- **R-4: Query spacing.** While a query answers `Processing`, times out or answers
  500 to 504, the gap before the next query doubles, from 5 seconds to at most 5
  minutes.
- **R-5: Sending again.** A payment is sent again only after a query answers 404,
  and always with the same `TransactionID` and `OrderNumber`. After 3 sends that
  each ended in a 404, the payment is `Failed` as not submitted.
- **R-6: Rate limited.** After a 429 the same request is sent again once
  `Retry-After` has passed, or 1 second where the header is missing. A refused send
  does not count as a send.
- **R-7: Refused.** On a send, a 400, a 401, or a 409 naming another
  `TransactionID` makes the payment `Failed`, logged at `Error`. On a query, a 400
  or a 401 makes the payment `Unresolved`, logged at `Error`, because the send
  before it may have reached the acquirer.
- **R-8: 24 hours.** A payment with no final answer 24 hours after it was recorded
  is `Unresolved`, logged at `Error`.
- **R-9: One payment, one worker.** Only one worker works on a payment at a time,
  so a payment recorded twice with the same app reference is sent to Acme once.
- **R-10: Every call is recorded.** Every call to Acme is recorded with the request
  as built, the token and the email address replaced by a fixed marker, the stored
  card named by its id, the answer as received, how the call ended, when it started,
  how long it took, the attempt and the trace id. No log line holds a token, an
  email address or a card number.
- **R-11: Every change is recorded.** Every change of a payment's state, and every
  final answer that confirms its state, is recorded with the state before and after,
  its source, the call it came from and when. An answer that leaves the payment as
  it was, such as `Processing`, leaves a call record and no change.
- **R-12: Unexpected failures.** A piece of work that fails in a way nobody planned
  for is tried again after 1, 5, then 15 minutes. After five such failures in a row,
  Acme is asked once by query: a 404 makes the payment `Failed`; a final answer is
  applied as R-2 states; anything else, or a sixth failure, makes it `Unresolved`.
  Each is logged at `Error`.
- **R-13: Circuit open.** While the client's circuit is open, no call is made, no
  call is recorded, and the payment waits until the circuit closes.
- **R-14: What the app sees.** `Recorded` and `OutcomeUnknown` show as `Pending`;
  every other state shows as its own status. The response code and client message
  are Acme's, from its final answer, and stay empty where Acme gave none, such as a
  payment not submitted. `Failed` means no money moved; `Unresolved` means it is not
  known whether money moved, and a person settles it with Acme.

Who may see each record: the app reads a payment as Recording payments R-8
states. The call records and the change records are read by no operation; an
administrator reads them in the database. No field in this Feature is read or
changed by a caller. Retention is D-9's: the call records and the change records
are kept for 5 years after the payment, with it.

## Failure modes

| What goes wrong | What the caller receives | Recovery |
|---|---|---|
| Acme answers within 35 seconds with a final answer | The app reads `Approved`, `Declined` or `Failed` | None needed |
| Acme does not answer within 35 seconds, the connection breaks, or Acme answers 500 to 504 | The app reads `Pending` | The worker asks by query, then sends again only on a 404 (R-3 to R-5) |
| Acme answers 429 | The app reads `Pending` | Sent again after `Retry-After` (R-6) |
| Acme refuses the send with 400 `E01` or 401 `E02` | The app reads `Failed`, with Acme's code and client message | A person fixes our request or our key; logged at `Error` |
| Acme answers 409 naming another `TransactionID` | The app reads `Failed` | A person investigates the duplicated `OrderNumber`; logged at `Error` |
| A query is refused with 400 or 401 | The app reads `Unresolved` | A person checks the payment with Acme; logged at `Error` |
| Acme authorises less or more than was sent | The app reads `Approved`, with the authorised amount | Below: none; above: a person refunds the customer, logged at `Error` |
| Acme answers `Complete`, `Reversed` or `Refunded` | The app reads `Approved` or `Declined` by the response code, then the status Acme Switch's notification gives | A reversal or refund is applied only from Acme Switch; logged at `Warning` |
| Acme answers with no response code, an empty client message, or an approval with no authorised amount | The app reads `Pending` | The worker asks by query (R-3) |
| Acme never received the payment three times | The app reads `Failed`, with no response code | The customer pays again; logged at `Warning` |
| No final answer after 24 hours | The app reads `Unresolved` | A person checks the payment with Acme; logged at `Error` |
| The circuit is open because Acme is failing | The app reads `Pending` | The payment waits until the circuit closes |
| Our own code fails five times in a row | The app reads `Failed` or the answer Acme gives, or `Unresolved` | Acme is asked once (R-12); logged at `Error` |
| A save finds the payment changed by another writer | The app reads the payment as it is | The worker reloads, asks the Domain again and saves, up to three times, then leaves it to its lease |
| The host stops while work is in flight | The app reads `Pending` | Claimed work finishes within the shutdown timeout; a killed host's work returns when its lease lapses |
| The card key or the Acme settings are missing on a host with the worker on | No host starts | The environment supplies them |

## Scenarios

```gherkin
Feature: Submitting Payments to Acme

  Background:
    Given the sub-merchant "ZAP0004182" is registered, trading as "Cafe Del Sol" in "Cape Town", region "WC", country "ZA", category "5812"
    And the customer "CUS-2001" holds a stored card with an email address

  @R-1 @R-2 @R-14
  Scenario: Payment approved by the acquirer is shown as approved
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-1"
    And Acme will answer the send with "Success" and response code "00"
    When the clock moves on until the worker has sent the payment
    Then the app reads status "Approved", response code "00" and client message "Approved."
    And Acme was sent the payment's id as TransactionID, its OrderNumber, 1999 "ZAR", the customer's token, "CIT", "ZAP0004182", "5812", its card acceptor name and the customer's email address

  @R-2
  Scenario: Payment declined by the acquirer is shown as declined
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-2"
    And Acme will answer the send with "Success" and response code "51"
    When the clock moves on until the worker has sent the payment
    Then the app reads status "Declined" and response code "51"

  @R-2
  Scenario: Payment the gateway fails is shown as failed
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-3"
    And Acme will answer the send with "Failure"
    When the clock moves on until the worker has sent the payment
    Then the app reads status "Failed"

  @R-2
  Scenario: Partial authorisation is approved with the authorised amount
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-4"
    And Acme will answer the send with "Success", response code "00" and an authorised amount of 1500
    When the clock moves on until the worker has sent the payment
    Then the app reads status "Approved" and an authorised amount of 1500

  @R-2
  Scenario: Approval above the amount sent is approved with the authorised amount
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-23"
    And Acme will answer the send with "Success", response code "00" and an authorised amount of 2500
    When the clock moves on until the worker has sent the payment
    Then the app reads status "Approved" and an authorised amount of 2500

  @R-3
  Scenario: Answer with an empty client message is asked about
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-24"
    And Acme will answer the send with "Success", response code "00" and an empty client message
    And Acme will answer the query with "Success", response code "00" and client message "Approved."
    When the clock moves on until the worker has queried the payment
    Then the app reads status "Approved" and client message "Approved."

  @R-3 @R-4 @R-14
  Scenario: Payment whose send times out is approved after a query
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-5"
    And Acme will answer the send with a timeout
    And Acme will answer the first query with "Processing" and the second with "Success" and response code "00"
    When the clock moves on until the worker has sent the payment
    Then the app reads status "Pending"
    And no query is made before 5 seconds have passed
    And after the first query no query is made before a further 10 seconds have passed
    And the app then reads status "Approved"
    And Acme was sent the payment once

  @R-2
  Scenario: Query that finds the payment reversed sets the outcome approved
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-25"
    And Acme will answer the send with a timeout, and the query with "Reversed" and response code "00"
    When the clock moves on until the worker has queried the payment
    Then the app reads status "Approved" and response code "00"

  @R-3
  Scenario: Payment that meets a gateway error is asked about before it is sent again
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-6"
    And Acme will answer the send with 503
    And Acme will answer the query with "Success" and response code "00"
    When the clock moves on until the worker has queried the payment
    Then the app reads status "Approved"
    And Acme was sent the payment once

  @R-3 @R-5
  Scenario: Payment Acme never received is sent again with the same identity
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-7"
    And Acme will answer the first send with a timeout, the query with 404, and the second send with "Success" and response code "00"
    When the clock moves on until the worker has sent the payment twice
    Then the app reads status "Approved"
    And both sends carried the same TransactionID and the same OrderNumber

  @R-5 @R-14
  Scenario: Payment never received after three sends fails as not submitted
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-8"
    And Acme will answer every send with a timeout and every query with 404
    When the clock moves on until the worker has queried the payment after its third send
    Then the app reads status "Failed" with no response code and no client message
    And Acme was sent the payment three times

  @R-6
  Scenario: Rate limited payment is sent again only after Retry-After
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-9"
    And Acme will answer the first send with 429 and a Retry-After of 2 seconds, and the second with "Success" and response code "00"
    When the clock moves on until the worker has sent the payment twice
    Then the second send was made no earlier than 2 seconds after the first
    And the app reads status "Approved"

  @R-7
  Scenario: Payment refused as malformed fails
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-10"
    And Acme will answer the send with 400 and response code "E01"
    When the clock moves on until the worker has sent the payment
    Then the app reads status "Failed" and response code "E01"

  @R-7
  Scenario: Payment refused for a bad API key fails
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-11"
    And Acme will answer the send with 401 and response code "E02"
    When the clock moves on until the worker has sent the payment
    Then the app reads status "Failed" and response code "E02"

  @R-7
  Scenario: Order number held by another transaction fails the payment
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-12"
    And Acme will answer the send with 409, response code "E09" and another TransactionID
    When the clock moves on until the worker has sent the payment
    Then the app reads status "Failed" and response code "E09"

  @R-3
  Scenario: Order number held by the payment itself is asked about
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-13"
    And Acme will answer the send with 409, response code "E09" and the payment's own TransactionID
    And Acme will answer the query with "Success" and response code "00"
    When the clock moves on until the worker has queried the payment
    Then the app reads status "Approved"

  @R-7
  Scenario: Query refused leaves the payment for a person
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-14"
    And Acme will answer the send with a timeout and the query with 401 and response code "E02"
    When the clock moves on until the worker has queried the payment
    Then the app reads status "Unresolved"

  @R-8
  Scenario: Payment with no final answer after 24 hours needs a person
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-15"
    And Acme will answer the send with a timeout and every query with "Processing"
    When the clock moves on 24 hours
    Then the app reads status "Unresolved"

  @R-9
  Scenario: Same payment recorded twice is sent to Acme once
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-16"
    And the app recorded the same payment again
    And Acme will answer every send with "Success" and response code "00"
    When the clock moves on until the worker has sent the payment
    Then Acme was sent one payment with the reference's TransactionID

  @R-10
  Scenario: Call record never holds the card token or the email address
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-17"
    And Acme will answer the send with "Success" and response code "00"
    When the clock moves on until the worker has sent the payment
    Then the payment has one call record, a send, answered with 200
    And its request holds the marker in place of the token and the email address, and holds neither value anywhere

  @R-11
  Scenario: Each change of a payment is recorded
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-18"
    And Acme will answer the send with a timeout, the first query with "Processing" and the second with "Success" and response code "00"
    When the clock moves on until the worker has queried the payment twice
    Then the payment has three call records
    And it has two changes: from "Recorded" to "OutcomeUnknown" by the send, and from "OutcomeUnknown" to "Approved" by the second query

  @R-12
  Scenario: Payment whose work keeps failing fails when Acme does not hold it
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-19"
    And the send throws an unexpected exception five times in a row
    And Acme will answer the query with 404
    When the clock moves on until the worker has queried the payment
    Then the app reads status "Failed"

  @R-12
  Scenario: Payment whose work keeps failing takes the answer Acme already holds
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-20"
    And the send throws an unexpected exception five times in a row
    And Acme will answer the query with "Success" and response code "00"
    When the clock moves on until the worker has queried the payment
    Then the app reads status "Approved"

  @R-12
  Scenario: Payment whose work keeps failing needs a person when Acme is unclear
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-21"
    And the send throws an unexpected exception five times in a row
    And Acme will answer the query with "Processing"
    When the clock moves on until the worker has queried the payment
    Then the app reads status "Unresolved"

  @R-13
  Scenario: Payment due while the circuit is open is not sent until it closes
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-2001" at sub-merchant "ZAP0004182" with its reference "SUB-22"
    And the client's circuit is open for 30 seconds, after which Acme will answer the send with "Success" and response code "00"
    When the clock moves on 29 seconds
    Then the app reads status "Pending" and the payment has no call record
    And when the clock moves on until the worker has sent the payment the app reads status "Approved"
```

The references and values are carried as each test's discriminator requires, by
`Testing.md` section 7. Each test substitutes `IAcmeService` in the host, gives it
the answers its Given names, and reads what it was sent from the substitute. The
call records and the changes are read through `database`, because no frozen
contract shows them. "The clock moves on" moves the host's `FakeTimeProvider`, and
the test then waits for the condition its Then names.

## How it is built

- **API:** `Zapper.PaymentsApi`. No operation is added. `GetPayment` already projects
  `GetStatus()`, which from this Feature maps every state. The worker lives at
  `Workers/PaymentSubmission/PaymentSubmissionWorker.cs`, with
  `PaymentSubmissionWorkerOptions` beside it binding `Zapper:PaymentSubmissionWorker`
  and holding only `IsEnabled`. `Workers.cs`, at the project root, registers it and
  sets the host's shutdown timeout to 45 seconds; `Program.cs` calls it once, after
  the startup check. The startup check reads `AcmeOptions` and
  `TokenEncryptionOptions` only when `PaymentSubmissionWorkerOptions.IsEnabled` is
  true, so the app-facing service, with every worker off, never holds either
  secret. `Program.cs` adds the key-per-file source, as optional.
  - **One piece of work:** a new scope and an `Activity`; load the payment with
    `AsTracking` and its stored card; ask `DecideNextCall`. For a send: decrypt the
    token with `ICryptographyManager`, the key from `TokenEncryptionOptions` and the
    customer reference as associated data; build `SubmitPaymentRequest` with a new
    `MessageId` from `Guid.CreateVersion7()`; call `SubmitPaymentAsync`. For a
    query: call `GetTransactionByTransactionIdAsync`. Unless the answer is
    `CircuitOpen`, make the `AcmeInteractionEntity`, its request being the request
    the worker built serialised with `Token` and `Email` replaced by `[removed]`.
    Pass the answer to `RecordSendAnswer` or `RecordQueryAnswer`, add the
    interaction, and save once with `SaveChangesWithOutcomeAsync`. On `Conflict`,
    reload, ask the Domain again with the same answer and save again, up to three
    times, then log at `Error` and leave the row to its lease.
  - **Log levels by result:** `Approved`, `Declined` and `Failed` at
    `Information`; `FailedNotSubmitted` and a timed-out, broken or degraded call at
    `Warning`; `FailedRefusedByAcme`, `FailedOrderNumberInUse`,
    `FailedAfterUnexpectedFailures` and `Unresolved` at `Error`. Each line names the
    payment by its id only.
  - **The claim:** one interpolated `SqlQuery<Guid>` that selects up to the free
    places, at most 10, of the rows whose `NextActionAt` is at or before now,
    oldest first, `FOR UPDATE SKIP LOCKED`, moves their `NextActionAt` 60 seconds
    on, adds one to `AttemptCount` and returns their ids. Free places are
    `AcmeOptions.MaxConcurrentCalls` less the pieces of work running.
  - **The two catches** of `BackendCode.md` section 23. Around a piece of work, one
    `ExecuteUpdateAsync` adds one to `UnexpectedFailureCount` and moves
    `NextActionAt` on by 1, 5, 15 and 15 minutes after the first four failures, and
    to now after the fifth (S-2).
  - **Every 30 seconds** one `Information` line with the number of payments due and
    not claimed, and the age of the oldest.
- **Frontend:** None. This Feature is tech-only.
- **Domain:**
  - `PaymentState` gains `OutcomeUnknown`, `Approved`, `Declined`, `Failed` and
    `Unresolved`. `GetStatus()` maps `Recorded` and `OutcomeUnknown` to `Pending`
    and each other state to the status of the same name.
  - `Zapper.Domain/Payment/AcmeAnswer.cs`: the class `AcmeAnswer`, what Acme said,
    so no Infrastructure type reaches the Domain. It holds `Type`, `ResponseSource`,
    `ResponseCode`, `ResponseDescription`, `ClientMessage`, `AuthorisationCode`,
    `AuthorisedAmount`, `ReconciliationId`, `RetrievalReferenceNumber`,
    `AcmeTransactionAt` and `RetryAfter`, and
    in the same file the enum `AcmeAnswerType`: `Success`, `Failure`, `Processing`,
    `NotFound`, `RateLimited`, `Refused`, `OrderNumberHeldByAnother`,
    `OrderNumberHeldByThis`, `Degraded`, `TimedOut`, `ConnectionFailed`,
    `Unreadable` and `CircuitOpen`. `AcmeService` sets the type by Appendix A; the
    Domain decides what it means for the payment.
  - `PaymentEntity` gains:
    - `DecideNextCall(TimeProvider time)`, returning `NextCallResult` (`Send`,
      `Query`, `Unresolved`). It answers `Unresolved`, and makes that change, when
      24 hours have passed since `RecordedAt` or `UnexpectedFailureCount` is above
      5; `Query` when the payment is `OutcomeUnknown` or `UnexpectedFailureCount` is
      5; otherwise `Send`.
    - `RecordSendAnswer(AcmeAnswer answer, Guid? acmeInteractionId, TimeProvider
      time)` and `RecordQueryAnswer(...)`, both returning `AcmeAnswerResult`, named
      for the decision both make: `Approved`, `Declined`, `Failed`,
      `FailedRefusedByAcme`, `FailedOrderNumberInUse`, `FailedNotSubmitted`,
      `FailedAfterUnexpectedFailures`, `SendAgain`, `QueryLater`, `Postponed` and
      `Unresolved`. They apply R-2 to R-8, R-12 and R-13: set the state and Acme's
      outcome, `SendCount` (one more on every send that reached Acme's door, not on
      a 429 or an open circuit), `QueryCount` (back to 0 on each send, one more on
      each query, giving the gap 5 seconds times 2 to the power of the count before
      it, at most 300 seconds), `UnexpectedFailureCount` (back to 0 on any answer),
      and `NextActionAt` (null once final). Each change of state, and each final
      answer that confirms the state, adds a `PaymentChangeEntity` to the payment's
      `Changes`. Acme Notifications adds applying held events and comparing held
      advices to both, when an answer sets the outcome.
    - `AcmeAnswerResult` is in `Zapper.Domain/Payment/AcmeAnswerResult.cs` and
      `NextCallResult` in `PaymentEntity.cs`.
  - `Zapper.Domain/Payment/AcmeInteractionEntity.cs`, its constructor taking every
    column below except `Id`.
  - `Zapper.Domain/Payment/PaymentChangeEntity.cs`, made only by `PaymentEntity`.
  - The reference entities `AcmeOperationTypeEntity` (enum `AcmeOperation`:
    `Send`, `Query`), `AcmeInteractionOutcomeTypeEntity` (enum
    `AcmeInteractionOutcome`: `Answered`, `TimedOut`, `ConnectionFailed`) and
    `PaymentChangeSourceTypeEntity` (enum `PaymentChangeSource`: `Send`, `Query`,
    `Worker`; Acme Notifications adds its two).
- **Data model:**
  - `Payment.PaymentStateType` gains the rows 2 `OutcomeUnknown`, 3 `Approved`,
    4 `Declined`, 5 `Failed`, 6 `Unresolved`.
  - `Payment.Payment` gains: `AcmeTransactionAt` `timestamp with time zone`,
    nullable, Acme's `TransactionDateTime` read as UTC+02:00 and stored in UTC;
    `AuthorisationCode` 6, `ReconciliationId` 50, `RetrievalReferenceNumber` 12, `ResponseSource` 50 and `ResponseDescription` 50,
    each nullable, Appendix A's maximums; `AttemptCount`, `SendCount`, `QueryCount`
    and `UnexpectedFailureCount`, each `integer`, with the default 0 chosen for the
    rows that exist; `IX_Payment_NextActionAt`; and `xmin` as its concurrency token,
    `builder.Property<uint>("Version").IsRowVersion()`, by D-6. `AuthorisedAmount`
    already exists from Recording payments, and this Feature fills it.
  - `Payment.AcmeInteraction`: `Id` (`PK_AcmeInteraction`); `PaymentId`
    (`FK_AcmeInteraction_PaymentId`, `IX_AcmeInteraction_PaymentId`); `StoredCardId`
    (`FK_AcmeInteraction_StoredCardId`, `IX_AcmeInteraction_StoredCardId`);
    `AcmeOperationTypeId` `integer` (`FK_AcmeInteraction_AcmeOperationTypeId`,
    `IX_AcmeInteraction_AcmeOperationTypeId`); `AcmeInteractionOutcomeTypeId`
    `integer` (`FK_AcmeInteraction_AcmeInteractionOutcomeTypeId`,
    `IX_AcmeInteraction_AcmeInteractionOutcomeTypeId`); `MessageId` `uuid`,
    nullable, a send's only; `Attempt` `integer`; `Request` `text`; `HttpStatus`
    `integer`, nullable; `Response` `text`, nullable, the body as received;
    `StartedAt` `timestamp with time zone`; `DurationMilliseconds` `integer`;
    `TraceId` 32. The two bodies are `text` because Acme adds members without
    notice; the client reads at most 64 KB of an answer.
  - `Payment.PaymentChange`: `Id` (`PK_PaymentChange`); `PaymentId`
    (`FK_PaymentChange_PaymentId`, `IX_PaymentChange_PaymentId`);
    `FromPaymentStateTypeId` and `ToPaymentStateTypeId`, `integer`
    (`FK_PaymentChange_FromPaymentStateTypeId`, `IX_PaymentChange_FromPaymentStateTypeId`,
    and the same for `To`), by S-1; `PaymentChangeSourceTypeId` `integer`
    (`FK_PaymentChange_PaymentChangeSourceTypeId`,
    `IX_PaymentChange_PaymentChangeSourceTypeId`); `AcmeInteractionId`, nullable
    (`FK_PaymentChange_AcmeInteractionId`, `IX_PaymentChange_AcmeInteractionId`);
    `ChangedAt` `timestamp with time zone`.
  - `Payment.AcmeOperationType` (1 `Send`, 2 `Query`),
    `Payment.AcmeInteractionOutcomeType` (1 `Answered`, 2 `TimedOut`, 3
    `ConnectionFailed`) and `Payment.PaymentChangeSourceType` (1 `Send`, 2 `Query`,
    3 `Worker`): reference tables, `Id` `integer` and `Name` 64.
- **Protection level of each field:** the new payment columns are personal
  information with the payment they belong to; none is shown to the app except the
  response code and client message, as before. `AcmeInteraction` is personal
  information, linked to the customer through the payment and the stored card; its
  `Request` holds no token and no email address, and its `Response` holds neither,
  because Acme never returns the token. `PaymentChange` is personal information with
  its payment. The reference tables are at no level. The token is decrypted only in
  memory, for the one call, and never stored, logged or recorded. Retention is
  D-9's.
- **Database privileges:** `ZapperPaymentsApiRole` gains `UPDATE` on `Payment`;
  `SELECT` and `INSERT` on `AcmeInteraction` and `PaymentChange`; `SELECT` on
  `AcmeOperationType`, `AcmeInteractionOutcomeType` and `PaymentChangeSourceType`.
  No `UPDATE` and no `DELETE` on the call or change records, so the API adds to a
  payment's history and never rewrites it.
- **Addresses it calls:** Acme Pay, at `AcmeOptions.BaseAddress`, which
  `AcmeValidateOptions` requires to be `https`: `POST /api/payment` and
  `GET /apirttq/query/Merchant/{MerchantID}`, with the payment's id as the
  `TransactionID` query parameter. Locally the stub, at
  `https://AcmeStub:8443`. No address depends on data received from outside. No call
  follows a redirect.
- **Slow or costly operations:** None among the operations. The calls to Acme are
  slow by nature and are kept off the app's request by D-1; they are limited by D-5:
  `MaxConcurrentCalls` pieces of work at once per instance, `RequestsPerSecond`
  calls a second per instance, a 35-second limit on each call, and the circuit
  breaker. A limit reached refuses nothing.
- **Infrastructure inventory:** the part `Acme Pay` (a payment service provider
  outside the product; every deployed environment, and locally the stub stands in
  for it; sensitive data, the card token; the internet reaches it); the part
  `AcmeStub` (a stand-in for Acme Pay; local only; no protection level, it holds only seeded test values; the internet cannot reach it; published on `127.0.0.1`, port `ACME_STUB_PORT`, 8444 by default, for the end-to-end script); the connection
  `Zapper.PaymentsApi` to `Acme Pay` (443, locally `AcmeStub` on 8443; HTTPS; sensitive
  data; the bearer API key; the address depends on no data from outside).
- **Names the standards leave to planning:**
  `Zapper.Infrastructure/Integrations/Acme/`: `IAcmeService` and `AcmeService`, with
  `SubmitPaymentAsync(SubmitPaymentRequest request, CancellationToken
  cancellationToken)` returning `SubmitPaymentResponse`, and
  `GetTransactionByTransactionIdAsync(Guid transactionId, CancellationToken
  cancellationToken)` returning `GetTransactionByTransactionIdResponse`; each
  response holds the `AcmeAnswer`, the `AcmeInteractionOutcome`, the HTTP status,
  the body as received, `StartedAt` and the duration. `AcmeOptions` binds
  `Zapper:Acme`: `BaseAddress`, `MerchantId`, `ApiKey`, `RequestsPerSecond` and
  `MaxConcurrentCalls`, each `[Required]`, bound without `ValidateOnStart` (A5.2);
  `AcmeValidateOptions` requires `https`, a GUID merchant id and both numbers at
  least 1. The secret file is `Zapper__Acme__ApiKey`. The worker and the Domain
  names above.
- **Decisions for this Feature only:**
  - **The client** (`IntegrationRegistration`, by D-5): `AddHttpClient<IAcmeService,
    AcmeService>()` with a primary `SocketsHttpHandler` whose `AllowAutoRedirect` is
    false, `MaxConnectionsPerServer` is `MaxConcurrentCalls` and
    `PooledConnectionIdleTimeout` is 50 seconds; `MaxResponseContentBufferSize` 64 KB;
    and one resilience handler from `Microsoft.Extensions.Http.Resilience`, outermost
    first: a token bucket rate limiter of `RequestsPerSecond` a second whose queue
    holds `MaxConcurrentCalls`, so a call waits rather than being refused; a circuit
    breaker that opens when half of at least 10 calls in 30 seconds end in 500 to
    504 or a timeout, for 30 seconds; and a 35-second timeout on the call. No retry
    strategy. `AcmeService` catches its own timeout and logs it at `Warning`
    (`BackendCode.md` section 16), and turns an open circuit into `CircuitOpen` with
    the break as `RetryAfter`.
  - **Reading an answer** (`AcmeService`, by Appendix A and `BackendCode.md` section
    9): a 200 whose `TransactionID` is not ours, whose `ResponseDetail.Code` is
    missing, whose `ResponseDetail.ClientMessage` is missing or empty, which is a
    `Success` with code `00` and no `AuthAmount`, whose `TransactionStatus` is not one
    this Feature names, or whose value passes Appendix A's maximum length, is `Unreadable`. `Complete`, `Reversed` and `Refunded` give `Success`, and the status as sent is kept in the call record, because Appendix A puts the outcome in `ResponseDetail.Code`. A 409 is read for its
    `TransactionID` and compared with ours. A member the client does not know is
    ignored. `TransactionDateTime` is read as `yyyy-MM-ddTHH:mm:ss.fff` at UTC+02:00.
  - **The request** (Appendix A): PascalCase; `MessageDateTime` in UTC+02:00 to the
    millisecond with no offset; `Channel` `App`; `PaymentService` `CardNotPresent`;
    `CardOnFileScenario` `CIT`, because the customer confirms each payment in the
    app; `CurrencyCode` always sent (D-10); the bearer header from `ApiKey`.
  - **Any broken connection is unknown**, even one .NET can tell never left, because
    asking is cheap and always safe.
  - **The stored request is the request the worker built**, serialised with the two
    values replaced, so the acceptance tests, which substitute the client, prove the
    marker; the client integration tests prove the wire body matches that request.
  - **The polling interval (250 milliseconds), the largest batch (10) and the lease
    (60 seconds)** are constants in the worker, because D-4 fixes them and nothing
    asks for them to vary. `MaxConcurrentCalls` is one setting, in `AcmeOptions`,
    because D-5 makes the connection limit equal the limit on work at once.
  - **The acceptance host runs on a `FakeTimeProvider`** that the tests move, from
    this Feature on, so the timed rules have scenarios. Recording payments'
    acceptance tests are unaffected: none asserts a time.
  - **The stub, this Feature's part** (D-14): `/AcmeStub/` with `POST /api/payment`
    and `GET /apirttq/query/Merchant/{MerchantID}`; the bearer check (401 `E02`),
    the parse check (400 `E01`), the 12-character retrieval reference number held
    once (409 `E09` naming its holder), 20 requests a second (429 `E29` with
    `Retry-After: 1`), the stored outcome replayed for a repeated `TransactionID`,
    `Processing` while a request is in progress, and one member Appendix A does not
    document on every answer. Its cards, by token, are the seeded cards: approve;
    decline with `05`, `51` and `54`; Payment Advice sent twice and reversal before
    the Payment Advice, which approve here and gain their notifications in Acme
    Notifications; a late answer, held 40 seconds and approved 45 seconds after it
    arrived; never received, whose first send is held 40 seconds and not kept, so
    the query answers 404 and the second send is approved (ruled by Francois); and
    503 once, whose first send answers 503 and is not kept. Any other token is
    declined with `05` and a log line. Its README states these as assumptions, and
    its server certificate comes from the `compose.yml` certificate script for the names `AcmeStub`, `localhost`, `127.0.0.1` and `::1` (D-13).
  - **`compose.yml`**: the init service also generates `Zapper__Acme__ApiKey`,
    `sk_test_` followed by `openssl rand -hex 24`; it and the card key are mounted
    read-only into `PaymentsApi`, and the API key into `AcmeStub`. `PaymentsApi`
    gains `Zapper__Acme__BaseAddress`, `Zapper__Acme__MerchantId` (a GUID made once
    and written for both the API and the stub), `Zapper__Acme__RequestsPerSecond`
    20, `Zapper__Acme__MaxConcurrentCalls` 50,
    `Zapper__PaymentSubmissionWorker__IsEnabled` true, `SSL_CERT_FILE` and
    `SSL_CERT_DIR`, and `stop_grace_period: 60s` (D-4). `AcmeStub` publishes `127.0.0.1:${ACME_STUB_PORT:-8444}:8443` (D-14).
  - **The session export** that D-17 names is made by Francois after this Feature's
    pull request is merged, and is not an increment.
  - **The standard changes S-1 and S-2** above, ruled by Francois on 2026-10-23.
  - **Three acceptance tests are finished in increment 2.** Increment 1 writes "Call
    record never holds the card token or the email address", "Each change of a payment
    is recorded" and "Payment due while the circuit is open is not sent until it
    closes" with their When, and their Then on the API's answer and on what the
    substituted `IAcmeService` was sent, because the call records, the changes and
    their states do not exist before increment 2. Increment 2 adds the reads through
    `database`, and the closing report names each test it changes. Ruled by Francois
    on 2026-10-24.
  - **`Workers.cs` is `WorkerRegistration.cs`**, holding the class
    `WorkerRegistration` and its method `AddWorkers`, wherever this plan names
    `Workers.cs`, because a class `Zapper.PaymentsApi.Workers` cannot sit beside the
    namespace of the `Workers/` folder. `BackendCode.md` section 23 states it. Ruled
    by Francois on 2026-10-24.
- **Tests:**
  - C# unit: `PaymentTests` gains every value of `NextCallResult` and of
    `AcmeAnswerResult`, from both methods; the gap at each count up to 300 seconds;
    24 hours; an authorised amount below and above the amount sent, and a decline
    with none and with one above 0; the fifth unexpected failure and
    each answer to its query.
  - API contract: no new fragment; `GetPayment`'s fragment is unchanged.
  - Database integration: `AcmeServiceIntegrationTests` at
    `Integrations/Acme/`, with `RecordingHandler` answering with Appendix A's own
    examples copied once into files (a 200, the 409 envelope, a 429 with
    `Retry-After`, a 503, a `Processing` query, a 404, and a 200 with a member it does
    not know): the request matches Appendix A's example; each answer gives its
    `AcmeAnswerType`; an empty client message, and an approval with no authorised amount, are each `Unreadable`; `Complete`, `Reversed` and `Refunded` each give `Success`, with code `00` approved and code `51` declined; a call that never answers is `TimedOut` after 35
    seconds; the
    circuit opens and gives `CircuitOpen`; a redirect is not followed.
    `PaymentSubmissionWorkerIntegrationTests` at `PaymentsApi/Workers/PaymentSubmission/`:
    every value of `AcmeAnswerResult` for one piece of work; two claims at the same
    moment never return the same row; a lapsed lease is claimed again; a `Conflict`
    is reloaded and applied again; a claim takes only free places; the unexpected
    failure count and its query. `AcmeInteractionPersistenceIntegrationTests` and
    `PaymentChangePersistenceIntegrationTests` prove each foreign key;
    `PaymentPersistenceIntegrationTests` gains `SaveOutcome.Conflict` on `xmin`. The
    migration, naming, reference table and error code tests cover the rest.
  - Acceptance: `SubmittingPaymentsToAcmeAcceptanceTests`, one test per scenario.
  - Authorization: none, because no operation is added.
  - Load: none, because no operation is slow or costly.

## Contracts

None. This Feature adds no API operation: the app keeps reading the payment through
`GET /v1/payments/{id}`, which Recording payments froze.

| Operation | Contract |
|---|---|
| None | None |

## Implementation order

1. **Failing acceptance tests.** One per scenario above, twenty-five, failing. The
   acceptance host gains the `FakeTimeProvider` and the substituted `IAcmeService`;
   `IAcmeService`, its request and response types and `AcmeAnswer` are written with
   no implementation, so the tests compile.
2. **Schema and Domain.** The new states, the three reference tables, the payment's
   new columns and `xmin`, `AcmeInteraction` and `PaymentChange`, one migration with
   the grants; `DecideNextCall`, `RecordSendAnswer` and `RecordQueryAnswer` with
   their unit tests; the two persistence test classes. Reviewable on its own: the
   migration, naming and reference table tests check it, and `GetPayment` maps every
   state.
3. **The Acme client.** `AcmeService`, `AcmeOptions`, `AcmeValidateOptions`, the
   registration with its resilience handler and the
   `Microsoft.Extensions.Http.Resilience` row in `Versions.md`;
   `AcmeServiceIntegrationTests`. Reviewable on its own: it touches no table.
4. **The worker.** `PaymentSubmissionWorker`, its options, `Workers.cs`, the
   key-per-file source and the startup check; `PaymentSubmissionWorkerIntegrationTests`.
   Reviewable on its own: every acceptance test passes.
5. **The stub and `compose.yml`.** The stub's two routes and behaviours, its
   `Dockerfile`, README and service, its certificate, the generated API key and the
   API's settings; the infrastructure inventory's rows and the cryptographic
   inventory's row for the stub's server certificate. Reviewable on its own:
   `docker compose up`, then `curl`, takes each seeded card to its expected status,
   and a call made without `SSL_CERT_FILE` is shown to fail (D-13).
6. **README.md and SOLUTION.md.** The README says how to watch a payment reach its
   final status. SOLUTION.md gains this Feature's facts: reading Acme's answers and
   asking first, sending again only on a 404, the 24-hour limit, partial
   authorisation, the worker and its limits, the call and change records, the stub's assumptions, the three statuses read like `Success` by the response code (an assumption), Acme always sending a client message with a response code (an assumption ruled by Francois; an answer without one is asked about by query), what each final status means for the customer and that `Unresolved`
   is never shown as a failure to retry, and the facts for Francois's AI workflow
   and validation sections.

# Feature: Acme Notifications

## What it delivers

Acme Pay's Payment Advices and Acme Switch's Transaction Status notifications reach
`Zapper.PaymentsApi`, are proven, stored as received, and kept from changing a payment
twice or against the facts already held, whatever order they arrive in and however
often. A Payment Advice confirms or contradicts the outcome the worker set, and one that arrives early is compared when the worker's answer sets the outcome. Reversals and refunds are applied to
approved payments by D-8's rules, held until the outcome is known when they arrive
first, and shown to the app as `Reversed`, `Refunded` or `PartiallyRefunded` with their
totals. The stub sends both notifications for each seeded card, so `docker compose up`
shows a duplicate, an out-of-order reversal, a reversal and a refund. SQL Daily Summary
depends on the events and their times; End to End validation checks walks every case.

## Rules

- **R-1: Proof.** A Payment Advice is accepted only with the Basic username and
  password Zapper gave Acme Pay, and a Transaction Status only with an `x-signature`
  that is the HMAC-SHA256 of its exact body with the webhook secret. Anything else is
  refused, nothing is stored, and the answer never says which part was wrong.
- **R-2: Stored, then answered.** Every proven notification that passes validation is
  stored with its body as received (a Payment Advice without `MaskedCardNumber`), the
  time it arrived, its trace id, the payment it matched or none, and what was done
  with it, in the same save as any change it makes to the payment. The answer is 200
  with no body, and says nothing about the payment.
- **R-3: Fields Acme adds or removes.** A field Acme adds is ignored and kept in the
  stored body. A field Appendix A requires that is missing, or a value longer than
  Appendix A allows, is refused and nothing is stored: a removed field is a change by
  Acme, settled with Acme.
- **R-4: Duplicate.** A notification's identity is its `transactionUUID` for a
  Transaction Status, and its `TransactionID` with its `RequestType` for a Payment
  Advice. A notification whose identity and mapped fields are both already held is a
  duplicate: answered 200, with nothing stored and nothing changed. Only the fields
  this Feature maps are compared, so a field Acme adds, or one that changes between
  deliveries, never makes a delivery different.
- **R-5: Contradicting notification.** A notification whose identity is held with
  different mapped fields keeps the first, is stored as a contradiction, and makes its
  payment `Unresolved`.
- **R-6: Matching.** A Payment Advice matches the payment whose id is its
  `TransactionID`, only where its `MerchantID` is Zapper's account at Acme. A
  Transaction Status matches the payment whose order number is its Retrieval Reference
  Number. A notification that matches no payment is stored, answered 200, never matched
  later, and changes nothing.
- **R-7: Advice details.** A matched Payment Advice whose amount, currency (where
  sent), order number or Retrieval Reference Number differs from the payment's is a
  contradiction, and makes the payment `Unresolved`.
- **R-8: An advice and the outcome.** A Payment Advice never sets the outcome. Of
  `RequestType` `Payment`, `Success` with code `00` says approved, `Success` with
  another code says declined with that code, and `Failure` says failed. Where the
  outcome is set, an advice that says the same is a confirmation, recorded as a change
  of the payment, and one that says otherwise is a contradiction that makes the payment
  `Unresolved`. Where the outcome is not set, the advice is held, and compared when the worker's next answer sets the outcome. A notification never changes when the worker next calls Acme, because that time is also the worker's lease (`BackendCode.md` section 23).
- **R-9: Advices that change nothing.** An `Authorize` or `Capture` advice, an advice
  whose `RequestType` or `TransactionStatus` is not one Appendix A names, an advice of
  `TransactionStatus` `Other`, and any advice for an `Unresolved` payment, is recorded
  and changes nothing.
- **R-10: Reversal.** A reversal applies to an approved payment, for exactly its
  authorised amount, and makes it `Reversed`. A second reversal with a different
  `transactionUUID`, a reversal after a refund, or a reversal for another amount is a
  contradiction: kept, not applied, and the payment `Unresolved`.
- **R-11: Refund.** Refunds may be partial and several. While the refunds and any
  reversal add up to less than the authorised amount the payment is
  `PartiallyRefunded`, and at exactly the authorised amount it is `Refunded`. A refund
  that would pass the authorised amount is kept, not applied, and makes the payment
  `Unresolved`.
- **R-12: Events before the outcome.** A reversal or refund for a payment whose outcome
  is not set is held. When the
  outcome is set, `Approved` applies every held event in the order Acme Switch
  processed them, by R-10 and R-11; `Declined` or `Failed` makes each a contradiction,
  and the payment `Unresolved`.
- **R-13: Events that are not applied.** An event with `isApproved` false or a
  `txResultCode` other than `00` is recorded and never applied. An event for an
  `Unresolved` payment is kept and not applied. An event for a `Declined` or `Failed`
  payment is a contradiction, and makes it `Unresolved`.
- **R-14: Currency.** An event whose currency differs from its payment's is a
  contradiction, and makes the payment `Unresolved`.
- **R-15: Unknown event.** A Transaction Status whose `transactionType` is neither
  `Reversal` nor `Refund` is recorded and changes nothing.
- **R-16: What the app sees.** A reversed payment shows `Reversed`, a refunded one
  `Refunded` and a partly refunded one `PartiallyRefunded`, each with its reversed and
  refunded totals and its authorised amount. A payment a contradiction made
  `Unresolved` shows `Unresolved`, and a person settles it with Acme.

Who may see each record: the app reads a payment as Recording payments R-8 states. The
notifications and events are read by no operation; an administrator reads them in the
database. No field in this Feature is read or changed by a caller. Retention is D-9's:
notifications and events are kept for 5 years after the payment, with it; an unmatched
notification for 5 years after it arrived.

## Failure modes

| What goes wrong | What the caller receives | Recovery |
|---|---|---|
| Missing or wrong Basic credentials, or the other notification's proof | 401 `Unauthorized`, logged at `Warning` | Acme resends three times; a person checks the credentials registered with Acme |
| Missing `x-signature`, a wrong signature, or a body changed in transit | 401 `Unauthorized`, logged at `Warning` | Acme Switch resends three times; a person checks the webhook secret |
| Notifications switched off on this service | 401 `Unauthorized`, logged at `Warning` | Acme's registered address is corrected to the service that answers requests |
| A required field missing or too long, or a time in the wrong format | 400 with the per-field map, logged at `Warning`; nothing stored | Acme resends three times, then stops; settled with Acme (R-3) |
| A body that is not JSON | 400 `BadRequest` | As above |
| A duplicate | 200; nothing stored | None needed |
| A notification matching no payment, or naming another account | 200; stored unmatched; logged at `Error` | A person checks it with Acme |
| A contradiction | 200; stored as a contradiction; the payment `Unresolved`; logged at `Error` | A person settles the payment with Acme |
| An advice or event that changes nothing (R-9, R-13, R-15) | 200; recorded; an unknown value logged at `Warning` | None needed, or a person reads the record |
| An approval above the amount sent, or a decline carrying an authorised amount | The app reads the status, with the authorised amount; logged at `Error` | A person refunds or checks the customer |
| The payment changed between load and save, by the worker or another notification | 500 `ConcurrentChange`, logged at `Warning`; nothing stored | Acme resends after 2 seconds (the stub) and the resend applies |
| Every delivery of one notification fails, through a fault of ours | 500 each time; the notification is lost | A person finds it on Acme's settlement file (SOLUTION.md) |
| Too many requests from one address | 429 `TooManyRequests`, from the provider limit | Acme resends |
| The database cannot be reached | 500 `Internal` | Acme resends |

## Scenarios

```gherkin
Feature: Acme Notifications

  Background:
    Given the sub-merchant "ZAP0004182" is registered, trading as "Cafe Del Sol" in "Cape Town", region "WC", country "ZA", category "5812"
    And the customer "CUS-3001" holds a stored card with an email address

  @R-1
  Scenario: Payment Advice without the agreed credentials is refused
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-1", which Acme approved with response code "00"
    When Acme sends a Payment Advice for that payment with a wrong password
    Then the answer is 401 with the code "Unauthorized"
    And no notification is stored for that payment

  @R-1
  Scenario: Transaction Status whose body was changed after signing is refused
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-2", which Acme approved with response code "00"
    When Acme Switch sends a reversal of 1999 "ZAR" for that payment whose body was changed by one byte after it was signed
    Then the answer is 401 with the code "Unauthorized"
    And the app reads status "Approved"

  @R-2 @R-8
  Scenario: Payment Advice that agrees with the outcome confirms it
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-3", which Acme approved with response code "00"
    When Acme sends a Payment Advice for that payment with "Success" and response code "00"
    Then the answer is 200 with no body
    And the notification is stored, matched to that payment, as a confirmation
    And the payment has a change from "Approved" to "Approved" by the Payment Advice
    And the app reads status "Approved"

  @R-3
  Scenario: Payment Advice carrying a field Acme added is accepted
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-4", which Acme approved with response code "00"
    When Acme sends a Payment Advice for that payment with "Success", response code "00" and a member "SettlementBatch" that Appendix A does not name
    Then the answer is 200 with no body
    And the stored body holds "SettlementBatch" and no "MaskedCardNumber"

  @R-3
  Scenario: Payment Advice missing a required field is refused
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-5", which Acme approved with response code "00"
    When Acme sends a Payment Advice for that payment with no "ResponseCode"
    Then the answer is 400 with "ResponseCodeRequired" against "ResponseCode"
    And no notification is stored for that payment

  @R-4
  Scenario: Payment Advice delivered twice changes nothing the second time
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-6", which Acme approved with response code "00"
    And Acme sent a Payment Advice for that payment with "Success", response code "00" and a delivery count of 1
    When Acme sends the same Payment Advice again with a delivery count of 2
    Then the answer is 200 with no body
    And one notification is stored for that payment
    And the payment has one change by a Payment Advice

  @R-4
  Scenario: Transaction Status delivered twice is applied once
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-7", which Acme approved with response code "00"
    And Acme Switch sent a refund of 500 "ZAR" for that payment
    When Acme Switch sends the same refund again
    Then the answer is 200 with no body
    And the app reads status "PartiallyRefunded" and a refunded total of 500

  @R-5
  Scenario: Same Payment Advice with a different outcome needs a person
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-8", which Acme approved with response code "00"
    And Acme sent a Payment Advice for that payment with "Success" and response code "00"
    When Acme sends a Payment Advice with the same TransactionID and RequestType, with "Success" and response code "05"
    Then the answer is 200 with no body
    And the second notification is stored as a contradiction
    And the app reads status "Unresolved"

  @R-6
  Scenario: Payment Advice for an unknown payment is kept
    When Acme sends a Payment Advice whose TransactionID names no payment
    Then the answer is 200 with no body
    And the notification is stored with no payment, as unmatched

  @R-6
  Scenario: Payment Advice naming another merchant account is not applied
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-9", which Acme approved with response code "00"
    When Acme sends a Payment Advice for that payment with another MerchantID, "Success" and response code "51"
    Then the answer is 200 with no body
    And the notification is stored with no payment, as unmatched
    And the app reads status "Approved"

  @R-6
  Scenario: Transaction Status for an unknown payment is kept
    When Acme Switch sends a refund of 500 "ZAR" whose Retrieval Reference Number names no payment
    Then the answer is 200 with no body
    And the notification is stored with no payment, as unmatched

  @R-7
  Scenario: Payment Advice for a different amount needs a person
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-10", which Acme approved with response code "00"
    When Acme sends a Payment Advice for that payment with an amount of 2500, "Success" and response code "00"
    Then the answer is 200 with no body
    And the app reads status "Unresolved"

  @R-8
  Scenario: Early Payment Advice is compared when the query sets the outcome
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-11"
    And Acme answered the send with a timeout, and will answer the query with "Success" and response code "00"
    When Acme sends a Payment Advice for that payment with "Success" and response code "00"
    Then the app reads status "Pending" and the notification is stored as held
    And the clock moves on until the worker has queried the payment
    And the app then reads status "Approved"
    And the payment has a change from "Approved" to "Approved" by the Payment Advice

  @R-8
  Scenario: Early Payment Advice that the query contradicts needs a person
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-12"
    And Acme answered the send with a timeout, and will answer the query with "Success" and response code "51"
    When Acme sends a Payment Advice for that payment with "Success" and response code "00"
    And the worker queries the payment
    Then the app reads status "Unresolved"

  @R-9
  Scenario: Authorisation advice is recorded and changes nothing
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-13", which Acme approved with response code "00"
    When Acme sends a Payment Advice for that payment with RequestType "Authorize", "Success" and response code "00"
    Then the answer is 200 with no body
    And the notification is stored as recorded
    And the payment has no change by a Payment Advice

  @R-9
  Scenario: Payment Advice with status Other is recorded and changes nothing
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-14", which Acme approved with response code "00"
    When Acme sends a Payment Advice for that payment with "Other" and response code "00"
    Then the answer is 200 with no body
    And the notification is stored as recorded
    And the app reads status "Approved"

  @R-10 @R-16
  Scenario: Approved payment reversed by Acme Switch shows as reversed
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-15", which Acme approved with response code "00"
    When Acme Switch sends a reversal of 1999 "ZAR" for that payment
    Then the answer is 200 with no body
    And the app reads status "Reversed", a reversed total of 1999 and a refunded total of 0

  @R-10
  Scenario: Second reversal of a payment needs a person
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-16", which Acme approved with response code "00"
    And Acme Switch sent a reversal of 1999 "ZAR" for that payment
    When Acme Switch sends another reversal of 1999 "ZAR" for that payment, with a different transactionUUID
    Then the app reads status "Unresolved" and a reversed total of 1999

  @R-10
  Scenario: Reversal after a refund needs a person
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-17", which Acme approved with response code "00"
    And Acme Switch sent a refund of 500 "ZAR" for that payment
    When Acme Switch sends a reversal of 1999 "ZAR" for that payment
    Then the app reads status "Unresolved", a reversed total of 0 and a refunded total of 500

  @R-11 @R-16
  Scenario: Partial refund shows as partially refunded
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-18", which Acme approved with response code "00"
    When Acme Switch sends a refund of 500 "ZAR" for that payment
    Then the app reads status "PartiallyRefunded" and a refunded total of 500

  @R-11 @R-16
  Scenario: Refunds that add up to the authorised amount show as refunded
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-19", which Acme approved with response code "00"
    And Acme Switch sent a refund of 500 "ZAR" for that payment
    When Acme Switch sends a refund of 1499 "ZAR" for that payment
    Then the app reads status "Refunded" and a refunded total of 1999

  @R-11 @R-16
  Scenario: Refund that would pass the authorised amount needs a person
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-20", which Acme approved with response code "00" and an authorised amount of 1500
    When Acme Switch sends a refund of 1999 "ZAR" for that payment
    Then the app reads status "Unresolved", an authorised amount of 1500 and a refunded total of 0

  @R-12
  Scenario: Reversal that arrives before the outcome is applied once the payment is approved
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-21"
    And Acme answered the send with a timeout, and will answer the query with "Reversed" and response code "00"
    When Acme Switch sends a reversal of 1999 "ZAR" for that payment
    Then the app reads status "Pending"
    And the clock moves on until the worker has queried the payment
    And the app then reads status "Reversed" and a reversed total of 1999

  @R-12
  Scenario: Reversal of a payment that is then declined needs a person
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-22"
    And Acme answered the send with a timeout, and will answer the query with "Success" and response code "51"
    When Acme Switch sends a reversal of 1999 "ZAR" for that payment
    And the worker queries the payment
    Then the app reads status "Unresolved"

  @R-13
  Scenario: Reversal the switch did not apply is recorded and changes nothing
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-23", which Acme approved with response code "00"
    When Acme Switch sends a reversal of 1999 "ZAR" for that payment with isApproved false and txResultCode "05"
    Then the answer is 200 with no body
    And the app reads status "Approved" and a reversed total of 0

  @R-13
  Scenario: Refund for a declined payment needs a person
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-24", which Acme declined with response code "51"
    When Acme Switch sends a refund of 500 "ZAR" for that payment
    Then the app reads status "Unresolved" and a refunded total of 0

  @R-13
  Scenario: Refund for a payment that needs a person is kept and not applied
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-25", which is "Unresolved"
    When Acme Switch sends a refund of 500 "ZAR" for that payment
    Then the answer is 200 with no body
    And the app reads status "Unresolved" and a refunded total of 0

  @R-14
  Scenario: Refund in another currency needs a person
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-26", which Acme approved with response code "00"
    When Acme Switch sends a refund of 500 "USD" for that payment
    Then the app reads status "Unresolved" and a refunded total of 0

  @R-15
  Scenario: Transaction Status of an unknown type is recorded and changes nothing
    Given the app recorded a payment of 1999 "ZAR" for customer "CUS-3001" at sub-merchant "ZAP0004182" with its reference "NOT-27", which Acme approved with response code "00"
    When Acme Switch sends a Transaction Status of type "Chargeback" of 1999 "ZAR" for that payment
    Then the answer is 200 with no body
    And the app reads status "Approved"
```

The references and values are carried as each test's discriminator requires, by
`Testing.md` section 7. "Which Acme approved" is arranged as Submitting Payments to
Acme's scenarios arrange it: the substituted `IAcmeService` answers and the clock moves
until the worker has sent the payment. "Which is `Unresolved`" is arranged through
`database`, with the payment's own domain methods. Each notification is sent by
`client` with the Basic credentials or the signature, made with the run's generated
webhook secret, as Appendix A fixes them. The notifications, their outcomes, the events
and the changes are read through `database`, because no frozen contract shows them.

## How it is built

- **API:** `Zapper.PaymentsApi`. Two operations:
  `Features/PaymentAdvices/RecordPaymentAdvice.cs` (`POST /v1/payment-advices`),
  grouped by `Features/PaymentAdvices/PaymentAdviceEndpoints.cs` with the tag
  `payment-advices`, requiring `SendPaymentAdvice`; and
  `Features/TransactionStatuses/RecordTransactionStatus.cs`
  (`POST /v1/transaction-statuses`), grouped by
  `Features/TransactionStatuses/TransactionStatusEndpoints.cs` with the tag
  `transaction-statuses`, requiring `SendTransactionStatus`. `Endpoints.cs` gains two
  lines. Each declares `.Produces(200)`, `.ProducesValidationProblem()`,
  `.ProducesProblem(401)` and `.ProducesProblem(500)`, and answers
  `TypedResults.Ok()`.
  - **The request types** carry `[JsonPropertyName]` with Acme's names (S-3). Acme's
    lists (`RequestType`, `PaymentService`, `TransactionStatus`, `transactionType`)
    and times are strings. `isApproved` is `bool?`.
  - **The validators** check what the contracts' Fields tables state: each required
    field present, each text field at most its length, each amount at least 1, each
    time exactly `yyyy-MM-ddTHH:mm:ss.fff`, and `isApproved` present.
  - **`RecordPaymentAdvice`'s handler, in this order:** read the body as received,
    which the Basic handler left buffered and rewound; remove `MaskedCardNumber` with
    `JsonNode`; read `TransactionDateTime` at UTC+02:00; make `AcmePaymentAdvice`;
    hash `GetContent()` with `ICryptographyManager.GetSha256Hash`; load the
    notifications already held with the same source and identity; load, `AsTracking`,
    the payment whose id is the `TransactionID` where it parses as a GUID, with its
    held events and held advices; make the `NotificationEntity`; where no payment was
    found, `RecordUnmatched()`; otherwise `payment.RecordPaymentAdvice(...)` with the
    account's merchant id from `AcmeNotificationOptions`; add the notification; save
    once with `SaveChangesWithOutcomeAsync`. `Saved` and `AlreadyExists` answer 200;
    `Conflict` answers 500 `ConcurrentChange`, logged at `Warning`.
  - **`RecordTransactionStatus`'s handler:** the same shape; the body is stored exactly
    as received; the payment is found by `OrderNumber` equal to the Retrieval Reference
    Number, with its events; `transactionDateTime` is read at UTC+02:00;
    `payment.RecordTransactionStatus(...)` decides.
  - **Log levels by result:** `Confirmed`, `Held`, `Applied` and a duplicate at
    `Information`; `Recorded` for an unknown value at `Warning`; `Unmatched` and
    `Contradiction` at `Error`. Each line names the notification and the payment by id
    only.
  - **`Authentication.cs`**, at the API project's root, registers the two schemes with
    the generic handlers, giving each its credentials from `AcmeNotificationOptions`,
    and the two permissions; `Program.cs` calls it once, beside `Workers.cs`.
- **Frontend:** None. This Feature is tech-only.
- **Domain:**
  - `Zapper.Domain/Payment/AcmePaymentAdvice.cs` and `AcmeTransactionStatus.cs`: what
    each notification said, so no request type reaches the Domain. Each holds its
    mapped fields, times as `DateTimeOffset`, and `GetContent()`, which writes the
    mapped fields in a fixed order, each with its name, as the text the content hash is
    made from.
  - `Zapper.Domain/Payment/NotificationEntity.cs`: its constructor takes the source,
    the identity, the content hash, the stored body, the advice's status and code
    where it is an advice, the trace id and `TimeProvider`. `RecordUnmatched()` is its
    one public method; `PaymentEntity` records its payment and outcome.
  - `Zapper.Domain/Payment/PaymentEventEntity.cs`, made only by `PaymentEntity`.
  - `PaymentEntity` gains:
    - `RecordPaymentAdvice(NotificationEntity notification, AcmePaymentAdvice advice,
      IReadOnlyCollection<NotificationEntity> heldWithSameIdentity, string
      accountMerchantId, TimeProvider time)`, returning `RecordPaymentAdviceResult`:
      `Confirmed`, `Held`, `Recorded`, `Unmatched` or `Contradiction`. It applies R-5 to
      R-9.
    - `RecordTransactionStatus(NotificationEntity notification, AcmeTransactionStatus
      status, IReadOnlyCollection<NotificationEntity> heldWithSameIdentity,
      TimeProvider time)`, returning `RecordTransactionStatusResult`: `Applied`,
      `Held`, `Recorded` or `Contradiction`. It applies R-5 and R-10 to R-15.
    - In `RecordSendAnswer` and `RecordQueryAnswer`: when an answer sets the outcome,
      each held event is applied or made a contradiction (R-12), in Acme Switch's
      processing order, and each held advice is compared (R-8). Each recorded as a
      change.
    - A notification never changes `NextActionAt`, which is the worker's lease: moving it would let a second worker send the payment while the first call is in progress. The worker's next answer applies the held events and compares the held advices.
    - `GetStatus()` maps `Reversed`, `Refunded` and `PartiallyRefunded` to the statuses
      of the same name.
  - Reference entities: `NotificationSourceTypeEntity` (enum `NotificationSource`:
    `PaymentAdvice`, `TransactionStatus`), `NotificationOutcomeTypeEntity` (enum
    `NotificationOutcome`: `Applied`, `Confirmed`, `Held`, `Recorded`, `Unmatched`,
    `Contradiction`), `PaymentEventTypeEntity` (enum `PaymentEvent`: `Reversal`,
    `Refund`), `PaymentEventStateTypeEntity` (enum `PaymentEventState`: `Held`,
    `Applied`, `NotApplied`, `Contradiction`). `PaymentState` gains `Reversed`,
    `Refunded`, `PartiallyRefunded`; `PaymentChangeSource` gains `PaymentAdvice`,
    `TransactionStatus`.
- **Data model:**
  - `Payment.PaymentStateType` gains 7 `Reversed`, 8 `Refunded`, 9 `PartiallyRefunded`.
  - `Payment.PaymentChangeSourceType` gains 4 `PaymentAdvice`, 5 `TransactionStatus`.
  - `Payment.PaymentChange` gains `NotificationId`, nullable
    (`FK_PaymentChange_NotificationId`, `IX_PaymentChange_NotificationId`).
  - `Payment.Notification`: `Id` (`PK_Notification`); `NotificationSourceTypeId`
    `integer` (`FK_Notification_NotificationSourceTypeId`,
    `IX_Notification_NotificationSourceTypeId`); `AcmeReference` 50; `RequestType` 20,
    nullable; `ContentHash` `bytea`, 32 bytes;
    `UX_Notification_NotificationSourceTypeId_AcmeReference_RequestType_ContentHash`,
    nulls not distinct; `PaymentId`, nullable (`FK_Notification_PaymentId`,
    `IX_Notification_PaymentId`); `NotificationOutcomeTypeId` `integer`
    (`FK_Notification_NotificationOutcomeTypeId`,
    `IX_Notification_NotificationOutcomeTypeId`); `TransactionStatus` 20, nullable;
    `ResponseCode` 3, nullable; `Body` `text`; `ReceivedAt` `timestamp with time zone`;
    `TraceId` 32.
  - `Payment.PaymentEvent`: `Id` (`PK_PaymentEvent`); `PaymentId`
    (`FK_PaymentEvent_PaymentId`, `IX_PaymentEvent_PaymentId`); `NotificationId`
    (`FK_PaymentEvent_NotificationId`, `UX_PaymentEvent_NotificationId`);
    `PaymentEventTypeId` `integer` (`FK_PaymentEvent_PaymentEventTypeId`,
    `IX_PaymentEvent_PaymentEventTypeId`); `Amount` `bigint`; `CurrencyCode` 3, as Acme
    sent it; `AcmeProcessedAt` `timestamp with time zone`; `PaymentEventStateTypeId`
    `integer` (`FK_PaymentEvent_PaymentEventStateTypeId`,
    `IX_PaymentEvent_PaymentEventStateTypeId`); `AppliedAt` `timestamp with time zone`,
    nullable.
  - Four reference tables, `Id` `integer` and `Name` 64: `Payment.NotificationSourceType`
    (1 `PaymentAdvice`, 2 `TransactionStatus`), `Payment.NotificationOutcomeType`
    (1 `Applied`, 2 `Confirmed`, 3 `Held`, 4 `Recorded`, 5 `Unmatched`,
    6 `Contradiction`), `Payment.PaymentEventType` (1 `Reversal`, 2 `Refund`),
    `Payment.PaymentEventStateType` (1 `Held`, 2 `Applied`, 3 `NotApplied`,
    4 `Contradiction`).
  - The error message table gains one row for each new code: `RequestTypeRequired` "A
    request type is required."; `RequestTypeTooLong` "A request type is at most 20
    characters."; `MerchantIdRequired` "A merchant id is required.";
    `MerchantIdTooLong` "A merchant id is at most 50 characters.";
    `TransactionIdRequired` "A transaction id is required."; `TransactionIdTooLong` "A
    transaction id is at most 50 characters."; `TransactionDateTimeRequired` "A
    transaction time is required."; `TransactionDateTimeMalformed` "A transaction time
    is written as yyyy-MM-ddTHH:mm:ss.fff, with no offset."; `OrderNumberTooLong` "An
    order number is at most 50 characters."; `ReconciliationIdTooLong` "A
    reconciliation id is at most 50 characters."; `RetrievalReferenceNumberRequired` "A
    retrieval reference number is required."; `RetrievalReferenceNumberTooLong` "A
    retrieval reference number is at most 12 characters.";
    `SystemsTraceAuditNumberRequired` "A systems trace audit number is required.";
    `SystemsTraceAuditNumberTooLong` "A systems trace audit number is at most 6
    characters."; `PaymentServiceRequired` "A payment service is required.";
    `PaymentServiceTooLong` "A payment service is at most 30 characters.";
    `TransactionStatusRequired` "A transaction status is required.";
    `TransactionStatusTooLong` "A transaction status is at most 20 characters.";
    `ResponseSourceRequired` "A response source is required.";
    `ResponseSourceTooLong` "A response source is at most 50 characters.";
    `ResponseCodeRequired` "A response code is required."; `ResponseCodeTooLong` "A
    response code is at most 3 characters."; `ResponseDescriptionTooLong` "A response
    description is at most 50 characters."; `AuthorisationCodeTooLong` "An
    authorisation code is at most 6 characters."; `CommunicationProtocolRequired` "A
    communication protocol is required."; `CommunicationProtocolTooLong` "A
    communication protocol is at most 4 characters."; `TransactionUuidRequired` "A
    transaction UUID is required."; `TransactionUuidTooLong` "A transaction UUID is at
    most 50 characters."; `TransactionTypeRequired` "A transaction type is required.";
    `TransactionTypeTooLong` "A transaction type is at most 20 characters.";
    `TransactionResultCodeRequired` "A transaction result code is required.";
    `TransactionResultCodeTooLong` "A transaction result code is at most 3
    characters."; `IsApprovedRequired` "Whether the event was approved is required.";
    `ConcurrentChange` "The payment changed while this was applied. Send it again."
    `CurrencyCodeRequired` "A currency code is required." is added here; `AmountBelowMinimum` and `CurrencyCodeTooLong` are reused from Recording payments.
- **Protection level of each field:** `Notification` and `PaymentEvent` are personal
  information with the payment they belong to; an unmatched notification is personal
  information too, since its body may name a customer's payment. `MaskedCardNumber` is
  removed before anything is stored or logged; no card number reaches the product. The
  three secrets are secrets by `Security.md` section 3: kept in the vault in production
  and in the secrets volume locally, read only by the service that answers requests,
  and never logged. The reference tables are at no level. Retention is D-9's.
- **Database privileges:** `ZapperPaymentsApiRole` gains `SELECT` and `INSERT` on
  `Notification`, so a notification is never rewritten; `SELECT`, `INSERT` and `UPDATE`
  on `PaymentEvent`, because a held event becomes applied; `SELECT` on the four new
  reference tables. No `DELETE`.
- **Addresses it calls:** None. Acme calls this Feature's operations; nothing in it calls
  out. The stub's calls to our API are listed under Infrastructure inventory.
- **Slow or costly operations:** None. Each notification is a few indexed reads and one
  save. Acme's own rate is limited by S-7's provider limit.
- **Infrastructure inventory:** the part `Zapper.PaymentsApi`'s "What it is" becomes "An
  API, which Zapper's app and Acme's notifications call", and the internet can reach it.
  The part `Acme Switch` (Acme's service that publishes reversals and refunds; every
  deployed environment, and locally the stub stands in for it; no data of ours held; the
  internet reaches it). The connections `Acme Pay` to `Zapper.PaymentsApi` (443; HTTPS;
  personal information; HTTP Basic with the Payment Advice username and password) and
  `Acme Switch` to `Zapper.PaymentsApi` (443; HTTPS; personal information; an
  HMAC-SHA256 signature with the webhook secret); locally both are `AcmeStub` to
  `Zapper.PaymentsApi` on 8443. No address depends on data received from outside.
- **Names the standards leave to planning:** the two operations, their request types and
  validators above; the schemes `AcmePaymentAdvice` and `AcmeTransactionStatus`; the
  permissions `SendPaymentAdvice` and `SendTransactionStatus`;
  `Zapper.ApiCommon/Security/BasicAuthenticationHandler.cs` with
  `BasicAuthenticationSchemeOptions`, and
  `Zapper.ApiCommon/Security/BodySignatureAuthenticationHandler.cs` with
  `BodySignatureAuthenticationSchemeOptions`, each given its credential as an option
  and refusing every request when it has none; `ICryptographyManager` gains
  `GetSha256Hash(string text)` and `GetHmacSha256Signature(ReadOnlySpan<byte> data,
  ReadOnlySpan<byte> key)`; `Zapper.PaymentsApi/AcmeNotificationOptions.cs` binding
  `Zapper:AcmeNotification`, with `IsEnabled` (`bool?`, `[Required]`), `MerchantId`,
  `PaymentAdviceUserName`, `PaymentAdvicePassword` and `WebhookSecret`, and
  `AcmeNotificationValidateOptions`, which requires the merchant id as a GUID and the
  three secrets when `IsEnabled` is true; it is bound without `ValidateOnStart` and read
  by the startup check; the secret files `Zapper__AcmeNotification__PaymentAdviceUserName`,
  `Zapper__AcmeNotification__PaymentAdvicePassword` and
  `Zapper__AcmeNotification__WebhookSecret`; `SecurityOptions.ProviderRateLimit`;
  `Zapper.PaymentsApi/Authentication.cs`; `ErrorMessageCode.ConcurrentChange`.
- **Decisions for this Feature only:**
  - **Applied inside the request** (Francois): a notification is checked, stored and
    applied in one save. A conflict with the worker's claim answers 500 and Acme's
    resend applies it. A lost Payment Advice costs nothing, because the worker's own
    answer sets the outcome; a reversal or refund arrives when the payment is no longer
    claimed. A notification whose every delivery fails through a fault of ours is lost,
    and SOLUTION.md says so.
  - **Comparing mapped fields** (Francois): duplicates and contradictions compare the
    fields this Feature maps, through the content hash in the unique index. An exact
    repeat is caught by `SaveOutcome.AlreadyExists`, as D-8 states; two different
    versions at the same moment are caught by `xmin` on the payment.
  - **Notifications switched off** (Francois): `AcmeNotificationOptions.IsEnabled` is
    true on the service that answers requests and false on the background service, so
    only the first holds the three secrets. Off, both operations answer 401. The merchant
    id is read here rather than from `AcmeOptions`, whose API key the first service must
    not hold, so `compose.yml` writes the same generated value into both.
  - **Both handlers leave the body buffered and rewound**, so the operation stores it as
    received; the body-signature handler reads it within the request size limit, as
    D-7 states.
  - **Acme's lists are text** (Francois), so a value Acme adds is recorded rather than
    refused; **Acme's times are text**, checked and read at UTC+02:00, because a time
    with no offset bound as `DateTimeOffset` would take the server's zone.
  - **The provider rate limit** is 2,400 a minute per address in `appsettings.json`:
    twice the Payment Advices our own sending can cause at D-5's 20 a second. The
    better production answer, an allow-list of Acme's published addresses, is in D-7's
    SOLUTION.md line.
  - **The stub, this Feature's part** (D-14, ruled by Francois): every kept outcome
    sends a Payment Advice about 1 second after the stub answers; a send not kept sends
    none. The card whose advice is sent twice sends it twice. The card whose reversal
    comes before the advice sends the reversal before it answers our send, then
    answers, then sends the advice. The extra undocumented member on every notification
    is a delivery count that changes on each delivery. `POST /stub/reversals`
    (`TransactionID`) and `POST /stub/refunds` (`TransactionID`, amount), each needing the stub routes' key (D-14), send a signed, approved (`00`) Transaction Status. A
    notification not answered 200 is sent three more times, 2 seconds apart. The stub
    calls `https://PaymentsApi:8443`, and reads the three secrets under Appendix A's
    names, `ACME_ADVICE_USERNAME`, `ACME_ADVICE_PASSWORD` and `ACME_WEBHOOK_SECRET`,
    from the same files. Its README states these as assumptions.
  - **`compose.yml`**: the init service also generates the three secret files with
    `openssl rand -hex 32` (D-13), mounted read-only into `PaymentsApi` and `AcmeStub` only, and the stub routes' key, by D-14; `PaymentsApi` gains `Zapper__AcmeNotification__IsEnabled` true and
    `Zapper__AcmeNotification__MerchantId`, the merchant id already generated for
    `Zapper__Acme__MerchantId`.
  - **The standard changes S-3 to S-7**, ruled by Francois on 2026-10-24.
  - **The reads through `database` are added in increment 2.** Increment 1 writes each
    acceptance test with its Given, its When, and its Thens on the API's answers and on
    the payment the app reads, because the notifications, the events and the changes'
    new source do not exist before increment 2. Increment 2 adds the reads through
    `database`, and the closing report names each test it changes. Ruled by Francois on
    2026-10-24.
  - **The delivery count is the member `DeliveryCount`**, a number, in the PascalCase of
    the Payment Advice's own members; the stub sends it on every notification. Ruled by
    Francois on 2026-10-24.
  - **Appendix A's worked example is signed with the webhook secret
    `k7Qf2mZr9XpL4vNcA8sTgH3wYbE6dJ5u`**, a value the brief publishes and no environment
    uses. `CryptographyManagerTests` keys the example's body, from the Transaction Status
    contract document, with it, and expects that document's `x-signature`. Ruled by
    Francois on 2026-10-24.
  - **A notification kept for a payment writes the payment's row** in the same save, so
    its `xmin` changes, even where nothing on the payment changed. Holding a
    notification otherwise adds rows and leaves the payment's row as it was, so a worker
    whose call was in flight saved its answer without seeing the held reversal or
    advice, and nothing applied it later. Now the worker's save meets `Conflict`,
    reloads the payment with the held facts and asks the Domain again, as
    `BackendCode.md` section 23 states; and a notification whose save meets the worker's
    answers 500 `ConcurrentChange`, and Acme's resend applies it.
    `PaymentSubmissionWorkerIntegrationTests` gains a reversal sent through the API
    while the worker's call is in flight. Found by the stub's reversal-first card in
    increment 6. Ruled by Francois on 2026-10-25.
  - **The stub's `/stub/` routes take their key as `Authorization: Bearer <key>`**, as
    Appendix A has our API send its key to Acme, and a refund's body is
    `{"TransactionID": ..., "Amount": ...}`, in Appendix A's PascalCase. Ruled by
    Francois on 2026-10-25.
- **Tests:**
  - C# unit: `PaymentTests` gains every value of `RecordPaymentAdviceResult` and
    `RecordTransactionStatusResult`; held events applied and made contradictions when
    the outcome is set; held advices confirmed and contradicted; refunds at exactly the
    authorised amount and one above it; a reversal after a refund. `AcmePaymentAdviceTests` and `AcmeTransactionStatusTests`: each
    mapped field changes `GetContent()`. `CryptographyManagerTests` gains
    `GetSha256Hash` against a known value and `GetHmacSha256Signature` against
    Appendix A's worked example.
  - API contract: `PaymentsApiV1ContractTests` covers the two new fragments.
  - Database integration: `RecordPaymentAdviceIntegrationTests` and
    `RecordTransactionStatusIntegrationTests` (every result, every validation key, a
    wrong username, a wrong password, a missing header, a wrong signature, a body
    changed by one byte, the other notification's proof, notifications switched off,
    and Appendix A's worked example accepted);
    `NotificationPersistenceIntegrationTests` (the unique index, nulls not distinct,
    and each foreign key) and `PaymentEventPersistenceIntegrationTests`;
    `PaymentSubmissionWorkerIntegrationTests` gains held events and held advices when
    the outcome is set. The migration, naming, reference table and error code tests
    cover the rest.
  - Authorization: `PaymentsApiV1AuthorizationTests` with the 401 sweep, whose two cases
    are the two operations (S-6).
  - Acceptance: `AcmeNotificationsAcceptanceTests`, one test per scenario. The acceptance
    host has notifications on, with credentials and a webhook secret made for the run.
  - Load: none, because neither operation is slow or costly.
  - Not tested: the `ConcurrentChange` path, because a test cannot place the worker's
    claim between the handler's load and its save, and the end-to-end script cannot
    cause it or see it (changed by Francois on 2026-10-24).

## Contracts

| Operation | Contract |
|---|---|
| `POST /v1/payment-advices` | `Docs/Contracts/PaymentsApi/v1/payment-advices/post.md` |
| `POST /v1/transaction-statuses` | `Docs/Contracts/PaymentsApi/v1/transaction-statuses/post.md` |

## Implementation order

1. **Failing acceptance tests.** One per scenario above, twenty-nine, failing. The two
   contracts move from `ai/Plans/Zapper_Team_Lead_Assement/Contracts/` to
   `Docs/Contracts/`, so their contract tests fail too.
2. **Schema and Domain.** The four reference tables and their rows, the new state and
   source rows, `Notification`, `PaymentEvent` and the change's notification, one
   migration with the grants; `AcmePaymentAdvice`, `AcmeTransactionStatus`,
   `NotificationEntity`, `PaymentEventEntity`, `RecordPaymentAdvice`,
   `RecordTransactionStatus`, and held events and advices in `RecordSendAnswer` and
   `RecordQueryAnswer`; the unit tests and the two persistence test classes.
   Reviewable on its own: nothing reaches it from outside yet, and the migration,
   naming and reference table tests check it.
3. **Acme's proof.** The two generic handlers, the two `ICryptographyManager` methods,
   `OperationSecurityTransformer` writing `http` `basic` and an `apiKey` in the header
   `x-signature` with the permission, `UseAuthentication` before `UseAuthorization`,
   the property name resolver reading `[JsonPropertyName]`, the provider rate limit,
   `AcmeNotificationOptions` with its validator and startup check, and
   `Authentication.cs`; the cryptographic inventory's rows for the webhook secret
   (HMAC-SHA256; used only by the body-signature handler; protects only the integrity
   of a Transaction Status) and the Payment Advice credentials (compared as SHA-256 in
   constant time). Reviewable on its own: no route uses it yet, and the manager's
   tests prove Appendix A's worked example.
4. **`RecordPaymentAdvice`.** The request, validator, handler and endpoint group, its
   codes in `ErrorMessageCode` with their message rows in a migration, and
   `RecordPaymentAdviceIntegrationTests`. Reviewable on its own: its contract test and
   every Payment Advice acceptance test pass.
5. **`RecordTransactionStatus`.** The same, `RecordTransactionStatusIntegrationTests`,
   and `PaymentsApiV1AuthorizationTests` with the 401 sweep. Reviewable on its own:
   every acceptance test and every contract test passes.
6. **The stub and `compose.yml`.** The stub's notifications, the two notification cards,
   the `/stub/` routes, the resends and the delivery count; the three generated secrets and the API's settings; the stub routes' key and its copy in `Code/Certificates/`, with the cryptographic inventory's row for it (random 32 bytes; used only by the stub's `/stub/` routes; protects only those routes, locally); the infrastructure inventory's rows. Reviewable on
   its own: `docker compose up`, then `curl`, takes a seeded card through a duplicate
   advice, an out-of-order reversal, a refund and a reversal.
7. **README.md and SOLUTION.md.** The README says how to send a reversal and a refund through the stub's routes, with the key in `Code/Certificates/AcmeStubRoutes.key`, and watch the status change. SOLUTION.md gains this
   Feature's facts: a notification applied inside its request and the one that can be
   lost, comparing mapped fields and a removed field being settled with Acme, a Payment
   Advice never setting the outcome, the authorised amount being authoritative and an
   approval above it, events and advices held until the worker's answer, and why a notification never moves the worker's lease, the merchant account check, the
   switch that keeps the secrets off the background service, the provider rate limit
   and the allow-list of Acme's addresses for production, the stub's assumptions
   (delays, resends 2 seconds apart, the delivery count, reversals and refunds always
   approved), and the facts for Francois's AI workflow and validation sections.

# Feature: SQL Daily Summary

## What it delivers

Zapper can produce, for any South African day, the daily payment summary the brief
asks for, grouped by sub-merchant and currency, with the count and value of approved
payments, declined payments, reversals and refunds, and beside them the pending,
failed and unresolved payments. It is one plain `SELECT` in
`Docs/Reports/DailyPaymentSummary.sql`, opened in pgAdmin, given a date, and run as
the data reader of D-18, which this Feature creates. Three indexes make a day cheap
to find in 5 years of payments. The Migrator's Debug build seeds eighteen finished
payments over two days, so the summary shows data on the first run. Settlement
depends on it.

## Rules

- **R-1: The day.** The summary is for one date, written once at the top of the
  file. A date is a South African calendar day, from 00:00 to 24:00 at UTC+02:00.
- **R-2: Rows.** One row for each sub-merchant and currency with at least one count
  above zero on that day, showing Zapper's reference for the sub-merchant, its
  trading name and the currency code, ordered by the reference and then the
  currency code.
- **R-3: Approved.** A payment Acme approved counts once, on the day of Acme's
  transaction time, whatever its status is now, with the authorised amount as its
  value.
- **R-4: Declined.** A payment Acme declined counts once, on the day of Acme's
  transaction time, whatever its status is now, with the amount requested as its
  value.
- **R-5: Reversals and refunds.** Each reversal and each refund applied to a payment
  counts on the day Acme Switch processed it, with the event's own amount. One that
  is held, not applied, or a contradiction does not count. An approval and its
  reversal each count on their own day.
- **R-6: Pending, failed and unresolved.** A payment whose status is now `Pending`,
  `Failed` or `Unresolved` counts on the day it was recorded, with the amount
  requested. An `Unresolved` payment Acme approved or declined also counts by R-3 or
  R-4.
- **R-7: Values.** Counts are whole numbers. Values are the amount in the lowest
  unit divided by 100, with exactly two decimal places. Nothing is `0` and `0.00`.
- **R-8: The data reader.** The data reader role can read every table, including
  any table added later, and can change none.

Who may see each record: the summary is read only by a person signed in as the data
reader; no operation serves it. Its rows are counts and totals per sub-merchant, at
no protection level, and it stores nothing, so nothing is kept, corrected or
de-identified. The data reader reads every table, personal information and the
encrypted token included, by D-18. Acme always sends `TransactionDateTime` on a final
answer: an assumption for SOLUTION.md, ruled by Francois.

## Failure modes

| What goes wrong | What the caller receives | Recovery |
|---|---|---|
| The date is not a real date, such as `2026-02-30`, or not written as `YYYY-MM-DD` | PostgreSQL's own error; no rows | The person corrects the date line |
| The date line is removed or changed into another form | A syntax error in the client; the acceptance tests fail, because each requires exactly one date line | The file is restored from the repository |
| A person signs in without the reader's certificate, or with another role's | The connection is refused by `pg_hba.conf` | The person uses `ZapperDataReaderRole`'s certificate |
| The reader's certificate is within 30 days of expiry | It is renewed by the next `docker compose up`, like every role's | The person points pgAdmin at the new files |
| The data reader tries to change a row | `permission denied` | None needed; the role reads only |
| An approval was stored without Acme's time, against the assumption | It counts on no day, with no warning | A person finds it by querying for a missing Acme time; SOLUTION.md states the cost |
| A held reversal or refund is applied after its day was summarised | That day's earlier run left it out; a new run includes it on its processing day | The person runs the day again; SOLUTION.md says so |
| A payment becomes `Unresolved` after its day was summarised | The money columns of its day do not change; its recording day's `Unresolved` count does | The person holds back that payment by hand when settling |
| A `Pending` payment finishes after its day was summarised | A new run counts it under its final status | None needed; `Pending` is a snapshot |
| The database cannot be reached | The client's connection error | The person starts the stack |

## Scenarios

```gherkin
Feature: SQL Daily Summary

  Background:
    Given the sub-merchant "ZAP0004182" is registered, trading as "Cafe Del Sol"
    And the sub-merchant "ZAP0005317" is registered, trading as "Book Nook"
    And the customer "CUS-4001" holds a stored card

  @R-3
  Scenario: Approved payments are counted with the authorised amount
    Given a payment of 1999 "ZAR" at "ZAP0004182" that Acme approved for 1999 at 2040-03-02T10:00:00.000 SAST
    And a payment of 1999 "ZAR" at "ZAP0004182" that Acme approved for 1500 at 2040-03-02T11:00:00.000 SAST
    When the summary runs for 2040-03-02
    Then the row for "ZAP0004182" in "ZAR" shows 2 approved payments worth 34.99

  @R-4
  Scenario: Declined payments are counted with the requested amount
    Given a payment of 2500 "ZAR" at "ZAP0004182" that Acme declined with response code "51" at 2040-03-02T10:00:00.000 SAST
    When the summary runs for 2040-03-02
    Then the row for "ZAP0004182" in "ZAR" shows 1 declined payment worth 25.00

  @R-1
  Scenario: Day runs from midnight to midnight South African time
    Given a payment of 1000 "ZAR" at "ZAP0004182" that Acme approved for 1000 at 2040-03-02T00:00:00.000 SAST
    And a payment of 2000 "ZAR" at "ZAP0004182" that Acme approved for 2000 at 2040-03-02T23:59:59.999 SAST
    And a payment of 4000 "ZAR" at "ZAP0004182" that Acme approved for 4000 at 2040-03-03T00:00:00.000 SAST
    When the summary runs for 2040-03-02
    Then the row for "ZAP0004182" in "ZAR" shows 2 approved payments worth 30.00

  @R-3 @R-5
  Scenario: Reversal on a later day counts on its own day
    Given a payment of 1999 "ZAR" at "ZAP0004182" that Acme approved for 1999 at 2040-03-02T10:00:00.000 SAST
    And Acme Switch reversed it for 1999 at 2040-03-03T09:00:00.000 SAST
    When the summary runs for 2040-03-02 and for 2040-03-03
    Then on 2040-03-02 the row for "ZAP0004182" in "ZAR" shows 1 approved payment worth 19.99 and 0 reversals worth 0.00
    And on 2040-03-03 it shows 0 approved payments worth 0.00 and 1 reversal worth 19.99

  @R-5
  Scenario: Several partial refunds are each counted
    Given a payment of 1999 "ZAR" at "ZAP0004182" that Acme approved for 1999 at 2040-03-02T10:00:00.000 SAST
    And Acme Switch refunded it for 500 at 2040-03-02T12:00:00.000 SAST and for 700 at 2040-03-02T15:00:00.000 SAST
    When the summary runs for 2040-03-02
    Then the row for "ZAP0004182" in "ZAR" shows 2 refunds worth 12.00

  @R-5
  Scenario: Reversal or refund that was not applied is not counted
    Given a payment of 1999 "ZAR" at "ZAP0004182" that Acme approved for 1999 at 2040-03-02T10:00:00.000 SAST
    And Acme Switch sent a refund of 500 for it at 2040-03-02T12:00:00.000 SAST with isApproved false
    And a payment of 1000 "ZAR" at "ZAP0004182" recorded at 2040-03-02T13:00:00.000 SAST and not yet answered
    And Acme Switch sent a reversal of 1000 for it at 2040-03-02T13:05:00.000 SAST, which is held
    When the summary runs for 2040-03-02
    Then the row for "ZAP0004182" in "ZAR" shows 0 refunds worth 0.00 and 0 reversals worth 0.00

  @R-6
  Scenario: Pending, failed and unresolved payments count on the day they were recorded
    Given a payment of 1000 "ZAR" at "ZAP0004182" recorded at 2040-03-02T09:00:00.000 SAST and not yet answered
    And a payment of 2000 "ZAR" at "ZAP0004182" recorded at 2040-03-02T23:00:00.000 SAST that Acme failed at 2040-03-03T00:30:00.000 SAST
    And a payment of 4000 "ZAR" at "ZAP0004182" recorded at 2040-03-02T10:00:00.000 SAST whose send timed out and whose query Acme refused
    When the summary runs for 2040-03-02
    Then the row for "ZAP0004182" in "ZAR" shows 1 pending worth 10.00, 1 failed worth 20.00 and 1 unresolved worth 40.00

  @R-3 @R-6
  Scenario: Unresolved payment that Acme approved counts as approved and as unresolved
    Given a payment of 1999 "ZAR" at "ZAP0004182" recorded and approved for 1999 at 2040-03-02T10:00:00.000 SAST
    And Acme Switch refunded it for 2500 at 2040-03-02T12:00:00.000 SAST, which made it unresolved
    When the summary runs for 2040-03-02
    Then the row for "ZAP0004182" in "ZAR" shows 1 approved payment worth 19.99, 0 refunds worth 0.00 and 1 unresolved worth 19.99

  @R-2
  Scenario: Each sub-merchant and currency has its own row
    Given a payment of 1000 "ZAR" at "ZAP0004182" that Acme approved for 1000 at 2040-03-02T10:00:00.000 SAST
    And a payment of 2000 "USD" at "ZAP0004182" that Acme approved for 2000 at 2040-03-02T10:00:00.000 SAST
    And a payment of 4000 "ZAR" at "ZAP0005317" that Acme approved for 4000 at 2040-03-02T10:00:00.000 SAST
    When the summary runs for 2040-03-02
    Then the rows are, in order, "ZAP0004182" "Cafe Del Sol" "USD", "ZAP0004182" "Cafe Del Sol" "ZAR" and "ZAP0005317" "Book Nook" "ZAR"

  @R-2
  Scenario: Sub-merchant with nothing on the day has no row
    Given a payment of 1000 "ZAR" at "ZAP0005317" that Acme approved for 1000 at 2040-03-01T10:00:00.000 SAST
    And a payment of 1000 "ZAR" at "ZAP0004182" that Acme approved for 1000 at 2040-03-02T10:00:00.000 SAST
    When the summary runs for 2040-03-02
    Then there is a row for "ZAP0004182" and none for "ZAP0005317"

  @R-7
  Scenario: Values show two decimals and empty columns show zero
    Given a payment of 100000 "ZAR" at "ZAP0004182" that Acme approved for 100000 at 2040-03-02T10:00:00.000 SAST
    When the summary runs for 2040-03-02
    Then the row for "ZAP0004182" in "ZAR" shows 1 approved payment worth 1000.00
    And every other count is 0 and every other value is 0.00

  @R-8
  Scenario: Data reader reads every table and changes none
    When the data reader reads each table in the database and tries to change a payment
    Then every read succeeds and the change is refused
```

The references and values are carried as each test's discriminator requires, by
`Testing.md` section 7, and a Then reads only the rows of the test's own
sub-merchants. The Given is arranged through `database`: each record is made by its
own constructor and moved by its own domain methods (`RecordSendAnswer`,
`RecordQueryAnswer`, `RecordTransactionStatus`), each given a `FakeTimeProvider` set
to its stated time, and saved with `SaveChangesAsync(CancellationToken.None)`. "The
summary runs" is the file with only its date line replaced, run as the data reader,
as "How it is built" states. "Reads each table" reads every table the model maps;
that R-8 also covers a table added later is proven by `DataReaderRoleIntegrationTests`.

## How it is built

- **API:** `Zapper.PaymentsApi` is unchanged. This Feature adds no operation: the
  summary is a SQL file (D-16), the role and its grants are in `compose.yml` and a
  migration (D-18), and the seed is in the Migrator.
- **Frontend:** None. This Feature is tech-only.
- **Domain:** None. No entity, enum or method is added; the summary reads what
  Recording payments, Submitting Payments to Acme and Acme Notifications store.
- **The file**, `Docs/Reports/DailyPaymentSummary.sql`:
  - It opens with a comment saying what it is, that it runs as
    `ZapperDataReaderRole`, and that only the date is changed. Its first common
    table expression is `"SummaryDay"`, on two lines of its own:
    `-- The South African day to summarise, as YYYY-MM-DD. Change only this date.`
    then `SELECT DATE '2026-01-01' AS "Day"`. That date line is the only line the
    acceptance tests replace, and they fail unless the file holds exactly one line
    of the form `SELECT DATE 'YYYY-MM-DD' AS "Day"`.
  - The day's start is the date at 00:00 less 2 hours, read as UTC
    (`("Day"::timestamp - INTERVAL '2 hours') AT TIME ZONE 'UTC'`), and its end is
    the start plus one day; a time counts when at or after the start and before the
    end. It never uses `AT TIME ZONE '+02'`, which PostgreSQL reads as two hours west
    of UTC.
  - States, event types and event states are found by joining their reference
    tables on `Name` (`'Approved'`, `'Reversal'`, `'Applied'` and so on), never by
    numeric id.
  - **Approved** (R-3): payments with at least one change into `Approved` and an
    `AcmeTransactionAt` inside the day; value `AuthorisedAmount`. **Declined** (R-4):
    the same with a change into `Declined`; value `Amount`. Acme's outcome is read
    from the changes because D-8 sets it once and every change is recorded.
  - **Reversals and refunds** (R-5): events whose state is `Applied` and whose
    `AcmeProcessedAt` is inside the day, of type `Reversal` or `Refund`; value the
    event's `Amount`; sub-merchant and currency from its payment.
  - **Pending, failed and unresolved** (R-6): payments whose `RecordedAt` is inside
    the day and whose state is now `Recorded` or `OutcomeUnknown` (shown as
    `Pending`), `Failed`, or `Unresolved`; value `Amount`.
  - Each fact is one row of sub-merchant, currency, column and amount; the rows are
    grouped by sub-merchant and currency, so only groups with a fact appear (R-2).
  - Each value is `(COALESCE(SUM(...), 0)::numeric / 100)::numeric(20, 2)`, and each
    count `COUNT(...)`, 0 where there is nothing (R-7).
  - The seventeen columns, in order: `SubMerchantReference`, `TradingName`,
    `CurrencyCode`, `ApprovedCount`, `ApprovedValue`, `DeclinedCount`,
    `DeclinedValue`, `ReversalCount`, `ReversalValue`, `RefundCount`, `RefundValue`,
    `PendingCount`, `PendingValue`, `FailedCount`, `FailedValue`, `UnresolvedCount`,
    `UnresolvedValue`. Ordered by `SubMerchantReference`, then `CurrencyCode`.
  - It is one `SELECT` and nothing else: no transaction, no `psql` command, no
    variable, so it runs unchanged in pgAdmin, `psql` or any other client.
- **Data model:** no table and no column is added. Three indexes, configured in the
  model: `IX_Payment_AcmeTransactionAt`, `IX_Payment_RecordedAt` and
  `IX_PaymentEvent_AcmeProcessedAt`, each on the one column, so the file reads one
  day rather than 5 years. The grouping runs over that day's rows only, so no index
  leads with the sub-merchant; `IX_Payment_SubMerchantId` already exists from
  Recording payments. Considered and rejected: a covering index with `INCLUDE`,
  because the payment row is updated often and its index would rarely be read
  alone.
- **Protection level of each field:** no field is added. The summary's rows are
  counts and totals per sub-merchant, at no protection level. The data reader's
  connection carries sensitive data, by D-18.
- **Database privileges:** `ZapperPaymentsApiRole` gains nothing. One migration,
  holding only `migrationBuilder.Sql` calls besides the three indexes, gives
  `ZapperDataReaderRole`:
  `GRANT USAGE ON SCHEMA "Error", "Migration", "Merchant", "Customer", "Payment"`;
  `GRANT SELECT ON ALL TABLES IN SCHEMA` the same five; and
  `ALTER DEFAULT PRIVILEGES GRANT SELECT ON TABLES` and
  `ALTER DEFAULT PRIVILEGES GRANT USAGE ON SCHEMAS`, each `TO
  "ZapperDataReaderRole"`. `ALTER DEFAULT PRIVILEGES` has no `FOR ROLE`, so it
  applies to whichever role runs the migrations: the Migrator's role in
  `compose.yml` and in production, the container's superuser in tests. Default
  privileges are kept per database, so every database cloned from the test template
  carries them. `Down` revokes all four. No `INSERT`, `UPDATE`, `DELETE` or schema
  change for the role.
- **Addresses it calls:** None.
- **Slow or costly operations:** None. No operation is added. The file reads one day
  through the three indexes; in production it runs on a read replica (D-16).
- **Infrastructure inventory:** one connection is added: `A person's database
  client, such as pgAdmin` to `The database, ZapperDb`; 5432, locally `127.0.0.1`
  port 5434; PostgreSQL over TLS; "Sensitive data"; a client certificate for the
  role `ZapperDataReaderRole`; the last column empty, because the address depends on
  no data from outside. The cryptographic inventory gains one row: the client
  certificate of `ZapperDataReaderRole`, issued by the local database authority;
  used only to sign a person in to `ZapperDb` as that role, locally; never for
  another role, a service or a deployed environment; protects only that sign-in.
- **Names the standards leave to planning:** `Docs/Reports/DailyPaymentSummary.sql`,
  `"SummaryDay"` and `"Day"`, and the seventeen column names above;
  `ZapperDataReaderRole`; the three indexes; `Zapper.Migrator/Seeding/SeedTimeProvider.cs`,
  a `TimeProvider` whose `GetUtcNow` returns the time the seed last set;
  `SqlDailySummaryAcceptanceTests`; `DataReaderRoleIntegrationTests`.
- **Decisions for this Feature only:**
  - **Facts, not status** (Francois): approvals, declines, reversals and refunds
    count by what Acme did, whatever the payment's status is now, so a day's money
    columns do not change once it has passed, except for an event applied late. The
    `Unresolved` columns are a warning beside them, and the person settling holds
    those payments back by hand.
  - **Acme always sends `TransactionDateTime`** (Francois): approvals and declines
    count only by it, with no fallback. An approval stored without it counts on no
    day, and SOLUTION.md states that cost.
  - **The seed's clock** (Francois): `AddZapperManagers` registers `TimeProvider.System`
    in place of `AddZapperApiCommon`, which calls it, so the Migrator's seed reads the
    current time from an injected `TimeProvider` to find the two days before its first
    run; `BackendCode.md` section 9 states it. Ruled by Francois on 2026-10-25.
  - **Two decimal places** (Francois): every currency on Zapper's list has two;
    a currency with another number would need its decimal places in
    `Payment.CurrencyType`.
  - **No standard change for the tests** (Francois): the acceptance tests run the
    file on D-16's authority. Each test reads the file, asserts with Shouldly that it
    exists and holds exactly one date line, replaces that line with its own date,
    opens an `NpgsqlConnection` to the run's database as the container's superuser,
    runs `SET ROLE "ZapperDataReaderRole"`, then runs the file with an
    `NpgsqlCommand` and reads the rows. No EF raw SQL method is used, and no row is
    written by SQL.
  - **Days in 2040**: every scenario uses days in 2040, after any time the
    acceptance host's `FakeTimeProvider` reaches, so a `Pending` payment a test makes
    is never due and the host's payment worker never claims it.
  - **The role in `compose.yml`**: the `roles` script and the `certificates` script
    each add `"${solution}DataReaderRole"` beside `"${solution}MigratorRole"` in the
    list of roles they make, so the role is created, and its certificate issued and
    renewed, exactly as the others are. The certificate and key land in
    `Code/Certificates/`, which git already ignores. The template fixture creates the
    role before the migrations run, as it creates the API's role.
  - **The seed** (D-12, Francois): `LocalSeeder` adds eighteen payments, each written
    only where no payment holds its app reference. Day 1 and day 2 are the two South
    African days before the first run. Every payment uses the stored card of the
    seed's approving card and its sub-merchant: A is the first sub-merchant the seed
    writes, B the second. Each is recorded at its stated SAST time, answered one
    second later (Acme's transaction time being that answer's time), and moved only
    by its own domain methods, with `SeedTimeProvider` set before each step; Acme's
    answers, notification bodies and event identities are made in Appendix A's
    shapes. Amounts are in the lowest unit.

    | Reference | At | Currency | Day and time | Amount | What happens |
    |---|---|---|---|---|---|
    | `SEED-01` | A | ZAR | 1, 10:00 | 15000 | Approved for 15000 |
    | `SEED-02` | A | ZAR | 2, 11:00 | 8000 | Approved for 6000 |
    | `SEED-03` | B | USD | 1, 09:30 | 2500 | Approved for 2500 |
    | `SEED-04` | A | ZAR | 1, 12:00 | 4500 | Declined with `51` |
    | `SEED-05` | B | USD | 2, 14:00 | 1200 | Declined with `05` |
    | `SEED-06` | A | USD | 1, 15:00 | 3000 | Failed by Acme's `Failure` |
    | `SEED-07` | B | ZAR | 2, 16:00 | 7000 | Failed by Acme's `Failure` |
    | `SEED-08` | A | ZAR | 1, 13:00 | 20000 | Approved for 20000; reversed for 20000 on day 2 at 09:00 |
    | `SEED-09` | B | USD | 1, 17:00 | 5000 | Approved for 5000; reversed for 5000 on day 2 at 10:00 |
    | `SEED-10` | A | USD | 2, 08:30 | 4000 | Approved for 4000; refunded 4000 on day 2 at 18:00 |
    | `SEED-11` | B | ZAR | 1, 11:30 | 9000 | Approved for 9000; refunded 2000 at 14:00 and 3000 at 19:00 on day 1 |
    | `SEED-12` | A | ZAR | 2, 12:30 | 6000 | Approved for 6000; a refund of 9000 on day 2 at 15:30 makes it `Unresolved` |
    | `SEED-13` | B | USD | 1, 08:00 | 3500 | Send timed out; query refused at 08:05; `Unresolved` |
    | `SEED-14` | A | USD | 1, 16:00 | 2200 | Approved for 2200; a refund of 1000 on day 1 at 18:30 with `isApproved` false |
    | `SEED-15` | B | ZAR | 2, 13:30 | 11000 | Approved for 11000 |
    | `SEED-16` | A | USD | 2, 19:00 | 1500 | Approved for 1500 |
    | `SEED-17` | B | ZAR | 1, 10:30 | 2600 | Declined with `51` |
    | `SEED-18` | B | USD | 2, 17:30 | 4200 | Approved for 4000 |

    On every run the Migrator logs, at `Information`, the two days the seeded payments
    are on, read back from `SEED-01` and `SEED-02`, so the person knows which dates to
    put into the file.
- **Tests:**
  - C# unit: none, because no domain, policy or manager method is added.
  - API contract: none, because no operation is added.
  - Database integration: `DataReaderRoleIntegrationTests` under `Database/`, on its
    class's cloned database, as the container's superuser with
    `SET ROLE "ZapperDataReaderRole"`: it reads every table the model maps; an
    insert, an update and a delete on a payment are each refused; and a table created
    after the migrations, by the role that ran them, is readable by the reader. The
    migration test covers `Down`; the database naming test covers the three indexes.
  - Acceptance: `SqlDailySummaryAcceptanceTests`, one test per scenario, twelve.
  - Authorization: none, because no operation is added.
  - Load: none, because no operation is slow or costly.
  - Not tested: the seeded payments, which a person checks by hand in pgAdmin by the
    README, because the end-to-end script calls API endpoints only (changed by
    Francois on 2026-10-24); connecting pgAdmin, which the README walks through.

## Contracts

None. This Feature adds no API operation: the summary is a SQL file (D-16).

| Operation | Contract |
|---|---|
| None | None |

## Implementation order

1. **Failing acceptance tests.** One per scenario above, twelve, failing. The
   template fixture creates `ZapperDataReaderRole` before the migrations run. Each
   summary test fails on Shouldly's assertion that the file exists; the R-8 test
   fails on its assertion that each read succeeds, because the role has no grant.
   No production code.
2. **The data reader role.** `compose.yml`'s `roles` and `certificates` scripts; the
   migration with the grants and default privileges; `DataReaderRoleIntegrationTests`;
   the infrastructure inventory's connection and the cryptographic inventory's row.
   Reviewable on its own: the R-8 scenario passes, and pgAdmin connects with the
   issued certificate after `docker compose up`.
3. **The summary.** `Docs/Reports/DailyPaymentSummary.sql`, and the three indexes in
   the model with their migration. Reviewable on its own: every acceptance test
   passes, and the naming test checks the indexes.
4. **The seed.** `SeedTimeProvider`, the eighteen payments in `LocalSeeder`, and the
   log line of the two days. Reviewable on its own: `docker compose up` run twice
   leaves eighteen seeded payments, and the file run in pgAdmin for each logged day
   shows rows for both sub-merchants in both currencies.
5. **README.md and SOLUTION.md.** The README says how to connect pgAdmin as
   `ZapperDataReaderRole` (host `localhost`, port 5434, database `ZapperDb`, SSL mode
   `verify-full`, the three files in `Code/Certificates/`, no password), open the
   file, set one of the two logged days, and run it, and how to see the `Pending`
   columns by recording a payment with the stub's late-answer card and running the
   summary for today within the 45 seconds that card holds it. SOLUTION.md gains this
   Feature's facts: the summary as one `SELECT` run in a client and why not an
   endpoint; counting facts rather than status, and `Unresolved` as a warning held
   back by hand; Acme's time as the only time and the cost if it is missing; two
   decimal places; Acme's outcome read from the change history; the three indexes;
   the data reader role, its default privileges, reading every table, and production
   as a group role of personal IAM accounts on a read replica with `Customer`
   limited by membership; why no seeded payment is `Pending` and why the seeded days
   stay those of the first run; how the schema would evolve, a settled summary stored
   per closed day once volume or audit needs it; and the facts for Francois's AI
   workflow and validation sections.

# Feature: End to End validation checks

## What it delivers

Zapper's reviewers can run one script against the running stack, from their own machine,
and see each case in the brief end in its expected status, through real TLS, the worker,
the real Acme client and the stub. The script calls API endpoints only: our API's and the
stub's routes. It walks ten cases, the brief's eight and two more from D-14 that prove D-3,
and reports each by its scenario title. CI runs it on every pull request and every push to
`dev`, `test` and `main` (D-15), and the README says how to run it. It adds no code to the
solution and nothing to the stub: it checks what the four Features before it built. The
records behind cases 3, 4, 5, 9 and 10, which the read answer does not show, are proven by
those Features' acceptance tests.

## Rules

- **R-1: Approved.** A payment with the seeded approving card ends `Approved`, with response
  code `00` and an authorised amount equal to the amount sent.
- **R-2: Declined.** A payment with each seeded declining card ends `Declined`, with that
  card's response code: `05`, `51` and `54`.
- **R-3: Submitted twice.** Recording the same payment twice with the same app reference
  returns the same id both times, and the payment ends `Approved`.
- **R-4: Payment Advice delivered twice.** A payment whose Payment Advice Acme sends twice
  ends `Approved`.
- **R-5: Out of order.** A payment whose reversal Acme Switch sends before Acme answers the
  send ends `Reversed`, with a reversed total equal to its authorised amount.
- **R-6: Reversal after approval.** An approved payment that Acme Switch reverses ends
  `Reversed`, with a reversed total equal to its authorised amount and a refunded total of 0.
- **R-7: Refund after approval.** An approved payment refunded in part ends
  `PartiallyRefunded`, with that refunded total. Refunded for the rest, it ends `Refunded`,
  with a refunded total equal to its authorised amount.
- **R-8: No answer in time.** A payment whose send Acme does not answer within our time limit
  reads `Pending` first, and ends `Approved` once a query resolves it.
- **R-9: Never received.** A payment that Acme never received ends `Approved`, once a query
  finds it missing and it is sent again.
- **R-10: A gateway error once.** A payment whose first send Acme answers with 503 ends
  `Approved`, once a query has been made.
- **R-11: The status holds.** Once every case has reached its expected status, each case's
  payment is read once more and still shows that status, so a notification that arrives
  after the status was first read cannot change it unseen.

Who may see each record: the script reads payments as Recording payments R-8 states, and
stores nothing. It sends only the seeded, synthetic customers and sub-merchants, and prints
only payment ids, references, statuses, codes and amounts. No personal information is kept,
corrected or de-identified by it.

## Failure modes

| What goes wrong | What the caller receives | Recovery |
|---|---|---|
| `curl` or `jq` is not installed | The script stops before any request, naming the missing program; exit 1 | The reviewer installs it; the README lists both |
| `Code/Certificates/ca.crt` or `Code/Certificates/AcmeStubRoutes.key` is missing | The script stops before any request, saying so; exit 1 | `docker compose up` writes it |
| The API does not answer `GET /v1/health` with 200 within 2 minutes | The script stops, printing the last answer or `curl`'s error; exit 1 | The reviewer runs `docker compose up -d --build --wait`, and reads `docker compose logs` |
| Recording a payment is refused, such as `CustomerHasNoStoredCard` | That case fails, printing the status and `code`; the other cases carry on; exit 1 | The seed is written by the Migrator's Debug build, which `compose.yml` builds |
| A case reaches a final status other than its expected one | That case fails at once, printing both statuses; exit 1 | A person reads the payment's records in pgAdmin and the API's log |
| A case is still `Pending` 2 minutes after it was recorded | That case fails, printing the last status read; exit 1 | As above; the stub's log shows what it answered |
| The stub's reversal or refund route refuses the call | Case 6 or 7 fails, printing the HTTP status; exit 1 | The reviewer checks the stub's key, as Acme Notifications' README describes |
| Our API answers 429 | No failure: the script waits for `Retry-After`, or 5 seconds where it is missing, then asks again, within the case's 2 minutes | None needed |
| A payment changes status after it first passed | The read at the end fails that case, printing both statuses; exit 1 | A person reads the payment's notifications and changes in pgAdmin |
| In CI, the script fails | The step fails, and the next step prints `docker compose logs` | The person reads the logs in the run |

## Scenarios

```gherkin
Feature: End to End validation checks

  Background:
    Given the stack is running from "docker compose up", with the seeded stored cards and sub-merchants
    And each payment is recorded at the first seeded sub-merchant, for 1999 "ZAR", with an app reference made for this run

  @R-1 @R-11
  Scenario: Payment with the approving card is approved
    When the app records a payment with the approving card
    Then the payment reads status "Approved", response code "00" and an authorised amount of 1999
    And it still reads "Approved" at the end of the run

  @R-2 @R-11
  Scenario: Payments with the declining cards are declined with their codes
    When the app records one payment with each declining card
    Then the payments read status "Declined" with response codes "05", "51" and "54"
    And each still reads "Declined" at the end of the run

  @R-3 @R-11
  Scenario: Same payment submitted twice returns the first payment
    When the app records a payment with the approving card
    And the app records the same payment again with the same app reference
    Then both answers are 201 with the same id
    And the payment reads status "Approved"
    And it still reads "Approved" at the end of the run

  @R-4 @R-11
  Scenario: Payment Advice delivered twice leaves the payment approved
    When the app records a payment with the card whose Payment Advice is sent twice
    Then the payment reads status "Approved"
    And it still reads "Approved" at the end of the run

  @R-5 @R-11
  Scenario: Reversal that arrives before the Payment Advice leaves the payment reversed
    When the app records a payment with the card whose reversal comes before the Payment Advice
    Then the payment reads status "Reversed", an authorised amount of 1999 and a reversed total of 1999
    And it still reads "Reversed" at the end of the run

  @R-6 @R-11
  Scenario: Approved payment reversed by Acme Switch shows as reversed
    Given the app recorded a payment with the approving card, which reads status "Approved"
    When the stub's reversal route reverses that payment
    Then the payment reads status "Reversed", a reversed total of 1999 and a refunded total of 0
    And it still reads "Reversed" at the end of the run

  @R-7 @R-11
  Scenario: Approved payment refunded in two parts shows as partially refunded then refunded
    Given the app recorded a payment with the approving card, which reads status "Approved"
    When the stub's refund route refunds 500 of that payment
    Then the payment reads status "PartiallyRefunded" and a refunded total of 500
    And when the stub's refund route refunds 1499 of that payment
    Then the payment reads status "Refunded" and a refunded total of 1999
    And it still reads "Refunded" at the end of the run

  @R-8 @R-11
  Scenario: Payment that Acme does not answer in time is approved after a query
    When the app records a payment with the late-answer card
    Then the payment's first read shows status "Pending"
    And the payment then reads status "Approved"
    And it still reads "Approved" at the end of the run

  @R-9 @R-11
  Scenario: Payment Acme never received is sent again and approved
    When the app records a payment with the never-received card
    Then the payment reads status "Approved"
    And it still reads "Approved" at the end of the run

  @R-10 @R-11
  Scenario: Payment that meets a gateway error once is approved after a query
    When the app records a payment with the card whose first send is answered 503
    Then the payment reads status "Approved"
    And it still reads "Approved" at the end of the run
```

Each scenario is one case of the script, named by its scenario title, by `Testing.md`
section 7's exception and `feature-execution` step 8 (ruled by Francois on 2026-10-24). No
C# acceptance test is written for them. "Reads status" means the script polls
`GET /v1/payments/{id}` until the status is final or the case's 2 minutes pass, as "How it
is built" states. The cards are the seeded stored cards of D-12, one per stub behaviour of
D-14, named by the customer references Recording payments' README lists.

## How it is built

- **API:** `Zapper.PaymentsApi`, unchanged. The script calls `GET /v1/health`,
  `POST /v1/payments` and `GET /v1/payments/{id}`, and the stub's reversal and refund routes.
- **Frontend:** None. This Feature is tech-only.
- **Domain:** None. No entity, enum or method is added.
- **The script**, `AcmeStub/end-to-end.sh`, outside the standards with the stub it drives
  (`Testing.md` section 1, D-14); only two repository rules reach it: no committed secret,
  and no em dash. A `bash` script using `curl` and `jq` only, run from the repository root.
  In this order:
  1. It checks that `curl` and `jq` are installed and that `Code/Certificates/ca.crt` and `Code/Certificates/AcmeStubRoutes.key` exist, and stops naming what is missing.
  2. It waits for `GET https://localhost:8443/v1/health` to answer 200, asking every 2
     seconds for up to 2 minutes. Every call to our API uses `--cacert
     Code/Certificates/ca.crt`. No call follows a redirect.
  3. It makes the run's references from the moment it started, in UTC, as
     `E2E-`, the start written as `yyyyMMddHHmmss`, a hyphen and the case number, such as
     `E2E-20261024143000-3`, so the script can run again against the same
     stack without meeting Recording payments R-5. Case 3 sends its one reference twice.
  4. It records the payments of every case one after another, each for 1999 `ZAR` at the
     first seeded sub-merchant, with the seeded customer of its card. The seeded customer
     and sub-merchant references are variables at the top of the script, with the values
     Recording payments' README lists.
  5. It polls each case that has not reached its expected status every 2 seconds. A final
     status other than the expected one fails the case at once. A case still not at its
     expected status 2 minutes after it was recorded fails, with the last status read. Case
     8 also fails when its first read is not `Pending`.
  6. Cases 6 and 7, once their payment reads `Approved`, call the stub's reversal route, or
     its refund route for 500 and then, once `PartiallyRefunded`, for 1499, at `https://localhost:$ACME_STUB_PORT`, the variable read with 8444 as its default, with `--cacert Code/Certificates/ca.crt` and the key read from `Code/Certificates/AcmeStubRoutes.key`, then poll again.
  7. A 429 from our API is not a failure: the script waits for `Retry-After`, or 5 seconds
     where it is missing, and asks again, within the case's 2 minutes.
  8. Once every case has passed, it reads each case's payment once more (R-11).
  9. It prints one line per case: `PASS` or `FAIL`, the scenario title, the expected status
     and the status read. It exits 0 when every case passes, and 1 otherwise.
- **Data model:** None. No table, column or index is added.
- **Protection level of each field:** None added. The script sends the seeded customer
  references, which are personal information by type though synthetic, and stores nothing.
- **Database privileges:** None. The script never reaches the database.
- **Addresses it calls:** our API at `https://localhost:8443`, and the stub's reversal and
  refund routes at `https://localhost:$ACME_STUB_PORT`, 8444 by default. No call follows a
  redirect. No address depends on data received from outside.
- **Slow or costly operations:** None added. The script's own load on our API stays under
  the 300 requests a minute each address is allowed (`Zapper:Security:RateLimit`), because it
  polls every 2 seconds and only the cases still waiting.
- **Infrastructure inventory:** two connections, both local and CI only: `The end-to-end
  script` to `Zapper.PaymentsApi` (locally `127.0.0.1` port 8443; HTTPS; personal
  information; nothing, because both app operations are public by D-2; the last column
  empty), and `The end-to-end script` to `AcmeStub` (locally `127.0.0.1` port `ACME_STUB_PORT`, 8444 by default; HTTPS; no protection level, seeded test values only; the stub routes' key; the last column empty).
- **Names the standards leave to planning:** `AcmeStub/end-to-end.sh`;
  the reference form `E2E-`, `yyyyMMddHHmmss`, a hyphen and the case number; the four CI steps "End-to-end: start the stack",
  "End-to-end: run the script", "End-to-end: logs on failure" and "End-to-end: stop the
  stack".
- **Decisions for this Feature only:**
  - **API endpoints only** (Francois): the script is for Zapper's reviewers to run against
    the API, so it never reads the database (D-15 as changed). The stored records behind
    each case are proven by the acceptance tests of the Feature that built them: Submitting
    Payments to Acme's "Same payment recorded twice is sent to Acme once", "Payment Acme
    never received is sent again with the same identity" and "Payment that meets a gateway
    error is asked about before it is sent again"; Acme Notifications' "Payment Advice
    delivered twice changes nothing the second time" and "Reversal that arrives before the
    outcome is applied once the payment is approved". The C# tests prove what is stored; the
    script proves that the real path ends in the right status.
  - **Record first, then wait** (Francois): the slow cases wait at the same time, so a run
    takes about two minutes rather than several.
  - **Every 2 seconds, at most 2 minutes, and a 429 is waited out** (Francois): the slowest
    case, the late answer, needs about 50 seconds by the stub's timings; polling every 2
    seconds keeps one address under our API's rate limit.
  - **The read at the end** (Francois): a Payment Advice arrives about a second after the
    worker's answer, so a case can pass before it arrives; reading every payment again at
    the end catches a later change.
  - **The stub's routes as built** (Francois): this Feature adds nothing to `compose.yml`,
    the certificate script or the stub; the script reaches the stub's routes as Acme
    Notifications built and documented them.
  - **CI** (D-15, kept by Francois on 2026-10-24): four steps in `.github/workflows/ci.yml`,
    after the step that uploads the test results: "End-to-end: start the stack" runs
    `docker compose up -d --build --wait`; "End-to-end: run the script" runs
    `AcmeStub/end-to-end.sh`; "End-to-end: logs on failure", with `if: failure()`, runs
    `docker compose logs`; "End-to-end: stop the stack", with `if: always()`, runs
    `docker compose down --volumes`. The runner's `curl` and `jq` are used. `Versions.md`
    gains rows for `curl` and Docker Compose as the runner image holds them, with the
    versions the pipeline's run proved; `jq` already has its row.
- **Tests:** none of any type in `Testing.md`. The script is this Feature's proof, by
  `Testing.md` section 7's exception (ruled by Francois on 2026-10-24); `dotnet test` passes
  unchanged in every increment.

## Contracts

None. This Feature adds no API operation: the script calls operations Recording payments
froze, and the stub's routes, which are not contracts.

| Operation | Contract |
|---|---|
| None | None |

## Implementation order

1. **The script, every case written and run.** `AcmeStub/end-to-end.sh` with the ten cases
   above and the run as "How it is built" states, and the infrastructure inventory's two
   connections. It is run against `docker compose up -d --build --wait`. A case fails only
   where it needs this Feature's own work; the increment's report lists each case and
   whether it already passes because of the Features before it (D-17 as changed).
   Reviewable on its own: running the script prints one line per case and its exit code.
2. **CI.** The four steps in `.github/workflows/ci.yml` and the `Versions.md` rows for `curl`
   and Docker Compose. Reviewable on its own: the steps run locally as written, and the pull
   request's run shows the script's lines.
3. **README.md and SOLUTION.md.** The README says what the script needs (Docker, `curl` and
   `jq`), how to run it after `docker compose up -d --build --wait`, that a run takes about
   two minutes and can be repeated, and how to read its lines. SOLUTION.md gains this
   Feature's facts for Francois's validation section: the script calls API endpoints only
   and why; what it proves and what the acceptance tests prove instead; the read at the end
   and why; polling within our own rate limit; and that CI runs it on every pull request and
   every push to `dev`, `test` and `main`. SOLUTION.md's \"Assumptions\", \"Design and key trade-offs\", \"Data model\" and \"AWS deployment approach\" are written in full by D-17. The AWS section covers how the service runs (one image as two ECS services, PostgreSQL on RDS, the Migrator as a one-off ECS task before each deploy), configuration and secrets, scaling, graceful shutdown, and the signals: the health check, the duration and outcome of each Acme call, 429s, the circuit's state, the payments due and not claimed and the age of the oldest, the `Unresolved` count, unmatched and contradicting notifications, 401s on the notification operations, and log lines carrying trace ids. The data model section covers how the schema would evolve. The AI workflow section names `CLAUDE.md` and the folders of `Claude/`.
