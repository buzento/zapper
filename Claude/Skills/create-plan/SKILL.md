---
name: create-plan
description: >-
  Plans one piece of work in Claude Code planning mode: asks what it is about
  and its title, interviews Francois, works out the decisions and the first list
  of Features with him, then plans each Feature in full: its rules, failure
  modes, scenarios, how it is built, its contracts and its implementation order.
  One approval, then it writes the plan to ai/Plans/<Plan_Title>.md and a
  contract for each new API operation. Ends by committing, pushing and opening
  a pull request into dev, and deletes the local branch once told it is merged.
  Resumable: a later run plans the Features an earlier one did not reach. Tech-only: plans no user interface.
  Use when the user says "create-plan", "plan this", "plan a feature" or
  "planning". Do NOT use to write code (that is feature-execution), or to create
  the solution or a project (that is create-solution or create-project).
---

# Create plan

This skill plans one piece of work, to the standards in `Claude/Standards/`. It
works the plan out with Francois, gets one approval, and writes it down. What it
writes is what `feature-execution` builds from, so a plan that is filled in
rather than worked out is built wrong.

**It writes two kinds of file, and nothing else:**

1. **The plan**, `ai/Plans/<Plan_Title>.md`, from
   `Claude/Templates/Plan.Template.md`. One file holds the whole plan: the
   outcome, the boundaries, the decisions, the list of Features, and one section
   per Feature. The plan goes in `ai/` because it is presented to Zapper. Ruled
   by Francois on 2026-10-22.
2. **A contract for each new API operation** a Feature needs, two files under
   `ai/Plans/<Plan_Title>/Contracts/<Api>/<version>/<route>/`, frozen on the
   day the plan is approved. **They wait there, not under `Docs/Contracts/`**,
   because the contract test fails for a fragment whose code does not exist, so
   a contract merged into `dev` before its code would leave `dev` failing.
   `feature-execution` moves them to the same path under `Docs/Contracts/` in
   the Feature's first increment. Ruled by Francois on 2026-10-22.

**Each plan is one feature, on a feature branch of its own.** The plan's title
gives `<Plan_Title>`, and `Branching.md` section 2 builds the branch, the commit
subject and the pull request title from it. Ruled by Francois on 2026-10-22.
Each Feature in the plan is later built by `feature-execution` on a branch of
its own, `Feature/<Feature_Title>`, with its own pull request.

**Every Feature it plans is tech-only.** No Feature has a user interface, so
there is no design to read and no design to sign off. A Feature that needs a
screen is out of scope for this skill. Ruled by Francois on 2026-10-22.

> **Every path in this skill is relative to the repository root, never to this
> file's own location.** Resolve every path below against the repository root.
> If you cannot determine it, ask. Do not guess.

## What this skill asks for, and what it reads

| What this skill asks for | When | What reads it |
|---|---|---|
| what the work is about | every run | the interview |
| the plan's title | every run | step 4, "Name the plan", which builds `<Plan_Title>` |

**Nothing is asked that the repository already knows.**

| What this skill reads | Why |
|---|---|
| `CLAUDE.md` | the rules and the vocabulary |
| every file in `Claude/Standards/` | the plan is written against them, and never repeats or re-argues them |
| every other plan in `ai/Plans/` | the decisions already made, which bind this plan |
| `SOLUTION.md` | the decisions that differ from the brief, and the assumptions |
| every contract under `Docs/Contracts/` and under `ai/Plans/*/Contracts/` | what is already frozen, built or waiting, and so cannot change except by a new version |
| `Docs/InfrastructureInventory.md` and `Docs/CryptographicInventory.md` | what exists, and what a Feature changes |
| `Code/<Solution>.slnx` | the solution's APIs, each project whose name ends in `Api` |

## What this skill never writes

- **Code, of any kind.** That is `feature-execution`'s.
- **An ADR, a BRS or a tech spec.** None of them exists in this process. What
  they held is in the plan: a decision that affects more than one Feature is in
  the plan's Decisions, what one Feature must do is in its Rules and Scenarios,
  and how it is built is in its "How it is built" and its implementation order.
  Ruled by Francois on 2026-10-22.
- **A user interface, or anything about one.**
- **A change to a standard, a skill or a template that Francois has not ruled
  on.** Step 7, "When the work shows the process is wrong", says what happens.

## How to interview: co-authorship, not a questionnaire

Every interview in this skill runs by this rule.

**The interview is two architects working a problem, and neither one's ideas may
be filtered out by the other in advance.** You bring proposals and defend them.
Francois brings proposals and defends them. The plan is what survives both. If
every idea in the plan came from you and Francois only chose between them, the
interview has failed, however good the choices were.

**The interview exists to produce a plan that is correct, not one that is
fast.** Fill in every section of the template and you have a document. Work the
problem with Francois and you have a design. Only the second is worth building
from, and the difference costs a conversation now instead of a rewrite once the
increments are half built.

**Ask in prose. Never as multiple choice.** `CLAUDE.md` rule 6 applies to every
question this skill asks, with no exception. Plan mode's own guidance recommends
a multiple-choice tool for clarifying an approach; this skill overrides it. A
list of options limits the answer to what you already imagined, and the answer
worth having is nearly always the one you did not imagine.

**Ask what only Francois can tell you. Propose everything else.** Constraints,
preferences, plans you have not been shown, anything you would otherwise fill in
by assumption: ask. Where you already have enough to form a view, give the view
instead of a question, with your reasoning and what it costs if you are wrong.
Handing back a question you could have answered yourself is not collaboration.

**Ask the way people actually ask.** One thing at a time, in the flow of what is
being discussed, not a set of questions delivered at once. Ask, listen, and let
the answer change what comes next.

**Be a second reviewer, not a form to complete.** Concretely:

- **Challenge what you are told.** If an answer will not work, will not scale,
  contradicts a standard or an earlier decision, or conflicts with something
  said ten minutes ago, say so at the time.
- **Bring what Francois has not thought of.** The failure mode nobody listed,
  the edge case a rule does not cover, the Feature that is really two.
- **Propose alternatives and argue them.** A recommendation, the reasoning
  behind it, and what it costs if it is wrong. Never a list with no opinion.
- **The better argument wins, whoever made it.** Argue with Francois's proposal
  as hard as you argue for your own, and when his is better, say so and adopt
  it. This is not permission to give in: neither agreeing to keep the peace nor
  refusing to move gets you to the right design.
- **Disagree when you disagree**, then accept the decision once it is made and
  record it.
- **Say plainly when you do not know**, and say what would settle it.

**Do not stop when the sections are filled. Stop when you both understand how
the thing works.** Those are different moments, and the first arrives much
earlier than it should.

You are doing this badly if: Francois's recent answers are choices rather than
sentences; every idea in the plan is one of yours; your questions exist only to
fill template sections; you took the first answer to a question that had a real
trade-off inside it; or you are heading for the approval because you have enough
to write rather than because the design is right.

**Let the problem set the order of the conversation.** The topics in steps 6 and
8 are the minimum, so nothing is missed, not an order to work through. Follow
whatever thread is live, finish it, and move on when it is finished rather than
when you have enough to write down.

## Preconditions

- **You must be in plan mode.** If the session is not already in plan mode, call
  `EnterPlanMode` before doing anything else, and before asking anything.
- **Run every command from the repository root.** Confirm it with:

  ```bash
  git rev-parse --show-toplevel
  ```

  It prints the repository root. Where that is not the folder you are in, change
  to it. Where it exits 128, this is not a git repository: stop and say so.
- **A solution exists.** Check with:

  ```bash
  find Code -maxdepth 1 -name '*.slnx'
  ```

  **One line** names the solution file: carry on. **No line**: stop, and say that
  `create-solution` creates the solution and runs first. **More than one line**:
  stop, and name every file the command printed.
- **`dev` is on the server**, because the plan's branch is cut from it. Check
  with:

  ```bash
  git fetch origin dev
  ```

  **Expect exit 0.** Where it fails, stop and say so.
- **`gh` is signed in**, because step 11 opens the pull request with it. Check
  with:

  ```bash
  gh auth status
  ```

  **Expect exit 0.** Where it fails, stop and say that `gh auth login` signs in.
- **The subject and the title come from Francois, and only from him.** The
  repository, git history and earlier plans are all within reach. **None of them
  is a source of intent.** Never infer what is being planned from them.

## The steps

### 1. Enter plan mode and check the repository

Call `EnterPlanMode` if the session is not in it. Then run the four checks in
the preconditions above. Where one stops, nothing has been asked and nothing has
been written.

### 2. Ask what the work is about

- **Nothing given.** Ask for it. Say what is useful: the outcome and who it is
  for, why now, roughly what is in and out of scope, and anything already
  decided. Make clear that a couple of sentences is enough to start.
- **A line or two given.** That is enough to start. A short description is a
  reason to work the shape out together, not a gap to close by questioning.
- **A detailed description given.** Carry on.

### 3. Ask for the plan's title

Ask for the title in plain words, such as `Payment capture`. It says what the
plan is about, the way a reader of the branch list would want to see it, and is
kept exactly as given.

### 4. Name the plan

**Build `<Plan_Title>` from the title**, by `Branching.md` section 2: the first
letter of each word capitalised, every other letter left as it was given, each
space written as an underscore, and anything that is not a letter, a digit or an
underscore dropped. The plan is `ai/Plans/<Plan_Title>.md`, and its branch is
`Feature/<Plan_Title>`.

**A title that leaves no letter and no digit is refused**, and asked for again.

### 5. Read what already exists

Read everything the table "What this skill reads" names, now and in full.

**If the branch `Feature/<Plan_Title>` exists locally**, an earlier run's pull
request is not merged yet, or that run did not finish. Check with
`git branch --list Feature/<Plan_Title>`, which prints the branch where it
exists. **Stop and say so**: a run starts only once the one before it is merged.

**If `ai/Plans/<Plan_Title>.md` exists, this run resumes it.** Say so, and say
which Features its Features table shows as "Not yet". This run plans those.
**What it already records was approved, and is not re-argued** unless Francois
says it should be. A planned Feature changes only when he asks for the change.

**A decision in another plan binds this one.** It is cited, never re-argued. Where
this work needs it changed, that is a decision this plan makes, stated in its
Decisions and naming the one it replaces.

**A frozen contract is never edited.** Where a Feature needs a frozen operation
to change, the change is a new version: a new route, and new files beside the
old ones.

### 6. Interview, then plan the top level

Stop and interview before producing anything, by "How to interview" above. The
minimum to cover:

- **The outcome.** What must be true when this work is done, for whom, and why
  now.
- **Boundaries.** What is in, what is out, and what this work depends on. Flag
  every capability that more than this work will use: it may belong in a plan of
  its own.
- **Decisions.** Each decision that affects more than one Feature, or more than
  this plan, with its reasoning, its cost and what was rejected. A decision a
  standard already makes is not a decision here.
- **The Features.** The units of work, in the order they are built, and what
  must be true when each is done. **A Feature is the size one branch and one
  pull request can carry**, and it ends in something that can be reviewed on its
  own. A Feature that needs a screen is out of scope: say so, and leave it out.

### 7. When the work shows the process is wrong

At any point, the work may show that a standard, a skill or a template is wrong,
or does not cover what this work needs. **Stop.** Do not plan around it, and do
not write it down as an accepted compromise. Put it to Francois by `CLAUDE.md`
rule 6: what was found, a recommendation, then the question.

His ruling is part of this plan's approval. Write it into the plan, under the
decision or the Feature it affects, as the exact change to the named file. Step
10, "Write the plan", makes that change first, before the plan is written.

### 8. Interview, then plan each Feature

For each Feature not yet planned, in order, interview again by the same rule.
This is where the design is actually worked out, so it is the part most worth
slowing down. The minimum, which is every section of a Feature in the template:

- **What it delivers.**
- **Rules.** What must always be true, in domain language, never as tables,
  columns or keys. Each has an id, `R-<n>`. The standards read several things
  from here, and each must be stated where the Feature touches it: who may see
  each record, and whether a person outside the relationship a record belongs to
  is told that it exists; who may read and change each field whose access
  differs from its record's; every limit; the order of a process's steps; and
  for personal information, how long each category is kept and how it is
  corrected and de-identified.
- **Every failure mode.** What goes wrong, what the caller receives, and what
  recovery looks like. Not only the happy path.
- **Scenarios.** Gherkin, one `Feature:` per Feature, every scenario tagged with
  the rule ids it proves. **Every rule has at least one scenario.** A rule with
  no scenario cannot be tested, so fix the rule.
- **How it is built.** Every line of the template's "How it is built": the API,
  the Domain, the data model, the protection level of each field, the database
  privileges, the addresses it calls, any slow or costly operation, the
  infrastructure inventory, every name a standard leaves to planning, the
  decisions for this Feature only, and the types of test. **A Feature whose API
  does not exist yet** says so, and its implementation order starts after
  `create-project` has added that API, which is a feature of its own.
- **Contracts.** One per new operation. See "Planning a contract" below.
- **The implementation order.** See "The implementation order" below.

**Where each decision belongs.** A decision whose reach is this Feature goes in
its "How it is built", with its reasoning. A decision that reaches further goes
in the plan's Decisions. Do not hide a wider decision inside one Feature to keep
moving.

**A plan too big for one session.** Planning a Feature in full uses a lot of
context. Where this session cannot plan every Feature properly, stop at the end
of a Feature, and say which are left. They stay "Not yet" in the Features table.
This run still goes to the approval, writes what it planned and ends with its own
pull request. Once that is merged, the next run of this skill plans the rest, on
a new branch of the same name cut from `dev`, which by then holds the plan. Never
plan a Feature thinly to fit it in.

### Planning a contract

A contract is planned for each operation a Feature adds to an API, and only for
an API operation. An interface and a queue message are not contracts.

- **The path is derived, never chosen**:
  `Docs/Contracts/<Name>Api/<version>/<route>/<method>.openapi.json`, with
  `<method>.md` beside it, as `Testing.md` section 4 and the contract template
  state. That is the path the Feature's Contracts table gives. This skill
  writes both files at the same path under `ai/Plans/<Plan_Title>/Contracts/`
  in place of `Docs/Contracts/`.
- **The fragment is exactly what the API will generate** for that operation, and
  nothing more, because the contract test compares the two in both directions.
  Write it by `BackendCode.md` sections 6, 7, 8 and 19 and `Testing.md` sections
  4 and 8: the route and its parameters, the request body, every response and
  its body, `summary`, `tags`, `security`, every schema it references, and no
  `operationId`. Take `openapi` and `info` from a fragment the same API already
  has.
- **The document** is cut from `Claude/Templates/Contract.Template.md`.
  **Governed by** names the decision that governs it, as `<Plan_Title>` D-<n>,
  or "No decision". **Every field's "User-facing?" is `n`**: a tech-only Feature
  has no screen for a person to perceive it on. A field a person would need to
  see means the Feature needs a user interface, and is out of scope.
- **The document and the fragment describe the same operation**: the same
  method, route, fields and whether each is required.

### The implementation order

`feature-execution` builds from this list and from nothing else, and
`Branching.md` section 4 takes each commit's number and total from it. Give it
the same attention as the data model.

- Increments in the order they are built, **numbered**.
- **Increment 1 is always the failing acceptance tests**, one per scenario.
- Each increment says what it delivers and what makes it reviewable on its own.

### 9. [GATE] One approval

Before presenting, check every one of these, and fix what fails:

- Every rule has at least one scenario, and every scenario is tagged with a rule
  id that exists.
- Every scenario title begins with a letter.
- Every Feature title, converted by `Branching.md` section 2, differs from the
  plan's title and from every Feature title in this plan and every other plan.
- Every Feature's implementation order starts with the failing acceptance tests.
- Every contract's document and fragment describe the same operation, and its
  path is derived from its route.
- No contract edits a frozen one.
- No section is deleted, and no `<…>` placeholder is left.
- Nothing in the plan repeats or contradicts a standard.

Then present, through `ExitPlanMode`, **the plan file exactly as it will be
written**, every contract's two files, and every change to a standard, a skill or
a template ruled under step 7. **One approval covers all of it.** Loops back to
the interview are expected. **No approval, nothing is written.**

### 10. Write the plan

Create the plan's branch, cut from `dev` on the server:

```bash
git fetch origin dev
git switch -c Feature/<Plan_Title> --no-track origin/dev
git symbolic-ref --short HEAD
```

The last command prints the branch. **If it prints `main`, `dev` or `test`, stop
and say so** rather than write anything.

Get the date:

```bash
date +%F
```

It prints `<Date>`. Then write, in this order:

1. **Every change ruled under step 7**, to the file it names, exactly as it was
   approved.
2. **Every contract's two files**, under `ai/Plans/<Plan_Title>/Contracts/`.
   The document's `Status` is `Frozen <Date>`.
3. **The plan**, `ai/Plans/<Plan_Title>.md`. Its `Status` is `Approved <Date>`.
   Each Feature planned in this run reads `<Date>` in the Features table's
   "Planned" column, and each left for a later run reads "Not yet".

### 11. Commit, push and open the pull request

Run `Branching.md` section 6, "How a skill ends its run": check that every change
is this run's, being only the files step 10 wrote; then one commit with the
subject `<Plan_Title>_1/1`, the push, and a pull request into `dev` titled
`<Plan_Title>`. **Its body is the closing report in step 12.** Ruled by Francois
on 2026-10-22.

### 12. Report, and wait for the merge

The closing report. Say plainly:

- the plan's path, and each Feature in it with whether it is planned;
- each contract's two paths;
- each change made to a standard, a skill or a template;
- **that no code was written.**

Then give the pull request's address. **Then wait.** When Francois says in this
conversation that the pull request is merged, run `Branching.md` section 6,
"After the merge", which deletes the local feature branch, and then stop.
Building a Feature is `feature-execution`'s, in a session of its own.

## Guardrails

- Nothing lives only in this chat. If it matters, it is in the plan, a contract
  or a standard.
- The subject and the title come from Francois. Never infer either.
- **The interview is co-authorship, not a questionnaire.** Never hurry to the
  approval, and never reduce Francois to picking from options you wrote.
- Nothing is written before the approval in step 9.
- A decision in a standard or an earlier plan is cited, never re-argued.
- A wrong standard stops the work until Francois rules, by `CLAUDE.md` rule 2.
- A frozen contract is never edited. A change to it is a new version.
- Every Feature is tech-only. A Feature that needs a screen is out of scope.
- Every rule has a scenario, and increment 1 is always the failing acceptance
  tests.
- This skill writes no code. It commits once, opens one pull request, and
  deletes its local branch only once told the pull request is merged.
