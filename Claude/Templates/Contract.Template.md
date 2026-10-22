> **Authoring rules.** A contract is two files side by side in the repo:
> `Docs/Contracts/<Api>/<version>/<route>/<method>.md`, which is this document,
> and `<method>.openapi.json` beside it, which is the frozen fragment. Until its
> Feature is built, both wait at the same path under
> `ai/Plans/<Plan_Title>/Contracts/` in place of `Docs/Contracts/`. **The path
> is derived from the route, never chosen.** The API is the first segment, the
> version comes from the URL because these APIs version by URL, the route
> segments are written exactly as the route declares them including braces such
> as `{id}`, and the file name is the HTTP method in lower case. Nothing about
> the path is a naming decision, so two sessions cannot file the same operation
> in two places. The path already names the operation, so **the operation is
> never restated as a title**: there is no `#` H1, and the highest heading in the
> body is `#`. Delete this blockquote and every `<…>` placeholder before use.

- **Fragment:** `<method>.openapi.json`, beside this file. **The fragment is the
  truth**; this document explains it.
- **Status:** Draft | Frozen <date>
- **Governed by:** <the decision that governs it, as `<Plan_Title>` D-<n>,
  or "No decision">
- **Produced by:** <service/feature>
- **Consumed by:** <service(s)/feature(s)>

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

<What boundary this defines and why it exists.>

# Definition

<The OpenAPI fragment beside this document is the truth; this doc explains it.
The fragment covers **exactly one method on one route**, never a whole-API
document and never a whole route, because a whole-API document changes every time
any feature adds an endpoint, and a contract that changes on every feature is not
frozen. `GET` and `POST` on the same route are two contracts, so they freeze and
version independently. Show the request and response shape below.>

```
<schema reference / interface signature>
```

# Fields

| Field | Type | Required | User-facing? | Notes |
|---|---|---|---|---|
| <…> | <…> | <y/n> | <y/n> | <…> |

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

- **CR-1: <name>.** <rule the contract guarantees / requires of implementers>
- **CR-2: <name>.** <rule>
