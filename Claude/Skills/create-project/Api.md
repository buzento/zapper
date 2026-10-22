# API recipe

`create-project`'s `SKILL.md` reads this file at its step 2, "Ask what is being
created", when the run creates an API. **It creates one API,
`Code/<Solution>.<Name>Api`**, with everything an API needs before its first
feature: the project, its `StartupHostedService.cs`, `Program.cs` and
`Endpoints.cs`, its two settings files,
the grant migration that lets its database role read what it reads at startup,
health's frozen contract, its test fixture, its contract test class, its line in
the acceptance fixture,
its Dockerfile, and its service in `compose.yml`, which runs it in a container.
Every file traces to a sentence in `BackendCode.md`, `Testing.md` or `Security.md`,
or to a recorded ruling.

`SKILL.md` asks for this recipe's values and runs its preconditions at its step 4,
"Ask the recipe's values, and run its preconditions", before any branch exists.
It carries out the steps below at its step 7, "Carry out the recipe", on
the feature branch.

> **Every path in this file is relative to the repository root**, the same as in
> `SKILL.md`. Run every command from the repository root, except where a step
> says to run it from `Code/`.

## The values, and the names derived from them

| Value | Asked or read | What it is |
|---|---|---|
| `<Name>Api` | asked | the API's own name, in the shape `SKILL.md` defines for an API |
| whether a customer's system calls the API | asked | yes or no. It decides where the documentation switch is written, which `Security.md` section 9 states |
| `<Description>` | asked | one sentence, written for the API's consumer, saying what the API does for them |
| `<Solution>` | read by `SKILL.md` step 1, "Check the repository" | the solution's name |
| `<DatabaseName>` and `<Port>` | read by a precondition below | the database's name and its server's port, from the Migrator's settings |

**Ask the description as one sentence, written for the consumer of the API**,
which is the test `BackendCode.md` section 19 sets for an operation's summary. It
says what the API does for its caller, and never how it is built. **It ends in a
full stop and holds no double quote and no backslash**, because it is written into
a JSON file as it stands. A sentence that does not fit is refused and asked for
again, and never mended.

**Every other name is derived, and none is asked for:**

| Name | Derived as | Where the rule is |
|---|---|---|
| `<Name>` | `<Name>Api` without `Api` | `BackendCode.md` section 3 |
| `<Solution>.<Name>Api` | the API's project and its folder, directly under `Code/` | `BackendCode.md` section 3 |
| `<Title>` | `<Name>`'s words separated by spaces, then `API`, so `CustomerOrders` gives `Customer Orders API` | Francois's ruling of 2026-10-11 |
| `<Role>` | the API's database role, `<Solution><Name>ApiRole` | `BackendCode.md` section 18 |
| `<Date>` | the date this run writes health's contract, as `yyyy-MM-dd` | `Contract.Template.md` |
| `<Image>` | `<Solution>-<Name>Api` in lower case, the name of the API's image, so `Zapper` and `OrdersApi` give `zapper-ordersapi` | Francois's ruling of 2026-10-21 |
| `<HostPort>` | the port the API's container is published on, on this machine: the first port from 8443 upward that `compose.yml` does not already publish and that is free here, found in step 16, "The API in its container" | Francois's ruling of 2026-10-21 |

**Derive `<Title>`** with:

```bash
printf '%s API\n' "$(printf '%s' '<Name>' | sed -E 's/([a-z])([A-Z])/\1 \2/g')"
```

It prints the title. `ApiInformationDocumentTransformer`, which `create-solution`
wrote, publishes it as the document's `info.title`, and the description as its
`info.description`.

## What this recipe creates, in this order

1. `Code/<Solution>.<Name>Api`, from the Web SDK's template, with its project file
2. `StartupHostedService.cs` and `Program.cs`
3. `Endpoints.cs`
4. `appsettings.json` and `appsettings.Development.json`
5. Its line in `Code/<Solution>.slnx`, the restore, and its database role with
   its certificate
6. The grant migration, and the Migrator's run that applies it
7. Health's frozen fragment
8. Health's contract document
9. Nothing: Zapper is API only, so there is no contract type generator
10. Its reference from `<Solution>.IntegrationTests`, and `Microsoft.AspNetCore.Mvc.Testing`
11. The step in the template database fixture that creates its role
12. `Code/<Solution>.IntegrationTests/<Name>ApiFixture.cs`
13. `Code/<Solution>.IntegrationTests/<Name>Api/<Name>ApiV1ContractTests.cs`
14. The acceptance fixture and the shared acceptance base class, where no API
    before this one wrote them, and its line in the fixture
15. Its rows in `Docs/InfrastructureInventory.md`
16. `Code/<Solution>.<Name>Api/Dockerfile`, its service in `compose.yml`, and
    its container, started

Step 17, "Verify", then verifies all of it.

## What this recipe never creates

- **An operation other than health's line.** `BackendCode.md` section 6 has health
  provided by `<Solution>.ApiCommon` as one call into the version group, and every
  other operation arrives with the feature that plans it.
- **The authorization test class**, `<Name>ApiV1AuthorizationTests`. It was settled on
  2026-09-25 that a theory fed no case fails outright, and health is public, so the class is written
  by whoever adds the API's first operation that is not health.
- **An options class of the API's own.** `<Solution>.ApiCommon`'s `SecurityOptions`
  and `OpenApiOptions` hold every setting an API has on day one, and the API
  supplies only their values.
- **An `appsettings.Production.json`**, or one for any other deployed environment,
  which `BackendCode.md` section 14 states.
- **An acceptance test.** `Testing.md` section 7 writes one per scenario in a plan.

## Preconditions

`SKILL.md`'s step 4, "Ask the recipe's values, and run its preconditions", runs
these, in this order, after it has asked for `<Name>Api`, whether a customer's
system calls the API, and `<Description>`. **Where one fails, stop, say which and
why, and write nothing.**

- **The .NET SDK, `dotnet ef`, `jq`, `nc` and `curl` are installed, and Docker
  is running.**

  ```bash
  dotnet --version
  dotnet ef --version
  jq --version
  command -v nc
  curl --version
  docker info
  docker compose version
  ```

  Each exits 0. `dotnet ef` generates the grant migration, `jq` reads the
  Migrator's settings below, `nc` checks `<HostPort>` in step 16, "The API in its
  container", `curl` calls the API in its container in step 17, the integration
  tests start a PostgreSQL container, and Docker Compose runs the local database,
  the Migrator and the API. **Node and npm are not needed. Ruled by Francois on
  2026-10-22: Zapper is API only.** Where any exits non-zero, stop and raise it to whoever ran
  this skill to resolve. This skill installs nothing on the machine it runs on.
- **Docker is version 26 or later.**

  ```bash
  docker version --format '{{.Server.Version}}'
  ```

  It prints the engine's version alone, such as `28.0.1`. The API's service
  mounts its own role's certificate folder by a volume's `subpath`, which needs
  Docker Engine 26 or later, as `create-solution`'s preconditions record. **A
  first number below 26 means it is too old: stop and raise it to whoever ran
  this skill to resolve.** Measured on Docker Engine 28.0.1 only.
- **`<Name>Api` fits its shape.**

  ```bash
  printf '%s\n' '<Name>Api' | grep -Ex '([A-Z][a-z]+)+Api'
  ```

  It prints the name back where it fits. **No output means it does not fit**:
  refuse it and ask again, and never adjust it.
- **The API does not exist yet**, as a folder, as a line in the solution file, or
  as a folder of contracts.

  ```bash
  test -e Code/<Solution>.<Name>Api
  grep -c 'Path="<Solution>.<Name>Api/' Code/<Solution>.slnx
  test -e Docs/Contracts/<Name>Api
  ```

  The first and the third exit 1, and the second prints `0`, where it does not
  exist. **Anything else means the name is taken**: say so, and ask for another
  name.
- **The Migrator's settings name the database and its port.** `create-solution`
  wrote them:

  ```bash
  jq -r '.<Solution>.Database.ConnectionString | split(";") | map(split("=") | {(.[0]): .[1]}) | add | .Database' Code/<Solution>.Migrator/appsettings.Development.json
  jq -r '.<Solution>.Database.ConnectionString | split(";") | map(split("=") | {(.[0]): .[1]}) | add | .Port' Code/<Solution>.Migrator/appsettings.Development.json
  ```

  The first prints `<DatabaseName>` and the second `<Port>`. Where either prints
  `null` or an error, `create-solution` has not finished in this repository:
  stop and say so. Measured on 2026-10-22: both printed what the Node commands
  they replace printed, a missing key printed `null`, and a missing section
  failed with exit 5.
- **The local database is running**, from the repository root:

  ```bash
  docker compose up -d --wait database
  ```

  Expect exit 0. `create-solution`'s step 12, "`<Solution>.Migrator`'s real
  `Program.cs`", wrote `compose.yml`, and this starts the server where it is
  stopped and leaves it alone where it runs. Step 5, "The solution file, and the
  restore", creates the API's role on it. **Where it fails**, stop, report what it
  said, and show what each service did with
  `docker compose logs certificates roles database`.

---

### 1. The project

`BackendCode.md` section 3 makes each API one project, `<Solution>.<Name>Api`, and
`create-solution`'s step 10, "`<Solution>.ApiCommon`", records that an API needs
the Web SDK, since ASP.NET Core does not reach it through a project reference.
**It starts from the .NET SDK's `web` template**, because that template writes the
API's `Properties/launchSettings.json`, which `Frontend.md` reads.

From `Code/`:

```bash
dotnet new web --name <Solution>.<Name>Api --output <Solution>.<Name>Api --no-restore
```

Expect exit 0 and a new folder, `Code/<Solution>.<Name>Api`, holding
`<Solution>.<Name>Api.csproj`, `Program.cs`, `appsettings.json`,
`appsettings.Development.json` and `Properties/launchSettings.json`. Steps 1 to 4
replace every file but the last. **`Properties/launchSettings.json` is the
template's, unchanged**: its `https` profile holds the API's development address,
a port the template chose, and its `environmentVariables` put the API in
`Development`, which `create-solution`'s step 12, "`<Solution>.Migrator`'s real
`Program.cs`", records as how an API has been getting `Development` all along.

Replace `Code/<Solution>.<Name>Api/<Solution>.<Name>Api.csproj` whole:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <ProjectReference Include="../<Solution>.Domain/<Solution>.Domain.csproj" />
    <ProjectReference Include="../<Solution>.Infrastructure/<Solution>.Infrastructure.csproj" />
    <ProjectReference Include="../<Solution>.ApiCommon/<Solution>.ApiCommon.csproj" />
  </ItemGroup>

  <ItemGroup Condition="'$(Configuration)' != 'Debug'">
    <Content Remove="appsettings.Development.json" />
  </ItemGroup>

</Project>
```

**Three project references, which are `BackendCode.md` section 3's reference
graph for an API.** No property: `TargetFramework`, `Nullable` and
`ImplicitUsings` come from `Code/Directory.Build.props`, which the template's
own file repeated. **No package**: everything the API uses comes through those
three projects and the Web SDK.

**`appsettings.Development.json` is removed outside Debug. Ruled by Francois on
2026-10-02.** The Web SDK carries every settings file into the build output and
into a publish, and a deployed host never reads the development file's database
role or its allowed origins. `create-solution`'s step 12,
"`<Solution>.Migrator`'s real `Program.cs`", records the same condition on the
Migrator's file, measured there.

### 2. `Program.cs`

`BackendCode.md` never shows a whole `Program.cs`, and states each concern's
wiring in its own section. **So this file is written whole, with every call in the
order it is made.** `Program.cs` does not change when a feature is added, which
`BackendCode.md` sections 6 and 9 state.

**The migration check and the load of both Error tables run in a hosted service**,
which `Program.cs` registers. Write
`Code/<Solution>.<Name>Api/StartupHostedService.cs`:

```csharp
using <Solution>.ApiCommon.Managers.ErrorMessage;
using <Solution>.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace <Solution>.<Name>Api;

public sealed class StartupHostedService : IHostedService
{
    private readonly IServiceScopeFactory scopeFactory;

    private readonly IErrorMessageManager errorMessageManager;

    private readonly ILogger<StartupHostedService> logger;

    public StartupHostedService(
        IServiceScopeFactory scopeFactory,
        IErrorMessageManager errorMessageManager,
        ILogger<StartupHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.errorMessageManager = errorMessageManager;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<<DatabaseName>Context>();
        var pendingMigrations =
            (await database.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pendingMigrations.Count > 0)
        {
            logger.LogCritical(
                "The database is missing migrations this API holds: {PendingMigrationCount}",
                pendingMigrations.Count);

            throw new InvalidOperationException(
                $"The database is missing migrations this API holds: {pendingMigrations.Count}.");
        }

        var statuses = await database.ErrorStatuses.ToListAsync(cancellationToken);
        var messages = await database.ErrorMessages.ToListAsync(cancellationToken);

        errorMessageManager.Populate(statuses, messages);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

Replace `Code/<Solution>.<Name>Api/Program.cs` whole:

```csharp
using <Solution>.<Name>Api;
using <Solution>.ApiCommon;
using <Solution>.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Add<Solution>Infrastructure();
builder.Services.Add<Solution>ApiCommon(1);
builder.Services.AddHostedService<StartupHostedService>();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use<Solution>ApiCommon();
app.MapEndpoints();

try
{
    await app.StartAsync();
}
#pragma warning disable CA1031 // Program.cs is not an operation, so BackendCode.md section 7's rule does not reach it
catch (Exception)
#pragma warning restore CA1031
{
    return 1;
}

logger.LogInformation("{Application} has started", app.Environment.ApplicationName);

await app.WaitForShutdownAsync();

return 0;
```

**Where each call is stated:**

| Call | `BackendCode.md` sections |
|---|---|
| `Add<Solution>Infrastructure()` and `Add<Solution>ApiCommon(1)` | 9, 14, 15, 19: one call per shared project, and the route versions the API serves |
| `AddHostedService<StartupHostedService>()`: the migration check, at `Critical`, and the load of both Error tables into `Populate`, inside the host's start | 7, 13, 15 |
| `UseForwardedHeaders()` | 9 |
| `UseExceptionHandler()` and `UseStatusCodePages()` | 7 |
| `Use<Solution>ApiCommon()` | 7, 9 |
| `MapEndpoints()` | 6 |
| the `catch` around `StartAsync()`, returning 1 and logging nothing | 15 |
| the one startup line | 15 |

**One scope holds the migration check and the load of both Error tables, and
`StartupHostedService` opens it inside the host's start. Ruled by Francois on
2026-10-13**, in place of his ruling of 2026-10-11, which opened that scope in
`Program.cs` after `builder.Build()`. `BackendCode.md` section 13 has an API ask
for the migrations it holds that the database has not applied, and exit where
there is any, and `BackendCode.md` section 15 logs that at `Critical`.
`BackendCode.md` section 7 has the host read both tables and hand them to
`<Solution>.ApiCommon`, which cannot reach `<Solution>.Infrastructure`, so the
class lives in the API project and each API's run of this recipe writes its own.
**The host logs a failed start only when the failure happens inside its own
start**, and `BackendCode.md` section 15 has it do so: "A failed start. The host
logs it at `Error` and stops." So after its `Critical` line the check raises an
exception rather than returning, and the host stops.

**Measured** with five starts, each on the `https` profile. Against
an empty database, and against a database missing one migration the API holds,
the `Critical` line was written, then the host's "Hosting failed to start" at
`Error`, carrying the check's exception, and the process exited 1 without
listening. Against the empty database, EF's line for its failed read of the
`MigrationHistory` table came first, at `Error`. With the API's role refused
`SELECT` on that table, EF's line for the failing command and the host's line,
carrying "42501: permission denied for table MigrationHistory", were written at
`Error`, and the process exited 1 without listening. A connection string
`DatabaseValidateOptions` refuses stops the host in its start, before
`StartupHostedService` runs, and the host's line carries the validation message.
With every migration applied, the startup line was the only line, and the first
request answered a problem document carrying its status row's title, detail and
code.

**The check raises `InvalidOperationException`, carrying the `Critical` line's
own sentence**, so the host's `Error` line says by itself why the API stopped.
Ruled by Francois on 2026-10-13.

**The class takes `IServiceScopeFactory`, and opens its scope with
`CreateScope()` and `using`. Ruled by Francois on 2026-10-13.** The database
context is scoped and a hosted service lives as long as the host, so the context
comes from a scope the class opens, and opening one is all the scope factory
does. `using` rather than `await using`, because `IServiceScope` declares no
`DisposeAsync`, the reason the Migrator's own `Program.cs` gives.
`IErrorMessageManager` and the logger are singletons, so the constructor takes
them directly. The constructor is written out, since `BackendCode.md` section 17
gives a class no primary constructor.

**The check is not a comparison for equality**, which `BackendCode.md` section 13
states: `GetPendingMigrationsAsync` lists only what the API holds and the database
lacks, so a database ahead of the API passes.

**`StartAsync`'s queries take its `cancellationToken`**, because `BackendCode.md`
section 16 says "A token is never accepted and then dropped." `StopAsync` does
nothing and calls nothing, so its token has nowhere to go. Its signature is the
framework's, declared by `IHostedService`.

**`await app.StartAsync()` sits inside a `catch` that returns 1 and logs
nothing**, because the host has already written the failure at `Error`, and
`BackendCode.md` section 15 lists a failed start under "What is not logged by
hand". Ruled by Francois on 2026-10-13. Without the `catch`, the runtime writes its
own text for an unhandled exception after the host's line, and the process exits
134, measured. With it, there was no such text, and exit
code 1, in each failed start. **It is written `catch (Exception)`**, which says
what it catches. `BackendCode.md` section 7's rule "No operation catches
`Exception`" does not reach `Program.cs`, which is not an operation. **The `catch`
sits between `#pragma warning disable CA1031`, with its reason, and `#pragma
warning restore CA1031`**, as `BackendCode.md` section 22 states, because `CA1031`,
the rule's enforcer, reaches every file.

**`AddHostedService` comes after `Add<Solution>ApiCommon(1)`**, since the class
takes what the two calls before it register.

**The calls between the logger line and `StartAsync()` are in a fixed order, and
`Use<Solution>ApiCommon` is fourth.** Settled on 2026-09-25: `UseForwardedHeaders`, `UseExceptionHandler`,
`UseStatusCodePages`, then `MapEndpoints`. Each call sits after everything it
depends on and before everything that depends on it, so the shared call comes after
`UseForwardedHeaders`, because the rate limiter counts the real caller's address,
after the two failure calls, so a 429 or a refused origin goes out as a problem
document, and before the routes. Measured: a 429, a 413 and a
404 sent from an allowed origin were each a problem document carrying the origin
and the four security headers.

**`UseForwardedHeaders()` is configured with nothing.** `BackendCode.md` section 9
configures it with the proxies or networks the infrastructure inventory states, the
inventory states none, and `BackendCode.md` section 20 sets nothing that nothing
asked for. Measured: with nothing configured, an
`X-Forwarded-For` header was not read, so a caller cannot choose the address it is
counted under. What that means for a deployed API is in the closing report.

**The host logs one line when it has started, naming itself**, which
`BackendCode.md` section 15 states. It is written once the server listens, through
`ILogger<Program>`, since `BackendCode.md` section 15 has everything that logs take
`ILogger<T>` naming the class that writes the line, and the class top-level
statements make is `Program`. **The framework's own start lines, and EF's line for
each query `StartupHostedService` sends, are switched off by two of the three
levels step 4, "The two settings files", sets**, so the startup line is the only
line. A
command that fails still writes its line at `Error`. Checked: an API started on its `https` profile wrote the
startup line and nothing else; with the API's role refused `SELECT` on the
`MigrationHistory` table, the failing command wrote its line at `Error`; and with
both levels taken away, three lines of EF's and five of the framework's own came
before the startup line.

### 3. `Endpoints.cs`

`BackendCode.md` section 6 puts `Endpoints.cs` at the API project's root: it
creates the version group and calls one method for each resource, and it calls
`MapDocumentation` once, which `BackendCode.md` section 19 states.

Write `Code/<Solution>.<Name>Api/Endpoints.cs`:

```csharp
using <Solution>.ApiCommon.Health;
using <Solution>.ApiCommon.OpenApi;
using <Solution>.ApiCommon.Versioning;

namespace <Solution>.<Name>Api;

public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        var v1 = app.MapVersion(1);

        v1.MapHealth();

        app.MapDocumentation();
    }
}
```

**Health is one line, mapped into the version group.** `BackendCode.md` section 6
has an endpoint every version serves provided by `<Solution>.ApiCommon` as one call
into the version group, and `create-solution`'s step 10, "`<Solution>.ApiCommon`",
wrote `MapHealth`. **The API starts at `v1`**, which `BackendCode.md` section 6
states, and `Program.cs` passes the same version to `Add<Solution>ApiCommon`, which
registers one document for it. **The class is `Endpoints`, with no first part**,
which `BackendCode.md` section 18 states. Its namespace is the project's name,
which `BackendCode.md` section 17 states for a file at a project's root.

### 4. The two settings files

`BackendCode.md` section 14 puts a default that is the same in every environment in
`appsettings.json`, anything that differs per environment in an environment
variable, and local defaults that are not secret in `appsettings.Development.json`.

Replace `Code/<Solution>.<Name>Api/appsettings.json` whole. **Where a customer's
system calls the API:**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "Microsoft.Hosting.Lifetime": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "Microsoft.AspNetCore": "Warning"
      }
    }
  },
  "<Solution>": {
    "OpenApi": {
      "IsEnabled": true,
      "Title": "<Title>",
      "Description": "<Description>"
    },
    "Security": {
      "RequestSizeLimit": 1048576,
      "RateLimit": 300
    }
  }
}
```

**Where only the product's own frontends and services call it**, the same file
without the line `"IsEnabled": true,`.

Replace `Code/<Solution>.<Name>Api/appsettings.Development.json` whole. **Where a
customer's system calls the API:**

```json
{
  "<Solution>": {
    "Database": {
      "ConnectionString": "Host=localhost;Port=<Port>;Database=<DatabaseName>;Username=<Role>;SSL Mode=VerifyFull"
    },
    "Security": {
      "AllowedOrigins": []
    }
  }
}
```

**Where only the product's own frontends and services call it**, with the switch
added:

```json
{
  "<Solution>": {
    "Database": {
      "ConnectionString": "Host=localhost;Port=<Port>;Database=<DatabaseName>;Username=<Role>;SSL Mode=VerifyFull"
    },
    "OpenApi": {
      "IsEnabled": true
    },
    "Security": {
      "AllowedOrigins": []
    }
  }
}
```

**The documentation switch is written where the answer to the question puts it.**
`Security.md` section 9 has an API a customer's system calls publish its document
in every environment, and an API only the product's own frontends and services
call publish in every environment except production. It was settled on 2026-09-17 that the first is set to
`true` in `appsettings.json`, and the second in `appsettings.Development.json` and
never in `appsettings.json`, so each deployed environment sets it by environment
variable and one that sets nothing does not start, because `OpenApiOptions` makes
the switch `[Required]`. **The title and the description go in `appsettings.json`.
Ruled by Francois on 2026-10-11**, because they are the same in every environment.

**`RequestSizeLimit` is 1048576 bytes and `RateLimit` 300 requests a minute for
one network address, in `appsettings.json`. Ruled by Francois on 2026-10-11.**
`<Solution>.ApiCommon`'s `SecurityOptions` refuses either one at `0`, so neither is
left out. **`AllowedOrigins` is written only in `appsettings.Development.json`,
and it is empty.** No frontend calls the API yet; `Frontend.md` adds each
frontend's development address to it, and each deployed environment sets its own
list by environment variable, which `create-solution`'s step 10,
"`<Solution>.ApiCommon`", states.

**The connection string is the Migrator's, with this API's own role in
`Username=`.** `BackendCode.md` section 14 has every host connect as its own role,
named in `Username=`, prove itself with a client certificate, carry no password
and no certificate path, and set `SSL Mode` to `VerifyFull`. `create-solution`'s
step 12, "`<Solution>.Migrator`'s real `Program.cs`", records that `Username=` is
the only difference between two hosts' files.

**The three `Serilog` levels make the host's own startup line the only one, and
keep the framework's request lines out of the log.** `BackendCode.md` section 15
takes only the levels from configuration, from the `Serilog` section, and has the
host log one line when it has started and no other. The framework writes lines of
its own when a host starts, under `Microsoft.Hosting.Lifetime`, at `Information`,
naming the addresses it listens on, the environment and the content root, so that
category is raised to `Warning`. EF writes every command it runs under
`Microsoft.EntityFrameworkCore.Database.Command`, at `Information`, and
`StartupHostedService`, which step 2, "`Program.cs`", writes, sends its queries at
startup, so that category is raised to `Warning` too. A command that fails is
written at `Error`, so it still reaches the log. **The framework also writes its
own lines for each request under `Microsoft.AspNetCore`, at `Information`, and the
address and its query string are in them.** `BackendCode.md` section 15 leaves
that logging off, under "What is not logged by hand", and lets no value at either
protection level `Security.md` section 2 defines reach a log, so that category is
raised to `Warning` as well. The framework's lines at `Warning` and above still
reach the log. Measured:
requests to `/v1/health?email=jane@example.test`, to a route that does not exist,
to a route answering with a code that has no message row, and to a route that
raises an exception wrote no framework line, the address never reached the log,
and the startup line and `ProblemDetailsCustomizer`'s `Error` line were still
written. **No level other than these three is set**,
because nothing has asked for another, which `BackendCode.md` section 20 states.

**The template's `Logging` and `AllowedHosts` members are not kept.** `Serilog`
replaces the framework's logging, and nothing has asked for a list of allowed host
names.

### 5. The solution file, and the restore

`Code/<Solution>.slnx` lists every project in the solution, and `Frontend.md`
checks a frontend's APIs against it.

**In `Code/<Solution>.slnx`, before the line `</Solution>`, add:**

```xml
  <Project Path="<Solution>.<Name>Api/<Solution>.<Name>Api.csproj" />
```

**The file is not rewritten, and no line in it changes.** `create-solution`'s
step 1, "The solution file", records that the opening and closing `<Solution>`
tags are the format's own root element and are not substituted.

**Restore the solution**, from `Code/`:

```bash
dotnet restore
```

Expect exit 0. It writes `Code/<Solution>.<Name>Api/packages.lock.json`, and step
6, "The grant migration", needs it, because `dotnet ef` fails on a project nothing
has restored, which `create-solution`'s step 11,
"`<Solution>.Infrastructure/Persistence/`", records.

**Create the API's role and its certificate**, from the repository root:

```bash
docker compose run --rm roles
```

**Expect exit 0, and the line `Created the role <Role>`.** `compose.yml` derives
the solution's roles from `Code/<Solution>.slnx`, so the line added above is what
gives the API its role. `docker compose run` first runs the `certificates`
service, which issues `Code/Certificates/<Role>.crt` and
`Code/Certificates/<Role>.key`, and then `roles`, which creates `<Role>` on the
server, `LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE` and granted nothing. **The
role exists before step 6**, because the grant migration names it and fails on a
role the server does not hold. Measured: after an API's line was added to the
solution file, this command issued its pair and created its role, and the pair
then signed in.

**`certificates` also writes `Issued the server certificate for <Name>Api` to
its log, and this command does not print it.** `create-solution`'s step 12 has
it issue each API's server pair, into the API's role's folder in the
`certificates` volume, from the same line in the solution file, and step 16,
"The API in its container", gives that pair to the API. Measured on 2026-10-21
under this command: it printed `Created the role <Role>` alone and exited 0,
and `docker compose logs certificates` then showed `Issued the certificate for
<Role>` and `Issued the server certificate for <Name>Api`, both from this run.

**Prove the sign-in**, from the repository root:

```bash
docker compose run --rm --no-deps --entrypoint sh certificates -c "cp /output/<Role>.key /tmp/role.key && chmod 600 /tmp/role.key && psql 'host=database dbname=<DatabaseName> user=<Role> sslmode=verify-full sslrootcert=/output/ca.crt sslcert=/output/<Role>.crt sslkey=/tmp/role.key' -Atc 'SELECT current_user'"
```

**Expect exit 0 and `<Role>` on standard output.** It runs a one-off container
of the `certificates` service, which already mounts `Code/Certificates/` at
`/output`, so it needs no `psql` on the machine. `--no-deps` starts nothing else
and `--rm` removes the container afterwards. Docker Compose writes its own
progress lines to standard error, so read standard output only. **The key is
copied first because `psql` refuses a key that the user running it does not
own**, and inside the container that user is `root`, while the file belongs to the
engineer. **`host=database` is the server's name inside Docker's network**, and
the server's certificate carries it, so `verify-full` checks the certificate
exactly as it does for `localhost`. Measured: this printed the role's name, with
`--rm` leaving no container behind. **Anything else is a real failure**: stop,
report what it said, and show what each service did with
`docker compose logs certificates roles database`.

### 6. The grant migration

`create-solution`'s step 11, "`<Solution>.Infrastructure/Persistence/`", grants
nothing to any API role, and records that each API's role gets a grant-only
migration of its own. `BackendCode.md` section 13 permits a migration that grants
and creates nothing, where the tables it grants on existed before the role that
needs them, and has the tool generate it.

**Generate it**, per `BackendCode.md` section 13's own command shape, from `Code/`:

```bash
dotnet ef migrations add Grant<Name>ApiRole \
    --project <Solution>.Infrastructure \
    --startup-project <Solution>.Migrator \
    --output-dir Persistence/Migrations
```

Expect exit 0, and a new migration under
`Code/<Solution>.Infrastructure/Persistence/Migrations/`, whose file name is a
timestamp and `_Grant<Name>ApiRole.cs`, with an empty `Up` and `Down`. **The model
snapshot does not change**, because the model has not.

**`Grant<Name>ApiRole` is the migration's name, and it is derived.**
`BackendCode.md` section 13 names a migration as a verb and a noun: it grants, and
what it grants to is the API's role.

**Replace that file whole**, keeping its name:

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace <Solution>.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Grant<Name>ApiRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("GRANT USAGE ON SCHEMA \"Error\" TO \"<Role>\";");
            migrationBuilder.Sql("GRANT USAGE ON SCHEMA \"Migration\" TO \"<Role>\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Error\".\"ErrorStatus\" TO \"<Role>\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Error\".\"ErrorMessage\" TO \"<Role>\";");
            migrationBuilder.Sql("GRANT SELECT ON \"Migration\".\"MigrationHistory\" TO \"<Role>\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("REVOKE SELECT ON \"Migration\".\"MigrationHistory\" FROM \"<Role>\";");
            migrationBuilder.Sql("REVOKE SELECT ON \"Error\".\"ErrorMessage\" FROM \"<Role>\";");
            migrationBuilder.Sql("REVOKE SELECT ON \"Error\".\"ErrorStatus\" FROM \"<Role>\";");
            migrationBuilder.Sql("REVOKE USAGE ON SCHEMA \"Migration\" FROM \"<Role>\";");
            migrationBuilder.Sql("REVOKE USAGE ON SCHEMA \"Error\" FROM \"<Role>\";");
        }
    }
}
```

**It is EF's file with the two bodies filled, and every other line as EF wrote
it.** Measured: the file
`migrations add` wrote for a grant migration held these lines with both bodies
empty, each line ending in a line feed alone and the file ending with a final
line break. **No exact edit can fill the bodies**, because each body's opening
brace and closing brace occur twice in that file, once for `Up` and once for
`Down`. **EF's file begins with a UTF-8 byte order mark, and this file is written
without one. Ruled by Francois on 2026-10-14.**

**The five grants are the ones `create-solution`'s step 11,
"`<Solution>.Infrastructure/Persistence/`", derives, and nothing else**: `USAGE`
on the two schemas, and `SELECT` on the two Error tables, which `BackendCode.md`
section 7 has the API read at startup, and on the history table, which
`BackendCode.md` section 13 has it read to decide whether it is behind. No role is
ever granted the right to change the schema, which `BackendCode.md` section 13
states. **The grants are written with `migrationBuilder.Sql`**, which
`BackendCode.md` section 13 states, every name in double quotes, because a name
PostgreSQL does not quote is folded to lowercase. **`Down` revokes each grant, in
the reverse order**, because `BackendCode.md` section 13 has every migration carry
a maintained `Down`, and the migration test runs it.

**Read the migration before it is applied**, as `BackendCode.md` section 13
requires of every generated migration: no `RenameColumn`, and no new column on a
table that holds rows. Neither can arise here, since the model did not change.

**Run the Migrator**, from `Code/`, so that it builds on the SDK `Code/global.json`
fixes:

```bash
dotnet run --project <Solution>.Migrator
```

**Expect exit 0.** It applies `Grant<Name>ApiRole` to the database, which is what
lets this API start against it: until it runs, the API's role can read none of the
three tables. `BackendCode.md` section 13 makes the Migrator the one owner of
migrations, and `create-solution`'s step 12, "`<Solution>.Migrator`'s real
`Program.cs`", records that a second run applies only what is missing. **Anything
other than exit 0 is a real failure**: stop and report what it said.

### 7. Health's frozen fragment

`Testing.md` section 4 freezes one method on one route as an OpenAPI fragment at
`Docs/Contracts/<Name>Api/<version>/<route>/<method>.openapi.json`, the path
derived from the route. `BackendCode.md` section 6 freezes health's response
shape. **Health is `GET /v1/health`**, so its fragment is at
`Docs/Contracts/<Name>Api/v1/health/get.openapi.json`.

Write `Docs/Contracts/<Name>Api/v1/health/get.openapi.json`:

```json
{
  "openapi": "3.1.1",
  "info": {
    "title": "<Title>",
    "version": "v1"
  },
  "paths": {
    "/v1/health": {
      "get": {
        "tags": [
          "health"
        ],
        "summary": "Reports that the API is running.",
        "responses": {
          "200": {
            "description": "OK"
          }
        },
        "security": [ ]
      }
    }
  }
}
```

**The operation is exactly what the API generates for health**, which step 13,
"The contract test class", compares in both directions. Its tag is the route
segment, its summary is the sentence `create-solution`'s step 10,
"`<Solution>.ApiCommon`", wrote, which `BackendCode.md` section 19 makes a field of
the frozen contract, its one response is 200 with no body, which was settled earlier, and **`"security": []` declares the operation public**,
which `Testing.md` section 8 requires of every fragment and `Security.md` requires
of health. **It names no schema and
no security scheme**, so it carries no `components`.

**`openapi` and `info` make the fragment a valid OpenAPI document**, which
`Testing.md` section 4 has the contract test prove. `openapi` is the version the API
generates, and `info` carries the two members the specification requires, the API's title and its
document's version. Nothing compares `info`, and nothing else from the document is
kept.

### 8. Health's contract document

`Testing.md` section 4 describes each fragment with `<method>.md` beside it, cut
from `Claude/Templates/Contract.Template.md`.

Get the date:

```bash
date +%F
```

It prints `<Date>`.

Write `Docs/Contracts/<Name>Api/v1/health/get.md`:

```markdown
- **Fragment:** `get.openapi.json`, beside this file. **The fragment is the
  truth**; this document explains it.
- **Status:** Frozen <Date>
- **Governed by:** No decision
- **Produced by:** `<Solution>.<Name>Api`
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

Health reports that `<Solution>.<Name>Api` is running and serving requests. A probe or an orchestrator in each deployed environment calls it to decide whether the host is up. It reaches nothing the API depends on, so it answers for the host alone.

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
```

**The four fields above the template's first quote are Francois's rulings of
2026-10-11**, the status is frozen on the day the contract is
written, no decision governs it, the API alone produces it, and a probe or an
orchestrator consumes it. **The template's two quotes stay, and its authoring
quote and every placeholder go**, which the template states. **Purpose,
Definition, Fields and the two rules come from what is already settled**: the 200
with no body, and `"security": []`.

### 9. No contract type generator

**Zapper is API only, so this step writes nothing.** Ruled by Francois on
2026-10-22. It added the API to the configuration of the contract type
generator, which gives a frontend the types of an API's contracts, and
`create-solution` no longer wires that tool in. **The step keeps its number** so
that every reference to the steps after it stays true.

### 10. The test project's reference, and its package

`Testing.md` section 5 makes `WebApplicationFactory<Program>`, from
`Microsoft.AspNetCore.Mvc.Testing`, the in-memory test server, and
`<Solution>.IntegrationTests` holds this API's tests, which `Testing.md` section 1
states.

**In `Code/<Solution>.IntegrationTests/<Solution>.IntegrationTests.csproj`, after
the line
`    <ProjectReference Include="../<Solution>.ApiCommon/<Solution>.ApiCommon.csproj" />`,
add:**

```xml
    <ProjectReference Include="../<Solution>.<Name>Api/<Solution>.<Name>Api.csproj" Aliases="<Name>Api" />
```

**So the newest API's reference comes first among the APIs'. Ruled by Francois
on 2026-10-14.** The line that closes an `ItemGroup` occurs twice in the file, so
the anchor is the last line `create-solution`'s step 14,
"`<Solution>.IntegrationTests`", wrote in this `ItemGroup`.

**Every API's reference carries an alias named for the API, from the first API
on. Ruled by Francois on 2026-10-11.** Every API declares `Program` in the global
namespace, so with two APIs referenced `WebApplicationFactory<Program>` is
ambiguous and fails `CS0433`, measured, and with the alias each file names one API's `Program` as
`<Name>Api::Program`. Measured: two aliased references built
with no warning, and each API's factory served its own API.

**Then check whether the package is already there:**

```bash
grep -c 'Include="Microsoft.AspNetCore.Mvc.Testing"' Code/Directory.Packages.props
```

**`1` means an API before this one added it**, with its reference: add nothing.
**`0` means it is missing**: in `Code/Directory.Packages.props`, before the line
`  </ItemGroup>`, add:

```xml
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="<Versions.md: Microsoft.AspNetCore.Mvc.Testing>" />
```

and in the integration test project file, after the line
`    <PackageReference Include="Testcontainers.PostgreSql" />`, add:

```xml
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
```

`BackendCode.md` section 2 puts every version in `Code/Directory.Packages.props`
and none in a project file. **Neither file is rewritten, and no existing entry is
touched.** 10.0.12 was the current stable release on 2026-10-11, the version of
every other ASP.NET Core package in the solution, and the one it was measured with.

### 11. The role in the template database

`create-solution`'s step 14, "`<Solution>.IntegrationTests`", records what the
template does not hold: the grant migration names this API's role, the test
container holds no role, and measured, the template then fails to build with `42704: role "<role>" does not
exist`. **Creating the role in the container before the migrations run fixes it,
and every clone carries the grant.**

In `Code/<Solution>.IntegrationTests/TemplateDatabaseFixture.cs`, inside
`InitializeAsync`, **add this line directly before the line
`        var connectionString = GetConnectionString(TemplateDatabaseName);`**:

```csharp
        await ExecuteAsync("CREATE ROLE \"<Role>\"");
```

**Each API adds its own line in the same place**, so the lines stand after
`container.StartAsync` in the order the APIs were created. A role is shared by
every database in the container, so it exists before `MigrateAsync` builds the
template, which is where it was settled to go.
**The role needs no privilege of its own and no sign-in**: the test container is
reached as its own administrator, and the role is there only so the grants can
name it.

### 12. The API fixture

`Testing.md` section 5 gives each test class of an API the in-memory test server
and the context for the class's cloned database, and `create-solution`'s step
14, "`<Solution>.IntegrationTests`", leaves each API's own fixture to this recipe.

Write `Code/<Solution>.IntegrationTests/<Name>ApiFixture.cs`:

```csharp
extern alias <Name>Api;

using <Solution>.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace <Solution>.IntegrationTests;

public sealed class <Name>ApiFixture : WebApplicationFactory<<Name>Api::Program>, IAsyncLifetime
{
    private readonly ClonedDatabaseFixture clonedDatabase;

    private readonly string connectionString;

    public <Name>ApiFixture(TemplateDatabaseFixture templateDatabase)
    {
        clonedDatabase = new ClonedDatabaseFixture(templateDatabase);

        // the clone's context was given its connection string when it was made, so it holds one
        connectionString = clonedDatabase.Database.Database.GetConnectionString()!;
    }

    public async ValueTask InitializeAsync() => await clonedDatabase.InitializeAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await clonedDatabase.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("<Solution>:Database:ConnectionString", connectionString);
        builder.ConfigureTestServices(ReplaceDatabase);
    }

    private void ReplaceDatabase(IServiceCollection services)
    {
        services.Remove(services.Single(service => service.ImplementationType == typeof(DatabaseValidateOptions)));
        services.RemoveAll<<DatabaseName>Context>();
        services.AddScoped(GetContext);
    }

    private <DatabaseName>Context GetContext(IServiceProvider provider)
    {
        var database = new <DatabaseName>ContextDesignTimeFactory().CreateDbContext([]);

        database.Database.SetConnectionString(connectionString);
        database.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        return database;
    }
}
```

**A test class takes it through `IClassFixture<<Name>ApiFixture>`**, and holds
what it uses in a field, which `Testing.md` section 5 states. **It is built on
`ClonedDatabaseFixture`**: it makes one for its class, so each class has its own
clone of the template, and it starts the API against that clone. xUnit gives it
the assembly's `TemplateDatabaseFixture` through its constructor, which was measured. **It sits at the root of `<Solution>.IntegrationTests`**,
beside the two database fixtures, for the reason `create-solution`'s step 14,
"`<Solution>.IntegrationTests`", gives for them, and its name is `Testing.md`'s word
for the thing, a fixture, after what it holds, which `BackendCode.md` section 18
states.

**The API starts against the clone in the test host only, with two registrations
replaced. Ruled by Francois on 2026-10-11.** The container is reached with a
password and with no TLS, which `BackendCode.md` section 14's rules do not reach,
by Francois's ruling of 2026-10-07 recorded in `create-solution`. So the fixture
sets the clone's connection string with `UseSetting`, removes the
`DatabaseValidateOptions` registration, which would refuse it, and replaces the
`<DatabaseName>Context` registration with one that makes each context the way
`TemplateDatabaseFixture` does: through `<DatabaseName>ContextDesignTimeFactory`,
given the connection string, and set to `NoTracking`. That also leaves the
certificate walk out, since only the replaced registration called it. Measured: the host started against its clone, which proves its migration
check passed, and a request to an unknown route answered 404 with the status row's
code and detail, which proves both tables were loaded. With `UseSetting` alone, the
host refused the connection string as the client was made. **Nothing in
`<Solution>.Infrastructure` changes.**

**`DatabaseValidateOptions` is removed by its class**, because
`ValidateDataAnnotations` registers a validator of the same service type for the
same options. **The connection string is read when the fixture is made**, from the
clone's context, because `ClonedDatabaseFixture` exposes no connection string of
its own and nothing has opened the context yet.

### 13. The contract test class

`Testing.md` section 4 states what a contract test proves, for each frozen
fragment, and names the class: one per API version, `<Name>ApiV<n>ContractTests`,
in that API's folder of `<Solution>.IntegrationTests`, which `Testing.md` section
1 places.

Write `Code/<Solution>.IntegrationTests/<Name>Api/<Name>ApiV1ContractTests.cs`:

```csharp
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Shouldly;
using Xunit;

namespace <Solution>.IntegrationTests.<Name>Api;

public sealed class <Name>ApiV1ContractTests : IClassFixture<<Name>ApiFixture>
{
    private const string ComponentSchemaPrefix = "#/components/schemas/";

    private readonly IServiceProvider provider;

    public <Name>ApiV1ContractTests(<Name>ApiFixture api) => provider = api.Services;

    public static TheoryData<string, string> FrozenFragments
    {
        get
        {
            var contracts = GetContractsFolder();
            var cases = new TheoryData<string, string>();

            foreach (var path in Directory.EnumerateFiles(Path.Combine(contracts, "v1"), "*.openapi.json", SearchOption.AllDirectories))
            {
                var route = Path.GetRelativePath(contracts, Path.GetDirectoryName(path) ?? contracts).Replace('\\', '/');

                cases.Add($"/{route}", Path.GetFileName(path).Replace(".openapi.json", string.Empty, StringComparison.Ordinal));
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(FrozenFragments))]
    public async Task GeneratedDocument_FrozenFragment_MatchesExactly(string route, string method)
    {
        var documentProvider = provider.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var fragmentText = await File.ReadAllTextAsync(
            Path.Combine(GetContractsFolder(), route.TrimStart('/'), $"{method}.openapi.json"),
            TestContext.Current.CancellationToken);
        var fragment = JsonNode.Parse(fragmentText);
        var fragmentOperation = fragment?["paths"]?[route]?[method];
        var fragmentComponents = GetReferencedComponents(fragment, fragmentOperation);

        var document = await documentProvider.GetOpenApiDocumentAsync(TestContext.Current.CancellationToken);
        var generated = JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1, TestContext.Current.CancellationToken));
        var generatedOperation = generated?["paths"]?[route]?[method];
        var generatedComponents = GetReferencedComponents(generated, generatedOperation);

        generatedOperation.ShouldNotBeNull($"The generated document holds no {method} on {route}.");
        JsonNode.DeepEquals(generatedOperation, fragmentOperation)
            .ShouldBeTrue($"The operation differs.\nGenerated: {generatedOperation}\nFrozen: {fragmentOperation}");
        JsonNode.DeepEquals(generatedComponents, fragmentComponents)
            .ShouldBeTrue($"A schema or a security scheme differs.\nGenerated: {generatedComponents}\nFrozen: {fragmentComponents}");
        OpenApiDocument.Parse(fragmentText, "json").Diagnostic.ShouldNotBeNull().Errors.ShouldBeEmpty();
    }

    private static JsonObject GetReferencedComponents(JsonNode? document, JsonNode? operation)
    {
        var schemas = new JsonObject();
        var pending = new Queue<JsonNode?>([operation]);

        while (pending.TryDequeue(out var node))
        {
            foreach (var name in GetSchemaReferences(node).Where(name => !schemas.ContainsKey(name)))
            {
                var schema = document?["components"]?["schemas"]?[name]?.DeepClone();

                schemas[name] = schema;
                pending.Enqueue(schema);
            }
        }

        var securitySchemes = new JsonObject();

        foreach (var name in operation?["security"]?.AsArray().SelectMany(GetSchemeNames) ?? [])
        {
            securitySchemes[name] = document?["components"]?["securitySchemes"]?[name]?.DeepClone();
        }

        return new JsonObject { ["schemas"] = schemas, ["securitySchemes"] = securitySchemes };
    }

    private static IEnumerable<string> GetSchemaReferences(JsonNode? node) => node switch
    {
        JsonObject members => members.SelectMany(member =>
            member is { Key: "$ref", Value: JsonValue reference }
            && reference.GetValue<string>() is { } target
            && target.StartsWith(ComponentSchemaPrefix, StringComparison.Ordinal)
                ? [target[ComponentSchemaPrefix.Length..]]
                : GetSchemaReferences(member.Value)),
        JsonArray items => items.SelectMany(GetSchemaReferences),
        _ => [],
    };

    private static IEnumerable<string> GetSchemeNames(JsonNode? requirement) =>
        requirement is JsonObject schemes ? schemes.Select(scheme => scheme.Key) : [];

    private static string GetContractsFolder()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);

        while (folder is not null && !folder.EnumerateFiles("*.slnx").Any())
        {
            folder = folder.Parent;
        }

        // the test assembly is built inside Code/, which holds the solution file
        return Path.Combine(folder!.Parent!.FullName, "Docs", "Contracts", "<Name>Api");
    }
}
```

**The test is one `[Theory]` whose cases are the route and method of every fragment
on disk for `v1`**, which `Testing.md` section 4 states, so a fragment added by a
later feature is tested with no change here. On day one it has one case, health.
**Its name is `Testing.md` section 3's three segments**: the subject is what the
test exercises, the generated document; the scenario is a frozen fragment; and the
outcome is `Testing.md` section 4's own words, exactly equal. **`FrozenFragments`
is named for what it supplies**, in the same section's words.

**What it proves, in the order `Testing.md` section 4 lists it:** the generated
document holds an operation for the fragment's route and method; that operation is
exactly equal to the fragment's, compared as JSON so the order of members does not
matter and any difference fails it in either direction; every schema the operation
references, followed through every schema those reference, and every security
scheme its `security` field names, is exactly equal by name; and the fragment
itself reads as OpenAPI with no error.

**The document comes from the host's container**, resolved by its key, `v1`, and
never by a request to the document route, which `Testing.md` section 4 states.
`<Name>ApiFixture.Services` starts the host. **It never switches documentation
on**: whether the API publishes decides only whether the route is mapped, and the
document is registered either way.

**The fragments are found from the solution file**, as `create-solution`'s step
11, "`<Solution>.Infrastructure/Persistence/`", finds `Code/Certificates/`:
`AppContext.BaseDirectory` is the one anchor that is the same however the tests
are run, which was measured.

### 14. The acceptance fixture, and the shared acceptance base class

`Testing.md` section 7 runs every acceptance test against one database for the
whole run, with each API reached by `client` on Kestrel at a real port, and
gives every acceptance test class a shared base class that supplies the
discriminator. **Ruled by Francois on 2026-10-11**: `AcceptanceFixture`, at the
root of `<Solution>.IntegrationTests`, is a collection fixture for one
`Acceptance` collection, and each API's recipe adds its API's line to it. **Ruled
by Francois on 2026-10-22: no frontend is built or served**, because Zapper is
API only, so the fixture starts the APIs and nothing else, and the base class is
written here rather than by `Frontend.md`.

From the repository root:

```bash
test -f Code/<Solution>.IntegrationTests/AcceptanceFixture.cs
```

**Exit 0 means an API before this one wrote it**, with its three companion files:
write nothing new, and add only this API's two lines, below. **Exit 1 means it is
missing**: write the four files below, with this API's two lines already in
`AcceptanceFixture.cs`.

Write `Code/<Solution>.IntegrationTests/AcceptanceCollection.cs`:

```csharp
using Xunit;

namespace <Solution>.IntegrationTests;

[CollectionDefinition("Acceptance")]
public sealed class AcceptanceCollection : ICollectionFixture<AcceptanceFixture>;
```

Write `Code/<Solution>.IntegrationTests/AcceptanceApiFactory.cs`:

```csharp
using <Solution>.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace <Solution>.IntegrationTests;

public sealed class AcceptanceApiFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly string connectionString;

    public AcceptanceApiFactory(string connectionString)
    {
        this.connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("<Solution>:Database:ConnectionString", connectionString);
        builder.ConfigureTestServices(ReplaceDatabase);
    }

    private void ReplaceDatabase(IServiceCollection services)
    {
        services.Remove(services.Single(service => service.ImplementationType == typeof(DatabaseValidateOptions)));
        services.RemoveAll<<DatabaseName>Context>();
        services.AddScoped(GetContext);
    }

    private <DatabaseName>Context GetContext(IServiceProvider provider)
    {
        var database = new <DatabaseName>ContextDesignTimeFactory().CreateDbContext([]);

        database.Database.SetConnectionString(connectionString);
        database.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        return database;
    }
}
```

Write `Code/<Solution>.IntegrationTests/AcceptanceFixture.cs`:

```csharp
extern alias <Name>Api;
using <Solution>.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace <Solution>.IntegrationTests;

public sealed class AcceptanceFixture : IAsyncLifetime
{
    private readonly ClonedDatabaseFixture clonedDatabase;

    private readonly string connectionString;

    private readonly List<IAsyncDisposable> hosts = [];

    private readonly Dictionary<string, HttpClient> clients = [];

    public AcceptanceFixture(TemplateDatabaseFixture templateDatabase)
    {
        clonedDatabase = new ClonedDatabaseFixture(templateDatabase);

        // the clone's context was given its connection string when it was made, so it holds one
        connectionString = clonedDatabase.Database.Database.GetConnectionString()!;
    }

    public <DatabaseName>Context Database => clonedDatabase.Database;

    public async ValueTask InitializeAsync()
    {
        await clonedDatabase.InitializeAsync();

        StartApi<<Name>Api::Program>("<Solution>.<Name>Api");
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var host in hosts)
        {
            await host.DisposeAsync();
        }

        await clonedDatabase.DisposeAsync();
    }

    public HttpClient GetClient(string api) => clients[api];

    private void StartApi<TProgram>(string api)
        where TProgram : class
    {
        var factory = new AcceptanceApiFactory<TProgram>(connectionString);
        factory.UseKestrel(0);
        factory.StartServer();

        hosts.Add(factory);
        clients[api] = factory.CreateClient();
    }
}
```

Write `Code/<Solution>.IntegrationTests/AcceptanceTestBase.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace <Solution>.IntegrationTests;

public abstract class AcceptanceTestBase
{
    protected static string Discriminator
    {
        get
        {
            // xUnit sets the test method for as long as a test runs, and only a running test reads this
            var testMethod = TestContext.Current.TestMethod!;
            var identity = $"{testMethod.TestClass.TestClassName}.{testMethod.MethodName}";

            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..12];
        }
    }
}
```

**The alias is on the file's first line, above its `using` lines, and the start of
this API is at the end of `InitializeAsync`.**

**Where an API before this one wrote the file, add this API's two lines to it.**
In `Code/<Solution>.IntegrationTests/AcceptanceFixture.cs`, before the line
`using <Solution>.Infrastructure.Persistence;`, add:

```csharp
extern alias <Name>Api;
```

so the alias comes after the alias of each API before this one. Then, after the
line `        StartApi<<PreviousName>Api::Program>("<Solution>.<PreviousName>Api");`,
where `<PreviousName>` is the name in the last line of the file that begins
`        StartApi<`, add:

```csharp
        StartApi<<Name>Api::Program>("<Solution>.<Name>Api");
```

**Each API's start line names its API, so the line the start is added after
occurs once**, and the APIs start in the order they were created. **The first
API's two lines are in the file's whole text above**, as ruled by Francois on
2026-10-14.

**It is a collection fixture for one collection, `Acceptance`**, so xUnit makes it
only when a test in that collection runs, and once for all of them. **A test class
joins it with `[Collection("Acceptance")]`**, derives from `AcceptanceTestBase`,
takes the fixture through its constructor, and holds `GetClient` for its API and
`Database` in fields, which `Testing.md` section 7 states. **A test class names an
API by its project, `<Solution>.<Name>Api`.** `AcceptanceCollection` holds the
collection's definition, since xUnit reads it from a class of its own.

**When it starts, it clones the run's one database**, which `Testing.md` section 7
states, through a `ClonedDatabaseFixture` it keeps until the run ends. **It then
starts every API on Kestrel**, with `UseKestrel(0)` and `StartServer()`, with the
run's database. Kestrel at a real port is kept, though no browser calls it,
because `Testing.md` section 12's load tests host the API the way section 7 does.

**Each API starts from `AcceptanceApiFactory`, a factory class of its own, and never
from `WithWebHostBuilder`.** Measured: a factory from
`WithWebHostBuilder` did not serve on Kestrel, binding the framework's default
address, and its client could not be made. **It replaces the same two
registrations `<Name>ApiFixture` replaces**, for the same reason. **It sets no
allowed origin**, since no frontend calls the API.

**`AcceptanceTestBase` is the shared acceptance base class** `Testing.md` section 7
names, and it supplies the discriminator and nothing else. **The discriminator** is
the first 12 characters, in lowercase, of the hexadecimal SHA-256 hash of the test
class's full name, a dot and the test method's name, so it is the same on every run
of the same test and never a `Guid`, which `Testing.md` section 7 states. **It is
`static`**, because it reads xUnit's `TestContext.Current` and nothing of the
instance. **It is abstract**, because it is designed for inheritance and nothing
runs it alone.

**Measured on 2026-10-22**, in a copy of this repository's `Code/` with
`Zubber` as `<DatabaseName>` and `PaymentsApi` as the API: the four files built
with 0 warnings and 0 errors, and an acceptance test deriving from
`AcceptanceTestBase` called `GET /v1/health` through `client` on Kestrel and got
200, with its display name and its `Rule` trait listed as written. Every test in
the solution passed.

### 15. The infrastructure inventory

`Security.md` section 11 keeps every part of the product and every connection
between parts in `Docs/InfrastructureInventory.md`. **This step adds the API and
the connection it wires, and fills in nothing it does not know.** Which
environments hold the API, the protection level of the data it holds or carries,
whether the internet can reach it, and the database's port in each environment all
depend on features and a deployment not yet planned, so each of those cells stays
empty.

**Each row goes directly after its table's separator line, so the newest row comes
first. Ruled by Francois on 2026-10-14.** The table under `## Parts` ends in an
empty line, which occurs more than once in the file. **The separator under
`## Parts` is also the start and the end of the separator under
`## Connections`**, so an Edit
whose text is `|---|---|---|---|---|` alone matches twice and is refused. The
edit's text is that line with the line break before it and the line break after
it, which occurs once.

In `Docs/InfrastructureInventory.md`, after the line `|---|---|---|---|---|`, add
one row. **Where a customer's system calls the API:**

```markdown
| `<Solution>.<Name>Api` | An API, which a customer's system calls | | | |
```

**Where only the product's own frontends and services call it:**

```markdown
| `<Solution>.<Name>Api` | An API, which only the product's own frontends and services call | | | |
```

Then, after the line `|---|---|---|---|---|---|---|`, add:

```markdown
| `<Solution>.<Name>Api` | The database, `<DatabaseName>` | | PostgreSQL over TLS | | A client certificate for the role `<Role>` | |
```

**The protocol and the proof are known**: `BackendCode.md` section 14 has the
connection require TLS and check the server's certificate, and has every host
connect as its own role with a client certificate, the same in every environment.
**The last column stays empty**, because the address comes from the API's own
settings and not from data received from outside.

**The API's container on this machine gets no row and changes neither row.
Ruled by Francois on 2026-10-21**: `create-solution`'s step 15 writes no row for
the Migrator, because no deployment yet decides which environments hold a part,
and the API's rows follow the same rule.

### 16. The API in its container

**Ruled by Francois on 2026-10-21: every host runs in a container**, a one-off
`migrator` service and one long-running service per API, so that a person who
clones the repository runs `docker compose up -d` and has the whole product
running. `create-solution`'s step 12, "`<Solution>.Migrator`'s real
`Program.cs`", wrote `compose.yml`, `Code/.dockerignore` and the Migrator's
Dockerfile, and records why each line is there. This step writes the API's
Dockerfile, adds the API's service to `compose.yml`, and starts it. **It comes
after the grant migration**, because the API does not start until its role can
read the three tables. Ruled by Francois on 2026-10-21: it is this recipe's last
step before Verify.

Write `Code/<Solution>.<Name>Api/Dockerfile`:

```dockerfile
FROM <Versions.md: .NET SDK, the build image> AS build
WORKDIR /source/Code
COPY --from=docs AcceptedVulnerabilities.md /source/Docs/
COPY . .
RUN dotnet restore <Solution>.<Name>Api/<Solution>.<Name>Api.csproj --locked-mode
RUN dotnet publish <Solution>.<Name>Api/<Solution>.<Name>Api.csproj --no-restore --configuration Release --output /app

FROM <Versions.md: ASP.NET Core runtime, an API's image>
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "<Solution>.<Name>Api.dll"]
```

**It is the Migrator's Dockerfile with the API's project, and the ASP.NET Core
runtime image in place of the .NET runtime image.** `create-solution`'s step 12
records each line: the two stages, Microsoft's chiseled image, ruled by Francois
on 2026-10-21, which runs as user `1654` and not as an administrator of its
container, as `Security.md` section 11 requires of every part; the restore in
locked mode, which `BackendCode.md` section 2 has every restore run and which
fails on a lock file that does not match; `Docs/AcceptedVulnerabilities.md`,
copied from the second build context named `docs`, because step 3 of
`create-solution` has every restore read it; and `COPY . .`, which runs the
restore again after any change under `Code/`. **The lock file exists**: step 5,
"The solution file, and the restore", wrote it.

**The image runs the published output**, which holds no launch profile and no
`appsettings.Development.json`, because step 1, "The project", removes that file
outside Debug. So the API in its container comes up in `Production` and reads
only `appsettings.json`. Measured on 2026-10-21 with an API built from this
recipe's steps 1 to 6: the image held no `appsettings.Development.json`, no
`.slnx`, no key and no `Certificates/` folder, and was 234 MB.

**Find `<HostPort>`**, from the repository root:

```bash
port=8443; while grep -qE "^ *- 127\.0\.0\.1:${port}:" compose.yml || nc -z 127.0.0.1 "$port" 2>/dev/null; do port=$((port + 1)); done; echo "$port"
```

It prints `<HostPort>`: the first port from 8443 upward that no line of
`compose.yml` publishes on `127.0.0.1`, the database's and every earlier API's
included, and that nothing on this machine holds. `nc -z` is the port check
`create-solution` uses, and exits 0 where something holds the port. **Ruled by
Francois on 2026-10-21**, with `nc` replacing a Node command on 2026-10-22. `docker
compose up -d` starts every API, and a developer who then runs one with `dotnet
run` uses the port in its launch profile, so the container never takes that
port. Measured on 2026-10-21, on a `compose.yml` publishing 5434 and 8443 with
8444 held by another process: it printed 8445. Measured again with `nc` on
2026-10-22, on the same `compose.yml`: 8444 when it was free, and 8445 once a
local server held 8444.

**Derive `<Image>`:**

```bash
printf '%s-%s\n' '<Solution>' '<Name>Api' | tr 'A-Z' 'a-z'
```

It prints `<Image>`. Measured: `Zapper` and `OrdersApi` gave `zapper-ordersapi`.

**In `compose.yml`, before the line `configs:`, add the API's service and an
empty line after it.** Where a customer's system calls the API:

```yaml
  <Name>Api:
    image: <Image>
    pull_policy: never
    build:
      context: ./Code
      dockerfile: <Solution>.<Name>Api/Dockerfile
      additional_contexts:
        docs: ./Docs
    restart: unless-stopped
    init: true
    depends_on:
      migrator:
        condition: service_completed_successfully
    environment:
      <Solution>__Database__ConnectionString: Host=database;Port=5432;Database=<DatabaseName>;Username=<Role>;SSL Mode=VerifyFull
      ASPNETCORE_HTTP_PORTS: ""
      ASPNETCORE_HTTPS_PORTS: "8443"
      ASPNETCORE_Kestrel__Certificates__Default__Path: /certificates/server.crt
      ASPNETCORE_Kestrel__Certificates__Default__KeyPath: /certificates/server.key
    ports:
      - 127.0.0.1:<HostPort>:8443
    volumes:
      - type: volume
        source: certificates
        target: /certificates
        read_only: true
        volume:
          subpath: <Role>

```

**Where only the product's own frontends and services call it**, the same
service with this line directly after the connection string's line:

```yaml
      <Solution>__OpenApi__IsEnabled: "true"
```

**`configs:` is the anchor, and the Edit's text is that line with the line break
before it.** `configs:` begins at the start of its line once in the file, and
`    configs:` under `certificates` and under `roles` ends with the same text, so
`configs:` alone matches three times and is refused. So each API's service comes
after the one before it, in the order the APIs were created. **No script in
`compose.yml` changes**, and no other service: the API's line in the solution
file already gave it its role, its pair and its server pair, in step 5, "The
solution file, and the restore".

**The service is named `<Name>Api` and its image `<Image>`. Ruled by Francois on
2026-10-21**: the service has the API's name in the PascalCase `BackendCode.md`
section 3 gives it, and the API's server certificate names it, while Docker
requires an image's name in lower case. Measured: a service named `OrdersApi`
with `build:` and no `image:` failed with "repository name must be lowercase",
and with `image: zapper-ordersapi` it built and ran, and another container
reached it as `OrdersApi`.

**`pull_policy: never` stops Docker Compose looking for the image on a
registry. Ruled by Francois on 2026-10-21.** Measured: with `image:` beside
`build:` and no pull policy, `docker compose up -d --wait` from nothing
printed "pull access denied for zapper-ordersapi" before it built the image.
With `pull_policy: never` there was no attempt and no warning, a missing image
was built, and the next `up -d --wait` built nothing and recreated nothing.

**It is built as the Migrator is**, from `Code/` with `Docs/` as the second
context named `docs`. **`restart: unless-stopped` brings it back** whenever
Docker starts, as it does the database. **`init: true` runs Docker's own small
init process as the container's process 1, with the API under it. Ruled by
Francois on 2026-10-21, for every host's service**, as `create-solution`'s step
12 records for the Migrator: an API that fails with an unhandled exception ends
itself with an abort, which does not end process 1. Measured on 2026-10-21 with
the documentation switch left out, a failure before `Program.cs`'s `try`:
without `init: true` the container kept running and served nothing; with it,
the container exited 134 in 1 second. Measured on 2026-10-21, in a full run
with two APIs and that switch left out of the first API's service in a
temporary override file: `docker compose up -d --wait` still exited 0 and
reported that API `Healthy`, after 10 seconds in one run and 107 in another,
while `restart: unless-stopped` started its container again 6 and 10 times,
each exit 134. **So this command's exit 0 does not prove an API started.** Step
17, "Verify", proves it by calling health, which failed there with exit 35.
**It starts only once `migrator` has
finished with 0**, so the API never starts against a database that is behind it.
Measured: when the Migrator failed, `docker compose up -d --wait` exited 1 and
no API was started.

**Its configuration comes from environment variables, and it reads no
`appsettings.Development.json`. Ruled by Francois on 2026-10-21.** So this
service supplies what that file gives the API under `dotnet run`:

- **The connection string**, the API's own from step 4, "The two settings
  files", with `Host=database` and port `5432`, the server's name and port inside
  Docker's network, which the server's certificate carries. `Username=` is
  `<Role>`, as `BackendCode.md` section 14 has every host connect as its own role.
- **The documentation switch, only for an API that only the product's own
  frontends and services call.** `Security.md` section 9 has that API publish in
  every environment except production, and a container on a developer's machine
  is not production. Without the line, `OpenApiOptions` refuses to start the API,
  because the switch is `[Required]`. An API a customer's system calls already
  has it in `appsettings.json`. Measured on 2026-10-21 with an API only the
  product's own frontends call: with the line, it started in its container and
  answered `/openapi/v1.json` and `/scalar/` with 200; without it, it wrote
  `OptionsValidationException` with "The IsEnabled field is required." and
  served nothing; its container did not exit until `init: true`, below, was
  added, and then exited 134.
- **No allowed origin**, because no frontend calls the API yet, and an
  environment that sets none refuses every origin.

**The four `ASPNETCORE_` variables make the API serve HTTPS on 8443 and nothing
else, with the server pair `create-solution`'s `certificates` service issued.**
Ruled by Francois on 2026-10-21: an API's container serves HTTPS only, with a
server certificate the local authority issues, as `Security.md` section 11
states that an API's address never answers plain HTTP; the pair is `server.crt`
and `server.key` in the API's role's folder; and it listens on 8443 in its
container. The certificate's two paths are the `Kestrel` section that
`BackendCode.md` section 14 names, given as environment variables.
**`ASPNETCORE_HTTP_PORTS: ""` is required**, because the chiseled image sets
`8080` itself. Measured on 2026-10-21: with these four, the API listened on
`https://[::]:8443` alone, port 8080 was closed, Kestrel took the PEM pair as it
is, and plain HTTP got "Empty reply from server".

**It is published on `127.0.0.1:<HostPort>` and on nothing else outside
Docker**, so only this machine reaches it, as the database's port is published.
Inside Docker's network, another container reaches it at
`https://<Name>Api:8443`. Measured on 2026-10-21.

**It mounts only its own role's folder, read-only, at `/certificates`**, by the
volume's `subpath`, as the Migrator does. The folder holds `ca.crt`, the role's
pair and the server pair, all owned by `1654` with both keys mode 600, measured.
`create-solution`'s step 11 has the API's `DatabaseDataSourceFactory` look there,
since the image holds no `.slnx`.

**It has no health check. Ruled by Francois on 2026-10-21**: the chiseled image
has no `curl` and no shell to run one. So `docker compose up -d --wait` counts
the API ready once it runs, measured, and step 17, "Verify", calls health
itself.

**Build the images and start the API**, from the repository root:

```bash
docker compose up -d --build --wait
```

**Expect exit 0.** It builds the API's image, and the Migrator's again, because
that image holds the grant migration; runs `certificates` and `roles`, which
issue and create nothing new, since step 5 did; runs `migrator`, which logs "No
migrations were applied", since step 6 applied the grant from the host; and
starts the API. **For the first API this is also the first run that creates a
`migrator` container**, because `create-solution` runs the Migrator with
`docker compose run --rm`, and it exits 0 because the API depends on
`migrator`. Measured on 2026-10-21, straight after `create-solution`: exit 0 in
27 seconds, with `migrator` exited 0 after "No migrations were applied" and the
API running and logging its startup line. **Every earlier API's image is built
again and its container recreated**, because its image holds the new migration
too. Measured on
2026-10-21: after a second API's project, line, grant migration and service were
added, this command exited 0 in 33 seconds, the new API
answered 200 over HTTPS, and the first API was recreated. **Anything other than
exit 0 is a real failure: stop, report what it said, and show what each service
did with `docker compose logs certificates roles database migrator <Name>Api`.**
The one to expect is `port is already allocated`, where something took
`<HostPort>` after it was found.

### 17. Verify

Run every command below from `Code/`, except where it says the repository root.
**Each one has an expected result, and anything else is a real failure: stop,
report what it said, and do not carry on.**

**Restore the solution, and then restore in locked mode:**

```bash
dotnet restore
dotnet restore --locked-mode
```

**Expect exit 0 from both.** The first writes the integration test project's lock
file again with the API and the package it added, and the second proves that every
lock file matches what its project asks for, which is how the pipeline restores.
`BackendCode.md` section 2 has every lock file committed and restore run in locked
mode.

**Check that every project in the solution has its lock file**, because locked mode
writes a missing one and still exits 0:

```bash
grep -o 'Path="[^"/]*/' <Solution>.slnx | sed 's/^Path="//; s#/$##' | while read -r project; do test -f "$project/packages.lock.json" || echo "$project"; done
```

**Expect no output.** The loop prints each project in the solution file that has no
`packages.lock.json`, the new API among them. A line printed is a lock file the
pipeline's own restore step would fail on: report it.

**Build the solution:**

```bash
dotnet build --no-restore
```

**Expect 0 warnings and 0 errors.** `Code/Directory.Build.props` turns any warning
into a failed build, so a build that succeeds has none.

**Run every test:**

```bash
dotnet test --no-build
```

**Run it from `Code/`, and from nowhere else.** `dotnet test` reads
`Code/global.json` only from the folder it is run in and the folders above it.
Measured: run from the
repository root, it ran in the older test mode and failed on its own arguments.
**Expect exit 0, with "Test run summary: Passed!" and every test passing.** The
contract test class runs one case for each fragment under
`Docs/Contracts/<Name>Api/v1/`, which is health's alone on day one. On a solution
holding only what `create-solution` wrote and this API, that is 21 tests: the 20
`create-solution`'s step 17, "Verify", counts, and health's contract test.
**Each API adds one test for each fragment it freezes**, so a second API on that
solution gives 22. Record the count for the closing report. **Exit 2 is a failing
test**: report which test failed and what it printed. The integration tests start a
PostgreSQL container, and the template now holds this API's role.

**Check the containers**, from the repository root:

```bash
docker compose ps -a
```

**Expect `certificates`, `roles` and `migrator` exited with 0, and `database`
and every API's service, `<Name>Api` among them, running.** Anything else is a
real failure: show
`docker compose logs certificates roles database migrator <Name>Api`.

**Call health over HTTPS in the container**, from the repository root:

```bash
curl --silent --show-error --cacert Code/Certificates/ca.crt --output /dev/null --write-out '%{http_code}\n' https://localhost:<HostPort>/v1/health
```

**Expect `200`.** `--cacert` makes `curl` trust the local authority, which issued
the API's server certificate, and nothing else, so the call proves that the API
serves HTTPS with that certificate and that the certificate names `localhost`.
It also proves the API started: its migration check passed and it loaded both
Error tables. Measured on 2026-10-21: `curl --cacert Code/Certificates/ca.crt`
to `https://localhost:<host port>/v1/health` and to
`https://127.0.0.1:<host port>/v1/health` both returned 200 over HTTP/2. With
the flags above, measured on 2026-10-21, it printed `200` alone and exited 0.

**Check that the address does not answer plain HTTP:**

```bash
curl --silent --show-error http://localhost:<HostPort>/v1/health
```

**Expect it to fail with "Empty reply from server" and exit 52**, which
`Security.md` section 11 requires: an API's address never answers plain HTTP.
Measured on 2026-10-21: it printed `curl: (52) Empty reply from server` and
exited 52.

**Check that no placeholder leaked**, from the repository root:

```bash
grep -rnI -e '<Solution>' -e '<Name>Api' -e '<DatabaseName>' -e '<Port>' \
    -e '<Role>' -e '<Title>' -e '<Description>' -e '<Date>' \
    -e '<Image>' -e '<HostPort>' \
    --exclude-dir=bin --exclude-dir=obj \
    Code/<Solution>.<Name>Api Code/<Solution>.IntegrationTests \
    Code/<Solution>.Infrastructure/Persistence/Migrations \
    Code/Directory.Packages.props \
    Docs/Contracts/<Name>Api Docs/InfrastructureInventory.md \
    compose.yml
```

**This is the one command in this recipe whose placeholders after `-e` are typed
exactly as written and not replaced**, because they are what it looks for. The
paths after them are replaced as everywhere else. **Expect no output and exit 1**,
which is how `grep` reports that it found nothing. A line printed means a value was
never substituted in the file it names: report every line. **Its ten tokens are
every placeholder this recipe writes into a file.** The API's Dockerfile is in
the API's folder, which is searched. The paths searched are
everything this recipe writes or changes, apart from the lock files, which
`dotnet restore` writes, and the solution file, whose own root element is the
literal text `<Solution>`, measured. **Check the solution file's new line instead**, from the repository root:

```bash
grep -c 'Path="<Solution>.<Name>Api/<Solution>.<Name>Api.csproj"' Code/<Solution>.slnx
```

**Expect `1`.**
