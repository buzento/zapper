---
name: feature-execution
description: >-
  Builds one Feature of an approved plan in ai/Plans/, increment by increment,
  in the order its implementation order gives. Finds the next Feature itself,
  creates or checks out its branch, and resumes from the last increment
  committed. Each increment is built, checked, committed and pushed; after the
  last one it opens a pull request into dev, and deletes the local branch once
  told it is merged. Asks nothing the plan already says, and stops when the
  plan does not say enough. Use when the user says "feature-execution",
  "build the next feature", "execute the plan" or "carry on building". Do NOT
  use to plan (that is create-plan), or to create the solution or an API (that
  is create-solution or create-project).
---

# Feature execution

This skill builds one Feature of a plan, to the standards in
`Claude/Standards/`. The plan was worked out and approved by `create-plan`, so
this skill **invents nothing and re-argues nothing**: what to build is the
Feature's section of the plan, and how to build it is the standards.

**One run builds one Feature, on its own branch, with its own pull request.**
The branch is `Feature/<Feature_Title>`, where `<Feature_Title>` is the
Feature's title from its `# Feature:` heading in the plan, converted by
`Branching.md` section 2. The title is not asked for: Francois gave it when the
plan was made. Ruled by Francois on 2026-10-22.

**A run normally builds every increment of the Feature in one conversation.** A
second run is needed only when a conversation ends before the Feature is
finished. It reads the branch's commits and carries on from the next increment.

**There is no interview, no plan mode and no approval per increment.** The run
says where it starts, then builds, checks, commits and pushes each increment in
turn. Francois reviews the whole Feature once, in the pull request. Ruled by
Francois on 2026-10-22.

**The review is manual.** `feature-review` is not written, so every check a
standard gives to it is made by Francois when he reviews the pull request. This
skill does not stand in for it. Ruled by Francois on 2026-10-22.

> **Every path in this skill is relative to the repository root, never to this
> file's own location.** Resolve every path below against the repository root.
> If you cannot determine it, ask. Do not guess.

## What this skill asks for, and what it reads

**It asks for nothing the repository knows.** It asks Francois only:

- which plan, when more than one plan has a Feature not yet built;
- a ruling, when the plan does not say something the code needs (step 9);
- what to do with changes no commit holds, when a run finds them (step 6).

| What this skill reads | When | Why |
|---|---|---|
| `CLAUDE.md` | at the start, and before each increment | the rules and the vocabulary |
| this file | before each increment | so a summarised conversation does not lose it |
| the plan, in full | at the start | its decisions bind every Feature |
| the Feature's section of the plan, and the plan's Decisions | before each increment | what to build |
| `BackendCode.md`, `Testing.md` and `Security.md`, in full | at the start | how to build it |
| the sections of those standards the increment touches | before each increment | so the increment is written against them, not against memory |
| `Branching.md` | at the start | the branch, the commits and the pull request |
| every contract the Feature's Contracts table names, under `ai/Plans/<Plan_Title>/Contracts/` until increment 1 moves it, then under `Docs/Contracts/` | at the start | what the code must generate, exactly |
| `Docs/InfrastructureInventory.md` and `Docs/CryptographicInventory.md` | at the start | what the Feature changes in them |
| `git log --format=%s origin/dev..HEAD` | at the start, and before each increment | which increments are finished |

`FrontendCode.md` is not read: every Feature is tech-only.

## What this skill never does

- **Plan.** It never adds, removes or changes a rule, a scenario, a contract or
  an increment. That is `create-plan`'s.
- **Edit a frozen contract**, or a contract's fragment to make a contract test
  pass. The code is corrected instead, by `Testing.md` section 4. Increment 1
  moves the Feature's contracts and changes nothing inside them.
- **Weaken an acceptance test to make it pass.** A test written in increment 1
  is changed later only to fix a mistake in it that does not change what its
  scenario proves, and the closing report names every such change.
- **Build more than one Feature in a run**, or start the next Feature before
  this one's pull request is merged.
- **Commit anything but a finished increment.** No checkpoint commits, by
  `Branching.md` section 4.
- **Merge, delete a branch, or commit to `dev`, `test` or `main`**, by
  `Branching.md` section 7.
- **Throw away changes without Francois's yes.**

## Preconditions

- **Not in plan mode.** This skill writes code. If the session is in plan mode,
  say so and stop.
- **Run every command from the repository root**, except where a step says
  `Code/`. Confirm it with:

  ```bash
  git rev-parse --show-toplevel
  ```

  It prints the repository root. Where that is not the folder you are in, change
  to it. Where it exits 128, this is not a git repository: stop and say so.
- **A solution exists.** Check with:

  ```bash
  find Code -maxdepth 1 -name '*.slnx'
  ```

  **One line**: carry on. **No line**: stop, and say that `create-solution`
  runs first. **More than one line**: stop, and name every file printed.
- **A plan exists.** Check with:

  ```bash
  ls ai/Plans/*.md
  ```

  **No file**: stop, and say that `create-plan` runs first.
- **`dev` is on the server.** Check with:

  ```bash
  git fetch origin dev
  ```

  **Expect exit 0.** Where it fails, stop and say so.
- **`gh` is signed in**, because step 3 asks it which Features are built and
  step 12 opens the pull request. Check with:

  ```bash
  gh auth status
  ```

  **Expect exit 0.** Where it fails, stop and say that `gh auth login` signs in.
- **Docker is running**, because the integration and acceptance tests start a
  PostgreSQL container and step 11 starts every host. Check with:

  ```bash
  docker info --format '{{.ServerVersion}}'
  ```

  **Expect a version number.** Where it fails, stop and say that Docker must be
  started.

## The steps

### 1. Check the repository

Run the checks in the preconditions above. Where one stops, nothing has been
written.

### 2. Find the plan

Where Francois named a plan, use it. Otherwise, the plan is the one in
`ai/Plans/` with a Feature not yet built, found by step 3. **Where more than one
plan has one, ask which.** Read the plan in full.

**The plan must be on `dev` on the server**, because step 4 cuts the Feature's
branch from there, and increment 1 moves contracts that `create-plan` committed
there. Check with:

```bash
git cat-file -e origin/dev:ai/Plans/<Plan_Title>.md
```

**Expect exit 0.** Anything else means the plan's pull request is not merged:
stop and say that the plan is built only once it is.

### 3. Find the Feature

The Features are built in the order of the plan's Features table, and a Feature
is built only after every Feature above it. **A Feature is built once its pull
request is merged.** For each Feature in the table, in order, build
`<Feature_Title>` from its title by `Branching.md` section 2, and ask `gh`:

```bash
gh pr view Feature/<Feature_Title> --json state --jq .state
```

- **`MERGED`**: built. Go to the next Feature.
- **It fails with "no pull requests found"**: not built. **This is the
  Feature.**
- **`OPEN`**: every increment is committed and the pull request waits for
  Francois. Stop and give its address: the next Feature starts once it is
  merged.
- **`CLOSED`**: the pull request was closed without a merge. Stop and ask
  Francois what happens to the Feature.

**Where every Feature is built**, say so and stop. **Where the Feature's
"Planned" column reads "Not yet"**, stop and say that `create-plan` plans it
first.

**Where the Feature's API does not exist** in `Code/<Solution>.slnx`, stop and
say that `create-project` adds it first, as a feature of its own.

Read the Feature's implementation order. **`<total>` is the number of
increments in it.**

### 4. Create or check out the branch

**First, check the working tree:**

```bash
git status --short
git symbolic-ref --short HEAD
```

- **No change listed**: carry on.
- **Changes listed, and the branch printed is `Feature/<Feature_Title>`**: an
  earlier run ended in the middle of an increment. Step 6 deals with it once
  the branch is read; carry on.
- **Changes listed on any other branch**: stop, show the list, and say that
  the working tree must be clean before the Feature's branch is checked out.

Then, by `Branching.md` section 3, **create the branch if it is missing and
check it out if it is present**:

- **It exists locally**, which `git branch --list Feature/<Feature_Title>`
  shows by printing it:

  ```bash
  git switch Feature/<Feature_Title>
  ```

- **It exists only on the server**, which
  `git ls-remote --exit-code --heads origin Feature/<Feature_Title>` shows by
  exiting 0:

  ```bash
  git fetch origin Feature/<Feature_Title>
  git switch -c Feature/<Feature_Title> --track origin/Feature/<Feature_Title>
  ```

- **It exists nowhere**, so this is the Feature's first run:

  ```bash
  git switch -c Feature/<Feature_Title> --no-track origin/dev
  ```

Then confirm it:

```bash
git symbolic-ref --short HEAD
```

**Expect `Feature/<Feature_Title>`.** If it prints `main`, `dev` or `test`, stop
and say so rather than write anything.

### 5. Read where the Feature stands

```bash
git log --format=%s origin/dev..HEAD
```

**Every line must be `<Feature_Title>_<n>/<total>`**, with `<total>` the number
of increments in the plan, and the `<n>` running from 1 with no gap. **A line
that does not match is an error, by `Branching.md` section 4: stop and show
it.** The highest `<n>` is the last increment finished. No line means none is.

### 6. Deal with changes no commit holds

Where step 4 found changes on the Feature's branch, an earlier run ended in the
middle of the next increment. **Stop, show Francois the list, and ask** whether
to continue from them or to throw them away and start that increment again.

**Never throw them away without his yes.** On his yes, from the repository root:

```bash
git restore --source=HEAD --staged --worktree -- .
git clean -fd
```

`git clean -fd` deletes every new file and folder that `.gitignore` does not
cover, so `bin/` and `obj/` stay. Then `git status --short` must list nothing.

### 7. Say where the run starts, and take the baseline

Say, in one short paragraph: the plan, the Feature, the branch, the increment
this run starts at, and `<total>`. **Then carry on without waiting.**

Take the baseline, from `Code/`:

```bash
dotnet build
dotnet test --no-build
```

**Expect the build to pass with 0 warnings.** Note every test that fails.
**Before increment 1, every test passes**, because `dev` holds no contract
without its code. **A failure here stops the run**: report it, because it was
not caused by this Feature. On a later run, the failures must be the ones the
last committed increment left, which step 10 describes.

### 8. Build each increment

For each increment from the next one to `<total>`, in order:

1. **Read again**, by the table at the top: `CLAUDE.md`, this file, the
   Feature's section of the plan and the plan's Decisions, the sections of the
   standards this increment touches, and `git log --format=%s origin/dev..HEAD`.
   **Each increment starts from the repository, never from memory.**
2. **Write what the increment delivers, and nothing more.** The next increment's
   work waits for the next increment.
3. **Change the inventories in the same increment** as what they list, by
   `Security.md` section 11, where the Feature's "Infrastructure inventory"
   names a change.
4. **Check it**, by step 10.
5. **Commit and push it**, by step 11.
6. **Say one line**: the increment's subject, what it delivered, and how many
   tests pass and fail.

Step 9 can stop this at any point.

**Increment 1 is the failing acceptance tests**, one per scenario in the
Feature's Scenarios, written by `Testing.md` section 7: one class,
`<Feature>AcceptanceTests`, in the `Acceptance` collection, deriving from
`AcceptanceTestBase`, each test named by its scenario title and carrying
`[Trait("Rule", "R-<n>")]` for each rule its scenario is tagged with. **No
production code is written in increment 1.**

**Increment 1 also moves the Feature's contracts** from where the plan froze
them to `Docs/Contracts/`, unchanged, by `Testing.md` section 4. Ruled by
Francois on 2026-10-22. For each row of the Feature's Contracts table, with
`<path>` the folder `<Name>Api/<version>/<route>` from its path:

```bash
mkdir -p Docs/Contracts/<path>
git mv ai/Plans/<Plan_Title>/Contracts/<path>/<method>.openapi.json Docs/Contracts/<path>/
git mv ai/Plans/<Plan_Title>/Contracts/<path>/<method>.md Docs/Contracts/<path>/
```

**Where a contract is not at its waiting path**, stop and say so: it was moved
by hand, or the plan names the wrong path.

### 9. When the plan does not say enough

At any point, the code may need something the plan does not say. **Never fill
the gap by assumption.** Stop the increment where it is, and put it to Francois
by `CLAUDE.md` rule 6: what was found, a recommendation, then the question.

- **A small gap**, such as a name the plan left open, or a choice inside one
  increment that changes no rule, scenario, contract or increment. His ruling is
  written into the Feature's "Decisions for this Feature only", in the plan, as
  `<ruling>. Ruled by Francois on <date>.`, and goes into this increment's
  commit. Then the increment carries on.
- **Anything that changes a rule, a scenario, a contract or the implementation
  order.** That is planning. **The run stops.** Say what was found and that
  `create-plan` must plan the change. Every committed increment stays on the
  branch; say which increment was in progress and that its changes are not
  committed. Francois decides what happens to the branch.
- **A standard, a skill or a template that is wrong**, by `CLAUDE.md` rule 2.
  His ruling is written into the file it changes, in this increment's commit,
  before the increment carries on.

`date +%F` prints `<date>`.

### 10. Check the increment

From `Code/`. **Each command has an expected result, and anything else is a
real failure.** Fix it within the increment where the fix is the increment's
own work. Where it is not, stop and report it.

```bash
dotnet restore
dotnet restore --locked-mode
```

**Expect exit 0 from both**, by `BackendCode.md` section 2.

```bash
dotnet build --no-restore
```

**Expect 0 warnings and 0 errors.**

```bash
dotnet test --no-build
```

`dotnet test` is run from `Code/`, and from nowhere else, by `Testing.md`
section 2. What it must show depends on the increment:

- **Increment 1.** Every new acceptance test fails, and **fails on its
  assertion**: Shouldly's message, such as a 404 where the scenario expects
  another status. A test that fails on a build error, an exception in its
  arrangement or a fixture that cannot start is not failing for the right
  reason: fix it. The contract test case of each contract it moved fails too,
  because the API does not generate the operation yet. Every other test
  passes.
- **Every later increment except the last.** The only failing tests are this
  Feature's acceptance tests and contract test cases, **and none that passed
  after the last increment fails now**. The plan's implementation order says
  which scenarios each increment should make pass; say where it did not.
- **The last increment.** **Every test passes.**

**After the last increment, check the containers too**, from the repository
root, as `create-project`'s API recipe, step 17, "Verify", does:

```bash
docker compose up -d --build --wait
docker compose ps -a
```

**Expect exit 0**, then `certificates`, `roles` and `migrator` exited with 0, and
`database` and every API's service running. Call the Feature's API's health over
HTTPS, with `<HostPort>` read from `compose.yml`:

```bash
curl --silent --show-error --cacert Code/Certificates/ca.crt --output /dev/null --write-out '%{http_code}\n' https://localhost:<HostPort>/v1/health
```

**Expect `200`.** Anything else: show
`docker compose logs certificates roles database migrator <Name>Api`.

### 11. Commit and push the increment

By `Branching.md` sections 4, 5 and 6, "How a skill ends its run". **First,
check that every change is this increment's:**

```bash
git status --short
```

Every line must name a file this increment wrote, changed or deleted. **Where a
line names anything else, stop and ask Francois** rather than commit it,
because `git add -A` takes every line. Then:

```bash
git add -A
git commit -m "<Feature_Title>_<n>/<total>"
git push -u origin Feature/<Feature_Title>
```

The first push creates the branch on the server. **The push is a backup and
nothing else**, by `Branching.md` section 5: nothing is built on a feature
branch.

### 12. Open the pull request

After the last increment is committed and pushed:

```bash
gh pr create --base dev --head Feature/<Feature_Title> --title "<Feature_Title>" --body-file - <<'EOF'
<the closing report>
EOF
```

**The body is the closing report in step 13**, by `Branching.md` section 6.
`gh pr create` prints the pull request's address.

### 13. Report, and wait for the merge

The closing report. Say plainly:

- the plan, the Feature and the branch;
- each increment's commit, from `git log --format='%h %s' origin/dev..HEAD`,
  with one line on what it delivered;
- each scenario, its rule ids, and its acceptance test, all passing;
- the number of tests, all passing;
- each contract the Feature implements, moved to `Docs/Contracts/` unchanged;
- each change to `Docs/InfrastructureInventory.md` or
  `Docs/CryptographicInventory.md`;
- each ruling written during the run, and the file it was written into;
- each acceptance test changed after increment 1, and why;
- **that the review is manual**: `feature-review` is not written, so every rule
  a standard gives to it is checked by reading the pull request.

Then give the pull request's address. **Then wait.** When Francois says in this
conversation that the pull request is merged, run `Branching.md` section 6,
"After the merge", which deletes the local feature branch, and then stop. The
next Feature is a new run, in a new conversation.

## Guardrails

- Nothing lives only in this chat. A ruling is written into the plan, a
  standard or a skill, in the increment it arose in.
- The plan is the only source of what to build, and the standards the only
  source of how. Neither is filled in by assumption.
- Each increment starts from the repository, never from memory.
- One commit per finished increment, pushed straight away. Nothing else is
  committed.
- A frozen contract is never edited, and an acceptance test is never weakened.
- A check that fails is fixed or reported, never skipped.
- Changes no commit holds are never thrown away without Francois's yes.
- This skill never merges, never commits to `dev`, `test` or `main`, and deletes
  its local branch only once told the pull request is merged.
