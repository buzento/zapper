> **Authoring rules.** A plan is one file, `ai/Plans/<Plan_Title>.md`, where
> `<Plan_Title>` is the plan's title converted by `Claude/Standards/Branching.md`
> section 2. `create-plan` writes it, and `feature-execution` builds each Feature
> from it. **The file name already names the plan, so the title is never
> restated**: there is no `#` H1 above the first section, and the highest heading
> in the body is `#`. Delete this blockquote and every `<…>` placeholder before
> use. A section that has nothing to say for a Feature says "None" and why, and
> is never deleted, so a reader can tell that it was considered.

- **Status:** Approved <date>
- **Planned on branch:** `Feature/<Plan_Title>`. Each Feature is built on a
  branch of its own, `Feature/<Feature_Title>`, by `feature-execution`.

> A **plan**, not an implementation. It states what must be true and how it is
> built, for every Feature in it, so that `feature-execution` invents nothing.
> The standards in `Claude/Standards/` read the parts of a Feature's section
> that they name: its rules, its scenarios, how it is built and its
> implementation order. Every Feature is **tech-only**: none has a user
> interface.

# Outcome

<What must be true when every Feature in this plan is built, and for whom. One
paragraph.>

# Boundaries

- **In scope:** <…>
- **Out of scope:** <…, especially what this work is easily confused with>
- **Depends on:** <a plan, an API or a service this work needs, or "Nothing">

# Decisions

> A decision is here when it affects more than one Feature, or more than this
> plan. A decision that affects one Feature only is written in that Feature's
> "How it is built". A decision a standard already makes is never repeated here,
> and never re-argued. A decision that replaces one in an earlier plan names it,
> as `<Plan_Title>` D-<n>.

- **D-1: <the decision, stated plainly>.** <Why. What it costs. What was
  considered and rejected, and why.> **Affects:** <Feature titles>.

# Features

In the order they are built. A Feature is built only after every Feature above
it, on its own branch and with its own pull request, and counts as built once
that pull request is merged. Every Feature title, once converted, differs from
the plan's and from every other Feature's in every plan, because
`feature-execution` finds a Feature's pull request by its branch name.

| # | Feature | Planned |
|---|---|---|
| 1 | <Feature title> | <date, or "Not yet"> |

# Feature: <Feature title>

## What it delivers

<One paragraph. What is true when this Feature is done, and who depends on it.>

## Rules

> What must always be true, in domain language, never as tables, columns or
> keys. Each rule has an id, `R-<n>`, numbered within this Feature. Every rule is
> covered by at least one scenario. The standards that read "the plan states"
> read it here: who may see each record, who may read and change each field,
> every limit, the order of a process's steps, how long each category of
> personal information is kept, and how it is corrected and de-identified.

- **R-1: <name>.** <rule>

## Failure modes

> Every way this Feature can fail, what the caller receives, and what recovery
> looks like. A Feature with only a happy path is not planned.

| What goes wrong | What the caller receives | Recovery |
|---|---|---|
| <…> | <status and error code> | <…> |

## Scenarios

> One Gherkin `Feature:` per plan Feature. `Testing.md` section 7 builds the
> acceptance test class from the `Feature:` line and each test from a scenario
> title, so every title begins with a letter. Each scenario is tagged with every
> rule id it proves.

```gherkin
Feature: <Feature title>

  @R-1
  Scenario: <title>
    Given <…>
    When <…>
    Then <…>
```

## How it is built

- **API:** `<Solution>.<Name>Api`, or a new API, which `create-project` adds
  first.
- **Frontend:** None. This Feature is tech-only.
- **Domain:** <entities, value objects and the rules `BackendCode.md` section 4
  places in the Domain>
- **Data model:** <each table, its schema, each column and its type, keys,
  unique constraints and indexes>
- **Protection level of each field:** <per `Security.md` section 2, and each
  column stored encrypted>
- **Database privileges:** <what each API's role is granted, per table>
- **Addresses it calls:** <each address outside the process, and whether a call
  follows a redirect, or "None">
- **Slow or costly operations:** <the time budget, the caller count and the
  limit on how many may run at once, or "None">
- **Infrastructure inventory:** <each row this Feature adds to or changes in
  `Docs/InfrastructureInventory.md`, or "None">
- **Names the standards leave to planning:** <each class, schema or setting a
  standard says is named in planning, or "None">
- **Decisions for this Feature only:** <each, with its reason, or "None">
- **Tests:** <which types of test from `Testing.md` this Feature needs; the
  standard says what each must cover>

## Contracts

> One row per operation this Feature adds. Each is two files, frozen when this
> plan was approved. They wait under `ai/Plans/<Plan_Title>/Contracts/`, and
> `feature-execution` moves them to the same path under `Docs/Contracts/` in
> this Feature's first increment. The table gives the path they are moved to.
> An operation that changes a frozen contract is a new version, so a new path.

| Operation | Contract |
|---|---|
| `<METHOD> /<version>/<route>` | `Docs/Contracts/<Name>Api/<version>/<route>/<method>.md` |

## Implementation order

> Numbered, in the order they are built. `feature-execution` commits each as
> `<Feature_Title>_<n>/<total>`, per `Branching.md` section 4. Increment 1 is
> always the failing acceptance tests. Each increment says what it delivers and
> what makes it reviewable on its own.

1. **Failing acceptance tests.** One per scenario above, failing.
2. **<…>.** <what it delivers, and what makes it reviewable on its own>
