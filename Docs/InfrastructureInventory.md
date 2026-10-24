# Infrastructure inventory

## Parts

| Part | What it is | Which environments hold it | Highest protection level of the data it holds or carries | Whether the internet can reach it |
|---|---|---|---|---|
| `Zapper.PaymentsApi` | An API, which only the product's own frontends and services call | | Sensitive data | |
| `Acme Pay` | A payment service provider outside the product | Every deployed environment; locally, `AcmeStub` stands in for it | Sensitive data, the card token | Yes |
| `AcmeStub` | A stand-in for `Acme Pay` | Local only | None: it holds only seeded test values | No; it is published on `127.0.0.1`, port `ACME_STUB_PORT`, 8444 by default, for the end-to-end script |

## Connections

| Connects | To | Port | Protocol | Highest protection level of the data it carries | How the connecting part proves who it is | Feature whose plan states what the address may be |
|---|---|---|---|---|---|---|
| `Zapper.PaymentsApi` | The database, `ZapperDb` | | PostgreSQL over TLS | Sensitive data | A client certificate for the role `ZapperPaymentsApiRole` | |
| `Zapper.PaymentsApi` | `Acme Pay`; locally, `AcmeStub` | 443; locally 8443 | HTTPS | Sensitive data | The bearer API key, `Zapper:Acme:ApiKey` | Submitting Payments to Acme; the address depends on no data from outside |
