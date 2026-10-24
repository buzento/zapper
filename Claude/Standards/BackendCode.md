# Backend Code Standards

The implementation standard for back end code: C#, ASP.NET Core, EF Core and
PostgreSQL. It fixes the technology, the conventions and the practice for every
concern the back end has, so that an increment can be written without inventing
any of them.

Planning reads this before it starts and plans against it. The plan is
written against it, and so is any code example in a plan.
`feature-execution` reads it before writing an increment.
`feature-review` checks the increment against it.

Every rule in this document is settled. Each one was argued and decided before it
was written here, and none of them is re-opened during execution. Where this
document states one way to do something, that is the only way, and an equivalent
is not substituted for it.

Code examples show the required shape, and every one of them is bound by the same
restriction. A sentence before the example says it is an example, and the first
line inside it repeats that. Every class, method, property, parameter, enum and
enum value named in an example is there to make the shape readable. The names are
illustrations, not names to look for in the solution. Finding one, or not finding
one, tells the reader nothing. Only the shape is the rule.

The codebase must read as though one person wrote it.

This standard applies to code that already exists. Code written before a rule
existed is still non-conforming, and correcting it is its own increment, never
part of an unrelated one.

Rules that an analyzer, `.editorconfig` or a build setting enforces are stated
here as well. Every rule has a named enforcer in section 22, including the rules
whose enforcer is nothing.

# 1. Scope

This document covers the back end, and the top-level layout of `Code/`. Source
layout is its subject. Test layout is not.

| Subject | Where it is stated |
|---|---|
| Web frontends | `FrontendCode.md` |
| Tests, and where a test file goes | `Testing.md` |
| What counts as sensitive, where secrets live, database roles, build gates | `Security.md` |
| A decision that affects more than one feature, such as how secrets are retrieved | a decision in a plan |
| What one feature must do | the rules and scenarios in its plan |
| How one feature is built | its plan |

**A security rule is in this document when it constrains how code is written.**
Anything else is `Security.md`'s. Where a rule here depends on what counts as
sensitive, it points at `Security.md` for that definition.

**The architecture is Vertical Slice Architecture**, with one limit: what several
APIs share over one database does not live in an operation. Sections 3 and 4
define what that is and where it lives. Everything else about a feature's
behaviour lives in its operation.

# 2. Technology baseline and package management

**The repository holds two roots.** `Code/` is the solution root and holds every
project and every file the build reads. `Docs/` sits beside it and never inside
it.

**A third kind of folder lives directly under `Code/`: solution tooling, under
`Code/Tools/`.** An API project answers requests. A frontend renders a UI. A
new piece of code that does neither, and runs to support the solution's build,
is a tool.

**Each tool is a folder of its own under `Code/Tools/`.** Section 18 states how
it is named.

**A tool's technology follows what it does.** An npm tool is a workspace of
`Code/package.json`, and its `package.json` `name` is its folder name in npm
form, the derivation `FrontendCode.md` section 3 states for a frontend. A tool
is not required to be an npm tool.

**The solution's name is written `<Solution>` in this document.** It is a
placeholder for the name `create-solution` asks for when it creates the
solution, and every other name derived from it is derived by a rule this
document states.

**`<Solution>` is one PascalCase token of letters only.** It carries no dot, no
hyphen, no underscore, no space and no digit, and where the product's name is
more than one word those words run together. Section 18's acronym rule applies
to it, so an acronym is written `Api` and not `API`. A value that does not fit
is refused, and is never adjusted to fit.

**The restriction is there because the value is concatenated into C# names.**
Section 9 derives every registration method's name from it, so a dot in the
value would make one of those names read to the compiler as
namespace-qualified. The same dot would add a level to every project's
namespace.

**The solution file is `Code/<Solution>.slnx`**, in the XML solution format,
which is what the SDK produces when nothing asks for the other one.

The back end is built on .NET 10 with C# 14. Every project targets `net10.0`.

| Concern | Technology |
|---|---|
| Framework | .NET 10 |
| Language | C# 14 |
| HTTP | ASP.NET Core minimal APIs |
| Database | PostgreSQL 18 |
| Data access | EF Core with Npgsql |
| Request validation | FluentValidation |
| Logging | Serilog, behind `ILogger<T>` |
| OpenAPI document | `Microsoft.AspNetCore.OpenApi` |
| API documentation UI | Scalar |

**The target framework is not changed by an increment.** No increment proposes
changing it.

**`LangVersion` is not set.** It stays at the SDK default, and is never set to
`latest` or `preview`.

**Every project takes its build settings from one file**,
`Directory.Build.props` at the solution root. No project sets any of them for
itself.

| Setting | Value |
|---|---|
| `Nullable` | `enable` |
| `ImplicitUsings` | `enable` |
| `TreatWarningsAsErrors` | `true` |
| `MSBuildTreatWarningsAsErrors` | `true` |
| `EnforceCodeStyleInBuild` | `true` |
| `RestorePackagesWithLockFile` | `true` |

**`TreatWarningsAsErrors` reaches the compiler's warnings and
`MSBuildTreatWarningsAsErrors` reaches the build engine's**, including those
NuGet, the SDK and a third-party target raise. A package version conflict is one
of the second kind, so without both settings a build can resolve one project's
dependency at a lower version than another project compiled against and still
report success.

**A warning that turns out to be noise is exempted by name**, and neither
setting is turned off.

**Every style rule in `.editorconfig` carries an explicit severity.** A style
entry written without one reports nothing, and so does a style entry with one
when `EnforceCodeStyleInBuild` is off, so a rule that looks configured enforces
nothing until both are present.

**`packages.lock.json` is committed for every project, and restore runs in
locked mode.** `RestorePackagesWithLockFile` makes the restore write one. A
build that adds or changes a package without updating the lock file fails
restore rather than resolving a version nobody reviewed.

**`ManagePackageVersionsCentrally` is on.** Every package version lives once, in
`Directory.Packages.props` at the solution root, as a `PackageVersion` entry. A
`PackageReference` in a project file names the package and never carries a
version.

**`CentralPackageTransitivePinningEnabled` is on as well.** Without it a project
resolves a transitive package from its own graph, so one project can compile
against a higher version than another project resolves, and a `PackageVersion`
entry for that package does not reach either of them. With it on, every version
in the solution comes from that one file, direct or transitive. Which packages
need an entry is not fixed here: one is added where two projects would otherwise
resolve the same package differently.

The two halves, with a package as an example:

```xml
<!-- Directory.Packages.props, at the solution root -->
<PackageVersion Include="Npgsql" Version="<version>" />

<!-- every project file that uses the package -->
<PackageReference Include="Npgsql" />
```

# 3. The solution: projects and reference rules

| Project | Holds |
|---|---|
| `<Solution>.Domain` | entities, enums, and the business rules they enforce, including the policies that span entities |
| `<Solution>.Infrastructure` | anything that calls outside the process: persistence, the logging pipeline and external integrations |
| `<Solution>.ApiCommon` | the shared response machinery, the validation filter, the manager classes, and the wiring each API calls once |
| `<Solution>.<Name>Api` | the operations of one API |
| `<Solution>.Migrator` | applies migrations, and in its Debug build seeds local data |

**One project per API**, named `<Solution>.<Name>Api`. `<Name>Api` is also the
first segment of that API's contract path, so that API's contracts sit under
`Docs/Contracts/<Name>Api/` and the path stays derived.

**The API's name is `<Name>Api`**, and `<Name>` is one PascalCase token that
takes the shape `<Solution>` carries.

**The reference graph:**

```
<Solution>.Domain           references nothing
<Solution>.Infrastructure   references Domain
<Solution>.ApiCommon        references Domain
<Solution>.<Name>Api        references Domain, Infrastructure and ApiCommon
<Solution>.Migrator         references Infrastructure and ApiCommon
```

No API references another API. Nothing references an API except
`<Solution>.IntegrationTests`. Infrastructure never references ApiCommon.

**`<Solution>.Migrator` references ApiCommon for its managers**, so code it
shares with an API, such as encryption, is written once. It registers only the
managers, as section 9 states, and so runs on the same ASP.NET Core runtime
image as an API. Ruled by Francois on 2026-10-23.

**There is no application or use case project.** An operation is never split
across projects: its endpoint and its handler are in the same API project.

**Infrastructure is organised in folders per concern, and there are three.**
`Persistence` owns the schema. `Logging` holds the logging pipeline every host
writes through. `Integrations` holds everything else that reaches outside the
process, one folder per provider. Nothing in Infrastructure belongs outside those
three. Sections 9, 10 and 15 state what is in each.

**A provider's folder holds its service class, that class's interface, and the
request and response types its own calls use.** The folder is named for the
concern, and it is created when its first file is written.

**A request or response type for an external call carries no business
meaning and never reaches Domain.** It is named for the interface method it
serves, without that method's `Async` suffix, plus `Request` or `Response`,
the same derivation section 18 gives an operation's own request and response.
One class per file, the file named for the class, the same as every other
file in Infrastructure.

**A concern leaves Infrastructure only on approval**, and the trigger for asking
is that only some APIs need it and it brings a heavy or risky dependency.

**Three questions place a new piece of code:**

- Does it call something outside the process? Infrastructure.
- Does it carry business meaning? Domain.
- Neither, and more than one API needs it? ApiCommon.

**Code in ApiCommon takes what it needs as arguments.** It never reaches into
Infrastructure. The API host reads what Infrastructure holds and passes it in.
Something that cannot be written that way does not belong in ApiCommon.

As an example of that rule, with two classes named as illustrations rather than
as names to look for: a `CryptographyManager` computes and calls nothing outside
the process, so it belongs in ApiCommon, while a `SecretService` calls the vault,
so it belongs in Infrastructure. The manager therefore cannot reach the vault,
and the key and the call that uses it meet in a handler.

**Cryptography is never written by hand.** A hash, a cipher, a signature, a MAC
or a random value that must not be guessed is made only by a well-known,
industry-validated implementation, such as what the .NET base class library or
a package `Security.md` section 3 accepts provides. `Security.md` section 3
states which mechanism and which parameters are allowed.

**ApiCommon is organised in folders per concern**, each created when its first
file is written. Sections 6, 7, 8 and 9 state each of them.

**A manager lives at `Managers/<Concern>/`**, holding `<Concern>Manager.cs` and
`I<Concern>Manager.cs`. In both this project and Infrastructure the folder is the
class name without its suffix, so the path is derived rather than chosen.

**Infrastructure owns the schema and never the queries.** It holds the
`DbContext`, the entity configurations, the migrations, the converters and the
interceptors. A query belongs to the handler that needs it, written against
`DbContext`. No shared project holds a query class or a repository.

**Entity configurations never live in an API project.** Section 10 states where
in Infrastructure they live.

**Request and response types are private to the API that serves them.** Two APIs
with an identical shape each declare their own. Section 7 states the one
exception, which is `PagedResponse<T>`. A contract never exposes a domain enum.

**An interface is declared in the lowest project that needs to see it**, and its
implementation may live higher. An interface for something that calls outside
the process is declared in Infrastructure, beside the class that implements it.

As an example again, and again an illustration rather than something to look
for: if Infrastructure
needs the identity of the acting user, that interface is declared in
Infrastructure and implemented in the API, because Infrastructure cannot
reference upward.

**Migrations have one owner.** `<Solution>.Migrator` applies them and deployment
runs it. No API migrates at startup.

**A feature touches three projects:** its entity in Domain, its schema change in
Infrastructure, its endpoint and handler in the API. A feature's behaviour lives
in one place and the schema it uses is shared.

**One entity model is shared by every API.** A change to an entity is a change to
every API and is planned as one change.

# 4. The Domain, and where a business rule lives

**`<Solution>.Domain` holds behaviour, not only data.** An entity protects its own
invariants.

**Domain is organised in a folder per schema.** Every file in Domain sits at
`<Solution>.Domain/<Schema>/`, directly, never inside a further folder named
for the entity. Section 18 states how a schema and an entity are named.

**A folder holds nothing until something needs it.** The rule above says where a
file goes, and a folder is created when its first file is written.

**One class per file, and the file is named for the class.**

**An enum that exists only for one class lives in that class's file.**

**There are no public setters on anything carrying a rule.** A property that a
rule constrains is set inside the entity and nowhere else.

**A state change happens through a method on the entity.**

**A rule about one entity's own data is a method on that entity.**

**A domain method that can refuse returns an enum with one value for each reason
it can refuse.** A method that cannot refuse returns what it computes, or returns
nothing.

**A domain method that makes a state change returns that same enum**, and it
changes state only on the value that allows the change.

**The enum is named `<Operation>Result`.** Where one method serves more than one
operation, it is named for the decision instead, decided in that feature's
planning session and recorded in its plan.

**A domain method never throws to refuse.** A broken business rule is returned.

The example below shows those four rules together. Its names are illustrations,
not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// <Solution>.Domain/Sales/OrderEntity.cs
public sealed class OrderEntity
{
    public bool IsDispatched { get; private set; }

    public bool IsCancelled { get; private set; }

    public CancelOrderResult Cancel()
    {
        if (IsDispatched)
        {
            return CancelOrderResult.AlreadyDispatched;
        }

        IsCancelled = true;

        return CancelOrderResult.Cancelled;
    }
}

public enum CancelOrderResult
{
    Cancelled,
    AlreadyDispatched
}
```

**A rule that needs more than one entity is a policy class.**

**A policy class is static.** It holds no state, it takes no dependencies, it is
not registered and it is not injected.

**A policy method is given everything it needs as parameters, and it never reads
a navigation property.**

**A policy method's parameters are entities, never a link row.**

**Methods taking the same entities live in one class.** The parameter list is the
whole test for which class a method belongs to.

**A policy class is named from the entities its methods take, each singular, plus
`Policy`.** The first name is the subject the policy applies to, and each name
after it is what belongs to that subject.

**A policy lives in its schema's `Policies` folder**, created when that schema's
first policy is written, the same as any other folder holding every file of one
kind, which section 18 states.

The example below shows a policy over two entities, and the naming that follows
from them. Its names are illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// <Solution>.Domain/Sales/Policies/CustomerOrderPolicy.cs
public static class CustomerOrderPolicy
{
    public static PlaceOrderResult CanPlaceOrder(
        CustomerEntity customer,
        IReadOnlyCollection<OrderEntity> openOrders)
    {
        if (!customer.IsActive)
        {
            return PlaceOrderResult.CustomerNotActive;
        }

        if (openOrders.Count >= customer.OpenOrderLimit)
        {
            return PlaceOrderResult.OpenOrderLimitReached;
        }

        return PlaceOrderResult.Allowed;
    }
}

public enum PlaceOrderResult
{
    Allowed,
    CustomerNotActive,
    OpenOrderLimitReached
}
```

**A computation that can also refuse is split into two methods.** Where it
cannot be split, the method returns a domain class named for the business thing
it carries, never a general result wrapper.

**There are no domain services.** Nothing in Domain does input or output.

**Domain declares no interface for anything outside the process, and no
repository interface.**

**There are no domain events.**

**Handlers fetch and persist. The Domain decides.** A handler never holds an `if`
that expresses a business rule.

**When a rule needs stored data, the handler queries for that data and passes it
in**, and the domain method decides on what it was handed. In the policy example
above, the handler runs the query for the open orders and hands the result in as
a parameter.

**The handler switches on the enum and maps each value to a status code and an
error code.** That mapping belongs to the API serving the operation and is never
shared. Two APIs may map the same value differently.

# 5. The operation file

**An operation is one HTTP method on one route of one API.** It is the unit of
code, and `CLAUDE.md` already uses the word: a contract is one operation on one
route of one API.

**An operation lives in one file**, holding its endpoint, its request, its
response, its validator and its handler. Sections 6, 7 and 8 state the form of
each of those parts.

**The operation class is `sealed`, and every method on it is static.** It is
never declared `static`, and nothing constructs it.

**The handler is a static method on the operation class.**

**The method that registers the operation's route is `Map`, and the handler is
`Handle`**, with `HandleAsync` where it is async. Section 16 states which
handlers are async.

**An operation never references another operation.**

**A change to one operation does not touch another.**

**An operation file is not split because it holds more than one class.**

**An operation adds a second file only for a class, record or enum that nothing
outside that operation uses.**

**The operation file lives under the `Features` folder, in a folder named for its
resource.** Section 18 states how that folder name and the route are derived.

The example below shows the layout. Its names are illustrations, not names to
look for:

```
Example only. The names are illustrations, not names to look for.

Features/Customers/GetCustomer.cs
Features/Customers/ListCustomers.cs
Features/Customers/RegisterCustomer.cs
Features/Employees/InviteEmployee.cs
```

**There is no file size limit.** An operation too large for one file is planned
as more than one increment, which the implementation order governs.

**A resource folder holds one file for every operation on that resource, and it
grows as operations are added.** It is not reorganised into a folder per
operation.

**The unit of code is called an operation, and the file is called the operation
file.** `Endpoint` names the route seen from outside, and is used only where the
route is the subject.

**No word for a unit of work ever names a unit of code.** A feature and an
increment are units of work. An increment is a commit, so it is never a unit of
code. `Features` is a folder name and not
the name of a unit of code.

# 6. Endpoints, routes and versioning

**The version is in the URL**, as the first segment of every route, and an API
starts at `v1`.

**Every route is versioned, and nothing is exempt.** An endpoint that carries no
business meaning is versioned like any other.

**Two versions may be served at the same time.**

**Registration is explicit.** There is no assembly scan and no discovery
mechanism.

**Endpoints are registered at three levels below the entry point.** Every file
named here belongs to the API project that serves the route:

- **`Program.cs`**, at the root of the API project, calls `MapEndpoints` once and
  calls nothing else to do with routes.
- **`Endpoints.cs`**, at the root of the API project, creates the version group
  and calls one method for each resource.
- **`<Resource>Endpoints.cs`**, in that resource's folder under `Features`,
  creates the group for the resource and calls each of its operations.
- **The operation file's `Map` method** declares that operation's own route into
  the group it is handed.

The example below shows the whole chain, from the entry point down. Its names are
illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// Program.cs, at the root of the API project
app.MapEndpoints();

// Endpoints.cs, at the root of the API project
public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        var v1 = app.MapVersion(1);

        v1.MapCustomers();
        v1.MapEmployees();
        v1.MapHealth();

        app.MapDocumentation();
    }
}

// Features/Customers/CustomerEndpoints.cs
public static class CustomerEndpoints
{
    public static void MapCustomers(this IEndpointRouteBuilder version)
    {
        var group = version.MapGroup("/customers").WithTags("customers");

        GetCustomer.Map(group);
        ListCustomers.Map(group);
        RegisterCustomer.Map(group);
    }
}

// Features/Customers/GetCustomer.cs
public sealed class GetCustomer
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", HandleAsync)
             .WithSummary("Returns a customer by id.")
             .Produces<GetCustomerResponse>(200)
             .ProducesProblem(404);

    // the response and the handler
}
```

**An operation attaches its own security requirement where it declares its
route, with `.RequireAuthorization(...)` naming the permissions a caller must
hold, or with `.AllowAnonymous()` where any caller may call it, on the same
statement as the route it guards.** Which permissions exist, and how they are
represented in code, is settled by the authorization feature's own planning.

**An operation declaring neither is reached by no caller.** `<Solution>.ApiCommon`
registers one fallback authorization policy, in the same call that registers
the managers and the shared machinery section 9 states, that refuses every
request to a route carrying neither call. An operation is never left reachable
by leaving both out.

**Authorization runs before a request binds.** A caller it refuses never
reaches the validator or the handler, so a request with no body or a malformed
one answers 401 or 403 exactly as a well-formed one would.

**`Program.cs` does not change when a feature is added.**

**A new operation edits its own file and its resource's group file.** A new
resource folder adds one line to the root file.

**The root file also calls `MapDocumentation` once**, which section 19 states.

**`MapVersion` lives in `<Solution>.ApiCommon`**, at
`Versioning/VersioningExtensions.cs`, and the version group it returns is created
once, in `Endpoints.cs`.

**No feature file contains a version string.** A resource group is one resource
segment, mapped into the version group it is given.

**A second version is a second list in the same root file**, reusing the
registration of each resource that did not change and calling a different one for
each resource that did.

**An endpoint that every version serves is provided by `<Solution>.ApiCommon`
as one call into the version group**, so it is not written again for each
version.

**It follows the same two files a resource follows**, in a folder named for its
concern: `<Concern>Endpoints.cs` holds the group method, and the operation file
beside it holds the route and the handler. **Health is that endpoint**, at
`Health/HealthEndpoints.cs` and `Health/GetHealth.cs`.

**Health's response shape is frozen.** `GetHealth` writes it by hand, and never
takes the health checks middleware's default writer.

Section 18 states how everything after the version segment is named and derived.

**The OpenAPI document route is not an API surface.** It has no contract, and none
of the rules above reaches it. Section 19 states what is published there.

# 7. The response standard

**The handler is the endpoint delegate.** No class sits between the route and the
code that serves it.

**A handler returns a typed result union naming every response code the operation
can produce.**

**A handler produces each of those results with `TypedResults`, and never with
the static `Results` class.** Section 21 names the symbol.

**A handler never returns JSON it built as text.** `TypedResults.Text` is never
called. Every response this document shapes is a typed object that
`TypedResults.Ok`, `TypedResults.Created` or one of the other typed results
serialises, and none of them is text assembled by hand. Section 21 names the
symbol.

**There is no result type.** No wrapper appears in any signature.

**There is no response envelope.** No response is wrapped in a class carrying a
success flag, a status code, a message or a section for paging.

**A successful response body carries no success flag, no status code, no message
and no timestamp.** The trace id of a successful call is in the headers.

**There is no base response type, and nothing inherits one.**

**Three shapes cover every response:**

| What happened | The shape |
|---|---|
| Any failure | `ProblemDetails` |
| A list | `PagedResponse<T>` |
| Anything else | the resource itself |

**The status codes an operation returns are 200, 201, 204, 400, 401, 403, 404 and
500.** An operation returns nothing else.

**A status code qualifies only when the client behaves differently because of
it.** A code that exists to be more specific is not added, and the specificity it
carried goes in the body instead.

**A business rule refusing a well-formed request is a 400**, the same as a
malformed one.

**That set is what an operation returns. It is not a list of what the process can
emit.** Routing produces a 405 for a wrong method and a 415 for a wrong content
type, with no operation choosing either.

**413 and 429 are two more codes the process emits, and no operation chooses
either.** A request body over its operation's size limit answers 413, and a
caller over its rate limit answers 429. Both qualify by the test above: a
caller who sent too much waits and retries, rather than fixing the request and
resubmitting the way a 400 tells it to.

**The client tells the two kinds of 400 apart by the per-field map, not by a
second status code.** A 400 carrying the map is shown inline against the inputs,
and a 400 without one is shown as a single message.

**`detail` is user-facing text.** It is always fit to show a customer. It never
carries an exception message, a stack trace, a database fragment or an internal
identifier, and the unhandled exception handler writes a fixed message rather
than anything taken from the exception. `Security.md` defines what counts as
sensitive.

**Every failure the process emits carries a code.** There are no exceptions and
no categories: a validation failure, a business rule, the authentication
middleware and routing all carry one. A response produced outside the process,
such as one from a gateway or a load balancer, does not, and nothing here changes
that.

**The code names the condition and never the resource.** One condition has one
code whatever resource it happens under.

**A condition that needs different wording anywhere is a different condition and
takes its own code.**

**Codes are PascalCase.**

**The member is `code`, added beside the RFC's `type`.** `type` is filled by the
framework with an RFC 9110 status URI and is not replaced. A 429 carries no
`type`.

**A handler passes an `ErrorMessageCode` constant and never types the string.**
Those constants live in `<Solution>.ApiCommon`, at `Errors/ErrorMessageCode.cs`.

**One shared method fills every failure body**, and it is the method assigned to
`CustomizeProblemDetails`. It lives at `Errors/ProblemDetailsCustomizer.cs`, and
the wiring assigns it rather than holding it.

**A code an operation set is never overwritten.** The shared method fills a code
derived from the status only when the response does not already carry one.

**Nine codes exist before any feature does**, because the framework rather than
an operation produces the failures they name. They are the status table's rows,
one for each status a failure can carry:

| Status | Code |
|---|---|
| 400 | `BadRequest` |
| 401 | `Unauthorized` |
| 403 | `Forbidden` |
| 404 | `NotFound` |
| 405 | `MethodNotAllowed` |
| 413 | `RequestTooLarge` |
| 415 | `UnsupportedMediaType` |
| 429 | `TooManyRequests` |
| 500 | `Internal` |

**No message text lives in code.** Not in a handler, not in a validator, nowhere.

**The text lives in two tables**, both shared by every API and both seeded by
migration. English only.

**The status table is keyed by the status code.** It holds one row for each status
a failure can carry, and each row holds that status's code, its title and its
baseline detail.

**The message table is keyed by the code.** It holds one row for each condition an
operation authors, and each row holds that condition's detail.

**Only a failure carries text.** A success carries none.

**Each API loads both tables into memory at startup**, and the lookup reads
memory. The lookup lives in `<Solution>.ApiCommon`, which may not reference
`<Solution>.Infrastructure`, so the API host reads the rows and hands them over.

**The lookup is a manager**, at `Managers/ErrorMessage/ErrorMessageManager.cs`
with `IErrorMessageManager.cs` beside it. Section 9 states how it is registered.

**`title` always comes from the status table, by the status of the response.** The
framework's English reason phrase is never left in `title`, and `title` does not
vary by code.

**`detail` and every entry in the per-field map come from the message table, by
the code.** A code the shared method derived from the status takes the baseline
detail on its status row instead, which is the normal path for a failure no
operation authored.

**Every constant names a row in exactly one of the two tables, and every row in
either table has a constant.**

**An operation's code with no message row falls back to the baseline detail on its
status row**, in `detail` and in each entry of the per-field map. `title` never
falls back, because it was never looked up by code.

**That fallback is not the norm, and it is never quiet.** Each use logs at `Error`
with the code that was missing.

**The generated document has to say what is actually sent**, so every response an
operation produces is declared where its route is declared.

**A success response is declared with `Produces<T>`**, naming the response class
where there is a body.

**Every failure other than a 400 is declared with `ProducesProblem`.**

**Every 400 is declared with `ProducesValidationProblem`.** Never
`ProducesProblem` for a 400, and never both calls on one operation.

**A typed result union contributes the status codes and no body**, so the
declaration is what supplies the body the document is otherwise missing.

**`ProblemHttpResult` contributes no response metadata at all.** An operation
returning `TypedResults.Problem` without a declaration is documented as a 200 with
no body, whatever status it actually sends.

**The `code` member is added to both problem schemas by one schema transformer**,
registered once in the wiring every API calls. No operation declares it. It lives
at `Errors/ProblemCodeSchemaTransformer.cs`.

The example below shows one operation file, declaring its responses and refusing
with its own code. Its names are illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// Features/Customers/RegisterCustomer.cs
public sealed class RegisterCustomer
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", HandleAsync)
             .WithSummary("Registers a customer.")
             .Produces<RegisterCustomerResponse>(201)
             .ProducesValidationProblem();

    private static async Task<
        Results<Created<RegisterCustomerResponse>, ProblemHttpResult>>
        HandleAsync(
            RegisterCustomerRequest request,
            <DatabaseName>Context database,
            CancellationToken cancellationToken)
    {
        // the query that decides

        return TypedResults.Problem(
            statusCode: 400,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = ErrorMessageCode.EmailExists
            });
    }
}

// the request, the response and the validator, in the same file
```

**A handler sets the code and never any text.** The shared method fills `title`
from the status and `detail` from the code, for the code the handler set and for a
code it derived from the status.

**An expected failure is an outcome, never an exception.** An exception is for a
condition the contract makes impossible.

**One `IExceptionHandler` turns an unhandled exception into `ProblemDetails`**,
at `Errors/UnhandledExceptionHandler.cs`.

**No operation catches `Exception`.**

**No operation writes an error body.**

**A cancelled request is not a failure.** The handler writes no response and logs
nothing. Section 16 states the rest.

**Three calls make the failures uniform, and every API makes all three:**
`AddProblemDetails`, `UseExceptionHandler` and `UseStatusCodePages`. With
`AddProblemDetails` alone, every failure the framework produces comes back with an
empty body.

**`RouteHandlerOptions.ThrowOnBadRequest` is pinned to false in every
environment**, so a malformed request body is a 400 everywhere.

**A request body, a query string or a form carrying the same member more than
once is refused with 400**, unless the operation's request type declares that
member as a collection. For a body,
`JsonSerializerOptions.AllowDuplicateProperties` is set to `false` in the JSON
options every API registers, which is what refuses it. Section 9 states where
that registration lives. A query string or a form binds a repeated scalar
member as one merged value rather than refusing it, so a validator cannot see
that it was repeated, and there is no equivalent setting for either.

**Every response an API sends carries `Cache-Control: no-store`,
`Strict-Transport-Security` with a `max-age` of at least one year and
`includeSubDomains`, `X-Content-Type-Options: nosniff`, and a
Content-Security-Policy whose `frame-ancestors` is `'none'`.** On a response
that is not the documentation page, the policy allows nothing to load and
nothing to connect to. Section 9 states the middleware that writes them.

**The documentation page carries its own Content-Security-Policy instead**,
under which the page loads and connects only to its own address and the APIs
it documents, runs script only from its own address, allows its one inline
start script by that script's hash, and sets a referrer policy of
`no-referrer`. Section 19 states where that policy is applied, because it
reaches only the documentation route.

**Every list returns `PagedResponse<T>`**, holding the operation's own response
class. It lives in `<Solution>.ApiCommon`, at `Paging/PagedResponse.cs`.

**It is the one response type shared by every API**, and it is the exception
section 3 names. The class it holds stays private to the API that serves it.

**It is a container, and nothing inherits anything.**

**It carries the rows, the page number, the page size and the total number of
matching records**, written on the wire as `items`, `page`, `pageSize` and
`totalCount`.

**Nothing derived is carried.** No page count, and no indication of whether a next
or a previous page exists.

**Adding a member to `PagedResponse<T>` is a decision, not a convenience.**

**The request sends `page` and `pageSize`**, and the first page is 1.

**The defaults are page 1 and page size 25.**

**The maximum page size is 100, and asking for more is refused rather than quietly
reduced.**

**The paging properties on the request class are nullable, and the handler applies
the default.** A non-nullable property with an initialiser is treated by the
binder as a required parameter, and the initialiser is never read.

**Each paging query parameter is named explicitly at the registration**, so the
generated document publishes the name the client actually sends.

The example below shows a different operation file, in which the container, the
nullable paging properties and the named query parameters appear together. Its
names are illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// Features/Customers/ListCustomers.cs
public sealed class ListCustomers
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", HandleAsync)
             .WithSummary("Returns a page of customers.")
             .Produces<PagedResponse<ListCustomersResponse>>(200)
             .ProducesValidationProblem();

    private static async Task<Ok<PagedResponse<ListCustomersResponse>>> HandleAsync(
        [AsParameters] ListCustomersRequest request,
        <DatabaseName>Context database,
        CancellationToken cancellationToken)
    {
        var page = request.Page ?? 1;
        var pageSize = request.PageSize ?? 25;

        // the count, the query and the projection
    }
}

public sealed record ListCustomersRequest(
    [property: FromQuery(Name = "page")] int? Page,
    [property: FromQuery(Name = "pageSize")] int? PageSize);

public sealed record ListCustomersResponse(Guid Id, string EmailAddress);

// the validator, in the same file
```

The example below shows the three bodies on the wire. Its names and values are
illustrations, not names to look for:

```jsonc
// Example only. The names and values are illustrations, not names to look for.

// a failure an operation refused.
// title comes from the status row for 400, detail from the message row for the code.
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "That email address is already registered.",
  "code": "EmailExists",
  "traceId": "00-201861b587fea567db0b2ce7f6d16a9f-5432d24c3e2f29bc-00"
}

// a failure carrying the per-field map.
// the code was derived from the status, so detail is that status row's baseline.
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "The request was not accepted.",
  "code": "BadRequest",
  "errors": {
    "pageSize": ["A page holds at most 100 records."]
  },
  "traceId": "00-201861b587fea567db0b2ce7f6d16a9f-5432d24c3e2f29bc-00"
}

// a page
{
  "items": [
    {
      "id": "0198f3c1-1a2b-7c3d-8e4f-5a6b7c8d9e0f",
      "emailAddress": "first@example.test"
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 1
}
```

**Sorting, filtering and whether a given list pages at all are not settled here.**
Each is decided for the feature that needs it, in its plan, with
the parameters written into the frozen contract.

# 8. Validation

**Every request type has one validator**, written with FluentValidation.

**The validator lives in the operation file**, beside the request it validates.

**A validator is run by an endpoint filter, and a handler never calls one.**

**The operation attaches its own validator where it declares its route**, with
`WithValidation<T>` on the same statement as the route it guards.
`WithValidation` lives in `<Solution>.ApiCommon`, at
`Validation/ValidationExtensions.cs`, and the filter it attaches is at
`Validation/ValidationFilter.cs` beside it.

**Validators are not discovered.** `AddValidatorsFromAssemblyContaining` is never
called, and there is no scan anywhere in the solution.

**A validator is given nothing except `TimeProvider`.** No `DbContext`, no
service, nothing else.

**A validator never reaches the database.**

**A validator judges the request by looking only at the request.** Required, a
length, a range, a format, a defined enum member, a rule across two fields of the
same request such as a start date not being after an end date, and a field
required only when another field holds a certain value.

**Every text field carries a longest length, and every number a range.** A
`string` property is never left without `MaximumLength`, and an `int`, a
`decimal` or another numeric property is never left without a range that the
field's own meaning bounds. Where the framework's own type is already the only
bound that makes sense, such as a `DateOnly`, none is added for its own sake.

**An operation never decodes a value the framework has already decoded.** A
route value, a query value and a header value arrive already decoded once, and
nothing calls `Uri.UnescapeDataString`, `HttpUtility.UrlDecode` or an
equivalent on any of them.

**Everything whose answer depends on what is already stored is decided after
validation**, by the handler and the Domain, as section 4 states. Whether a
record exists, whether a value is already taken, whether it conflicts with
something already stored, and whether a stored balance allows it are all outside
the validator.

**No message text lives in a validator.** Every rule carries `WithMessage`
naming an `ErrorMessageCode` constant.

**The per-field map holds codes**, and the same lookup that fills `detail`
translates every entry of it, as section 7 states.

**Each key in the map is the request property's name in camelCase**, matching the
name that property carries on the wire. Where the property carries
`[JsonPropertyName]`, as section 18 allows for a provider's notification, the key
is that name. The property name resolver set in the
wiring every API calls produces it, and nothing per validator or per operation
sets it. It lives at `Validation/PropertyNameResolver.cs`, and the wiring assigns
it rather than holding it.

**A request that fails validation never reaches the handler.**

**The filter's failure is a 400 carrying `ValidationProblemDetails` and its
per-field map, and the filter is the only thing that produces that map.** A
handler refusing a well-formed request is a 400 carrying `ProblemDetails` and no
map. Section 7 states how both are declared.

**A validation failure carries no code of its own**, so `code` and `detail` come
from the status. The codes a validator names appear in the map and nowhere else.

The example below shows one operation file: the validator attached at the route,
and each kind of rule the validator may hold, every one of them naming a code.
Its names are illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// Features/Employees/InviteEmployee.cs
public sealed class InviteEmployee
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", HandleAsync)
             .WithValidation<InviteEmployeeValidator>()
             .WithSummary("Invites an employee.")
             .Produces<InviteEmployeeResponse>(201)
             .ProducesValidationProblem();

    // the handler
}

public sealed record InviteEmployeeRequest(
    string EmailAddress,
    EmploymentTerm EmploymentTerm,
    DateOnly StartDate,
    DateOnly? EndDate);

public enum EmploymentTerm
{
    Permanent,
    FixedTerm
}

public sealed class InviteEmployeeValidator : AbstractValidator<InviteEmployeeRequest>
{
    public InviteEmployeeValidator()
    {
        RuleFor(request => request.EmailAddress)
            .NotEmpty().WithMessage(ErrorMessageCode.EmailAddressRequired)
            .EmailAddress().WithMessage(ErrorMessageCode.EmailAddressMalformed);

        RuleFor(request => request.EmploymentTerm)
            .IsInEnum().WithMessage(ErrorMessageCode.EmploymentTermNotDefined);

        RuleFor(request => request.EndDate)
            .NotNull().WithMessage(ErrorMessageCode.EndDateRequired)
            .GreaterThan(request => request.StartDate)
                .WithMessage(ErrorMessageCode.EndDateBeforeStartDate)
            .When(request => request.EmploymentTerm == EmploymentTerm.FixedTerm);
    }
}

// the response and the handler, in the same file
```

The example below shows the map that validator produces, with the rest of the
body as section 7 has it. Its names and values are illustrations, not names to
look for:

```jsonc
// Example only. The names and values are illustrations, not names to look for.
// The map only. The rest of the body is section 7's.

"errors": {
  "emailAddress": ["An email address is required."],
  "endDate": ["A fixed term employment needs an end date."]
}
```

# 9. Registration and startup wiring

**Handlers are not registered.** A handler is a static method on the operation
class, so there is no handler object for a container to build.

**A handler's collaborators arrive as parameters.** Minimal APIs bind a parameter
whose type the container holds, and that parameter carries no attribute.

**Validators are not registered and are not discovered.** Section 8 states how an
operation attaches its own.

**Every shared service is registered once per host, in `Program.cs`.** A host is
any project with an entry point that references a shared project. Each API is
one, and so is `<Solution>.Migrator`. Every rule in this section holds for a host,
not only for an API.

**A shared project that holds something to register owns one extension method**,
and `Program.cs` calls it once. A project's registrations are made in that
project's own method and nowhere else.

**That method lives in a class named for the project plus `Registration`, at the
project's root.** So `<Solution>.ApiCommon/ApiCommonRegistration.cs`, and
`<Solution>.Infrastructure/InfrastructureRegistration.cs`.

**A project whose registrations cover more than one concern splits them, one
registration class per concern**, and the project's own method calls each of
them. `Program.cs` still calls one method per project and knows nothing about the
split.

**A concern's registration class is named for the concern plus `Registration`,
and it lives in that concern's folder.** Section 3 already puts Infrastructure's
concerns in folders, so `Persistence/PersistenceRegistration.cs` sits beside
what section 10 states.

**A host calls the project's method even when it needs only one concern.** A
registration a host never resolves constructs nothing.

**An extension method is named `Add<Solution>` plus its class's name without
`Registration`.** Where the method registers middleware on the application
rather than services, it is `Use<Solution>` plus that same name.

**`Add<Solution>ApiCommon` takes the route versions the API serves**, and
`Program.cs` passes them. It is the one registration method that takes an
argument, because section 19 registers one document per version and a document is
registered before any route exists.

**A registration method assigns and never implements.** Where a framework hook
takes a method, that method lives in a named class in its concern folder and the
registration names it. No logic is written inline as a lambda.

| Project | What its extension method registers |
|---|---|
| `<Solution>.Infrastructure` | the `DbContext`, the logging pipeline, and one service per external provider |
| `<Solution>.ApiCommon` | `TimeProvider`, the managers, and the shared machinery that sections 7, 8 and 19 state |

**`Program.cs` does not change when a feature is added.** Section 6 states this
for the routing table, and it holds for the container as well. The number of
registration calls follows the number of components, never the number of
features.

**Nothing is registered per feature.** There is no per-feature registration file,
no per-feature extension method, and no shared file that a feature edits in order
to register something.

**Lifetimes:**

| What is registered | Lifetime |
|---|---|
| anything that touches the `DbContext` | scoped |
| a stateless service | singleton |

**Nothing is registered transient by habit.**

**A manager is registered through its interface, as a singleton, by
`ManagerRegistration`**, at `<Solution>.ApiCommon/Managers/ManagerRegistration.cs`,
whose method is `Add<Solution>Managers`. `Add<Solution>ApiCommon` calls it, so an
API registers its managers by calling `Add<Solution>ApiCommon`, as before.

**A host that serves no API calls `Add<Solution>Managers` in place of
`Add<Solution>ApiCommon`.** The rest of ApiCommon's registrations, and the
settings they validate when the host starts, exist to serve requests and mean
nothing to such a host. `<Solution>.Migrator` is that host. Ruled by Francois on
2026-10-23.

**One service is registered per external provider**, in `IntegrationRegistration`,
naming both the interface it is resolved by and the class that implements it. No
provider is registered anywhere that persistence is registered.

**A provider's service class, its interface, and its own request and response
types live at `Integrations/<Concern>/`**, which section 3 states.
`IntegrationRegistration` sits at the root of `Integrations` and registers
every provider, so no file above it changes when a provider is added.

**A request or response type for an external call is never registered.** It
is neither a service nor a manager, so nothing here constructs one; it passes
between a handler and the interface it is shaped for.

**A handler takes that interface**, whatever the provider is reached by.

**Where a provider is reached over HTTP, its service is registered with
`AddHttpClient<TInterface, TClass>`.**

**A service registered that way keeps the lifetime `AddHttpClient` gives it.** No
lifetime is chosen for it and none is changed.

**No handler takes `HttpClient` or `IHttpClientFactory`.**

**Resilience and retry are configured on the builder `AddHttpClient` returns.**
Which policies apply is settled in a decision in a plan.

**A provider's service never turns off or replaces the certificate check of the
connection it makes.** `HttpClientHandler.ServerCertificateCustomValidationCallback`
and `SocketsHttpHandler.SslOptions` are never touched. Section 21 names the
symbols.

**A provider's service never follows a redirect**, unless the feature's plan
states that it does and states the addresses the redirect may reach. The
builder `AddHttpClient` returns configures a primary handler with
`AllowAutoRedirect` set to `false`.

**A provider's service checks what its provider returns before a handler uses
it.** A field the provider's own contract does not guarantee is checked for
being present and of the shape expected before it is read, the same as any
other value received from outside.

**When a provider cannot be reached, only what needs it refuses, and every
other operation keeps serving.** Nothing here has to arrange that: an
operation never references another, so one operation's call to a provider
cannot reach another operation's request, and the one exception handler
section 7 states turns the failure into a 500 for that request alone, not for
the host.

**A provider's service never turns a failure it could not handle into a
success.** It never catches the exception a failed call throws and returns a
result as though the provider had answered; an unhandled exception is what
section 7 already requires, since no operation catches `Exception` and the one
exception handler turns it into a 500.

**Every API reads the origins it lets read its responses from its own
settings, and allows no other.** The list is a member of an options class read
the way section 14 states, bound and validated the same way, and it is given
to `AddCors` where `Add<Solution>ApiCommon` registers the shared machinery. No
response allows every origin.

**Every API reads its request size limit and its rate limit from its own
settings.** A request over the size limit answers 413, and a caller over the
rate limit answers 429, both before the handler runs. Both limits are members
of an options class read the way section 14 states, and both are registered
where `Add<Solution>ApiCommon` registers the shared machinery.

**Every response carries the headers section 7 states, written by one piece of
middleware that runs on every request.** It is registered where
`Add<Solution>ApiCommon` registers the shared machinery, and it is the one place
that sets them; no operation sets a header section 7 already assigns. It lives
at `<Solution>.ApiCommon/Security/SecurityHeadersMiddleware.cs`.

**An operation declaring neither `.RequireAuthorization` nor `.AllowAnonymous`
is refused by the fallback authorization policy**, which
`Add<Solution>ApiCommon` registers with `AddAuthorizationBuilder`, requiring an
assertion that is always false.

**Every API reads the caller's network address from the part of the product in
front of it, and never from a header the caller can set.** `UseForwardedHeaders`
is configured with the known proxies or networks the infrastructure inventory
states, and every rule that logs or limits a caller by its address reads the
address `HttpContext.Connection.RemoteIpAddress` carries once that middleware
has run.

**Every API's JSON options set `AllowDuplicateProperties` to `false`.** Section
7 states what a duplicate member then answers.

The example below shows a project's own method calling one per concern, a project
with no split, a provider's two files in its own folder, and a handler taking a
scoped service and a provider's interface as parameters. Its names are
illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// <Solution>.Infrastructure/InfrastructureRegistration.cs
public static class InfrastructureRegistration
{
    public static IServiceCollection Add<Solution>Infrastructure(this IServiceCollection services) =>
        services.Add<Solution>Persistence()
                .Add<Solution>Integration();
}

// <Solution>.Infrastructure/Persistence/PersistenceRegistration.cs
public static class PersistenceRegistration
{
    public static IServiceCollection Add<Solution>Persistence(this IServiceCollection services)
    {
        // the DbContext registration, and the options class it reads

        return services;
    }
}

// <Solution>.Infrastructure/Integrations/IntegrationRegistration.cs
public static class IntegrationRegistration
{
    public static IServiceCollection Add<Solution>Integration(this IServiceCollection services)
    {
        services.AddHttpClient<IPaymentGatewayService, PaymentGatewayService>();

        return services;
    }
}

// <Solution>.Infrastructure/Integrations/PaymentGateway/IPaymentGatewayService.cs
public interface IPaymentGatewayService
{
    // what the handler calls
}

// <Solution>.Infrastructure/Integrations/PaymentGateway/PaymentGatewayService.cs
public sealed class PaymentGatewayService : IPaymentGatewayService
{
    private readonly HttpClient client;

    public PaymentGatewayService(HttpClient client) => this.client = client;
}

// <Solution>.ApiCommon/ApiCommonRegistration.cs
public static class ApiCommonRegistration
{
    public static IServiceCollection Add<Solution>ApiCommon(
        this IServiceCollection services,
        params int[] versions)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IErrorMessageManager, ErrorMessageManager>();
        services.AddSingleton<IPasswordHashManager, PasswordHashManager>();

        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = ProblemDetailsCustomizer.Customize);

        services.AddExceptionHandler<UnhandledExceptionHandler>();

        foreach (var version in versions)
        {
            var documentName = $"v{version}";

            services.AddOpenApi(documentName, options =>
            {
                options.ShouldInclude = description =>
                    description.RelativePath is string route
                    && route.StartsWith($"{documentName}/", StringComparison.Ordinal);

                options.AddSchemaTransformer<ProblemCodeSchemaTransformer>();
                options.AddDocumentTransformer<ApiInformationDocumentTransformer>();
                options.AddOperationTransformer<OperationSecurityTransformer>();
            });
        }

        ValidatorOptions.Global.PropertyNameResolver = PropertyNameResolver.Resolve;

        return services;
    }
}

// <Solution>.<Name>Api/Features/Invoices/PayInvoice.cs
public sealed class PayInvoice
{
    // the Map method

    private static async Task<Results<Ok<PayInvoiceResponse>, NotFound, ProblemHttpResult>>
        HandleAsync(
            Guid id,
            <DatabaseName>Context database,
            IPaymentGatewayService paymentGateway,
            CancellationToken cancellationToken)
    {
        // the query, the call and the save
    }
}
```

# 10. Persistence

**There is one `DbContext` for the solution.** Every API uses it, and a second
context is not added.

**There is no repository and no unit of work.** Nothing wraps the `DbContext`,
and no abstraction stands between a handler and it.

**Everything in this section lives in Infrastructure's `Persistence` folder**,
which is that project's folder for the concern this section governs. These are
the actual names, not illustrations:

```
<Solution>.Infrastructure/
    Persistence/
        <DatabaseName>Context.cs
        <DatabaseName>ContextDesignTimeFactory.cs
        PersistenceRegistration.cs   section 9 states what it registers
        Configurations/   one class per entity
        Converters/       one class per value converter
        Interceptors/     one class per interceptor
        Migrations/       generated, and section 13 states the rules for them
```

**The database's name is written `<DatabaseName>` in this document.** It is a
placeholder for the name `create-solution` asks for, and it takes the same
shape `<Solution>` does: one PascalCase token of letters only, with no dot, no
hyphen, no underscore, no space and no digit, and the words run together where
the name is more than one word. Section 2 states that shape and why it is
there, and section 18 requires every database identifier to be PascalCase.

**The context class is named `<DatabaseName>Context`**, and its file is named
for the class.

**The design-time factory sits beside the context.** Section 13 names it and
states the setting it applies.

**A folder holds nothing until something needs it.** The paths above say where
each kind of file goes, and a folder is created when its first file is written.

**Wherever the context is taken as a parameter, that parameter is named
`database`.** This holds in a handler and everywhere else, no other name is ever
used for it, and it is never shortened.

**The context declares a `DbSet<T>` property for every entity it maps.**

**`OnModelCreating` holds one `ApplyConfiguration` call for each of those
entities and nothing else.** No mapping is written inline in it.

**`ApplyConfigurationsFromAssembly` is never called.**

**Every entity is mapped by an `IEntityTypeConfiguration<T>` class**, one class
per entity.

**Mapping is never written as an attribute on the entity.**

The example below shows one configuration class. Its names are illustrations, not
names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// <Solution>.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs
public sealed class CustomerConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("Customer", "Sales");

        builder.HasKey(customer => customer.Id)
               .HasName("PK_Customer");

        builder.Property(customer => customer.EmailAddress)
               .HasMaxLength(256)
               .IsRequired();

        builder.HasIndex(customer => customer.EmailAddress)
               .IsUnique()
               .HasDatabaseName("UX_Customer_EmailAddress");
    }
}
```

**A configuration class is named for the entity it maps, plus `Configuration`.**

**A value converter maps a property's C# value to the value its column holds, and
back**, wherever the two are not the same representation.

**A converter is named for the two representations it maps between, plus
`Converter`**, the model side first and the column side second.

**An interceptor runs on the operations EF performs**, such as saving changes or
executing a command, and applies what holds for every entity rather than what
holds for one.

**An interceptor is named for what it does, plus `Interceptor`.**

**Which converters and which interceptors a feature needs is stated in that
feature's plan**, never here.

**The context's default query tracking behaviour is
`QueryTrackingBehavior.NoTracking`.** It is set on the context and not on each
query.

**A write asks for tracking with `AsTracking`**, on the query that loads what it
changes.

**A read projects to its response type inside the query.** It never loads
entities and maps them afterwards.

The example below shows one operation file whose read projects in the query. Its
names are illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// Features/Customers/GetCustomer.cs
public sealed class GetCustomer
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", HandleAsync)
             .WithSummary("Returns a customer by id.")
             .Produces<GetCustomerResponse>(200)
             .ProducesProblem(404);

    private static async Task<Results<Ok<GetCustomerResponse>, NotFound>> HandleAsync(
        Guid id,
        <DatabaseName>Context database,
        CancellationToken cancellationToken)
    {
        var customer = await database.Customers
            .Where(candidate => candidate.Id == id)
            .Select(candidate => new GetCustomerResponse(
                candidate.Id,
                candidate.EmailAddress))
            .SingleOrDefaultAsync(cancellationToken);

        return customer is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(customer);
    }
}

public sealed record GetCustomerResponse(Guid Id, string EmailAddress);
```

The example below shows one operation file whose write asks for tracking. Its
names are illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// Features/Orders/CancelOrder.cs
public sealed class CancelOrder
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/{id:guid}/cancellation", HandleAsync)
             .WithSummary("Cancels an order.")
             .Produces(204)
             .ProducesProblem(404)
             .ProducesValidationProblem();

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> HandleAsync(
        Guid id,
        <DatabaseName>Context database,
        CancellationToken cancellationToken)
    {
        var order = await database.Orders
            .AsTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (order is null)
        {
            return TypedResults.NotFound();
        }

        var result = order.Cancel();

        // the switch on result, returning a 400 for each value that refuses.
        // Section 4 states it.

        await database.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
```

**A limited quantity, such as a customer's seats, is never taken beyond its
limit by two requests that arrive at the same moment.** The row that holds the
count against the limit is changed with `ExecuteUpdateAsync`, in a query whose
`Where` also states that the limit is not yet reached, and never by loading the
row, changing it in memory and calling `SaveChangesAsync`. `ExecuteUpdateAsync`
returns the number of rows it changed: one when the request was within the
limit, and zero when another request already took the last of it. A handler
reading zero refuses the request the same way a refused domain result is
refused. Where taking from the limit and a tracked change, such as creating the
record the limit was checked for, belong to the same request, the operation is
the exception below and opens its explicit transaction around both.

**An operation saves once**, with `SaveChangesAsync` or with
`SaveChangesWithOutcomeAsync`. An operation needing a second save is split rather
than wrapped in a transaction.

**A record that must exist only once is guarded by a unique index, and the save
that could break it is `SaveChangesWithOutcomeAsync`.** It is a method of
`<DatabaseName>Context`, and it is the one place in the solution that catches a
`DbUpdateException`. It returns `SaveOutcome`, an enum at
`Persistence/SaveOutcome.cs`:

| Value | When |
|---|---|
| `Saved` | the changes were stored |
| `AlreadyExists` | a unique index refused them: the exception's inner exception is a `PostgresException` whose `SqlState` is `PostgresErrorCodes.UniqueViolation` |
| `Conflict` | a concurrency token refused them: the exception is a `DbUpdateConcurrencyException` |

**Any other exception is not caught**, and section 7's exception handler turns it
into a 500. Nothing was stored for either refusal, so the caller decides what it
means. An operation receiving `AlreadyExists` reads the record that already
exists, or refuses with its own code, as its plan states. An operation receiving
`Conflict` answers 500 with a code of its own and logs at `Warning`, and the
caller's retry applies cleanly. A worker receiving `Conflict` reloads and applies
its change again, as section 23 states.

**`INSERT ... ON CONFLICT` is not written.** It needs statements outside the
tracked entities and an explicit transaction around them.

**A row that more than one writer may change carries a concurrency token**, the
PostgreSQL system column `xmin`, mapped in its configuration class as a `uint`
shadow property with `builder.Property<uint>("Version").IsRowVersion()`. Npgsql
maps a `uint` row version to `xmin`, so no column is added and no migration
writes one. Which tables carry it is stated in the plan of the feature that first
changes them. `SELECT ... FOR UPDATE` is not used for this, because it holds a
lock for as long as the work takes. Ruled by Francois on 2026-10-23.

**An operation does not open a transaction.** There is one exception, and nothing
else qualifies for one.

**The exception is an operation that must combine `ExecuteUpdateAsync` or
`ExecuteDeleteAsync` with tracked changes atomically.** That operation opens an
explicit transaction with `BeginTransactionAsync`.

The example below shows that one case. Section 16 states the rules it follows for
`await using` and for the cancellation token. Its names are illustrations, not
names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// Features/Orders/ClearOrderLines.cs
public sealed class ClearOrderLines
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:guid}/order-lines", HandleAsync)
             .WithSummary("Removes every line from an order.")
             .Produces(204)
             .ProducesProblem(404);

    private static async Task<Results<NoContent, NotFound>> HandleAsync(
        Guid id,
        <DatabaseName>Context database,
        CancellationToken cancellationToken)
    {
        var order = await database.Orders
            .AsTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (order is null)
        {
            return TypedResults.NotFound();
        }

        await using var transaction =
            await database.Database.BeginTransactionAsync(cancellationToken);

        await database.OrderLines
            .Where(line => line.OrderId == id)
            .ExecuteDeleteAsync(cancellationToken);

        order.ResetTotal();

        await database.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(CancellationToken.None);

        return TypedResults.NoContent();
    }
}
```

# 11. Primary keys

**Every table has a `Guid` primary key.** Section 13 states the one exception,
which is a reference table.

**The key is the value a URL carries.** A record has no second identifier for its
URL.

**The key value is created in code, with `Guid.CreateVersion7()`.**

**The entity assigns its own id, in its constructor.** No handler creates a key
value, and no id is passed into an entity.

**A row loaded from the database keeps the id it was stored with.** EF assigns
the stored value after the constructor has run.

**A key value is never `Guid.NewGuid()`.**

The example below shows an entity making its own id. Its names are illustrations,
not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// <Solution>.Domain/Billing/InvoiceEntity.cs
public sealed class InvoiceEntity
{
    public InvoiceEntity(Guid customerId)
    {
        Id = Guid.CreateVersion7();
        CustomerId = customerId;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }
}
```

**The database never produces a key value.** No column carries a
`gen_random_uuid()` default, and no key is configured `ValueGeneratedOnAdd`.

**Every key is configured `ValueGeneratedNever()`**, in that entity's
configuration class. Section 10 states where those classes live.

The example below shows the key on one configuration class. Its names are
illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// <Solution>.Infrastructure/Persistence/Configurations/InvoiceConfiguration.cs
public sealed class InvoiceConfiguration : IEntityTypeConfiguration<InvoiceEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceEntity> builder)
    {
        builder.ToTable("Invoice", "Billing");

        builder.HasKey(invoice => invoice.Id)
               .HasName("PK_Invoice");

        builder.Property(invoice => invoice.Id)
               .ValueGeneratedNever();
    }
}
```

**No service generates ids.** There is no id generator interface, nothing is
registered to produce a key, and no handler is given anything that makes one.

**The key is a plain `Guid` in C#.** No key has a wrapper class or record of its
own.

**A route that takes a key constrains it as `"/{id:guid}"`, and never as
`"/{id}"`.** Section 18 names the route parameter.

**A malformed id and an id that names no record produce the same 404.** An
operation that takes a key declares that 404, and it never declares a 400 for a
malformed id.

The example below shows the constraint on one operation file. Its names are
illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// Features/Invoices/GetInvoice.cs
public sealed class GetInvoice
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", HandleAsync)
             .WithSummary("Returns an invoice by id.")
             .Produces<GetInvoiceResponse>(200)
             .ProducesProblem(404);

    // the response and the handler
}
```

**A published version 7 id discloses when the record was created, and the order
in which records of that type were created.**

# 12. Time

**A point in time is a `DateTimeOffset` in C#.** Nothing else carries one.

**Every stored time is in UTC. There is no exception.** No API writes a time in
a local zone.

**A time that arrives with a non-zero offset is converted to UTC before it is
stored.**

**A date with no time is a `DateOnly`.** A date of birth and a leave day are
dates and not points in time.

**A time with no date is a `TimeOnly`.** A shift start is one.

**Each of the three maps to one PostgreSQL column type:**

| C# | Column |
|---|---|
| `DateTimeOffset` | `timestamp with time zone` |
| `DateOnly` | `date` |
| `TimeOnly` | `time without time zone` |

**Each row is the default mapping**, so no configuration class sets a column
type for a time or a date.

**The application sets every time value, and the database sets none.** No column
carries a `now()` or a `CURRENT_TIMESTAMP` default, and no trigger stamps a row
with a time.

**`DateTime` is banned as a type, not as two of its members.** No field,
property, parameter, return type, local variable or constructor call anywhere in
the solution is a `DateTime`.

**`DateOnly` and `TimeOnly` are separate types and neither is affected by that
ban.**

**`DateTimeOffset.Now` and `DateTimeOffset.UtcNow` are banned.**

**The one exception to the `DateTime` ban is a library that hands one over.** The
value is converted to a `DateTimeOffset` at that boundary, the converted value is
what is stored or passed on, and the `DateTime` never travels further into the
code.

**Now is read from an injected `TimeProvider`, with `GetUtcNow()`.** Nothing else
reads the clock.

**`GetLocalNow()` is never called.**

**`TimeProvider.System` appears only where `TimeProvider` is registered.**

**A handler that reads the clock takes `TimeProvider` as a parameter.**

**A domain method that reads the clock takes `TimeProvider` as a parameter**, and
the handler passes it the one it was given.

**Today's date is `DateOnly.FromDateTime(now.UtcDateTime)`**, where `now` is the
value `GetUtcNow()` returned. It is never built from `now.Year`, `now.Month` and
`now.Day`.

**The time of day is `TimeOnly.FromDateTime(now.UtcDateTime)`**, from that same
value.

**Both are the UTC date and the UTC time of day.**

The example below shows an entity holding all three kinds of value, and a domain
method reading the clock from the `TimeProvider` it is handed. Its names are
illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// <Solution>.Domain/People/ShiftEntity.cs
public sealed class ShiftEntity
{
    public ShiftEntity(Guid employeeId, DateOnly shiftDate, TimeOnly startTime, TimeOnly endTime)
    {
        Id = Guid.CreateVersion7();
        EmployeeId = employeeId;
        ShiftDate = shiftDate;
        StartTime = startTime;
        EndTime = endTime;
    }

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public DateOnly ShiftDate { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public PublishShiftResult Publish(TimeProvider time)
    {
        if (PublishedAt is not null)
        {
            return PublishShiftResult.AlreadyPublished;
        }

        PublishedAt = time.GetUtcNow();

        return PublishShiftResult.Published;
    }
}

public enum PublishShiftResult
{
    Published,
    AlreadyPublished
}
```

The example below shows the operation file that calls it, taking `TimeProvider`
as a handler parameter and passing it on. Its names are illustrations, not names
to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// Features/Shifts/PublishShift.cs
public sealed class PublishShift
{
    // the Map method

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> HandleAsync(
        Guid id,
        <DatabaseName>Context database,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        var shift = await database.Shifts
            .AsTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (shift is null)
        {
            return TypedResults.NotFound();
        }

        var result = shift.Publish(time);

        // the switch on result, returning a 400 for each value that refuses.
        // Section 4 states it.

        await database.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
```

# 13. Migrations and reference data

**A migration is generated by the tool.** No migration is written by hand from
scratch.

**A migration file the tool generates is generated code, and section 17's rules
do not reach it.** `.editorconfig` marks every file in the migrations folder as
generated:

```ini
[<Solution>.Infrastructure/Persistence/Migrations/*.cs]

generated_code = true
```

**The generating command names the project, the startup project and the output
directory.** Left to its defaults the tool writes to a `Migrations` folder at the
project's root, which is not where section 10 puts them, and nothing fails. These
are the actual project names, not illustrations:

```bash
dotnet ef migrations add <VerbNoun> \
    --project <Solution>.Infrastructure \
    --startup-project <Solution>.Migrator \
    --output-dir Persistence/Migrations
```

**The output directory is relative to the project**, and the model snapshot is
written beside the migration.

**The migrations history table is `MigrationHistory`, in a `Migration`
schema.** Left to itself the tool creates that table with no schema named at
all, which lands it in the default schema, and section 18 requires every schema
to be named explicitly and never `public`. So the table's name and its schema
are both set.

**The setting is applied in one place that both callers use.**
`PersistenceRegistration` builds the context for a running host, and the tool
builds it through a factory implementing `IDesignTimeDbContextFactory<T>`, which
section 18's rule for a class implementing a framework interface names
`<DatabaseName>ContextDesignTimeFactory`. Section 10 places that file. The tool
never goes through the registration, so the call that sets the table and the
schema lives in one method and both callers call it. Where the two disagree the
tool writes to one table and a running API reads the other, so every API sees
every migration as pending and refuses to serve with nothing saying why.

**A generated migration is read before it is committed.** Two things are read
for, and the rest of this subsection states them.

**EF infers a rename rather than being told about one.** A property removed and
an unrelated property added in the same change are paired into one
`RenameColumn`, which carries the removed column's data into the new column and
loses the values that were there. A `RenameColumn` nobody asked for is the first
thing the reader looks for.

**A new column on a table that already holds rows is nullable, or carries a
default that somebody chose.** EF fills in the CLR type's own default and reports
nothing, so a required column added without one produces a migration that
succeeds and writes that value into every existing row.

The example below shows both, as they appear in a generated migration. Its names
and values are illustrations, not names to look for:

```csharp
// Example only. The names and values are illustrations, not names to look for.

// <Solution>.Infrastructure/Persistence/Migrations/, in the generated migration.
// The file name is generated and carries a timestamp.

// a rename nobody asked for. One property was removed and an unrelated one
// added, and EF paired them.
migrationBuilder.RenameColumn(
    name: "EmailAddress",
    schema: "People",
    table: "Employee",
    newName: "PreferredName");

// a default nobody chose. Every existing row takes the first of January,
// year one.
migrationBuilder.AddColumn<DateOnly>(
    name: "StartDate",
    schema: "People",
    table: "Employee",
    type: "date",
    nullable: false,
    defaultValue: new DateOnly(1, 1, 1));
```

**One migration per increment that changes the schema.**

**A migration is named as a verb and a noun.**

**A migration is never edited once it has been applied anywhere other than a
developer machine.**

**Every migration has a `Down`, and it is maintained.** `Testing.md` states the
test that applies a migration up, then down, then up again.

**An API refuses to serve against a database that is behind it.** At startup it
asks for the migrations it holds that the database has not applied, with
`GetPendingMigrationsAsync`, and exits when there are any.

**That check is not a comparison for equality.** A database holding migrations
the API does not know about passes. Only a database missing something the API
knows about fails.

**`<Solution>.Migrator` is a console application and returns an exit code.** Zero
means the schema is up to date, and anything else means it is not.

**`<Solution>.Migrator` does not change when a migration is added.** It holds no
list of migrations and nothing in it names one.

**Only static data that the product owns and ships goes into a migration.** A
list of provinces and a list of currencies are examples of that shape, named as
illustrations rather than as things to look for.

**No transactional data of any kind goes into a migration, and nothing a customer
can edit.**

**Data a developer needs on their own machine is seeded by
`<Solution>.Migrator`, after the migrations, and only in its Debug build.** The
seed lives at `<Solution>.Migrator/Seeding/`, compiled only in the Debug
configuration by a condition in the project file, and its one call in
`Program.cs` sits inside `#if DEBUG`. The Migrator's `Dockerfile` takes the
build configuration as an argument, Release by default, and `compose.yml` builds
it as Debug. A Release image holds no seed code. The seed writes through the
entities, so every rule an entity enforces holds for seeded rows, and running it
again adds nothing twice. Which rows it seeds is stated in the plan of the feature
that needs them. Ruled by Francois on 2026-10-23.

**A reference list is a real table**, with a real foreign key from whatever uses
it.

**A reference table's primary key is an `int`.** This is the one exception
section 11 names.

**The key values are the values of an enum in code**, so a row's id and its enum
member are the same number.

**A reference table has at least an `Id` column and a `Name` column.** `Id` holds
the enum member's value and `Name` holds the enum member's name.

**Those two are the minimum and not the whole table.** A reference table may
carry more columns, such as a description used in reporting. It may not carry
fewer.

**The reference entity's key property is declared as the enum and not as `int`.**
Declared as `int`, the model refuses to build, because the enum foreign key
pointing at it is not a compatible type. The column is an `int` either way.

**A table qualifies as a reference table only when all three of these are true:**
its rows ship with the code, nothing creates a row at runtime, and an enum names
them. A table failing any one of them is an ordinary table with a `Guid` primary
key.

**The rows are declared with `HasData`, in that entity's configuration class.**
Section 10 states where those classes live. `InsertData` is never written into a
migration by hand.

**A new value is a new enum member and a new row, in the same increment.**

**Every enum member has a row, and every row has an enum member.** `Testing.md`
states the test.

The example below shows a reference table: the enum, the entity, and the
configuration class that declares its rows. Its names are illustrations, not
names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// <Solution>.Domain/People/EthnicityTypeEntity.cs
public enum Ethnicity
{
    Indian = 1,
    Caucasian = 2,
    Black = 3
}

public sealed class EthnicityTypeEntity
{
    public Ethnicity Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}

// <Solution>.Infrastructure/Persistence/Configurations/EthnicityTypeConfiguration.cs
public sealed class EthnicityTypeConfiguration : IEntityTypeConfiguration<EthnicityTypeEntity>
{
    public void Configure(EntityTypeBuilder<EthnicityTypeEntity> builder)
    {
        builder.ToTable("EthnicityType", "People");

        builder.HasKey(ethnicityType => ethnicityType.Id)
               .HasName("PK_EthnicityType");

        builder.Property(ethnicityType => ethnicityType.Id)
               .ValueGeneratedNever();

        builder.Property(ethnicityType => ethnicityType.Name)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasData(
            new { Id = Ethnicity.Indian, Name = nameof(Ethnicity.Indian) },
            new { Id = Ethnicity.Caucasian, Name = nameof(Ethnicity.Caucasian) },
            new { Id = Ethnicity.Black, Name = nameof(Ethnicity.Black) });
    }
}
```

The example below shows the entity that uses it, and the two calls its
configuration class makes. Its names are illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// <Solution>.Domain/People/EmployeeEntity.cs
public sealed class EmployeeEntity
{
    public EmployeeEntity(Ethnicity ethnicity)
    {
        Id = Guid.CreateVersion7();
        Ethnicity = ethnicity;
    }

    public Guid Id { get; private set; }

    public Ethnicity Ethnicity { get; private set; }
}

// <Solution>.Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs,
// inside Configure
builder.Property(employee => employee.Ethnicity)
       .HasColumnName("EthnicityTypeId");

builder.HasOne<EthnicityTypeEntity>()
       .WithMany()
       .HasForeignKey(employee => employee.Ethnicity)
       .HasConstraintName("FK_Employee_EthnicityTypeId");
```

The example below shows the migration EF generates when one member and one row
are added to the two above. Both halves are generated. Its names and values are
illustrations, not names to look for:

```csharp
// Example only. The names and values are illustrations, not names to look for.

// <Solution>.Infrastructure/Persistence/Migrations/, in the generated migration.
// The file name is generated and carries a timestamp.

protected override void Up(MigrationBuilder migrationBuilder) =>
    migrationBuilder.InsertData(
        schema: "People",
        table: "EthnicityType",
        columns: new[] { "Id", "Name" },
        values: new object[] { 4, "Coloured" });

protected override void Down(MigrationBuilder migrationBuilder) =>
    migrationBuilder.DeleteData(
        schema: "People",
        table: "EthnicityType",
        keyColumn: "Id",
        keyValue: 4);
```

Section 18 states how a reference table and the foreign key column pointing at it
are named.

**A migration that creates or alters a table also grants each API role that
needs it the privileges its plan states, with `migrationBuilder.Sql`, in
the same migration.** Which API roles need which privileges on a table is
settled in that feature's planning session and recorded in its plan,
never here. No role is ever granted the right to change the schema.

**A migration may grant and create nothing**, where the tables it grants on
existed before the role that needs them. The rule above puts a grant in the
same migration as the table's creation, and a new API's role needs privileges
on tables created before that API existed, so its grant has a migration of its
own. The tool still generates the file, which against an unchanged model it
does with an empty `Up` and `Down`, so the migration holds only its `Sql` calls
and the rule that no migration is written by hand from scratch is unaffected.

# 14. Configuration

**One options class per configuration section.** Nothing collects every option in
the solution into one place.

**`IConfiguration` is never injected.** Not into a handler, not into a service,
not into anything.

**Nothing reloads.** `IOptions<T>` is the only way an options class is read.
`IOptionsSnapshot<T>` and `IOptionsMonitor<T>` are never used.

**An options class lives with the code that reads it.** In a project organised in
folders per concern, it sits in the folder of the concern that reads it. In an
API project, which has no concern folders, it sits at the project root beside
`Program.cs`, except a worker's, which sits beside its worker, as section 23
states.

**An options class never lives in an operation file or a resource folder.**

**An options class is named for what it configures, plus `Options`.**

These are the actual paths, not illustrations. `<Concern>` is the provider's own
folder, which section 3 derives.

| What it configures | Where it lives |
|---|---|
| the database | `<Solution>.Infrastructure/Persistence/DatabaseOptions.cs` |
| one external provider | `<Solution>.Infrastructure/Integrations/<Concern>/<Concern>Options.cs` |
| one concern of `<Solution>.ApiCommon` | that concern's folder |
| one API's own settings | that API project's root |
| one worker | `<Solution>.<Name>Api/Workers/<Concern>/<Concern>WorkerOptions.cs`, binding `<Solution>:<Concern>Worker` |

**A configuration section that an options class binds is `<Solution>:` followed
by the class name without `Options`.** So `DatabaseOptions` binds
`<Solution>:Database`. In an environment variable each colon becomes a double
underscore.

**Two configuration sections are outside that prefix, and neither binds an
options class.** Section 15 names `Serilog`. The other is `Kestrel`, which the
framework reads, and it holds only the path to an API's HTTPS certificate and
the path to its key, each given as an environment variable by the environment
the API runs in. Ruled by Francois on 2026-10-21.

**The connection string is a member of the database's own options class**, in
that section, and not in the framework's `ConnectionStrings` section.

**An options class is bound and validated where its project's registrations are
made**, which section 9 states.

**The binding call is `AddOptions<T>`, then `BindConfiguration`, then
`ValidateDataAnnotations`, then `ValidateOnStart`.** `BindConfiguration` takes
the section name and reads configuration from the container, so no registration
method takes a configuration parameter.

**An options class holding a secret that only some of the hosts registering it
use is bound without `ValidateOnStart`.** `ValidateOnStart` checks an options
class when the host starts, whether or not that host ever reads it, so it would
make every host calling the registration hold a secret it never uses, which
`Security.md` section 3 forbids. Each host that uses the secret reads the
options class before it first needs it: an API in its startup check, and
`<Solution>.Migrator` before the step that needs it. That read runs the same
validation, so a host that needs the secret still refuses to start without it.
Ruled by Francois on 2026-10-23.

**Options are validated with data annotations, never with FluentValidation.**

**A member that must be supplied by configuration carries `[Required]`.** Bound
against a section name that does not exist, every member takes its type's default
and the host starts, so `[Required]` is what turns a missing or misspelled
section into a refusal to start.

**Anything too complex for an attribute is an `IValidateOptions<T>` class**, named
for what it validates plus `ValidateOptions`, and registered as a singleton
beside the options it validates. So the class validating `DatabaseOptions` is
`DatabaseValidateOptions`.

**A failure of either kind stops the host at startup.**

**Where a setting comes from:**

| Source | What it holds |
|---|---|
| `appsettings.json` | defaults that are the same in every environment |
| an environment variable | anything that differs per environment |
| the vault | secrets; on a developer's machine, a folder of files that stands in for it |

**The section name is the same whichever source supplies a value.** One options
class takes each member from the source that suits it, and an environment
variable overrides a value a file also sets.

**On a developer's machine the vault's stand-in is a folder of files, one file
per secret, read by the key-per-file configuration source.** A file named
`<Solution>__<Section>__<Member>` supplies `<Solution>:<Section>:<Member>`. Each
host that reads a secret adds the source once, in `Program.cs`, as optional, so a
deployed environment, which gives each secret as an environment variable read
from the vault, needs no folder. `compose.yml` generates the files on the first
`docker compose up`, writes them to a volume, and mounts that volume read-only
into only the hosts that read them. No secret file is committed. Ruled by
Francois on 2026-10-23.

**There is no `appsettings.Production.json` in the repository**, and none for any
other deployed environment. `appsettings.Development.json` stays, for local
defaults that are not secret.

**No secret appears in any `appsettings` file.** `Security.md` defines what counts
as sensitive and states where secrets live.

**No secret is written into back end source code.** Not a default value on an
options member, not a constant, not a value in a comment or a test fixture
outside the test projects' own fixtures.

**The database connection string requires TLS and checks the server's
certificate.** `DatabaseOptions`' connection string sets `SSL Mode` to
`VerifyFull`, which both requires an encrypted connection and checks the
server's certificate against a trusted root; `DatabaseValidateOptions` refuses
a connection string whose `SSL Mode` is anything else, and the host does not
start.

**The connection string carries no password, in any environment.** Every host
connects as its own database role, named in the connection string's `Username=`,
and proves itself with a client certificate. That is the same arrangement in
every environment.

**A test container is outside the rule that `SSL Mode` is `VerifyFull` and the
rule that the connection string carries no password.**

**The connection string carries no certificate path, in any environment.**
`PersistenceRegistration`, which section 9 places, gives the certificates to the
data source it builds as objects rather than as paths. So no path to a
certificate is written into a connection string or into any `appsettings` file.

**A certificate file is never committed.** The certificates a deployed
environment uses are supplied by that environment, and this repository holds
none of them.

The example below shows one options class, the class that validates what an
attribute cannot, and the binding call that registers both. Its names are
illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// <Solution>.Infrastructure/Integrations/PaymentGateway/PaymentGatewayOptions.cs
public sealed class PaymentGatewayOptions
{
    [Required]
    public string BaseAddress { get; init; } = string.Empty;

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 30;
}

// <Solution>.Infrastructure/Integrations/PaymentGateway/PaymentGatewayValidateOptions.cs
public sealed class PaymentGatewayValidateOptions
    : IValidateOptions<PaymentGatewayOptions>
{
    public ValidateOptionsResult Validate(string? name, PaymentGatewayOptions options) =>
        options.BaseAddress.StartsWith("https://", StringComparison.Ordinal)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("BaseAddress must be https.");
}

// <Solution>.Infrastructure/Integrations/IntegrationRegistration.cs, inside the method
services.AddOptions<PaymentGatewayOptions>()
        .BindConfiguration("<Solution>:PaymentGateway")
        .ValidateDataAnnotations()
        .ValidateOnStart();

services.AddSingleton<IValidateOptions<PaymentGatewayOptions>,
                      PaymentGatewayValidateOptions>();
```

# 15. Logging

**Everything that logs takes `ILogger<T>` as a parameter**, where `T` is the class
that writes the line. Nothing constructs a logger.

**A static class that logs takes `ILogger<<Class>LogCategory>`**, where
`<Class>LogCategory` is an empty `internal sealed` class written below the static
class, in the same file. The empty class sits between `#pragma warning disable
S2094`, with its reason, and `#pragma warning restore S2094`:

```csharp
// Example only. The names are illustrations, not names to look for.

// Orders/ReceiptFormatter.cs, below the static class ReceiptFormatter
#pragma warning disable S2094 // ILogger<T> needs a type, and ReceiptFormatter is static, BackendCode.md section 15
internal sealed class ReceiptFormatterLogCategory;
#pragma warning restore S2094
```

**A handler takes `ILogger<T>` naming its own operation class.**

**The non-generic `ILogger` is never taken as a parameter.** It is not registered,
so it binds as a body parameter and the failure reaches every route in the host
rather than only the operation that asked for it.

**There is no static logger.** Serilog's `Log` class and Serilog's own `ILogger`
are never used.

**A log message is a template.** Every value in it is named in the template and
passed as an argument. A message is never built with string interpolation.

**An exception is passed to the log call as the exception**, and is never
rendered into the message.

The example below shows a handler taking its logger and writing one line. Its
names are illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// Features/Orders/CancelOrder.cs
public sealed class CancelOrder
{
    private static async Task<Results<NoContent, NotFound>> HandleAsync(
        Guid id,
        <DatabaseName>Context database,
        ILogger<CancelOrder> logger,
        CancellationToken cancellationToken)
    {
        // the query, the domain call and the save

        logger.LogInformation("Order {OrderId} was cancelled", id);

        return TypedResults.NoContent();
    }
}
```

The two forms below are never written:

```csharp
// Example only. The names are illustrations, not names to look for.

// the message built by interpolation, which leaves no field behind
logger.LogInformation($"Order {id} was cancelled");

// the exception flattened into the message, which loses its type,
// its inner exception and its stack trace
logger.LogError("Could not cancel {OrderId}: {Error}", id, exception.Message);
```

**The levels:**

| Level | What it means |
|---|---|
| `Critical` | the process cannot continue, and our own code is what stopped it |
| `Error` | a failure the system could not handle, and a person may need to act |
| `Warning` | a failure the system handled and expects to see occasionally |
| `Information` | a business event worth reading after the fact |
| `Debug` | development only |
| `Trace` | never written |

**What must be logged, and at which level:**

| What happened | Level |
|---|---|
| An unhandled exception | `Error` |
| A business rule refusing a request | `Information` |
| The host started | `Information` |
| A business event a customer could later dispute | `Information` |
| An operation's own code with no message row was used | `Error` |
| A database command timeout | `Error` |
| An outbound call timed out, and a retry recovered it | `Warning` |
| An outbound call timed out, and nothing recovered it | `Error` |
| The migration check finding the database behind the code | `Critical` |
| A sign-in attempt, and its outcome, with the factors used | `Warning` on a failure, `Information` on success |
| A user locked, and a user unlocked | `Information` |
| A session ending, with the reason | `Information` |
| A password set or reset | `Information` |
| A second factor added, replaced or removed | `Information` |
| An invitation sent, used or cancelled | `Information` |
| A system caller failing to authenticate | `Warning` |
| An authentication token refused | `Warning` |
| A request refused with 401 or 403 | `Warning` |
| A change to a user's permissions or roles | `Information` |
| A change to a customer's security setting | `Information` |
| A request refused by validation, with the code of the refusal | `Warning` |
| A request refused for being over its limit | `Warning` |
| A file refused for holding malware | `Warning` |
| A security control failing, such as a certificate check | `Error` |

**Each of those rows is a security event**, and `Security.md` section 12 states
what else counts as one, kept and read the way that section states.

**An unhandled exception is logged once, by the one exception handler section 7
states.** An operation never logs it as well.

**A refusal line carries the code the operation set and the ids the operation was
given.**

**The host logs one line when it has started, naming itself.** That line carries
no version, and it is the only startup line.

**A business event a customer could later dispute is logged where it happens.**
Leave approved, a payroll run started and finished, an employee terminated and a
pay change are illustrations of that kind of event rather than a list to build.

**Section 7 states the rule for an operation's code with no message row**, and
what that line carries. **Section 16 states how a timeout is told from a
cancellation**, and where a deadline is handled. **Section 13 states the migration
check.**

**What is not logged by hand:**

- Incoming requests and responses.
- Calls out of the process.
- A cancelled request. Section 7 states what one produces.
- A failed start. The host logs it at `Error` and stops.

**A whole object is never written into a log message.** The `@` destructuring
operator is never used in a template.

**Ids are logged, never people.** `{EmployeeId}`, never `{EmployeeName}`.

**A secret, a token and a connection string are never logged.** `Security.md`
defines what counts as sensitive.

**No value at either protection level `Security.md` section 2 defines, and no
secret, reaches a log in any form**, hashed, masked or encoded included. A log
may hold a record's key and the network address a request came from. Three
things can carry one in: what a handler passes to a log call; the framework's
own request logging, whose query string can hold a value such as an email
address, which is why section 15 already leaves that logging off by hand; and
the database libraries, where EF Core logs a command's parameters only as `?`,
and `EnableSensitiveDataLogging` and `Include Error Detail=true` on the
connection string are never turned on outside a developer's own machine.

**Every line written while a request is served also carries the caller**, being
the key of the signed-in user or the identifier of the system caller, and the
network address the request came from, both as enriched properties beside
`TraceId` and `Application`. Section 9 states where the network address is
read from.

**A value written into a log line can never begin a new line.** The output
template's message is `{Message:lj}`, which writes a line break inside a value
as `\r\n` rather than as one. No enricher or destructuring is added that writes
a value's own text unescaped.

**Every log line written inside a request carries the trace id.** The framework's
`Activity` supplies it and Serilog carries it, so nothing is written to put it
there and no enricher is added for it.

**A worker's piece of work carries a trace id too.** The worker starts an
`Activity` for each piece of work, as section 23 states, so every line written
while it runs carries that trace id, and a call it makes to a provider carries it
as `traceparent`. Ruled by Francois on 2026-10-23.

**A line written outside a request and outside a piece of work carries none.**

**Every log line carries the name of the host that wrote it**, in a property named
`Application`.

**The sink writes both of those.** A formatter that writes every property does it
by itself, and an output template names each one.

**The pipeline is configured in code**, at
`<Solution>.Infrastructure/Logging/LoggingPipeline.cs`, in a static `Configure`
method that `LoggingRegistration` names. Section 9 states that rule. Every host gets
the pipeline from the call it already makes.

**Only the levels come from configuration**, read from the `Serilog` section with
`ReadFrom.Configuration`.

**`Serilog` is one of the two configuration sections that are not under
`<Solution>:`**, and it binds no options class. Section 14 names both.

**The pipeline is the one place that reads `IConfiguration`**, resolved from the
service provider. Section 14 states the rule everywhere else.

**The console is the default sink, and every host has it.** Its output template
is the actual one, not an illustration:

```
{Timestamp:u} | {Level:u3} | Application={Application} | Source={SourceContext} | TraceId={TraceId} | {Message:lj}{NewLine}{Exception}
```

**Every field is separated by a pipe and named on its left.** The timestamp, the
level and the message are not named: the first two identify themselves by their
format, and the message is everything after the last pipe.

**The timestamp is UTC**, which `{Timestamp:u}` writes. A log line never carries
the machine's own zone.

**The sink that exports telemetry is a decision in a plan**, and it is added to this same
pipeline.

The example below shows the pipeline and the call that names it. The path, the
class and the method are the actual ones, not illustrations:

```csharp
// <Solution>.Infrastructure/Logging/LoggingPipeline.cs
public static class LoggingPipeline
{
    private const string OutputTemplate =
        "{Timestamp:u} | {Level:u3} | Application={Application} | " +
        "Source={SourceContext} | TraceId={TraceId} | " +
        "{Message:lj}{NewLine}{Exception}";

    public static void Configure(IServiceProvider provider, LoggerConfiguration pipeline) =>
        pipeline.ReadFrom.Configuration(provider.GetRequiredService<IConfiguration>())
                .Enrich.WithProperty(
                    "Application",
                    provider.GetRequiredService<IHostEnvironment>().ApplicationName)
                .WriteTo.Console(outputTemplate: OutputTemplate);
}

// <Solution>.Infrastructure/Logging/LoggingRegistration.cs, inside the method
services.AddSerilog(LoggingPipeline.Configure);
```

The lines below are what that template writes. Their names and values are
illustrations, not names to look for:

```
2026-08-25 14:29:12Z | INF | Application=<Solution>.<Name>Api | Source=CancelOrder | TraceId=2d3b8a00393606912eeb546030e9e75a | Order 0198f3c1-1a2b-7c3d-8e4f-5a6b7c8d9e0f was cancelled
2026-08-25 14:31:07Z | ERR | Application=<Solution>.<Name>Api | Source=UnhandledExceptionHandler | TraceId=a4511eb27f6fb9dfbed3eae2bf8dc73a | An unhandled exception stopped the request
System.InvalidOperationException: could not reach the database
 ---> System.TimeoutException: the command timed out
   --- End of inner exception stack trace ---
2026-08-25 14:29:05Z | INF | Application=<Solution>.<Name>Api | Source=Program | TraceId= | <Solution>.<Name>Api has started
```

The last line is the startup line, which carries no trace id because no request
is in progress.

# 16. Async and cancellation

**Everything that does input or output is async.** A query, a call out of the
process and anything that waits on either are all async, and there is no
sync-over-async anywhere.

**Section 18 states the naming rule that goes with it.**

**`ConfigureAwait` is never called.**

**Our own signatures return `Task`, and never `ValueTask`.** Consuming a
`ValueTask` that a library returns is fine.

**`await using` is used wherever a type offers `DisposeAsync`.**

**A handler returns a materialised response, and never `IAsyncEnumerable<T>`.**

**`async void` is never written.**

**Nothing is started that outlives the request.** No `Task.Run` inside a request,
and nothing is started without being awaited. Section 21 names the symbols.

**Queries in a handler run one after another.** One `DbContext` performs one
operation at a time, so two are never in flight against it together.

The two forms below are never written and always written. Their names are
illustrations, not names to look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// never written. Two operations in flight on one context.
var counts = await Task.WhenAll(
    database.Employees.CountAsync(cancellationToken),
    database.LeaveRequests.CountAsync(cancellationToken));

// written instead. One after another.
var employeeCount = await database.Employees.CountAsync(cancellationToken);
var leaveRequestCount = await database.LeaveRequests.CountAsync(cancellationToken);
```

**Every handler declares `CancellationToken cancellationToken`, last, always.** It
is declared on an operation that makes one call and on an operation that makes
several. A handler which does no input or output and calls nothing declares no
cancellation token.

**Every query, every call out of the process and every method that accepts a
token is given the request token.**

**A token is never accepted and then dropped.** A method that takes one passes it
on.

**`SaveChangesAsync` takes `CancellationToken.None`, written out.** So does the
`CommitAsync` of the one explicit transaction section 10 names. Neither is ever
omitted, and neither is ever given the request token.

**An operation never makes a token of its own.** No `CreateLinkedTokenSource`,
and no token that the framework did not supply.

The example below shows the token on the query and on the outbound call, and
`CancellationToken.None` on the commit. Its names are illustrations, not names to
look for:

```csharp
// Example only. The names are illustrations, not names to look for.

// Features/Invoices/PayInvoice.cs
public sealed class PayInvoice
{
    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> HandleAsync(
        Guid id,
        <DatabaseName>Context database,
        IPaymentGatewayService paymentGateway,
        CancellationToken cancellationToken)
    {
        var invoice = await database.Invoices
            .AsTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (invoice is null)
        {
            return TypedResults.NotFound();
        }

        var reference = await paymentGateway.ChargeAsync(invoice.Total, cancellationToken);

        var result = invoice.MarkPaid(reference);

        // the switch on result, returning a 400 for each value that refuses.
        // Section 4 states it.

        await database.SaveChangesAsync(CancellationToken.None);

        return TypedResults.NoContent();
    }
}
```

**Section 7 states what a cancelled request produces.** The rest of it is here.

**A timeout and a cancellation cannot be told apart where they arrive.** Both are
an `OperationCanceledException`, both are a `TaskCanceledException` in the
ordinary case, and both carry a token that reports itself cancelled. An outbound
call that ran out of its own time reports a cancelled token even when the caller
passed `CancellationToken.None`, so asking whether the request was aborted
answers yes for a timeout that no client caused.

**So a deadline is handled where it is imposed.** The code that imposes a
deadline catches its own timeout and logs it, and what leaves that code is never
a bare cancellation.

**An `OperationCanceledException` that reaches the exception handler is the
client leaving.**

**An `OperationCanceledException` is never caught and rethrown as something
else.**

**A timeout is never represented as a bare cancellation.** It carries what timed
out and what the budget was.

**Two deadlines are imposed, and no others.**

**A database command timeout is set once, where the database connection is
configured, and never on a query.** It rides on the connection string, which
section 14 makes a member of the database's own options class. A query that
exceeds it is a failure and not a cancellation.

**Every call out of the process carries a timeout.** Section 9 states that
resilience is configured on the builder `AddHttpClient` returns, and which
settings apply is settled in a decision in a plan.

**A write long enough for cancellation to matter does not belong in a request.**

**Section 15 states the level each of those is logged at.**

# 17. C# language rules

**A comment explains why.**

**What the code does is carried by the name, never by a comment.**

**A comment that exists because a name is poor is a naming defect**, and the fix
is the name.

**No `///` comment is written anywhere in the solution.** Where something has to
be said about a class, a method or a member, it is an ordinary comment, and the
three rules above govern it.

**`GenerateDocumentationFile` is not set on any project.**

**What a consumer of the API reads is written at the route registration, with
`.WithSummary()`.** Section 19 states it.

**Every file declares its namespace file-scoped.** The block form is never
written.

**The namespace is the project name followed by the folder path**, so a file's
namespace and its folder are the same words. Section 18 states how a folder is
named.

**Every local variable is declared `var`.** A type is never written out on a
local.

**A member is expression-bodied when it is one expression, and has a block body
otherwise.**

**`is null` and `is not null`.** Never `== null` and never `!= null`.

**Collection expressions, pattern matching and the `field` keyword are used.**
The older form of each is not written in its place.

**Every class not designed for inheritance is `sealed`.** A class declared
`static` is not also `sealed`, because a class cannot be both.

**A record declares its members in a primary constructor. A class never has
one.**

**A request type and a response type are records.**

**An entity is a class, and never a record.**

**A tuple is used inside a method and never on a surface.** It never appears on a
response type or on anything a contract describes.

**`#region` is never written.**

**`dynamic` is never written.**

**`partial` appears only where a source generator requires it.**

**`required` is used on a response type and never on a request type.**

**Whether a request field must be supplied is the validator's rule**, which
section 8 states.

**A request type's nullability describes what the handler sees after the
validator has run, and not what the binder produced.** A non-nullable property
the caller left out of the body arrives as null, or as whatever value the
property was initialised to, and nothing is reported either way.

**Section 7 states what a non-nullable property does when its value arrives in
the URL**, which is not the same behaviour.

**The null-forgiving operator `!`, `#pragma warning disable` and
`[SuppressMessage]` each carry a reason at the point of use.**

**No suppression covers a whole file or a whole project.**

**No em dash appears in code or in a comment.** `CLAUDE.md` rule 4 states the
whole rule, which reaches further than code.

# 18. Naming

**Every name says what the thing is.** A name that says only that there is a
category here, without saying a category of what, is not a name.

**A name is never abbreviated.** Every word of a class, method, property,
parameter, variable, namespace, folder, table, column, index or route is written in
full, so `AuthorisedAmount` and never `AuthAmount`. The one exception is a term the
industry uses as a word of its own, such as `Api`, `Json`, `Http`, `Url` or `Id`,
which the acronym rule then writes in PascalCase. A name a provider fixes on the
wire is not ours, and it is mapped as the rule on a provider's notification states.
Ruled by Francois on 2026-10-24.

**`Kind` is never written**, alone or as the last word of a name. The word that
belongs there is the one saying what varies, such as an employment term or a
document format. Where that word cannot be found, the thing is not understood
well enough to be written, and that is a planning question rather than a naming
one.

**Casing:**

| What | Casing |
|---|---|
| a public member | PascalCase |
| a constant, both `const` and `static readonly` | PascalCase |
| a field, whatever its accessibility | camelCase, with no underscore prefix |
| a parameter | camelCase |
| a local variable | camelCase |

**An interface carries `I` and a type parameter carries `T`.**

**An acronym is PascalCase and never uppercase**, so `Api` and not `API`, `Http`
and not `HTTP`, `Id` and not `ID`.

**A boolean reads as a question**, so `IsActive`, `HasBalance`, `CanApprove`. A
member of an options class reads the same way, and the configuration key is that
member's name.

**A `DateOnly` property is suffixed `Date`, a `TimeOnly` property is suffixed
`Time`, and a `DateTimeOffset` property is suffixed `At`.** `StartDate`,
`EndTime`, `PublishedAt`.

**An enum is singular and PascalCase**, unless it carries `[Flags]`.

**`this.` is written where a field and a parameter share a name, and nowhere
else.**

**Every async method carries the `Async` suffix, and there is no exception.**
Section 16 states which methods are async.

**A method is named for the action it performs and what that action applies
to.** The action may be more than one word, so a state change carries the state
it moves to.

**The name is read at the call site.** It says what happens and to what, with
nothing missing and nothing the call site already shows repeated.

**Where the method's own class is named for what the action applies to, the
method is the action alone.** An instance method acting on its own object's
state always meets this.

**Where the action applies to something the class is not named for, the method
names it**, even where the class's name carries one of the same words.

**An extension method always names what the action applies to.** Its call site
shows the receiver and never the class.

**A read, change, store or removal of a record uses `Get`, `Update`, `Save` or
`Delete`**, and no other word names those four actions.

**Every other action takes the word that names it, and there is no list to
choose from.**

**`Get` also names reading or producing a value that is not a record**, and the
name carries that value.

**`Get<Noun>` alone is reserved for a record lookup by the record's own row
id.** A lookup keyed by anything else carries that field's name,
`Get<Noun>By<Field>`, never a bare `Get` for a key that is not the id.

**A record lookup returns the row it found, never one of that row's own
fields.** A lookup returns everything its key identifies, and a caller reads
off it whatever field it needs. A record lookup is never written per field,
however many a row carries.

**A method a framework interface or base class declares carries the name it
declares.**

**A method that performs a framework action, or that extends a framework
builder, takes the framework's own word for it.** So `Map` beside `MapGet`,
`Use` beside `UseCors`, `With` beside `WithTags`.

**A method that decides whether an operation is allowed, without making the
change, is named `Can` plus the operation its result enum is named for.**
Section 4 names that enum `<Operation>Result`, so a policy returning
`PlaceOrderResult` is `CanPlaceOrder`. A method that makes the change is named
for the action instead.

**A check for whether a record exists, rather than a lookup of it, returns
`bool` and carries no `Get`.** It's named for what it checks plus `Exists`,
and it already reads as a question, so it takes no `Is`/`Has`/`Can` prefix
either.

**A file under `Features` is named for its operation, and an operation's name
repeats its resource**, so `GetCustomer` and never `Get`.

**An operation file holds four types, each named for the operation plus what it
is:**

| Type | Name |
|---|---|
| the operation class | `<Operation>` |
| the request | `<Operation>Request` |
| the response | `<Operation>Response` |
| the validator | `<Operation>Validator` |

**None of them is nested inside the operation class.**

**One row of a paged response is that operation's response type**, so a list
operation returns `PagedResponse<T>` holding its own `<Operation>Response`.
Section 7 states the container.

**A class is named for what it is about, plus what it is.** What it is comes
last.

**Where a rule below, or in another section of this document, states what a class
of that kind is, that word is used.**

**Where no rule states one, the word is taken in this order: this document's own
word for the thing, then the framework's own word for it, then the noun formed
from the one action the class performs.**

**Two classes carry one half only.** An operation class is the operation's own
name and takes no second part, which the table above states. The `Endpoints`
class at an API project's root takes no first part, because the project it sits
in is what it is about, which section 6 states.

**A class that reaches outside the process is `<Concern>Service`**, in
Infrastructure, with `I<Concern>Service` beside it.

**A class that computes and reaches nothing outside is `<Concern>Manager`**, in
ApiCommon, with `I<Concern>Manager` beside it.

**An entity's class carries the suffix `Entity`.** `Entity` is added to
whatever name this document already derives for that entity, an ordinary
table's own name, or a reference table's name plus `Type`. Every other rule
that names something from an entity, a policy class, a configuration class, a
table, a manager's own concern, continues to use that name without `Entity`.
The suffix reaches only the class itself, and its file, since a file is named
for its class.

**Section 4 names a policy class, and the enum a domain method returns.**

**A class implementing a framework interface is named for what it does, plus the
noun that interface carries, with the framework's own qualifier dropped.** Where
the interface's name is a verb phrase carrying no noun, the whole name after the
`I` is carried instead, which is what gives section 14 its `ValidateOptions`.

The four below are the actual classes sections 7, 8 and 19 place, not
illustrations:

| Interface | The class |
|---|---|
| `IEndpointFilter` | `ValidationFilter` |
| `IOpenApiSchemaTransformer` | `ProblemCodeSchemaTransformer` |
| `IOpenApiOperationTransformer` | `OperationSecurityTransformer` |
| `IExceptionHandler` | `UnhandledExceptionHandler` |

**The drop is not mechanical.** `IOpenApiSchemaTransformer` drops `OpenApi`
because that is the technology, and `IExceptionHandler` keeps `Exception`
because that is part of the noun.

**A class holding extension methods is named for its concern plus `Extensions`**,
unless a settled rule already names it. Sections 6 and 9 name the endpoint and
registration classes, and neither takes `Extensions`.

**Where the first part of a name is something no rule and no section of this
document already names, it is named in that feature's planning session and
recorded in its plan.** It is never named by whoever happens to be writing
the code. The second part always comes from the rules above.

**Every folder is PascalCase**, and what it is named for follows what it holds:

| Folder | Its name |
|---|---|
| a resource folder under `Features` | the resource, plural |
| a concern folder | the concern |
| a folder under `Managers` or `Integrations` | the class name without its suffix |
| a folder in `<Solution>.Domain` | the schema |
| a folder holding every file of one kind | that kind, plural |
| a tool folder under `Code/Tools/` | the tool's concern |

**A route is lowercase**, and a segment of more than one word is kebab-case.

**A resource segment naming a collection is plural**, and one member of it is
addressed by its key.

**A resource folder and its route segment are the same word in two casings**, so
`Features/LeaveRequests/` serves `/leave-requests`.

**No verb appears in a path.** The file names the operation, the route names the
resource, and the HTTP method carries the verb.

**An action that is not create, read, update or delete is a noun sub-resource.**

**No route segment stands for the caller.** There is no endpoint naming the
caller and no equivalent of one.

**The route parameter of the route's own resource is `id`.**

**A sub-collection segment is the linked table's name, kebab-case and plural, and
its key is that table's name plus `Id` in camelCase.**

**The `:guid` constraint belongs to the C# route template and to nothing else.**
It is never written into a route as a client calls it, and never into a contract
path. Section 11 states the constraint.

The example below shows the derivation from the file to the route to the
contract. Its names are illustrations, not names to look for:

```
Example only. The names are illustrations, not names to look for.

Features/Customers/ListCustomers.cs      GET   /v1/customers
Features/Customers/GetCustomer.cs        GET   /v1/customers/{id}
Features/Employees/InviteEmployee.cs     POST  /v1/employees/{id}/invitations
Features/Orders/RemoveOrderLine.cs       DELETE
                                         /v1/orders/{id}/order-lines/{orderLineId}

Docs/Contracts/<Name>Api/v1/customers/{id}/get.openapi.json
```

**A property is PascalCase in C# and camelCase on the wire.** The framework
applies that policy, and nothing configures it.

**A member added to a response body is written to the wire exactly as it is
spelled**, because the policy does not reach it. So `code`, and anything added
beside it, is spelled in camelCase in the code that adds it. Section 7 states
where members are added.

**A request whose shape a provider's contract fixes, being a notification the
provider sends to an endpoint registered with it, names each property in C# for
what it is, and carries `[JsonPropertyName]` with the provider's exact name on the
wire.** No other type carries `[JsonPropertyName]`. So the frozen fragment
publishes exactly what the provider sends, and the contract test proves it. The
property name resolver section 8 places reads a property's `[JsonPropertyName]`
where it carries one, so the per-field map names the field as the provider sent
it. Ruled by Francois on 2026-10-24.

**Every database identifier is PascalCase.** A test's cloned database is outside
this rule. It is named with a new `Guid` in its 32-character form, in double
quotes.

**The database role a host connects as is that host's project name with the dot
removed, plus `Role`.** So `<Solution>.Migrator` connects as
`<Solution>MigratorRole`, and `<Solution>.<Name>Api` as
`<Solution><Name>ApiRole`.

**A table name is singular**, and it is the entity's own name.

**The table and its schema are stated with `ToTable(name, schema)`**, the name
first, in that entity's configuration class. Section 10 states where those
classes live.

**Every schema is named explicitly, and `public` is never used.**

**Which schema a table belongs to is settled in that feature's planning session
and recorded in its plan**, never here.

**Every table's primary key column is `Id`.**

**A foreign key column is `<ReferencedTable>Id`. Where one table refers to the
same table more than once, each such column is a word naming its role, then
`<ReferencedTable>Id`, such as `FromPaymentStateTypeId` and
`ToPaymentStateTypeId`, and its foreign key is named for that column.** Ruled by
Francois on 2026-10-23.

**A reference table, and the entity that maps it, is named for what it lists plus
`Type`. The enum naming its rows is that name without `Type`.** Section 13 states
what qualifies as a reference table.

**Every key, index and constraint carries its name explicitly:**

| Object | Pattern |
|---|---|
| Primary key | `PK_<Table>` |
| Foreign key | `FK_<Table>_<Column>` |
| Index | `IX_<Table>_<Columns>` |
| Unique index | `UX_<Table>_<Columns>` |
| Check constraint | `CK_<Table>_<Rule>` |

**Where an index covers more than one column, `<Columns>` is the column names
joined by underscores, in the index's column order.** As an example, with the
names illustrations rather than names to look for, an index on `Employee` over
`LastName` and then `FirstName` is `IX_Employee_LastName_FirstName`.

**The name is written even where EF would produce the same one.**

# 19. The published API surface

**An API serves the OpenAPI document it generates.** Nothing serves the frozen
fragments, and no code assembles a document from them.

**An API generates one document per route version, named `v` followed by that
version.** Each document holds only the routes of its own version, so a consumer
reads the version they are on and nothing else.

**One document is registered per version the API serves**, in ApiCommon's
extension method, each one filtered to the routes whose first segment is its own
version. Section 9 states that method and shows the registration.

**Every API states whether it publishes.** An API that must not publish is
switched off in that deployment's configuration, and no code names an
environment.

**When it is off, neither the document route nor the documentation UI is
registered at all.** Neither is registered and then hidden.

**The switch and the document's own heading are one options class**, at
`<Solution>.ApiCommon/OpenApi/OpenApiOptions.cs`, binding `<Solution>:OpenApi`.
Section 14 states how an options class is bound and validated.

| Member | What it does |
|---|---|
| `IsEnabled` | registers the document route and the UI, or neither |
| `Title` | the title the document publishes |
| `Description` | the description the document publishes |

**All three are supplied per API, and the host does not start without any one of
them.**

**`IsEnabled` is a nullable `bool`.** A non-nullable one is never missing, so the
host would start on a configuration that says nothing about whether the API
publishes.

**The document's version is the document's name, and is not configured.** Nothing
can set it to a value the document contradicts.

**One document transformer sets those three members of the document**, and no
operation sets any of them.

**Both route calls live in one method**, `MapDocumentation`, which reads the
switch and registers both or neither. **`Endpoints.cs` calls it once**, which
section 6 shows.

**The documentation page's Content-Type carries its character set.** The Scalar
call takes no configuration, so `MapDocumentation` wraps the page's response in
a small piece of middleware, registered on the same route, that adds
`charset=utf-8` to the `Content-Type` header when the page does not already
carry one.

**The documentation route's Content-Security-Policy is the one section 7
states for it, and not the one every other response carries.** The same
wrapper `MapDocumentation` registers sets it, replacing the header the shared
security-headers middleware wrote, since the page has to run its own inline
start script and every other response does not.

**One operation transformer writes each operation's security requirement, and
the schemes it names, into the generated document**, at
`<Solution>.ApiCommon/OpenApi/OperationSecurityTransformer.cs`, implementing
`IOpenApiOperationTransformer` and registered beside the schema and document
transformers. It reads the operation's authorization metadata rather than
anything an operation states by hand, and no operation declares its own
`security` field.

**The documentation UI is Scalar**, from the `Scalar.AspNetCore` package,
registered with `MapScalarApiReference`.

**Neither route is configured.** The document is served at
`/openapi/<version>.json` and the page at `/scalar/<version>`.

**The Scalar call takes no configuration.** No theme, no title and no options
object.

The example below shows the three files this section places. The paths, the
classes and the members are the actual ones, not illustrations:

```csharp
// <Solution>.ApiCommon/OpenApi/OpenApiOptions.cs
public sealed class OpenApiOptions
{
    [Required]
    public bool? IsEnabled { get; init; }

    [Required]
    public string Title { get; init; } = string.Empty;

    [Required]
    public string Description { get; init; } = string.Empty;
}

// <Solution>.ApiCommon/OpenApi/ApiInformationDocumentTransformer.cs
public sealed class ApiInformationDocumentTransformer : IOpenApiDocumentTransformer
{
    private readonly OpenApiOptions options;

    public ApiInformationDocumentTransformer(IOptions<OpenApiOptions> options) =>
        this.options = options.Value;

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info.Title = options.Title;
        document.Info.Description = options.Description;
        document.Info.Version = context.DocumentName;

        return Task.CompletedTask;
    }
}

// <Solution>.ApiCommon/OpenApi/OpenApiExtensions.cs
public static class OpenApiExtensions
{
    public static void MapDocumentation(this WebApplication app)
    {
        if (app.Services.GetRequiredService<IOptions<OpenApiOptions>>().Value.IsEnabled
            is not true)
        {
            return;
        }

        app.MapOpenApi();
        app.MapScalarApiReference();
    }
}
```

**Every operation declares `.WithSummary()`**, and the string is the `summary` its
frozen contract carries.

**The summary is one sentence, written for the consumer of the API.** It says what
the operation does for the caller, and never how it is implemented.

**An operation declares `.WithDescription()` where its frozen contract carries a
`description`, and nowhere else.**

**Neither string is worded while the code is written.** Both are fields of the
frozen contract, so both are settled before the operation is built.

**Each resource group declares its tag, and the tag is the resource's route
segment**, so `/leave-requests` gives `leave-requests`. Section 6 shows where a
group is created and section 18 states how the segment is derived.

**No operation declares an `operationId`, and `.WithName` is never called.**

# 20. Anti-speculation

**This section governs behaviour and machinery, and never structure.** The
project set, the reference graph and every folder path are fixed by sections 3,
4, 9, 10 and 14. A project or a folder that one of those sections places is
created because that section places it, and no rule here delays it.

**Nothing is written that no increment needs.** Every part of an increment
answers to a step of the implementation order, and the test for a class, a method
or a member is which step asked for it.

**A need nobody has does not buy code now.** Something that might be wanted later
is written when it is wanted, in the increment that wants it.

**Nothing is written to prevent a mistake that nothing in the solution can
make.** A constructor nobody calls and a member nobody calls are the same shape.

**An interface, a base class or a type parameter is written where a rule in this
document requires one, and nowhere else.** Sections 3, 9 and 18 require an
interface for a service, for a manager and for what a handler resolves. The test
is which rule requires the type, never how many callers it has.

**A setting exists because something asked for it.** A value that nothing asked
to vary is written in the code, and does not become a member of an options class
or a key in an `appsettings` file. Section 14 states how a setting that was asked
for is bound and validated.

**A condition the caller cannot produce gets no check, no branch and no catch.**
Where the route constraint, the validator or the frozen contract has already
excluded a value, the handler does not test for it again. Section 8 states what a
validator guarantees, and section 11 states the route constraint.

**An id that names no record is not one of those conditions.** The route
constraint fixes the shape of the value and not whether a row exists, so the
operation still handles the record it did not find. Section 11 states what it
returns.

**The same holds below the handler.** A domain method does not test for a state
the entity's own invariants prevent, and a manager handles what its parameters
can carry and nothing more. Section 4 states the invariants.

**Section 7 states what happens when a condition the contract makes impossible
happens anyway.** Nothing here changes it.

# 21. Banned constructs

**Three analyzer packages are used, and no other is added:** the analyzers that
ship with the .NET SDK, `Microsoft.CodeAnalysis.BannedApiAnalyzers`, and
`SonarAnalyzer.CSharp`.

**No analyzer rule is enabled in bulk.** `AnalysisMode` is not set, so the
built-in analyzers stay at their default set, and every rule this document
relies on is raised by its id in `.editorconfig` with an explicit severity.
Section 2 states the build settings a severity needs before it reports anything.

**`SonarAnalyzer.CSharp` is the one that is on without being raised.** Its rules
ship enabled when the package is referenced, so a Sonar rule that demands
something this document refuses is switched off by its id in `.editorconfig`,
and it is never satisfied by changing the code.

**Three settings are deliberately off, and an increment does not switch one on:**

| Setting | State | The rule it would fight |
|---|---|---|
| `AnalysisMode` | not set | this section, above |
| `CA2007`, forward `ConfigureAwait` | not raised | section 16 |
| `SonarAnalyzer.CSharp` `S1118` | switched off | section 5 |

## The banned symbols

**Every ban here is a rule stated somewhere else in this document**, named in the
third column, which is where the rule and everything around it are. This table
carries the symbol the analyzer matches on and what is written in its place.

| Symbol | What is written instead | Stated in |
|---|---|---|
| `T:System.DateTime` | `DateTimeOffset`, `DateOnly` or `TimeOnly` | 12 |
| `P:System.DateTimeOffset.Now`, `P:System.DateTimeOffset.UtcNow` | `GetUtcNow()` on an injected `TimeProvider` | 12 |
| `M:System.Threading.Tasks.Task.Run` | the call, awaited where the handler needs it | 16 |
| ``P:System.Threading.Tasks.Task`1.Result``, `M:System.Threading.Tasks.Task.Wait`, `M:System.Runtime.CompilerServices.TaskAwaiter.GetResult`, ``M:System.Runtime.CompilerServices.TaskAwaiter`1.GetResult`` | `await` | 16 |
| `T:System.Threading.CancellationTokenSource` | the token the handler was given | 16 |
| `M:System.Net.Http.HttpClient.#ctor` | the provider's own interface, taken as a handler parameter | 9 |
| `T:Microsoft.AspNetCore.Http.Results` | `TypedResults` | 7 |
| `M:Microsoft.AspNetCore.Http.TypedResults.Text` | one of the other `TypedResults` calls | 7 |
| `M:System.Uri.UnescapeDataString`, `M:System.Web.HttpUtility.UrlDecode` | nothing; the value arrived decoded | 8 |
| `P:System.Net.Http.HttpClientHandler.ServerCertificateCustomValidationCallback`, `P:System.Net.Http.SocketsHttpHandler.SslOptions` | nothing; the default check | 9 |
| `T:Serilog.Log`, `T:Serilog.ILogger` | `ILogger<T>`, taken as a parameter | 15 |
| `M:Microsoft.Extensions.Logging.LoggerExtensions.LogTrace` | `LogDebug`, where the line is for development | 15 |
| `T:Microsoft.Extensions.Configuration.IConfiguration` | an options class, read through `IOptions<T>` | 14 |
| ``T:Microsoft.Extensions.Options.IOptionsMonitor`1`` | `IOptions<T>` | 14 |
| `M:Microsoft.EntityFrameworkCore.ModelBuilder.ApplyConfigurationsFromAssembly` | one `ApplyConfiguration` call per entity | 10 |
| `M:FluentValidation.ServiceCollectionExtensions.AddValidatorsFromAssemblyContaining`, `M:FluentValidation.ServiceCollectionExtensions.AddValidatorsFromAssembly`, `M:FluentValidation.ServiceCollectionExtensions.AddValidatorsFromAssemblies` | `WithValidation<T>` on the route | 8 |
| `M:Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.FromSqlRaw`, `M:Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw`, `M:Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync`, `M:Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.SqlQueryRaw` | the interpolated form of the same call | here |

**A method entry is written once for each of that method's overloads.** An entry
naming a method with no parameter list matches none of them and reports nothing,
and nothing reports an entry that matched nothing.

**The entries live in `BannedSymbols.txt`, at the solution root.**

**`Testing.md` states the separate list the test projects carry.**

**The raw SQL methods are the one rule this section states rather than points
at.** The four methods whose names end in `Raw` are banned. The interpolated and
parameterised forms of the same calls are written instead, being `FromSql`,
`FromSqlInterpolated`, `ExecuteSql`, `ExecuteSqlAsync`, `ExecuteSqlInterpolated`,
`ExecuteSqlInterpolatedAsync` and `SqlQuery`.

## What a banned entry cannot carry

**A banned entry catches less than the rule it enforces.** Each rule below is
stated in full in the section named beside it, and nothing on the build sees the
part named here.

| Rule | What a banned entry cannot carry | Stated in |
|---|---|---|
| A `DateTime` held rather than made | The ban reaches where a value is made. A `DateTime` declared as a field, property, parameter or return type compiles | 12 |
| A `DateTime` a library hands over, converted at that boundary | An entry cannot carry an exception to itself | 12 |
| `BeginTransactionAsync` outside the one case | The same. The call has one legitimate use, so no entry is written for it | 10 |
| An `OperationCanceledException` caught and rethrown as something else | The same. The catch has a legitimate site | 16 |
| `IOptionsSnapshot<T>` | It declares only `Get`, so the ordinary read of `.Value` resolves to `IOptions<T>` and an entry on it never fires | 14 |

**A construct that is in neither table is not permitted by its absence.** Every
rule in this document holds whether or not an analyzer can see it.

# 22. Enforcers

Every rule in this document has an enforcer, and this section names it. Where
that thing is nothing, the last list says so, because a column that claims
coverage it does not have is worse than no column.

The three parts below are one question asked three ways: is the rule caught
without anyone reading the increment, only by reading it, or not at all.

## What is caught without reading the increment

| Rule | Enforcer |
|---|---|
| A `PackageReference` never carries a version | `NU1008`, an MSBuild error |
| No API references another API, nothing but `<Solution>.IntegrationTests` references an API, Infrastructure never references ApiCommon, and code in ApiCommon never reaches into Infrastructure | The project reference graph `create-solution` and `create-project` write, and `feature-review` |
| Every operation is registered | The contract tests, which walk the frozen fragments and fail on a fragment with no live route |
| Every 400 is declared with `ProducesValidationProblem`, never `ProducesProblem` and never both | The contract tests, which compare the generated document against the frozen fragment |
| Every response an operation produces is declared where its route is declared | The contract tests, for the same reason |
| Every constant names a row in exactly one of the two tables, and every row in either has a constant | The error code test, which `Testing.md` states |
| An operation's code with no message row was used | The `Error` line the fallback writes |
| No operation catches `Exception` | CA1031, raised by its id. It reaches every file, so the `catch` in a host's `Program.cs` sits between `#pragma warning disable CA1031`, with its reason, and `#pragma warning restore CA1031` |
| `RouteHandlerOptions.ThrowOnBadRequest` is pinned to false | `create-solution`, which writes the wiring, and `feature-review` |
| `AddProblemDetails`, `UseExceptionHandler` and `UseStatusCodePages` are all called | `create-solution` and `create-project`, which write the wiring |
| The `code` member reaches both problem schemas | The schema transformer, registered once, and the contract tests |
| A handler produces its results with `TypedResults` | Banned-symbols analyzer, `RS0030` |
| Every rule in a validator carries `WithMessage` naming an `ErrorMessageCode` constant | `feature-review`, and the `Error` line the fallback writes when a named code has no row |
| No validator is found by a scan | Banned-symbols analyzer, `RS0030` |
| Each key in the per-field map is camelCase | `create-solution`, which writes the property name resolver into the wiring |
| The `WithValidation` call is on the route | `feature-review`, and the validation failure branch `Testing.md` requires a test for whenever the plan names one |
| A service a handler takes is registered | The first request to any route. The unregistered parameter is inferred as a body, so it breaks the whole endpoint data source rather than only its own route |
| Anything touching the `DbContext` is scoped | The container's validation when the host builds, which refuses a singleton taking a scoped service. It runs in the Development environment only |
| A read is not tracked | The context's `QueryTrackingBehavior.NoTracking` default, which removes the rule rather than policing it |
| A write that forgot `AsTracking` | That increment's own test. Nothing is written and nothing throws |
| `ApplyConfigurationsFromAssembly` is never called | Banned-symbols analyzer, `RS0030` |
| An operation taking a key declares a 404, and never a 400 for a malformed id | `feature-review`, and the contract tests, which fail on a declared response the frozen fragment does not carry |
| A `DateTime` is never constructed, and no static `DateTime` member is read | Banned-symbols analyzer, `RS0030` |
| Now is never read from a static `DateTimeOffset` member | Banned-symbols analyzer, `RS0030` |
| Every migration has a `Down` | The migration test, which `Testing.md` states |
| Every enum member has a row, and every row has an enum member | The reference table test, which `Testing.md` states |
| A reference entity's key property is declared as the enum and not as `int` | The model builder, which refuses to build a relationship whose principal key is not a compatible type |
| A new value is a new enum member and a new row, in the same increment | `feature-review`, and the reference table test |
| An API exits at startup when the database is missing a migration it knows about | `create-project`, which writes the check once, and `feature-review` |
| `<Solution>.Migrator` returns zero when the schema is up to date | `create-solution`, and the deployment stage, which fails on anything else |
| `IConfiguration` is never read | Banned-symbols analyzer, `RS0030` |
| `IOptionsMonitor<T>` is never used | Banned-symbols analyzer, `RS0030` |
| A misspelled or missing section name | The host, which does not start, where the member carries `[Required]` |
| An `IValidateOptions<T>` class that refuses | The host, which does not start |
| No registration method takes a configuration parameter | The compiler, for the three signatures section 9 fixes, and `feature-review` for a new one |
| A section an options class binds is `<Solution>:` plus the class name without `Options` | `feature-review`, and the host, where a member of that class is `[Required]` |
| A log message is never built with string interpolation | `SonarAnalyzer.CSharp`, rule `S2629` |
| There is no static logger | Banned-symbols analyzer, `RS0030` |
| `Trace` is never written | Banned-symbols analyzer, `RS0030` |
| The non-generic `ILogger` is never taken as a parameter | The first request to any route in the host, which is the failure any unregistered service produces |
| An exception is passed to the log call and never rendered into the message | `SonarAnalyzer.CSharp`, rule `S6667`, which reaches a catch clause and not an `IExceptionHandler` |
| The console sink and its output template | `create-solution`, which writes the pipeline, and `feature-review` |
| The log timestamp is UTC | The output template, through `{Timestamp:u}` |
| The pipeline is the one place that reads `IConfiguration` | Banned-symbols analyzer everywhere else, `RS0030` |
| Every log line carries the trace id and the `Application` property | The pipeline, which `create-solution` writes. A line cannot be written without them |
| A blocking call on work already made asynchronous | Banned-symbols analyzer, `RS0030` |
| `Task.Run` inside a request | Banned-symbols analyzer, `RS0030` |
| `SaveChangesAsync` or `CommitAsync` written with no token at all | CA2016, raised by its id |
| A token accepted and then not passed on | The same CA2016 |
| An operation never makes a token of its own | Banned-symbols analyzer, `RS0030` |
| A handler returns a materialised response and never `IAsyncEnumerable<T>` | `feature-review`, and the contract tests, because a frozen fragment describes a body |
| Every file declares its namespace file-scoped | IDE0161, which does not reach a migration file the tool generates, marked `generated_code = true` as section 13 states |
| The namespace is the project name followed by the folder path | IDE0130 |
| Every local variable is declared `var` | IDE0007 |
| A member is expression-bodied when it is one expression | IDE0022 |
| A class declared `static` is not also `sealed` | The compiler, `CS0441` |
| Every async method carries the `Async` suffix | IDE1006, through a naming rule keyed on the `async` modifier |
| Every casing rule in section 18's table | IDE1006, through the `.editorconfig` naming rules |
| `this.` is written where a field and a parameter share a name | The compiler, `CS1717` with `CS0649`, and section 2's `TreatWarningsAsErrors` |
| The four types in an operation file, and none nested | `feature-review`, and the contract tests, since a nested type publishes a schema under its own short name |
| A paged response's row type | `feature-review`, and the contract tests |
| Every route rule, and the route parameter | The contract, approved at the planning gate and then frozen, and the contract tests, which prove the running code matches it |
| A property's wire name | The framework's own policy, which needs no configuration, and for a provider's notification the contract tests |
| Table, schema, key, index and foreign key naming | The database naming test, which reads EF's built model and which `Testing.md` states |
| A reference table's `Type` suffix, and its enum without it | `feature-review`, and the reference table test |
| Which schema a table belongs to | Planning, and the plan |
| A class no rule in this document reaches | Planning, which writes the name into the plan, and `feature-review` |
| Every operation declares `.WithSummary()`, and the string is its fragment's | Planning, which writes the fragment, and the contract tests, which compare the two |
| `.WithDescription()` only where the fragment carries a `description` | The contract tests, in both directions |
| The tag is the resource's route segment | Planning, which writes the tag into the fragment, and the contract tests |
| No `operationId`, and `.WithName` is never called | The contract tests, since `.WithName` emits an `operationId` the fragment does not carry |
| `IsEnabled`, `Title` and `Description` are supplied per API | The host, which does not start |
| `IsEnabled` is a nullable `bool` | The host, and only in this shape. A non-nullable value type is never missing |
| One document per route version, each filtered to its own routes | `create-solution`, which writes the registration, and `feature-review` |
| The switch is stated per API and turned off by configuration | `create-solution` and `create-project`, which write it, and `feature-review` |
| The document's version is the document's name | `create-solution`, which writes the transformer once, and `feature-review`. It is not a configured member, so nothing can set it to a value the document contradicts |
| A private member nobody uses | IDE0051, raised by its id |
| A Sonar rule that fights a rule in this document | The build, which is red until that rule is switched off by its id |
| `S1118` is switched off | The build, which is red on every operation file until it is |
| The four raw SQL methods | Banned-symbols analyzer, `RS0030` |
| An operation declaring neither `.RequireAuthorization` nor `.AllowAnonymous` | The fallback authorization policy, which refuses the first request to any route carrying neither |
| A handler returning JSON it built as text | Banned-symbols analyzer, `RS0030`, on `TypedResults.Text` |
| An operation decoding a value the framework already decoded | Banned-symbols analyzer, `RS0030`, on `Uri.UnescapeDataString` and `HttpUtility.UrlDecode` |
| A provider's service turning off or replacing the certificate check | Banned-symbols analyzer, `RS0030`, on `ServerCertificateCustomValidationCallback` and `SslOptions` |
| The `security` field on each operation in the generated document | `OperationSecurityTransformer`, registered once, and the contract tests |
| Every response carrying `Cache-Control`, HSTS, `X-Content-Type-Options` and the CSP | `create-solution`, which writes the one middleware that sets them, and `feature-review` |
| The documentation page's Content-Type carrying its character set | `create-solution`, which writes the wrapper around `MapDocumentation`, and `feature-review` |
| Every API's size limit, rate limit and allowed origins supplied per API | The host, which does not start, where each limit carries `[Range(1, int.MaxValue)]`. Nothing at startup requires `AllowedOrigins` |
| Every log line written inside a request carrying the caller | The enrichment, written once into the logging pipeline, and `feature-review` |
| A value written into a log line beginning a new line | The output template, through `{Message:lj}` |
| The caller's network address read from what is in front of the API | `create-project`, which writes `UseForwardedHeaders` once, and `feature-review` |
| A request body carrying the same member twice | `JsonSerializerOptions.AllowDuplicateProperties` set to `false`, which `create-solution` writes |
| `packages.lock.json` missing a package a project now restores | `NU1004`, when restore runs in locked mode |

## What is caught only by reading the increment

**Every rule this document states that the table above does not name, and that
the list below does not name, is caught by `feature-review` and by nothing
else.** Nothing on the build sees any of them, and no test proves any of them.

That is most of this document. A rule reaching this part is not a weaker rule,
and it is not optional because the build is quiet about it.

## What nothing catches

**Four rules have no enforcer at all.** Nothing on the build sees them, and
reading the increment does not show them either.

- **A public member nobody calls.** Nothing on the build knows whether something
  outside the project calls it, and the caller is not in the increment that adds
  the member. Section 20 states the rule.
- **A method entry in `BannedSymbols.txt` written with no parameter list.** It
  matches none of that method's overloads and reports nothing, and nothing
  reports an entry that matched nothing. Section 21 states the rule.
- **A style rule in `.editorconfig` written with no explicit severity.** It
  reports nothing, and nothing reports that it reported nothing. Section 2 states
  the rule.
- **A rule this document relies on that is left out of `.editorconfig`.** A rule
  reports nothing until it is raised by its id, so one that is never raised is
  silent and nothing reports the silence. Section 21 states the rule.

# 23. Background work

**Background work runs in a worker, hosted in an API project.** There is no
worker project. A project per worker would multiply projects, and one project
hosting many workers is deployed the same way an API is. Ruled by Francois on
2026-10-23.

**A worker lives at `<Solution>.<Name>Api/Workers/<Concern>/<Concern>Worker.cs`,
and derives from `BackgroundService`.** Its options class,
`<Concern>WorkerOptions`, sits beside it and binds `<Solution>:<Concern>Worker`.
These are the actual paths, not illustrations.

**`Workers.cs`, at the API project's root, registers every worker, and
`Program.cs` calls it once**, the same way it calls `MapEndpoints`. A new worker
adds one line to `Workers.cs`. Workers are registered after the startup check that
refuses a database behind the API, so no worker starts against one. `Workers.cs`
also sets the host's shutdown timeout, below.

**Every worker's options carry `IsEnabled`, a nullable `bool`, required.** One
image can then run as a service that answers requests with every worker off, and
as a service that does background work with every worker on. Locally and in tests
every worker is on.

**A worker takes `IServiceScopeFactory`, the interface of each provider it calls,
and `TimeProvider`.** It creates one scope for each piece of work and takes the
context from it, so no context outlives one piece of work.

**A worker fetches and persists. The Domain decides.** A worker holds no `if`
that expresses a business rule, the same as section 4 states for a handler.

**A worker finds its work by polling the database**, on a `PeriodicTimer` driven
by `TimeProvider`, at an interval its options state. A row that is due for work
carries the time it is next due, in a column whose name ends in `At`, with an
index over it.

**A worker claims rows with one SQL statement.** It selects a batch of due rows
`FOR UPDATE SKIP LOCKED`, oldest due first, limited to the number of places the
worker has free, moves each row's due time forward by a lease, adds one to its
attempt count, and returns their ids. It is written with the interpolated
`SqlQuery`, every value a parameter, which section 21 allows. The lock lasts only
that statement. The lease protects the row from then on, and a lapsed lease makes
the row due again. The lease is longer than the longest call a piece of work
makes. This is the one place a worker writes SQL, and because the statement holds
table and column names as text, the worker's integration test runs it against the
database, as `Testing.md` section 5 states.

**No two workers work on one row at once.** `SKIP LOCKED` gives each due row to
one claim, and the lease keeps it from being claimed again while it is worked on.

**A worker claims only into free places**, so a claimed row is always being
worked on.

**A worker saves once after each call it makes outside the process.** The save
holds what the call returned, the change the Domain made, and the row's next due
time. Nothing is saved before the call except the claim. On `SaveOutcome.Conflict`
it reloads the row, asks the Domain again, and saves again, up to three times in
all, then logs at `Error` and leaves the row to its lease.

**A worker catches `Exception` at exactly two places**, each between
`#pragma warning disable CA1031` and its restore, with its reason on the disable
line:

1. Around one piece of work. It logs at `Error` with the row's id, then in one
   `ExecuteUpdateAsync` adds one to the row's count of unexpected failures and
   moves its due time forward by 1, 5, then 15 minutes. After five in a row the
   same update makes the row due at once. The next piece of work asks the Domain
   before anything else, and the Domain decides what follows by the rule its plan
   states, recording any change of state as it records every other; the worker
   logs at `Error`. Ruled by Francois on 2026-10-23. The count goes back to zero when an
   attempt records an outcome. If that update fails too, the lease brings the row
   back.
2. Around one polling pass. It logs at `Error` and waits for the next interval.

**The stopping token's cancellation is never caught.** Anything that escapes the
loop stops the host, and the environment starts a fresh one.

**On the stop signal a worker stops claiming at once, and finishes the work it
has claimed.** The stopping token goes only to the wait between passes and to the
claim. Claimed work makes its call and its save with `CancellationToken.None`,
bounded by its own time limits, and `StopAsync` waits for it. The host's shutdown
timeout, set in `Workers.cs`, is longer than the longest call a piece of work
makes plus its save. **Correctness never depends on a graceful stop**: the lease
and the next attempt cover a host that is killed.

**Each piece of work runs inside an `Activity` the worker starts for it**, so its
log lines carry one trace id, as section 15 states, and a call to a provider
carries it as `traceparent`.

**A worker's log lines name a row by its id only**, and never carry data at
either protection level.

**Every 30 seconds each worker logs one `Information` line with the number of
rows that are due and not claimed, and the age of the oldest.** The deployed
environment turns those lines into metrics.

**Event buses, message queues, `LISTEN` and `NOTIFY` are not used to carry work
to a worker.** A plan that needs one replaces this section by a decision.
