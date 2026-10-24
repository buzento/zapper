# Acme stub

A stand-in for the Acme Pay payment gateway, so the payments API has something to
call when it runs locally. It follows the brief's Appendix A, not this
repository's standards: it lives outside `Code/` and the solution, and only two
repository rules reach it, no committed secret and no em dash (plan D-14).

It is one ASP.NET Core minimal API on .NET 10, with no package beyond the
framework. Everything it holds is in memory, so a restart forgets every payment.

## Running it

`docker compose up -d` from the repository root builds and starts it with the
rest of the stack. Inside the stack the payments API calls it at
`https://AcmeStub:8443`. It is also published on `127.0.0.1`, port 8444, for the
end-to-end script. When 8444 is taken, set `ACME_STUB_PORT` to a free port, both
for `docker compose up` and for the script:

```bash
ACME_STUB_PORT=9444 docker compose up -d
```

Its server certificate is issued by the `compose.yml` certificate script, from
the local authority in `Code/Certificates/ca.crt`, for the names `AcmeStub`,
`localhost`, `127.0.0.1` and `::1`. A call from your machine trusts that authority
with `curl --cacert Code/Certificates/ca.crt`.

## Configuration

| Setting | Where it comes from |
|---|---|
| `AcmeStub:ApiKey` | The file `AcmeStub__ApiKey`, a copy of the API key the payments API sends. The certificate script generates it on the first `docker compose up`, as `sk_test_` followed by 48 random hexadecimal characters, into a volume only the stub mounts. |
| `AcmeStub:MerchantId` | `compose.yml`, the same GUID the payments API is given. |

The stub refuses to start without either.

## Routes

- `POST /api/payment`, a payment, as Appendix A describes it.
- `GET /apirttq/query/Merchant/{MerchantID}?TransactionID=...`, the Real Time
  Transaction Query. It also takes `OrderNumber` in place of `TransactionID`.

Both check the bearer key, then the rate limit.

## Behaviour by card token

The stub chooses what to do by the card's token. Each token is one of the cards
the Migrator's Debug build seeds.

| Token | What the stub does |
|---|---|
| `tok_test_approve` | Approves, with response code `00` and the amount sent as the authorised amount. |
| `tok_test_decline_05` | Declines, with response code `05`. |
| `tok_test_decline_51` | Declines, with response code `51`. |
| `tok_test_decline_54` | Declines, with response code `54`. |
| `tok_test_advice_twice` | Approves. Its Payment Advice, sent twice, is added with Acme Notifications. |
| `tok_test_reversal_first` | Approves. Its reversal, sent before its Payment Advice, is added with Acme Notifications. |
| `tok_test_late_answer` | Holds the request for 40 seconds, so the payments API times out at 35, and approves the payment 45 seconds after it arrived. A query before then answers `Processing`. |
| `tok_test_never_received` | Holds the first send for 40 seconds, then drops the connection and keeps nothing, so a query answers 404. The second send with the same `TransactionID` is approved. |
| `tok_test_gateway_error_once` | Answers the first send with 503 `E50` and keeps nothing, so a query answers 404. The second send is approved. |
| Any other token | Declines with response code `05`, and logs a line naming the `TransactionID`. |

## What the stub assumes

Appendix A leaves these open, so the stub chooses:

- **The outcome is stored against the `TransactionID` for 24 hours** and replayed
  for a repeated request, as Appendix A states. A repeated request for a payment
  still in progress answers `Processing`.
- **Each Retrieval Reference Number is held once.** It is the first 12 characters
  of `OrderNumber`. A payment whose number is already held by another
  `TransactionID` answers 409 `E09`, naming the `TransactionID` that holds it.
- **The rate limit is 20 requests in each calendar second**, across both routes.
  The 21st answers 429 `E29` with `Retry-After: 1`.
- **A payment is refused with 400 `E01`** when the body cannot be parsed, or when
  it has no `MessageHeader.MessageID`, `MerchantID`, `TransactionID`, an
  alphanumeric `OrderNumber`, a whole-number `Amount`, a card `PaymentService` or
  a `Card.Token`. The value of `MerchantID` on a payment is not checked; the query
  answers 404 for any merchant id other than its own.
- **A missing or wrong bearer key answers 401 `E02`.**
- **Every answer that carries a response code carries its client message.**
  Approvals and declines take theirs from Appendix A's table of codes. Every error
  takes "We could not process that payment.", the one client message Appendix A
  gives an error.
- **Every answer carries one member Appendix A does not document**,
  `GatewayRegion`, because Acme adds members without notice.
- **The delays are the stub's own**: 40 seconds to make the payments API time out,
  and 45 seconds before a late answer is approved.
- **Partial authorisation is not simulated.** An approval authorises the amount
  sent.
