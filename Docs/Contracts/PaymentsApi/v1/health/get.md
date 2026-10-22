- **Fragment:** `get.openapi.json`, beside this file. **The fragment is the
  truth**; this document explains it.
- **Status:** Frozen 2026-10-22
- **Governed by:** No decision
- **Produced by:** `Zapper.PaymentsApi`
- **Consumed by:** a probe or an orchestrator in each deployed environment

> A **contract**, not an implementation. It freezes **one operation on one route
> of one API**: the surface a caller outside this codebase depends on. Callers
> depend only on this contract, never on a concrete type or another feature's
> internals. A frozen contract changes only by a new **version**, never an
> in-place edit, and because these APIs version by URL a new version is a new
> path and therefore a new file. So `v1` and `v2` are different files that
> coexist, and neither replaces the other. **There is no freeze counter in the
> `Status` line**: a contract's version is the API version in its path, and what
> changed between two freezes of one file is in git.
>
> **Only API surfaces are contracts.** An interface is proved by unit tests and a
> queue message by integration tests, so neither is frozen and neither belongs
> under `Docs/Contracts/`. If a boundary is not exposed by an API, it is not a
> contract.

# Purpose

Health reports that `Zapper.PaymentsApi` is running and serving requests. A probe or an orchestrator in each deployed environment calls it to decide whether the host is up. It reaches nothing the API depends on, so it answers for the host alone.

# Definition

The fragment beside this document covers `GET /v1/health` and nothing else. A successful call answers 200 with no body.

```
GET /v1/health
200, no body
```

# Fields

| Field | Type | Required | User-facing? | Notes |
|---|---|---|---|---|
| none | | | n | The response has no body, so it carries no field |

> **User-facing? = yes** → the signed-off design must already account for this
> field. The test is not whether the UI renders the value, but whether the field
> implies a **state, action, or piece of information the user should perceive**:
> a status the UI reflects, an id the user selects or routes by, a count, a
> limit, an error the user has to act on. **Plumbing is no**: values the UI never
> shows and the user never acts on, such as correlation ids, timestamps, internal
> metadata and persistence keys. If a user-facing field is not in the design,
> that is a **design iteration**, not a quiet addition: the design is revisited in
> Claude Design and re-signed-off before this contract freezes. **A contract never
> silently introduces a user-facing concept.**

# Rules

- **CR-1: No body.** A successful call answers 200 with no body, and nothing is ever added to it.
- **CR-2: Public.** The operation names no security scheme, `"security": []`, so any caller may call it, signed in or not.
