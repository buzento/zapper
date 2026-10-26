# Testing Standards

The technical implementation standard for tests. It fixes the technology, the
conventions and the coverage obligations for every test in the solution, so that
a feature can be executed without inventing any of them.

`feature-execution` reads this before writing an increment. `feature-review`
checks against it. A feature's plan says only which types of test
apply to that feature and why any do not; the definitions live here.

Every rule in this document is settled. Each one was argued and decided before it
was written here, and none of them is re-opened during execution. Where this
document states one way to do something, that is the only way, and an equivalent
is not substituted for it.

Code examples show the required shape, and every one of them is bound by the same
restriction. A sentence before the example says it is an example, and the first
line inside it repeats that. Every name in an example, whether it is a test class,
a test method, a type, a method, a component or a file, is there to make the shape
readable. The names are illustrations, not names to look for in the code. Finding
one, or not finding one, tells the reader nothing. Only the shape is the rule.

This standard states what must be true and names the tools. A tool's version is
the one its row in `Versions.md` approves. A tool's package is added to a test
project by the first test that needs it, and its row is written in `Versions.md`
by that standard's rules. `create-solution` and `create-project` put the first
implementation in place when a project is created: fixtures, base classes,
the project reference graph. After that it is ordinary code
in the repo, and it changes during execution like any other code. Changing what
a test must prove is a change to this document.

Every rule here has a named enforcer in section 10, including the rules whose
enforcer is nothing.

# 1. Scope

Five types of test:

1. C# unit
2. API contract
3. Database integration
4. React unit
5. Acceptance

And two obligations that cut across those types rather than forming types of
their own: authorization and data protection, and accessibility.

**Zapper is API only. Ruled by Francois on 2026-10-22.** So no React unit test is
written, and the accessibility obligation reaches nothing, because both need a
frontend. Sections 6 and 9 are kept for review, with `FrontendCode.md`, and say
so at their start. Section 7's acceptance tests call the API, and no browser
drives them.

Nothing else is a type of test. If a test does not fit one of the five, it is one
of the five written badly.

A load test is the one exception, written only for an operation the plan
names as slow or costly. Section 12 states it in full.

**The end-to-end script in `/AcmeStub/` is outside this document**, with the
stub it drives. It checks the running stack from outside, through real TLS and
the real provider client, and no test type here replaces it or is replaced by
it. Ruled by Francois on 2026-10-23.

**Three names in this document are placeholders, and `BackendCode.md` defines
each of them.** `<Solution>` is the solution's name, from its section 2.
`<Name>Api` is an API, which is the API project `<Solution>.<Name>Api` without
the solution's name, from its section 3. `<DatabaseName>Context` is the
solution's one context, from its section 10. **A fourth is a frontend's folder
for one API**, `src/api/<name>-api/`, which `FrontendCode.md` sections 4 and 8
state.

**The solution holds two C# test projects.** `<Solution>.UnitTests` holds C#
unit tests, and section 3 states what it may reference.

**`<Solution>.IntegrationTests` holds every other C# test this document
names**: API contract, database integration, authorization, and acceptance.
All four need the real database and the API host, and one project for all four
is simplest. Acceptance is not split into a project of its own.

## Folder layout

Each test project mirrors what it tests. In the tree below, a name in angle
brackets stands for what it holds and is not a name to look for. Every other
name is the actual name.

```
<Solution>.UnitTests/
    Domain/
        <Schema>/
            <Entity>/
                <Entity>Tests.cs
                <Policy>Tests.cs
    Managers/
        <Concern>/
            <Concern>ManagerTests.cs

<Solution>.IntegrationTests/
    TemplateDatabaseFixture.cs
    ClonedDatabaseFixture.cs
    AcceptanceTestBase.cs
    <Name>ApiFixture.cs
    AcceptanceFixture.cs
    AcceptanceCollection.cs
    AcceptanceApiFactory.cs
    <Name>Api/
        <Name>ApiV<n>ContractTests.cs
        <Name>ApiV<n>AuthorizationTests.cs
        Features/
            <Resource>/
                <Operation>IntegrationTests.cs
        Workers/
            <Concern>/
                <Concern>WorkerIntegrationTests.cs
    Integrations/
        <Concern>/
            <Concern>ServiceIntegrationTests.cs
            RecordingHandler.cs
    Database/
        <Boundary>IntegrationTests.cs
    Acceptance/
        <Feature>AcceptanceTests.cs
```

**`<Solution>.UnitTests` mirrors `<Solution>.Domain`'s own `<Schema>/<Entity>/`
folders, and `<Solution>.ApiCommon`'s `Managers/<Concern>/` folders**, which
`BackendCode.md` sections 3, 4 and 18 state.

**`<Solution>.IntegrationTests` holds one folder per API, named for the API.**
Contract and authorization tests sit at that folder's root, one class per
version. An operation's tests sit under `Features/<Resource>/`, mirroring
that API's own folders, which `BackendCode.md` section 6 states.

**The tests that prove the database itself, and an entity's mapping or
constraint, sit under `Database/`.** Acceptance tests sit under `Acceptance/`,
one file per feature in a plan.

**A folder holds nothing until something needs it**, the same rule
`BackendCode.md` section 4 states for `<Solution>.Domain`.

# 2. Rules common to every test

**Every rule in `BackendCode.md` reaches C# test code, and every rule in
`FrontendCode.md` reaches a frontend test file.** This section names the
exceptions, and there are no others.

**A C# test method's name is the first exception on the C# side.** It carries no
`Async` suffix. It is the three-segment name with underscores that section 3
states, and an acceptance test's name is its scenario title in PascalCase, which
section 7 states.

**A cancellation token is the second exception on the C# side.** An async call in
a C# test that accepts a token is given `TestContext.Current.CancellationToken` in
place of the request token. `SaveChangesAsync` is given `CancellationToken.None`,
written out, as `BackendCode.md` section 16 states.

**A frontend test file's name is the first exception on the frontend side.** It is
named for the file it tests, plus `.test`. As an example, with `LeaveBalance` an
illustration rather than a name to look for, `LeaveBalance.test.tsx` is the test
file for `LeaveBalance.tsx`.

**The Vitest setup file is the second exception on the frontend side.** It exports
nothing, and it is `src/test/setup.ts`, which section 6 places.

**A fixture's name is the third exception on the frontend side.** Its export and
its file are named by the rule section 6 states, with an underscore between each
part.

**The accessibility file is the fourth exception on the frontend side.** It holds
data and no code, and it is `src/test/accessibility.json`, which section 6 places.

**A test passes when run alone, when run in any order, and when run
repeatedly.**

**One behaviour per test.** Multiple assertions are fine when they describe one
outcome. Two outcomes are two tests.

**Arrange, act and assert are separated by blank lines**, and never by comment
headers.

**Test data is written inline in the test.** A builder is written only once the
same shape repeats.

**Test data is synthetic, always.** It is never a copy, a subset or a scrub of
production data.

**A test holds no conditional logic.**

**No assertion is written inside a loop.** A table of cases is `[Theory]` with
`[InlineData]`, or `it.each`. A `[Theory]` is written only once its table has a
case.

**Randomness in a test is seeded.**

**The act is never wrapped in `try`/`catch`.** A throw is asserted instead.

**A test waits for a condition, and never for a duration.** `Thread.Sleep` and
`Task.Delay` are never called.

**A C# test asserts with Shouldly, and never with `Xunit.Assert`.**

**The test projects carry the production list in `BannedSymbols.txt`, and a list
of their own beside it.** `BackendCode.md` section 21 states the production list,
and it already bans every `DateTime` member and the `HttpClient` constructor in
test code as it does everywhere else. The test list holds only what the
production list does not, and these are its entries:

| Symbol | What is written instead |
|---|---|
| `M:System.Threading.Thread.Sleep(System.Int32)`, `M:System.Threading.Thread.Sleep(System.TimeSpan)` | a wait on the condition the test needs |
| `M:System.Threading.Tasks.Task.Delay(System.Int32)`, `M:System.Threading.Tasks.Task.Delay(System.Int32,System.Threading.CancellationToken)`, `M:System.Threading.Tasks.Task.Delay(System.TimeSpan)`, `M:System.Threading.Tasks.Task.Delay(System.TimeSpan,System.Threading.CancellationToken)`, `M:System.Threading.Tasks.Task.Delay(System.TimeSpan,System.TimeProvider)`, `M:System.Threading.Tasks.Task.Delay(System.TimeSpan,System.TimeProvider,System.Threading.CancellationToken)` | a wait on the condition the test needs |
| `T:Xunit.Assert` | Shouldly |

**`dotnet test` is run from `Code/`.**

# 3. C# unit tests

Shouldly for assertions. xUnit v3 on Microsoft.Testing.Platform.
`FakeTimeProvider`, from `Microsoft.Extensions.TimeProvider.Testing`, for time.

**`<Solution>.UnitTests` references only `<Solution>.Domain` and
`<Solution>.ApiCommon`.** It never references an API project or
`<Solution>.Infrastructure`.

**It references no package other than the libraries this section names and the
analyzer packages `BackendCode.md` section 21 names.**

**A unit test's subject is a domain method, a policy method or a manager method.**

**A handler and a validator are not unit tested.** Section 5 states the test that
proves them.

**Naming.** One test class per type under test, named `<TypeUnderTest>Tests`, in
a file of the same name. A test method is three segments,
`Subject_Scenario_ExpectedOutcome`, where the subject is the method under test.

The example below shows two unit tests of one domain method, one for each value of
the enum it returns, with time given as a `FakeTimeProvider`. `Shift`,
`Publish`, `PublishShiftResult`, `PublishedAt` and every value are illustrations,
taken from `BackendCode.md` section 12's example. They are names this document
uses to show the shape, and the shape is the rule.

```csharp
// Example only. The names are illustrations, not names to look for.

// ShiftTests.cs
public sealed class ShiftTests
{
    [Fact]
    public void Publish_NotYetPublished_ReturnsPublished()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 14, 7, 30, 0, TimeSpan.Zero));
        var shift = new Shift(Guid.CreateVersion7(), new DateOnly(2026, 9, 15), new TimeOnly(8, 0), new TimeOnly(16, 0));

        var result = shift.Publish(time);

        result.ShouldBe(PublishShiftResult.Published);
        shift.PublishedAt.ShouldBe(time.GetUtcNow());
    }

    [Fact]
    public void Publish_AlreadyPublished_ReturnsAlreadyPublished()
    {
        var time = new FakeTimeProvider();
        var shift = new Shift(Guid.CreateVersion7(), new DateOnly(2026, 9, 15), new TimeOnly(8, 0), new TimeOnly(16, 0));
        shift.Publish(time);

        var result = shift.Publish(time);

        result.ShouldBe(PublishShiftResult.AlreadyPublished);
    }
}
```

**Must always cover:**

- Every value of the enum a domain method or a policy method returns.
- Every computation a manager method makes.

**Must never test:**

- The framework.
- A private method, other than through the public method that calls it.
- Log output, unless a business rule requires the log entry to exist.

**A unit test never reads the clock.** Time is given as a `FakeTimeProvider`.

**A unit test substitutes nothing.** A domain method, a policy method and a
manager method are given real objects.

# 4. API contract tests

The frozen artefact is an **OpenAPI fragment covering exactly one method on one
route**, stored at
`Docs/Contracts/<Name>Api/<version>/<route>/<method>.openapi.json` and described
by `<method>.md` beside it, cut from `Claude/Templates/Contract.Template.md`.

The path is derived from the route rather than chosen, so the API is the first
segment and the endpoint is the rest.

**A fragment reaches `Docs/Contracts/` in the first increment of the Feature that
implements it**, moved there from `ai/Plans/<Plan_Title>/Contracts/`, where the
plan froze it. So the contract test never meets a fragment whose code is not on
the same branch, and `dev` never fails by design. Ruled by Francois on
2026-10-22.

**Only API surfaces are contracts.** An interface and a queue message are not
frozen and have no contract test.

**What a contract test proves**, for each frozen fragment:

- The document the application generates for that API version holds an operation
  for the fragment's route and method. The test fails when it does not.
- That operation is exactly equal in the generated document and in the fragment.
  Exactly equal covers the route and its parameters, the request body, every
  response and its body, `summary`, `description`, `tags`, `security`, and
  `operationId` being absent.
- Every schema the operation references is exactly equal in the generated
  document and in the fragment, compared by schema name.
- Every security scheme the operation's `security` field names is exactly equal
  in the generated document and in the fragment, compared by scheme name.
- The fragment is itself valid OpenAPI.

**Any difference fails the test, in either direction**, including something the
generated document has that the fragment does not.

**The comparison covers only the method the fragment declares.** An operation on
another route, or another method on the same route, never fails it.

**A contract test proves nothing about behaviour.** Sections 5 and 7 state the
tests that do.

**The test resolves the generated document from the host's container.** The host
is the in-memory test server section 5 names. The test resolves
`IOpenApiDocumentProvider` by its key, which is the document's name, `v` followed
by the version, and calls `GetOpenApiDocumentAsync`. The provider is not
registered without a key, so a resolve without one returns nothing.

**The test never requests the document route over HTTP, and never switches
documentation on.**

**Naming.** One class per API version, `<Name>ApiV<n>ContractTests`, in a file
of the same name. The test is one `[Theory]` whose cases are the route and
method of every fragment on disk for that API version. The method name is the
three segments section 3 states.

The example below shows the contract test class for one API version. The class,
the method, the member that supplies the cases and the field are illustrations,
not names to look for.

```csharp
// Example only. The names are illustrations, not names to look for.

// <Name>ApiV1ContractTests.cs
public sealed class <Name>ApiV1ContractTests
{
    private readonly IServiceProvider provider;

    // the constructor, taking the host's service provider from the fixture

    [Theory]
    [MemberData(nameof(FrozenFragments))]
    public async Task GeneratedDocument_ForFrozenFragment_MatchesIt(string route, string method)
    {
        var documentProvider = provider.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");

        var document = await documentProvider.GetOpenApiDocumentAsync(TestContext.Current.CancellationToken);

        // the fragment at Docs/Contracts/<Name>Api/v1/<route>/<method>.openapi.json,
        // compared with the operation in document as this section states
    }

    // FrozenFragments, the route and method of every fragment under Docs/Contracts/<Name>Api/v1/
}
```

**Rules:**

- A frozen version keeps its contract tests for as long as that version is
  served. They are deleted when the version is retired.
- A failing contract test is never fixed by editing the fragment. The code is
  corrected, or the change is a new version, which planning decides. A
  new version is a new route and a new fragment beside the old one, never an edit
  to it.
- Contract tests live with the tests of the API that produces the contract.
- There are no consumer-side contract tests, and none is written.

# 5. Database integration tests

A real PostgreSQL through Testcontainers. Never an in-memory database provider.

**A database integration test is one of five things:**

- an operation, called through the in-memory test server;
- a worker, run one pass at a time;
- a provider's service, built through its production registration;
- an entity mapping or a constraint, proved through the context;
- the migration test, the database naming test, the reference table test or the
  error code test.

**Isolation.** One container for the run. Migrations run once into a template
database. Each test class clones its own database from the template and drops it
with `DROP DATABASE ... WITH (FORCE)` when the class finishes. The migration test
is the exception, and uses an empty database of its own. The database naming test
uses no database.

**Container reuse stays off.**

**Migrations run into the template with pooling disabled, or the pool is cleared
before the first clone.** A disposed pooled connection still holds the template
open, and cloning then fails with "source database is being accessed by other
users".

**The template holds what the migrations create and the fixed role accounts, and
nothing else.** What the migrations create is the schema and the rows they seed,
being the status table's, the message table's and every reference table's, which
`BackendCode.md` sections 7 and 13 seed by migration. Data a test needs, that test
creates. A fixed role account is a sign-in account, which sections 7 and 8 use.
Each API's database role is created in the container by the template fixture,
before the migrations run.

**A test class takes what it uses from the fixture, and holds each one in a
field.** The `HttpClient` the in-memory test server gives is held in `client`. The
context for the class's cloned database is held in `database`.

**Naming.**

- An operation's tests are one class, `<Operation>IntegrationTests`, in a file of
  the same name.
- Every other class is named for the boundary it proves,
  `<Boundary>IntegrationTests`, in a file of the same name. **The boundary is one
  entity's persistence, named for that entity plus `Persistence`, or one property
  of the schema the whole solution shares, named for that property.**
- A test method is the three segments section 3 states. Its subject is the
  operation, or what the test exercises where that is not an operation.

## Operations through the host

**The in-memory test server is `WebApplicationFactory<Program>`, from
`Microsoft.AspNetCore.Mvc.Testing`.** A test sends its request through the
`HttpClient` that `CreateClient` returns. No class of our own sends a request.

**A handler and a validator are proved here**, by a request to the operation's
route.

**For every operation, its tests prove:**

- every result the handler's typed result union names;
- every refusal, being each value of the enum the domain returns that refuses,
  mapped to its status code and its `code`;
- the validation 400 and its per-field map, whenever the plan names the rule;
- the 404 for an id that names no record;
- that a query returns the records it should and no others;
- for every result that changes stored data, the change, read back through
  `database` after the response arrives.

**What a test checks in a failure:**

- A refusal: the status code, and `code` compared with the `ErrorMessageCode`
  constant the handler sets.
- A validation 400: the status code, and the keys of the per-field map, being
  each request field the plan's rule names, in camelCase.
- A 404: the status code.

**No test checks `title`, `detail` or the text of an entry in the per-field map,
and no test types message text.**

**A test adds the data it needs through `database`**, and saves it with
`SaveChangesAsync` before it sends the request. Each record is an entity made by
its own constructor, and put into any state it needs by its own domain methods.
A test never creates its data through another operation's route, and never with
raw SQL.

**An operation that requires signing in is called by a caller it allows**, signed
in as section 8 states. Section 8 states the authorization tests, which run on
this same server.

**Where a result depends on the clock, the host is given a `FakeTimeProvider` in
place of `TimeProvider.System`**, and the test sets the time on it.

**NSubstitute is the substitute library.**

- A substitute is registered in the host in place of a provider's service, and it
  substitutes the interface a handler takes, which `BackendCode.md` section 9
  states.
- Only an interface we own is substituted. A type we do not own is never
  substituted. A provider is reached through its own service interface, and that
  interface is what is substituted.
- The type under test is never substituted.

The example below shows a test giving a substitute its answer before it sends the
request. `PayInvoice`, `Invoice`, `IPaymentGatewayService` and `ChargeAsync` are
illustrations taken from `BackendCode.md` sections 9 and 16. `ChargeAsync`'s
parameter types and its return type are illustrations too, and so are the class,
the method, the fields and the route. None of them is a name or a signature to look
for.

```csharp
// Example only. The names and types are illustrations, not names to look for.

// PayInvoiceIntegrationTests.cs
public sealed class PayInvoiceIntegrationTests
{
    private readonly HttpClient client;

    private readonly <DatabaseName>Context database;

    private readonly IPaymentGatewayService paymentGateway;

    // the constructor, taking all three from the fixture, which registered
    // paymentGateway in the host in place of the provider's service

    [Fact]
    public async Task PayInvoice_ChargeAccepted_ReturnsNoContent()
    {
        var invoice = new Invoice(Guid.CreateVersion7());
        database.Invoices.Add(invoice);
        await database.SaveChangesAsync(CancellationToken.None);
        paymentGateway.ChargeAsync(Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns("charge-reference");

        var response = await client.PostAsync(
            $"/v1/invoices/{invoice.Id}/payment", null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        // the change read back through database, as the example below shows
    }
}
```

The example below shows three tests of one operation: a result that changes
stored data and is read back, a refusal, and the 404. `PublishShift`, `Shift`,
`Publish` and `PublishedAt` are illustrations taken from `BackendCode.md` section
12's example. The class, the methods, the fields, the route and
`ErrorMessageCode.AlreadyPublished` are illustrations too, not names to look for.

```csharp
// Example only. The names are illustrations, not names to look for.

// PublishShiftIntegrationTests.cs
public sealed class PublishShiftIntegrationTests
{
    private readonly HttpClient client;

    private readonly <DatabaseName>Context database;

    // the constructor, taking both from the fixture

    [Fact]
    public async Task PublishShift_NotYetPublished_StoresPublishedAt()
    {
        var shift = new Shift(Guid.CreateVersion7(), new DateOnly(2026, 9, 15), new TimeOnly(8, 0), new TimeOnly(16, 0));
        database.Shifts.Add(shift);
        await database.SaveChangesAsync(CancellationToken.None);

        var response = await client.PostAsync(
            $"/v1/shifts/{shift.Id}/publication", null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var stored = await database.Shifts.SingleAsync(
            candidate => candidate.Id == shift.Id, TestContext.Current.CancellationToken);
        stored.PublishedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task PublishShift_AlreadyPublished_ReturnsAlreadyPublished()
    {
        var shift = new Shift(Guid.CreateVersion7(), new DateOnly(2026, 9, 15), new TimeOnly(8, 0), new TimeOnly(16, 0));
        shift.Publish(new FakeTimeProvider());
        database.Shifts.Add(shift);
        await database.SaveChangesAsync(CancellationToken.None);

        var response = await client.PostAsync(
            $"/v1/shifts/{shift.Id}/publication", null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        // the code read from the body, compared with ErrorMessageCode.AlreadyPublished
    }

    [Fact]
    public async Task PublishShift_UnknownId_ReturnsNotFound()
    {
        var id = Guid.CreateVersion7();

        var response = await client.PostAsync(
            $"/v1/shifts/{id}/publication", null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
```

## Workers

**A worker's tests are one class, `<Concern>WorkerIntegrationTests`**, at
`<Name>Api/Workers/<Concern>/`, on the in-memory test server against the class's
cloned database. The worker's loop is switched off by its `IsEnabled` setting,
and the test runs one polling pass at a time. The provider's interface is
substituted, as above, and time is a `FakeTimeProvider`.

**They prove:** every outcome of one piece of work that the worker's plan names;
that two claims made at the same moment never return the same row; that a row
whose lease has lapsed is claimed again; that a `Conflict` is reloaded and applied
again; and that the fifth unexpected failure in a row leads to what the worker's
plan names. The claim's SQL runs against the real database in every one of
them. Ruled by Francois on 2026-10-23.

## Providers

**A provider's service is tested by one class, `<Concern>ServiceIntegrationTests`**,
at `Integrations/<Concern>/`. The service is built through its production
`AddHttpClient` registration, with only the primary handler replaced by
`RecordingHandler`, a class in the same folder that records each request and
answers with the response the test gives it. Resilience runs as registered, with
a `FakeTimeProvider`.

**`RecordingHandler` is the one exception to "Only an interface we own is
substituted"**, and it is the only kind of class a test writes for it. A
provider's own documented examples are copied once into files beside the test and
are the responses it answers with.

**They prove:** that each request matches the provider's documented example; that
each documented response is read, a member the service does not know included;
that each failure the provider documents maps to the outcome the plan names; and
that a call that never answers fails with what timed out and its time limit.

**A provider's service is never tested against a stub of the provider.** That
proves only that two readings of the provider's documentation agree. Ruled by
Francois on 2026-10-23.

## The database itself

**These tests prove:**

- An entity mapping round-trips without silent loss.
- A constraint or a unique index carrying business meaning rejects what it should.

The example below shows a constraint test writing through the context. `Employee`,
`Ethnicity` and the foreign key the save breaks are illustrations taken from
`BackendCode.md` section 13's example. The class and the method are illustrations
too, not names to look for.

```csharp
// Example only. The names are illustrations, not names to look for.

// EmployeePersistenceIntegrationTests.cs
public sealed class EmployeePersistenceIntegrationTests
{
    private readonly <DatabaseName>Context database;

    // the constructor, taking the database from the fixture

    [Fact]
    public async Task SaveEmployee_WithUnlistedEthnicity_ViolatesForeignKey()
    {
        database.Employees.Add(new Employee((Ethnicity)99));

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }
}
```

**Four test classes cover the whole solution rather than one operation.** The
migration test is one test. Each of the other three classes holds one test for
each failure listed for it below.

**The migration test** applies every migration to an empty database, then applies
every `Down`, then applies every migration again. After every `Down`, no
migration is recorded as applied and no table the model maps is left.

**The database naming test** reads EF's built model, and needs no connection and
no cloned database. It fails on a name that breaks `BackendCode.md` section 18:

- a table not named for its entity, or a schema not named or named `public`;
- a primary key column not named `Id`, or a primary key not named `PK_<Table>`;
- a foreign key not named `FK_<Table>_<Column>`, or a foreign key column not
  named `<ReferencedTable>Id`, except `<Role><ReferencedTable>Id` on a table
  holding more than one foreign key to the same table;
- an index not named `IX_<Table>_<Columns>`, or a unique index not named
  `UX_<Table>_<Columns>`.

**The reference table test** reads the stored rows of every reference table
the model maps. It reads `Id` and `Name` and ignores every other column. It fails
when:

- an enum member has no row;
- a row has no enum member;
- a row's `Name` is not its enum member's name;
- a reference table is not named for its enum plus `Type`.

**The error code test** reads the stored rows of the status table and the
message table. It fails when an `ErrorMessageCode` constant names no row, when a
constant names a row in both tables, or when a row in either table has no
constant.

# 6. React unit tests

**This section applies to a frontend only, and Zapper has none.** Ruled by
Francois on 2026-10-22: Zapper is API only, so nothing here is written. The
section is kept for review, with `FrontendCode.md`.

Vitest with React Testing Library, and MSW for the network.

**A test file sits beside the file it tests.** Section 2 states its name.

**A React unit test's subject is a component or a page, rendered.**

- A query hook, a mutation hook, a store and a form's schema get no test of their
  own. Each is proved through the component that uses it.
- A function in `src/lib/` gets a test of its own.
- A component primitive copied from shadcn gets no test.

**Must always cover:**

- For a component that reads a query: its pending state, its failed state and its
  data.
- For a form: every message its schema carries, a per-field map shown against the
  fields it names, `detail` shown on the form, and its result announced in its
  `<output>`, whether the mutation succeeded or failed.

**Must never test:** which hook fired, what a store or a component holds, or how
many times something rendered.

## The test support files

**Each frontend holds its own test support files, under `src/test/`.** These are
the actual names, not illustrations:

| Path | What it is |
|---|---|
| `src/test/setup.ts` | The Vitest setup file, named in the `test` block of `vite.config.ts`. It exports nothing |
| `src/test/server.ts` | Exports `server`, the MSW server made with `setupServer` from `msw/node`. It holds the default request handlers and exports nothing else |
| `src/test/fixtures/` | One file per fixture. The folder is created with the first fixture |
| `src/test/renderWithProviders.tsx` | Exports `renderWithProviders`, which renders a component or a page for a test |
| `src/test/accessibility.json` | The frontend's axe ruleset and exclusion list, read by the component scan and by the acceptance scan. Section 9 states what it holds |
| `src/test/axe.ts` | Exports `axe`, made with `vitest-axe`'s `configureAxe` from `src/test/accessibility.json`. It exports nothing else |

**A test renders its subject with `renderWithProviders`**, and never with Testing
Library's own `render`.

**What surrounds every test:**

- A component is rendered inside the providers the application renders it inside,
  being the query client's provider and the router. The router is a memory router
  whose one route matches every path.
- Each test gets its own query client, with its retries off, and no two tests
  share one.
- Every store starts each test from its initial state.
- A handler a test replaces is put back before the next test starts.
- A request that no handler answers fails the test.

## The network

**The network is answered by MSW.** `fetch` is never replaced, and neither is
any API client under `src/api/`, which `FrontendCode.md` section 8 places.

**A handler's address is `*` followed by the contract route, with each `{key}`
written as `:key`.** As an example, with the route an illustration, the route
`/v1/employees/{id}/invitations` is answered at `*/v1/employees/:id/invitations`.

**The default handler for the settings file, at `*/config.json`, is the one
handler whose address is not a contract route.**

**A test that needs a different answer replaces the handler for that test**, with
`server.use`.

## Fixtures

**A fixture is one response body of one operation**, typed with `satisfies`
against that response in the generated types of the API that serves it, which
`FrontendCode.md` section 8 places.

**A fixture is written for a success body a default handler returns, and for a
response body more than one test uses.** A body only one test uses is written
inline in that test. A test that needs a variation writes it inline, by spreading
the fixture.

**No builder is written on the frontend.** A response body used by more than one
test is a fixture.

**A response with no body has no fixture.**

**A fixture's export is named from its operation's contract path**,
`Docs/Contracts/<Name>Api/<version>/<route>/<method>.openapi.json`, from its
status code and from its `code`:

1. The method, in lowercase.
2. For each segment after `<Name>Api`, in route order: a segment that is not a
   key, in PascalCase with the words of a kebab-case segment joined, or a key,
   as the two parts `By` and the key in PascalCase.
3. The status code.
4. For a failure, the `code` the body carries.
5. Each part joined to the next by an underscore.

**The fixture's file is that name plus `.ts`.** Section 2 states that the name is
an exception.

The table below shows the rule applied. The routes, the statuses and the codes are
illustrations, not names to look for.

| Operation and body | Fixture |
|---|---|
| `GET /v1/customers`, 200 | `get_V1_Customers_200` |
| `GET /v1/customers/{id}`, 404 | `get_V1_Customers_By_Id_404_NotFound` |
| `GET /v1/employees/invitations/{id}`, 200 | `get_V1_Employees_Invitations_By_Id_200` |
| `GET /v1/employees/{id}/invitations`, 200 | `get_V1_Employees_By_Id_Invitations_200` |
| `POST /v1/employees/invitations`, 400 carrying the per-field map | `post_V1_Employees_Invitations_400_BadRequest` |
| `POST /v1/orders/{id}/cancellation`, 400 refused | `post_V1_Orders_By_Id_Cancellation_400_AlreadyDispatched` |
| `DELETE /v1/orders/{id}/order-lines/{orderLineId}`, 404 | `delete_V1_Orders_By_Id_OrderLines_By_OrderLineId_404_NotFound` |

## Naming and conventions

**`describe` names the subject, being the name of the component, the page or the
function under test, and `it` completes the sentence.** At most two levels of
`describe`: the subject, then the situation.

- Vitest's `expect`, with the Testing Library DOM matchers.
- Queries by role, label and visible text only. Never by test id, class or DOM
  path.
- `userEvent`, never `fireEvent`.
- `it.only` and `it.skip` are never written.
- No snapshot tests.

The example below shows a fixture for a form's failure, and two tests of that form:
the per-field map shown against a field, and the result announced in the form's
`<output>`. The second test is answered by the default handler in
`src/test/server.ts`. `InviteEmployeeForm`, its path, the route, the labels, the
button name and the text of every message are illustrations taken from
`FrontendCode.md` section 11's example. The accessor into the generated types, the
fixture's members and their values are illustrations too. `renderWithProviders`
is shown taking the element it renders, and that parameter is an illustration, not
a signature to look for.

```tsx
// Example only. The names are illustrations, not names to look for.

// src/test/fixtures/post_V1_Employees_Invitations_400_BadRequest.ts
import type { paths } from '@/api/<name>-api/types';

export const post_V1_Employees_Invitations_400_BadRequest = {
  type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
  title: 'Validation Error',
  status: 400,
  detail: 'The request was not accepted.',
  code: 'BadRequest',
  errors: {
    startDate: ['Choose a start date from today onwards.'],
  },
} satisfies paths['/v1/employees/invitations']['post']['responses']['400']['content']['application/problem+json'];

// src/modules/employees/pages/invite-employee/InviteEmployeeForm.test.tsx
import { describe, expect, it } from 'vitest';
import { screen, within } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { InviteEmployeeForm } from '@/modules/employees/pages/invite-employee/InviteEmployeeForm';
import { post_V1_Employees_Invitations_400_BadRequest } from '@/test/fixtures/post_V1_Employees_Invitations_400_BadRequest';
import { renderWithProviders } from '@/test/renderWithProviders';
import { server } from '@/test/server';

describe('InviteEmployeeForm', () => {
  describe('when the server refuses the start date', () => {
    it('shows the message against the start date', async () => {
      server.use(
        http.post('*/v1/employees/invitations', () =>
          HttpResponse.json(post_V1_Employees_Invitations_400_BadRequest, { status: 400 }),
        ),
      );
      const user = userEvent.setup();
      renderWithProviders(<InviteEmployeeForm />);

      await user.type(screen.getByLabelText('Work email'), 'starter@example.test');
      await user.type(screen.getByLabelText('Start date'), '2026-09-01');
      await user.click(screen.getByRole('button', { name: 'Send invitation' }));

      expect(await screen.findByText('Choose a start date from today onwards.')).toBeInTheDocument();
    });
  });

  describe('when the server accepts the invitation', () => {
    it('announces the result in its output', async () => {
      const user = userEvent.setup();
      renderWithProviders(<InviteEmployeeForm />);

      await user.type(screen.getByLabelText('Work email'), 'starter@example.test');
      await user.type(screen.getByLabelText('Start date'), '2026-10-01');
      await user.click(screen.getByRole('button', { name: 'Send invitation' }));

      expect(await within(screen.getByRole('status')).findByText('Invitation sent')).toBeInTheDocument();
    });
  });
});
```

# 7. Acceptance tests

C# with xUnit, calling the API over HTTP. **No browser drives a scenario. Ruled by
Francois on 2026-10-22: Zapper is API only**, so an acceptance test sends the
requests its scenario names to the API and asserts what comes back. Playwright,
Chromium and the acceptance scan of section 9 are not used.

**Must always cover: every scenario in its plan.**

A Feature whose plan states that the end-to-end script in `/AcmeStub/` proves its
scenarios is the one exception: each of its scenarios is one case of that script,
and this section does not reach it. Ruled by Francois on 2026-10-24.

**Naming.**

- One class per feature, `<Feature>AcceptanceTests`, in a file of the same name.
  `<Feature>` is the `Feature:` line of its plan's scenarios, converted by the
  rule below.
- One test per scenario. Its method name is the scenario title converted by the
  rule below, and its `DisplayName` is the scenario title exactly as the plan writes
  it.
- A test carries one `[Trait("Rule", "<id>")]` for each rule id its scenario is
  tagged with, with the id written as the plan writes it.

**A title becomes a name by this rule:**

1. Take the text after `Feature: ` or after `Scenario: `.
2. Remove every apostrophe.
3. Split it into words at every character that is not a letter or a digit.
4. Write each word with its first letter in upper case and the rest in lower case.
5. Join the words with nothing between them.

**A scenario title begins with a letter.**

The table below shows the rule applied. The titles are illustrations, not titles to
look for.

| Title | Name |
|---|---|
| `Manager invites an employee who has not been invited before` | `ManagerInvitesAnEmployeeWhoHasNotBeenInvitedBefore` |
| `Employee's leave is refused after a 30-day notice` | `EmployeesLeaveIsRefusedAfterA30DayNotice` |
| `HR admin exports the VAT report` | `HrAdminExportsTheVatReport` |

## Which API a scenario calls

**A scenario calls the API its feature's plan names.** The plan states which API
the feature is built on.

**The When is the request, or the requests, the scenario's When names, sent by
`client`.** The Then is asserted on what the API returned. Where the scenario's
outcome is a stored change, it is read back through an operation of the API, and
through `database` only where no operation's frozen contract reads it.

**An acceptance test never calls a provider, or a stub of one.** The provider's
interface is substituted in the test host, as section 5 states. Where the
provider calls the API, such as a notification, the test sends that call itself
through `client`, carrying the credential or the signature the provider's contract
fixes. Ruled by Francois on 2026-10-23.

## The database and the data

**One database for the whole run.** It is a real PostgreSQL through
Testcontainers, in the container section 5's tests use. The run clones one database
from section 5's template when it starts, and keeps it until the run ends. No test
and no class clones a database of its own.

**The fixed role accounts in the template are never changed by a test.** A test
that needs to change a user creates its own.

**A scenario passes when run alone.** It creates the data it needs, and never relies
on data another scenario created.

**The shared base class supplies each test a discriminator, derived from that
test's own identity.** It is never a `Guid`.

**A value a test creates carries the discriminator where a unique constraint or a
unique index covers it, or where the test's Then finds its record by it.** No other
value carries it.

**An assertion is scoped to the records the test created.** It never counts across
a table and never reads the first row.

## Arranging

**The Given is arranged through the API or through the database.**

**A scenario whose act is a step other scenarios arrange sends that step as its
When.** A registration scenario sends the registration request.

**A request to the API is sent by `client`**, the `HttpClient` that
`WebApplicationFactory<Program>.CreateClient` returns, with the host running on
Kestrel on a real port. `client` is taken from the fixture and held in a field. No
class of our own sends a request.

**The database is used only where no operation's frozen contract produces the state
the test needs.** The test then writes through `database`, the context for the
run's database, taken from the fixture and held in a field. Each record is an
entity made by its own constructor, put into any state it needs by its own domain
methods, and saved with `SaveChangesAsync(CancellationToken.None)`. Where that state
depends on the clock, the domain method is given a `FakeTimeProvider`. Never raw
SQL.

## Signing in

**A test is signed in by the system's own sign-in, and never by a credential the
test makes itself.**

**Only a scenario whose act is signing in sends the sign-in as its When.** Every
other test is signed in before its first request.

## The shared base class

**Every acceptance test class derives from `AcceptanceTestBase`**, which supplies
the discriminator and nothing else. **Retries are off**: nothing in xUnit retries
a test, and nothing turns it on.

The example below shows one acceptance test that arranges its Given through the
API, then sends the When and asserts the Then on the response. The class, the
scenario title, the rule id, the routes, the response type and every value are
illustrations, not names to look for. The constructor that takes `client` from
the fixture, and signing in, are shown as comments.

```csharp
// Example only. The names are illustrations, not names to look for.

// EmployeeInvitationsAcceptanceTests.cs
[Collection("Acceptance")]
public sealed class EmployeeInvitationsAcceptanceTests : AcceptanceTestBase
{
    private readonly HttpClient client;

    // the constructor, taking client from the fixture

    [Fact(DisplayName = "Manager sees an invitation that was already sent")]
    [Trait("Rule", "R-2")]
    public async Task ManagerSeesAnInvitationThatWasAlreadySent()
    {
        // signed in as a manager, before the first request
        var workEmail = $"starter+{Discriminator}@example.test";
        await client.PostAsJsonAsync(
            "/v1/employees/invitations",
            new { WorkEmail = workEmail, StartDate = new DateOnly(2026, 10, 1) },
            TestContext.Current.CancellationToken);

        var invitations = await client.GetFromJsonAsync<ListInvitationsResponse>(
            "/v1/employees/invitations",
            TestContext.Current.CancellationToken);

        invitations!.Items.ShouldContain(invitation => invitation.WorkEmail == workEmail);
    }
}
```

# 8. Authorization and data protection

**These tests run on section 5's in-memory test server, against the cloned
database of their class, and never through the browser.**

**Every operation that is not public carries three negative authorization tests**,
except an operation a provider's system calls, which the end of this section
states:

- Signed out, it receives 401.
- Signed in with a role its security requirement does not allow, it receives 403.
- Signed in as a person outside the relationship a record belongs to, it receives
  403 or 404, and never that record.

An operation whose security requirement allows every role has no 403 for a role.

**A test is signed in by the system's own sign-in, and never by a credential the
test makes itself**, as section 7 states. A test signed in with a role is signed in
as that role's fixed role account.

**Every fragment declares its operation's security requirement, or declares that
the operation is public.**

## The sweeps and the coverage test

**One class per API version, `<Name>ApiV<n>AuthorizationTests`, in a file of the
same name, holds three theories: the 401 sweep, the 403 sweep and the coverage
test.** The class, with its 401 sweep and its coverage test, is written with the
version's first operation that is not public. The 403 sweep is written with the
version's first operation whose security requirement does not allow every role.
Each theory's cases come from the fragments on disk for that API version.
Each method name is the three segments section 3 states.

**The 401 sweep** sends each operation's method to its route, signed out. Its
cases are every fragment whose operation is not public.

**The 403 sweep** sends each operation's method to its route, signed in with each
role the operation's security requirement does not allow. Its cases are every
fragment except a public operation and an operation whose requirement allows every
role. **A fragment that declares neither a requirement nor a public operation fails
the 403 sweep.**

**The coverage test** fails for each fragment whose operation is not public and
whose path no test carries in an `Ownership` trait.

**An operation a theory skips is left out of that theory's cases**, and is never
skipped inside the test.

**A sweep fills each key in the route with its own `Guid.CreateVersion7()`, inside
the test, and sends no body.**

**A sweep checks the status code only.**

The example below shows the 401 sweep for one API version. The class, the method
and the member that supplies the cases are illustrations, not names to look for.
The constructor, building the request, and the member that supplies the cases
are shown as comments.

```csharp
// Example only. The names are illustrations, not names to look for.

// <Name>ApiV1AuthorizationTests.cs
public sealed class <Name>ApiV1AuthorizationTests
{
    private readonly HttpClient client;

    // the constructor, taking client from the fixture

    [Theory]
    [MemberData(nameof(OperationsRequiringSignIn))]
    public async Task Operation_SignedOut_ReturnsUnauthorized(string route, string method)
    {
        // request, sending method to route with each key filled with its own
        // Guid.CreateVersion7(), and no body

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // OperationsRequiringSignIn, the route and method of every fragment under
    // Docs/Contracts/<Name>Api/v1/ whose operation is not public
}
```

**An operation a provider's system calls carries the 401 sweep, and neither the
403 sweep nor an ownership test.** Its caller holds one permission and no role,
and it returns no record. Such an operation is one whose fragment names a scheme
other than `http` `bearer`, because `Security.md` section 4 gives every other
caller an authentication token. Its own integration tests prove each refusal its
plan names as a 401, the other provider's credential included. The 403 sweep and
the coverage test leave it out of their cases. Ruled by Francois on 2026-10-24.

## Ownership tests

**An operation's ownership test is in that operation's `<Operation>IntegrationTests`
class, which section 5 names.** It takes `client` and `database` from the fixture
and adds the data it needs as section 5 states. Its method name is the three
segments section 3 states, and its subject is the operation.

**An ownership test carries
`[Trait("Ownership", "<Name>Api/<version>/<route>/<method>")]`.**
The value is the path of its operation's fragment under `Docs/Contracts/`, without
`.openapi.json`, written exactly as that path is on disk.

The table below shows the trait's value for several operations. The routes are
illustrations, not values to look for.

| Operation | Trait value |
|---|---|
| `GET /v1/invoices/{id}` in `<Name>Api` | `<Name>Api/v1/invoices/{id}/get` |
| `POST /v1/employees/{id}/invitations` in `<Name>Api` | `<Name>Api/v1/employees/{id}/invitations/post` |
| `GET /v1/employees/invitations/{id}` in `<Name>Api` | `<Name>Api/v1/employees/invitations/{id}/get` |
| `DELETE /v1/orders/{id}/order-lines/{orderLineId}` in `<Name>Api` | `<Name>Api/v1/orders/{id}/order-lines/{orderLineId}/delete` |
| `GET /v1/employees/{id}` in `<OtherName>Api` | `<OtherName>Api/v1/employees/{id}/get` |

**The plan states whether a person outside the relationship a record belongs to is
told that the record exists.** An ownership test asserts 404 where that rule says
the person is not told, and 403 where it says they are. No default applies.

**An ownership test checks the status code, and checks that the body's members are
exactly `type`, `title`, `status`, `detail`, `code` and `traceId`**, compared by
name and in any order. It checks no value.

The example below shows one ownership test. `GetInvoice` and `Invoice` are
illustrations taken from `BackendCode.md` section 11, and the class, the method, the
fields, the route, the trait's value, and a business rule that does not tell the
caller the invoice exists are illustrations too, not names to look for. The
constructor, signing in and reading the body's members are shown as comments.

```csharp
// Example only. The names are illustrations, not names to look for.

// GetInvoiceIntegrationTests.cs
public sealed class GetInvoiceIntegrationTests
{
    private readonly HttpClient client;

    private readonly <DatabaseName>Context database;

    // the constructor, taking both from the fixture

    [Fact]
    [Trait("Ownership", "<Name>Api/v1/invoices/{id}/get")]
    public async Task GetInvoice_AnotherCustomersInvoice_ReturnsNotFound()
    {
        var invoice = new Invoice(Guid.CreateVersion7());
        database.Invoices.Add(invoice);
        await database.SaveChangesAsync(CancellationToken.None);
        // signed in as a caller the invoice does not belong to, by the system's own sign-in

        var response = await client.GetAsync(
            $"/v1/invoices/{invoice.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        // the body's member names, compared with the six this section lists
    }
}
```

# 9. Accessibility

**This section applies to a frontend only, and Zapper has none.** Ruled by
Francois on 2026-10-22: Zapper is API only, so nothing here is written. The
section is kept for review, with `FrontendCode.md`.

**Every page a scenario in a plan visits is scanned with `axe-core` inside its acceptance
test, through `Deque.AxeCore.Playwright`.**

**Every React unit test file holds one test that scans its subject with
`vitest-axe`.** A subject that reads a query is scanned once its data has rendered.

The ruleset is WCAG 2.2 AA, configured centrally, so a test cannot quietly weaken
it.

**Each frontend's ruleset and exclusion list are in `src/test/accessibility.json`**,
which section 6 places. The component scan reads it, and the acceptance scan reads
the file of the frontend its scenario drives, which section 7 states.

**A component scan uses the `axe` that `src/test/axe.ts` exports**, and never
`vitest-axe`'s own.

**Each exclusion entry names an axe rule id, the CSS selector it applies to, and a
written reason.**

**An exclusion entry is applied to the results.** A violation is dropped only when
its rule id matches an entry and its element matches that entry's selector.

**Any violation that is not dropped fails the test.** There is no severity
threshold.

## What reaches the acceptance scan only

**Each distinct URL is scanned once per run**, memoised on the shared base class.

**The failure message says plainly that it is an accessibility violation, and names
the rule, the impact and the element.**

**Incomplete results are reported every run, never dropped, and never fail.** axe
returns a result as incomplete when it cannot decide it, such as contrast measured
over a background image.

**A clean scan is never cited as evidence of conformance.**

# 10. Enforcers

Every rule in this document has an enforcer, and this section names it. Where that
thing is nothing, the last list says so, because a column that claims coverage it
does not have is worse than no column.

The three parts below are one question asked three ways: is the rule caught
without anyone reading the increment, only by reading it, or not at all.

## What is caught without reading the increment

| Rule | Enforcer |
|---|---|
| Every `BackendCode.md` rule reaches C# test code | Banned-symbols analyzer, `RS0030`, through the production list, and `feature-review` |
| An async call in a C# test that accepts a token is given `TestContext.Current.CancellationToken`, and `SaveChangesAsync` is given `CancellationToken.None` | xUnit1051, for a call given no token, and `feature-review` |
| A frontend test file is named for the file it tests, plus `.test` | `eslint-plugin-check-file`, for the casing, and `feature-review` |
| `Thread.Sleep` and `Task.Delay` are never called | Banned-symbols analyzer, `RS0030`, through the test list |
| A C# test asserts with Shouldly, and never with `Xunit.Assert` | Banned-symbols analyzer, `RS0030`, for `Xunit.Assert`, and `feature-review` |
| A unit test never reads the clock | Banned-symbols analyzer, `RS0030`, for `DateTime` and `DateTimeOffset`, and `feature-review` |
| A fixture is typed with `satisfies` against its response in the generated types | `tsc`, for a fixture typed with `satisfies`, and `feature-review` |
| A fixture's name follows the rule section 6 states | `eslint-plugin-check-file`, for the shape, and `feature-review` |
| Queries by role, label and visible text only, `userEvent` and never `fireEvent`, no `it.only` or `it.skip`, and no snapshot tests | `eslint-plugin-testing-library`, a `no-restricted-syntax` selector for a query by test id, and `@vitest/eslint-plugin`, and `feature-review` |
| A scenario title begins with a letter | The compiler, for a method name converted by section 7's rule, and `feature-review` |
| Waiting is done on a condition, and never by a duration | Banned-symbols analyzer, `RS0030`, for `Thread.Sleep` and `Task.Delay`, and `feature-review` |
| `dotnet test` is run from `Code/` | The pipeline, whose "Test" step runs from `Code/`; on an engineer's machine, nothing |

## What is caught only by reading the increment

**Every rule this document states that the table above does not name, and that the
list below does not name, is caught by `feature-review` and by nothing else.**
Nothing on the build sees any of them, and no test proves any of them.

That is most of this document. A rule reaching this part is not a weaker rule, and
it is not optional because the build is quiet about it.

## What nothing catches

**Three rules have no enforcer at all.** Nothing on the build sees them, and
reading the increment does not show them either.

- **A test that does not pass when run alone, in any order, or repeatedly.** A
  dependency on order or on shared state shows only in a run that happens to meet
  it, and the increment's code does not show it. Section 2 states the rule.
- **A clean scan cited as evidence of conformance.** A citation is made outside an
  increment, so there is nothing in one to read. Section 9 states the rule.
- **The built product not compared against the signed-off design.** The comparison
  happens at the completion gate in `feature-execution`, after the last increment,
  and no screenshot comparison is written. Section 11 states the rule. This rule
  applies to a frontend only, and Zapper has none, so `feature-execution` has no
  such gate. Ruled by Francois on 2026-10-22.

# 11. Not in scope

**No coverage percentage is set.**

**No mutation test is written.**

**No performance test is written.**

**A load test is written where section 12 requires it.**

**No penetration test is written.**

**A vulnerable dependency failing the build, secret scanning and every other build
gate are `Security.md`'s. Static analysis is the code standards'.**

**A security rule that constrains how production code is written is the code
standards'.** The ban on `dangerouslySetInnerHTML` is `FrontendCode.md` section
16's, and the ban on the four raw SQL methods is `BackendCode.md` section 21's.

**The built product is compared against the signed-off design by a person, at the
completion gate in `feature-execution`.** No screenshot comparison is written.
**This applies to a frontend only, and Zapper has none**, so `feature-execution`
has no such gate. Ruled by Francois on 2026-10-22.

# 12. Load tests

k6, run as its own script and its own command, never through `dotnet test`.

**A load test is written for an operation the plan names as slow or costly.**
No other operation gets one.

**The API is hosted the way section 7 hosts it, on Kestrel and a
real port, so k6 can reach it as an external process.** The load test's own host
is its own instance, cloned from section 5's template the same way, and it is
never the run section 7 states.

**A caller the script drives is signed in the way section 7 states**, before the
script sends the requests its scenario makes.

**What a load test proves, for its operation:**

- A request that succeeds answers within the time budget the plan states,
  at the caller count the plan states. A threshold on the successful
  requests' duration asserts it.
- A request sent once the plan's limit on how many may run at once is
  already reached is refused rather than run alongside the rest. A check that
  passes only on the refusing status, with a threshold requiring at least one
  pass, asserts it.

**Response times are measured here, and nowhere else.** No test of any other
type asserts one.

**Naming.** One script per operation that needs one, named for the operation.

The example below shows a load test for one operation, sending it more callers at
once than its plan allows and asserting both halves of the rule above.
`PublishShift`, its route, the caller count, the time budget and the scenario's
name are illustrations taken from section 5's own example, not values to look
for.

```javascript
// Example only. The names are illustrations, not names to look for.

// PublishShift.load.js
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  scenarios: {
    publish_shift: {
      executor: 'constant-vus',
      vus: 50,
      duration: '30s',
    },
  },
  thresholds: {
    'http_req_duration{expected_response:true}': ['p(95)<2000'],
    'checks{check:limit_enforced}': ['rate>0'],
  },
};

export default function () {
  const response = http.post(
    'https://localhost:5001/v1/shifts/00000000-0000-0000-0000-000000000000/publication',
  );

  check(
    response,
    { limit_enforced: (r) => r.status === 429 },
    { check: 'limit_enforced' },
  );
}
```
