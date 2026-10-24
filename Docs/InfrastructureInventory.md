# Infrastructure inventory

## Parts

| Part | What it is | Which environments hold it | Highest protection level of the data it holds or carries | Whether the internet can reach it |
|---|---|---|---|---|
| `Zapper.PaymentsApi` | An API, which only the product's own frontends and services call | | Sensitive data | |

## Connections

| Connects | To | Port | Protocol | Highest protection level of the data it carries | How the connecting part proves who it is | Feature whose plan states what the address may be |
|---|---|---|---|---|---|---|
| `Zapper.PaymentsApi` | The database, `ZapperDb` | | PostgreSQL over TLS | Sensitive data | A client certificate for the role `ZapperPaymentsApiRole` | |
