# Infrastructure inventory

## Parts

| Part | What it is | Which environments hold it | Highest protection level of the data it holds or carries | Whether the internet can reach it |
|---|---|---|---|---|
| `Zapper.PaymentsApi` | An API, which Zapper's app and Acme's notifications call | | Sensitive data | Yes |
| `Acme Pay` | A payment service provider outside the product | Every deployed environment; locally, `AcmeStub` stands in for it | Sensitive data, the card token | Yes |
| `Acme Switch` | Acme's service that publishes reversals and refunds, outside the product | Every deployed environment; locally, `AcmeStub` stands in for it | None: it holds no data of ours | Yes |
| `AcmeStub` | A stand-in for `Acme Pay` and `Acme Switch` | Local only | None: it holds only seeded test values | No; it is published on `127.0.0.1`, port `ACME_STUB_PORT`, 8444 by default, for the end-to-end script |

## Connections

| Connects | To | Port | Protocol | Highest protection level of the data it carries | How the connecting part proves who it is | Feature whose plan states what the address may be |
|---|---|---|---|---|---|---|
| `Zapper.PaymentsApi` | The database, `ZapperDb` | | PostgreSQL over TLS | Sensitive data | A client certificate for the role `ZapperPaymentsApiRole` | |
| `Zapper.PaymentsApi` | `Acme Pay`; locally, `AcmeStub` | 443; locally 8443 | HTTPS | Sensitive data | The bearer API key, `Zapper:Acme:ApiKey` | Submitting Payments to Acme; the address depends on no data from outside |
| `Acme Pay`; locally, `AcmeStub` | `Zapper.PaymentsApi` | 443; locally 8443 | HTTPS | Personal information | HTTP Basic with the Payment Advice username and password, `Zapper:AcmeNotification:PaymentAdviceUserName` and `PaymentAdvicePassword` | Acme Notifications; the address depends on no data from outside |
| `Acme Switch`; locally, `AcmeStub` | `Zapper.PaymentsApi` | 443; locally 8443 | HTTPS | Personal information | An HMAC-SHA256 signature of the body, in `x-signature`, with the webhook secret, `Zapper:AcmeNotification:WebhookSecret` | Acme Notifications; the address depends on no data from outside |
