---
name: create-project
description: >-
  Adds one API to a solution that create-solution created. Zapper is API only,
  so a frontend is refused, and Frontend.md is kept for review. Asks what is
  being created, the feature title, the API's name, whether a customer's system
  calls it and one sentence describing it, and reads everything else from the
  repository. Ends by committing, pushing and opening a pull request into dev,
  and deletes the local branch once told it is merged. Runs every time the
  product grows an API. Use when the user says "create-project", "create an API"
  or "add an API". Do NOT use to create the solution (that is create-solution),
  or for writing a feature's own code (that is feature-execution).
---

# Create project

This skill adds one project to a solution, to the standards in
`Claude/Standards/`, and never to habit or to what another product once did.
Every file it writes traces to a sentence in a standard or to a recorded ruling;
nothing here is invented.

**It runs every time the product grows a part.** `create-solution` runs once
per repository and creates what every project shares. This skill adds one API or
one frontend to that, and it is the only thing that does.

**Each run is one feature, on a feature branch of its own.** This skill asks for
the feature title, and `Branching.md` section 2 builds the branch, the commit
subject and the pull request title from it. The run has no plan,
because the recipe fixes what is created and the tests that prove it.

**Each kind of project has its own recipe beside this file**, so creating one
never loads the other's. `Api.md` is an API's, and `Frontend.md` a frontend's.
This file holds what both share: the values, the preconditions, the branch and
the order of the run.

> **Every path in this skill is relative to the repository root of the solution
> being added to, never to this file's own location.** `Code/<Solution>.slnx`
> means `<repo root>/Code/<Solution>.slnx`. Resolve every path below against the
> repository root. If you cannot determine it, ask. Do not guess.

## The values this skill asks for, and what it reads

| What this skill asks for | When | What reads it |
|---|---|---|
| what is being created, a frontend or an API | every run | step 2, "Ask what is being created", which chooses the recipe |
| the feature title | every run | step 3, "Ask for the feature title", and step 5, "Name the branch", which builds the branch name from it |
| the project's name, `<Name>Api` for an API and `<Frontend>` for a frontend | every run | every file the recipe writes |
| whether a customer's system calls the API, and one sentence describing it | for an API | `Api.md` |
| the API or APIs the frontend calls, each as `<Name>Api` | for a frontend | `Frontend.md`, once per API |

**Nothing is asked that the repository already knows.**

| What this skill reads | Where from |
|---|---|
| `<Solution>` | the name of `Code/<Solution>.slnx`, without `.slnx` |
| the project list | each `Path` attribute in `Code/<Solution>.slnx`, the part before the first `/` |
| the solution's APIs | every project in that list named `<Solution>.<Name>Api` |
| every version the recipe writes | `Claude/Standards/Versions.md`: each `<Versions.md: ROW>` in `Api.md` and `Frontend.md` is replaced by the version in the row whose first column reads ROW, without backticks, and where two rows share a name, by the row whose "Where it is fixed" names the file being written |

**An API is a project named `<Solution>.<Name>Api`**, which `BackendCode.md`
section 3 names, and `<Name>Api` is that name without `<Solution>.`.
`<Solution>.ApiCommon` does not end in `Api` and is not an API. `<Name>` takes the
shape `<Solution>` carries, which `BackendCode.md` section 2 states: one
PascalCase token of letters only, carrying no dot, no hyphen, no underscore, no
space and no digit. So `PaymentsApi` fits, and `Payments`, `paymentsApi`,
`Payments-Api` and `Payments.Api` do not.

**`<Frontend>` takes the same shape**, which `FrontendCode.md` section 3 states.

**A value that does not fit is refused, and is never adjusted to fit.** Ask for
it again rather than mend it.

**A frontend's APIs are asked for, and never derived from its name.** A frontend's
name is its own, `FrontendCode.md` section 3 states that it calls one or more
APIs, and nothing stops two frontends calling one API.

## What this skill never creates

- **The solution, or anything `create-solution` creates.** This skill adds to a
  solution that exists, and step 1, "Check the repository", stops where none does.
- **A database, a script in `compose.yml`, or a change to any service there but
  the API's own.** `create-solution` wrote `compose.yml`, and it creates the
  database. An API's role, its certificate and its server certificate come from
  the API's line in the solution file, which the API recipe adds and then runs
  `compose.yml`'s `roles` service for. **The API recipe adds one service, the
  API's own**, by an exact edit, and changes nothing else in the file. A
  frontend's recipe does not change `compose.yml`.
- **A plan.** The recipe fixes what is created and the tests that
  prove it.
- **A README, and nothing of that kind under `Docs/`.** This skill writes no
  directions into the repository. It states at the end what it reports to whoever
  runs it, and recording those steps in the product's own documentation is
  theirs.
- **Anything that deploys, and no line of `.github/workflows/ci.yml`.** Each new
  project joins the pipeline `create-solution` wrote with no change to it. What
  a project's deployment must do is reported at the end.
- **An entry in `Docs/AcceptedVulnerabilities.md`.** `Security.md` section 13
  accepts a finding only with the reason why it cannot be fixed, which is a
  person's to give. The recipe's verify names every finding, and the report says
  what has to happen to each.
- **`.editorconfig`, `BannedSymbols.txt`, and the ESLint and Prettier
  configuration.** All of them belong to the solution-wide enforcer pass, and
  `FrontendCode.md` section 4 puts the ESLint and Prettier configuration at
  `Code/` and never in a frontend's folder.

## Preconditions, shared by both recipes

These hold for every run, whatever is being created. **Nothing is written, and no
branch is created, until every value is asked for and checked**, the recipe's own
included. So a refusal at any point leaves no file and no branch behind.

- **Run every command from the repository root**, `cd`-ing into `Code/` only
  where a step says to. Confirm the root with:

  ```bash
  git rev-parse --show-toplevel
  ```

  It prints the repository root. Where that is not the folder you are in, change
  to it. Where it exits 128, this is not a git repository: stop and say so.
- **`Code/` holds exactly one `.slnx` file, directly inside it.** Check with:

  ```bash
  find Code -maxdepth 1 -name '*.slnx'
  ```

  **One line** names the solution file: carry on. **No line** means no solution
  exists here: stop, and say that `create-solution` creates the solution and
  runs first. **More than one line** means this skill cannot tell which solution
  it is adding to: stop, and name every file the command printed. Never choose
  one.
- **`dev` is on the server**, because the feature branch is cut from it. This
  skill reads no other branch. Step 1, "Check the repository", checks it before
  anything is asked.
- **`gh` is signed in**, because step 8 opens the pull request with it. Step 1,
  "Check the repository", checks it before anything is asked.
- **Ask for the feature title, every run, with no exception.** `Branching.md`
  section 2 derives every branch name, commit subject and pull request title from
  it, and `Branching.md` section 3 states that the feature branch is created before
  any code is written.
- **The feature branch is created if missing and checked out if present**, per
  `Branching.md` section 3, cut from the current state of `dev` on the server.
  Step 6, "Create or check out the feature branch", runs it, once everything
  before it has passed.
- **Never write a file directly on `main`, `dev` or `test`.** Step 6, "Create or
  check out the feature branch", checks it.
- **A step that changes a file that already exists uses exactly one of three
  forms, and names it:**

  1. **Whole.** "Replace `<path>` whole:", followed by the file's full text.
  2. **Command.** The step's own command, run as written, where a tool owns the
     file: npm, dotnet, openssl, the shadcn CLI, `rm`, `cp`. JSON a step changes
     is changed by a `node -e` command, as `create-project`'s `Frontend.md`,
     step 17, "Each API's allowed origin", does. A file a tool later changes is
     written first in the form that tool keeps.
  3. **Exact edit.** "In `<path>`, after the line `<anchor>`", or "before the
     line", or "replace the line `<old>` with", followed by the new lines with
     their indent. The anchor is a whole line, quoted, that occurs exactly once in
     the file, or the end of the file. Nothing else in the file changes. The edit
     is made as one exact text replacement with the Edit tool, never by a script
     written for the run.

## The steps

### 1. Check the repository

Confirm the repository root and find the solution file, with the two commands in
the preconditions above. Where either of them stops, nothing has been written.

Then read `<Solution>` from the file's name:

```bash
basename Code/*.slnx .slnx
```

It prints `<Solution>`. Then read the project list:

```bash
grep -o 'Path="[^"/]*/' Code/<Solution>.slnx | sed 's/^Path="//; s#/$##'
```

It prints one project's name per line, such as `<Solution>.Domain`. **The
solution's APIs are the lines that end in `Api`.** Keep both lists for the
recipe.

Then check that `dev` is on the server:

```bash
git fetch origin dev
```

**Expect exit 0. Where it fails, `dev` is not on the server: stop and say so.**
It is checked here, before anything is asked, so that no value is asked for a
branch that cannot be cut.

Then check that `gh` can open the pull request:

```bash
gh auth status
```

**Expect exit 0.** Where it fails, stop and say that `gh auth login` signs in.

### 2. Ask what is being created

Ask whether this run creates a frontend or an API.

- **An API.** Read `Api.md`, beside this file, now and in full, and carry on to
  step 3, "Ask for the feature title".
- **A frontend.** Stop, and say that Zapper is API only. **Ruled by Francois on
  2026-10-22.** `Frontend.md` and `FrontendCode.md` are kept for review and are
  not run: `create-solution` no longer writes the npm workspace root and the
  contract type generator that `Frontend.md` needs.

### 3. Ask for the feature title

Ask for the feature title, in plain words, such as `Payments API` or
`Customer portal`. **It says what this run adds**, the way a reader of the branch
list would want to see it. It is kept exactly as given, and step 5, "Name the
branch", builds the branch name from it.

**A title that leaves no letter and no digit once step 5's rule has dropped every
character that is not a letter, a digit or an underscore is refused**, and asked
for again: "!!! ???" leaves `_`, which would cut `Feature/_`. Ruled by Francois
on 2026-10-21.

### 4. Ask the recipe's values, and run its preconditions

Ask for the values the recipe's own table names, and run every one of its
preconditions, in the order the recipe gives them. **Where one refuses, stop:**
nothing has been written, and no branch exists.

### 5. Name the branch

Every value is now asked for and checked. **Build `<FeatureTitle>` from the feature
title**, per `Branching.md` section 2's naming scheme: the first letter of each
word capitalised, every other letter left as it was given, each space written as
an underscore, and anything that is not a letter, a digit or an underscore
dropped. The branch is `Feature/<FeatureTitle>`.

### 6. Create or check out the feature branch

Fetch `dev`, then create the branch or check it out:

```bash
git fetch origin dev
git switch <branch> 2>/dev/null || git switch -c <branch> --no-track origin/dev
```

where `<branch>` is the name step 5, "Name the branch", built. **If
`git fetch origin dev` fails, `dev` is not on the server: stop and say so** rather
than cut the branch from anything else.

Then confirm where you are:

```bash
git symbolic-ref --short HEAD
```

It prints the branch just created or checked out. **If it prints `main`, `dev` or
`test`, stop and say so** rather than write anything.

### 7. Carry out the recipe

Carry out the recipe's steps in its own order, ending with its verify. Each of its
commands has an expected result, and anything else is a real failure: stop,
report what it said, and do not carry on.

### 8. Commit, push and open the pull request

Run `Branching.md` section 6, "How a skill ends its run": check that every change
is this run's, against the closing report's last item, which lists them; then one
commit with the subject `<FeatureTitle>_1/1`, the push, and a pull request into
`dev` titled `<FeatureTitle>`. **Its body is the closing report below** for the
kind of project created. Ruled by Francois on 2026-10-22.

### 9. Report, and wait for the merge

Say every item of the closing report below for the kind of project created, and
what the recipe's verify found, then give the pull request's address. **Then
wait.** When Francois says in this conversation that the pull request is merged,
run `Branching.md` section 6, "After the merge", which deletes the local feature
branch, and then stop.

## The closing report for an API

Say all fifteen of these plainly:

- **The API is created and its verify passed.** `Code/<Solution>.<Name>Api` exists,
  it is in `Code/<Solution>.slnx`, every project has its lock file, the solution
  built with 0 warnings and 0 errors, and `dotnet test` from `Code/` passed. Give
  the test count it reported.
- **Health's contract is frozen.** `Docs/Contracts/<Name>Api/v1/health/get.openapi.json`
  and the document beside it freeze `GET /v1/health`, 200 with no body and public,
  and the contract test class proves the API generates exactly that operation.
- **The API's role can read what the API reads at startup, and nothing else.** The
  grant migration gives `<Role>` `USAGE` on the `Error` and `Migration` schemas and
  `SELECT` on the two Error tables and the history table, and the Migrator applied
  it to the development database. The API connects as `<Role>` over TLS, with its
  client certificate and no password.
- **It refuses to serve against a database that is behind it.** At startup it logs
  at `Critical` and exits 1 where the database lacks a migration it holds, and it
  loads both Error tables before it serves. **Each deployed environment runs the
  Migrator before the API**, or the API does not start.
- **Whether it publishes its documentation.** Say which answer was given. Where a
  customer's system calls it, `<Solution>:OpenApi:IsEnabled` is `true` in
  `appsettings.json`, so it publishes in every environment. Where only the
  product's own frontends and services call it, the switch is set only in
  `appsettings.Development.json`, so **each deployed environment sets
  `<Solution>__OpenApi__IsEnabled`, to `true` everywhere except production and to
  `false` in production**, which `Security.md` section 9 states, and an environment
  that sets nothing does not start.
- **Its limits, and who may read its responses.** It refuses a request body over
  1048576 bytes with 413, and more than 300 requests a minute from one network
  address with 429, health included. Its development list of allowed origins is
  empty, and `Frontend.md` adds each frontend that calls it. **Each deployed
  environment sets its own list**, one environment variable per address, such as
  `<Solution>__Security__AllowedOrigins__0`, holding only the product's own
  frontends that call it, which `Security.md` section 10 states. An environment that
  sets none refuses every origin.
- **Every caller of a deployed API shares one rate limit until the part in front of
  it is configured.** `UseForwardedHeaders()` is configured with nothing, because
  `BackendCode.md` section 9 configures it with the proxies or networks the
  infrastructure inventory states, and the inventory states none. So every request a
  deployed API receives through the part in front of it counts against that part's
  one address, and all its callers share one limit of 300 a minute. The
  infrastructure work configures it, with the inventory.
- **The API runs in Docker, at `https://localhost:<HostPort>`.** From the
  repository root, `docker compose up -d` builds its image and starts it once the
  Migrator has finished with 0, so a fresh clone needs Docker 26 or later and
  nothing else. `curl --cacert Code/Certificates/ca.crt
  https://localhost:<HostPort>/v1/health` calls it, and plain HTTP gets no
  answer. After a change under `Code/`, `docker compose up -d --build` builds
  the images again. **A Migrator that fails leaves no API running**, measured,
  and `docker compose logs certificates roles database migrator <Name>Api` shows
  what each service did. `dotnet run` still runs the API on the address in its
  launch profile, with the .NET development certificate, beside its container.
- **A deployed API needs its database settings and its certificates from its
  environment.** `appsettings.Development.json` is left out of every build but
  Debug, so each deployed environment sets `<Solution>__Database__ConnectionString`,
  naming `<Role>`, and puts the role's files at `/certificates`, which
  `create-solution`'s closing report records for every host outside this
  machine's Docker. It also supplies the API's own HTTPS certificate, through the
  two `ASPNETCORE_Kestrel__Certificates__Default__` variables `compose.yml` sets
  here. Ruled by Francois on 2026-10-21: deploying to AWS is not a requirement.
- **Its tests.** `<Name>ApiFixture` starts the API against a clone of the template,
  `<Name>ApiV1ContractTests` tests every fragment under `Docs/Contracts/<Name>Api/v1/`,
  and the template fixture creates `<Role>` before it migrates. **Say whether this
  run wrote the acceptance fixture**, `AcceptanceFixture.cs` with
  `AcceptanceCollection.cs`, `AcceptanceApiFactory.cs` and `AcceptanceTestBase.cs`,
  or found it already written by an earlier API, and that this API's line is in
  it. No acceptance test exists yet. One calls the API over HTTP and needs Docker,
  and no browser.
- **The authorization test class is not written.** `<Name>ApiV1AuthorizationTests`
  is written by whoever adds this API's first operation that is not health, because
  a theory fed no case fails, and health is public.
- **The pipeline is unchanged and picks the API up**, through the solution file. No
  line of `.github/workflows/ci.yml` was written.
- **The API needs a deployment, and this repository holds none of it.** Neither this
  skill nor the pipeline names a deployment platform. **The API's deployment builds
  from this repository, does not deploy a commit until the CI run for that commit
  has passed, and runs the published output, never `dotnet run` against the
  source**, because a launch profile puts a host in `Development` and a published
  artefact carries none. Its Dockerfile builds an image of that published
  output. **Its address never answers plain HTTP**, which
  `Security.md` section 11 states, and no other frontend or API shares its host name,
  which `Security.md` section 10 states.
- **`Docs/InfrastructureInventory.md` holds the API and its connection to the
  database**, with every cell this skill does not know left empty. A feature's plan
  states the rest, and `Security.md` section 11 has the inventory changed in the
  same increment as what it lists.
- **What the commit holds.** The new folder with its Dockerfile, `compose.yml`,
  the grant migration and its designer
  file, `Code/<Solution>.slnx`, the two contract
  files, the inventory, and in `Code/<Solution>.IntegrationTests/` its project file,
  its `packages.lock.json`, `TemplateDatabaseFixture.cs`, the API fixture, the
  contract test class and the acceptance fixture's files are all in the commit
  `<FeatureTitle>_1/1`, and so is `Code/Directory.Packages.props` where
  this run added `Microsoft.AspNetCore.Mvc.Testing`.

**The product's own developer setup document records the whole of this list.**

## The closing report for a frontend

Say all twelve of these plainly:

- **The frontend is created and its verify passed.** `Code/<Solution>.<Frontend>`
  exists, it is a workspace of `Code/package.json`, the one lockfile is still
  `Code/package-lock.json`, and the build ran the frontend's own TypeScript and not
  the contract type generator's. Name each API it calls.
- **It shows no page yet.** Its route table holds the layout route and no page
  route, so no address matches a route, and every address shows the error
  boundary's fixed message, "Something went wrong". The first page arrives with
  the first feature that plans one, and a module is never created here.
- **Its types are generated and committed with it.** Each API's
  `src/api/<name>-api/types.ts` was written by the contract type generator from
  that API's frozen fragments, and the frontend's own build regenerates them
  before its type check. The failure type every query and mutation throws is
  declared once, in `src/api/query.d.ts`, and is `never` until an API freezes an
  operation that declares a failure.
- **Each API it calls lets it read a response in development, and in no deployed
  environment yet.** `Security.md` section 10 has an API let a page at another
  origin read its responses only when that origin is on the API's list. The
  development server answers at `http://localhost:<FrontendPort>`, a port fixed in
  `vite.config.ts` that no other project uses, and each called API's
  `appsettings.Development.json` holds that address. **Each deployed environment
  sets each API's own list**, holding the address that environment gives this
  frontend.
- **`public/config.json` holds each API's development address**, read from the
  `https` profile of that API's `Properties/launchSettings.json`. Everything in it
  can be downloaded by anyone, which is why `FrontendCode.md` section 7 allows no
  secret in it. **Each deployed environment serves its own `config.json`**, with
  the address that environment gives each API, in place of the one built from this
  repository.
- **`npm audit` reported what the verify named**, and each finding is fixed, or
  accepted by an entry in `Docs/AcceptedVulnerabilities.md` under `Security.md`
  section 13, before the pipeline passes. Name every finding again here. **Never
  run `npm audit fix --force`**: it removes a finding by installing an old major
  version of a package, which can no longer produce what this frontend uses.
- **The frontend has its test support and its first two test files.** `src/test/`
  holds the setup file, the MSW server, `renderWithProviders`, the accessibility
  file and `axe`, and `vite.config.ts` names the setup file in its `test` block.
  `npm test` ran the tests of the application frame and the error boundary, each
  file with its accessibility scan, and the pipeline's `npm test` step runs them
  too. The first fixture, and with it `src/test/fixtures/`, arrives with the
  increment that first writes one. **Say whether this run wrote the shared
  acceptance base class**, `Code/<Solution>.IntegrationTests/AcceptanceTestBase.cs`,
  with its two packages, or found it already written by an earlier frontend. No
  acceptance test exists yet.
- **A machine runs acceptance tests only once Playwright's own Chromium is
  installed on it.** Playwright needs its own build of Chromium, and an
  acceptance test on a machine without it fails with "Executable doesn't exist",
  measured. After a build, each
  machine that runs the tests installs it once. Playwright's documented command,
  from `Code/`, is `pwsh <Solution>.IntegrationTests/bin/Debug/net10.0/playwright.ps1
  install chromium`, which needs PowerShell. Where PowerShell is not installed, the
  Node that Playwright ships in the same build folder runs the same installer, from
  that folder: `.playwright/node/<platform>/node .playwright/package/cli.js install
  --only-shell chromium`, where `<platform>` is the one folder under
  `.playwright/node/`, which also holds a `LICENSE` file. Installed that way, the
  acceptance test then ran.
- **The pipeline is unchanged and picks the frontend up.** Its `npm run build`
  and `npm test` steps reach every workspace in `Code/package.json`, and its
  contract types step regenerates this frontend's types and fails on any file the
  run changes. No line of `.github/workflows/ci.yml` was written.
- **The frontend needs a deployment, and this repository holds none of it.**
  Neither this skill nor the pipeline names a deployment platform. **The
  frontend's deployment builds from this repository, and does not deploy a commit
  until the CI run for that commit has passed.** It serves the built `dist/`
  folder, which holds no source map. **Every response it sends carries what
  `Security.md` section 10 states**: `Strict-Transport-Security` with a `max-age`
  of at least one year and `includeSubDomains`, `X-Content-Type-Options: nosniff`,
  and a Content-Security-Policy whose `frame-ancestors` is `'none'`. Every page
  also carries `Referrer-Policy: no-referrer` and a Content-Security-Policy under
  which it loads and connects to nothing but its own address and the addresses of
  the APIs it calls, with `object-src` and `base-uri` set to `'none'`, script run
  only from its own address, no inline script and no code built from text, and
  images allowed from object URLs its own script made. Every other response
  carries a Content-Security-Policy that allows nothing to load. **Its address
  answers plain HTTP only by redirecting to HTTPS**, which `Security.md` section 11
  states, and no other frontend or API shares its host name, which `Security.md`
  section 10 states.
- **`Docs/InfrastructureInventory.md` holds the frontend and its connections**,
  with every cell this skill does not know left empty. A feature's plan
  states the rest, and `Security.md` section 11 has the inventory changed in the
  same increment as what it lists.
- **What the commit holds.** The new folder, `Code/package.json`,
  `Code/package-lock.json`, the generator's configuration file, each called API's
  `appsettings.Development.json` and the inventory are all in the commit
  `<FeatureTitle>_1/1`. Where this run wrote the shared acceptance base
  class, so are `Code/Directory.Packages.props`, and in
  `Code/<Solution>.IntegrationTests/` its project file, its `packages.lock.json` and
  `AcceptanceTestBase.cs`.

**The product's own developer setup document records the whole of this list.**
