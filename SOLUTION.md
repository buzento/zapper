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
