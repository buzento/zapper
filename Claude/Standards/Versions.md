# Versions Standards

The list of every program and package the product is built, tested and run with,
each with its one approved version. `create-solution` and `create-project` take
every version from it and write none of their own.

Every rule here has a named enforcer in section 5, "Enforcers", including the rules
whose enforcer is nothing.

# 1. Scope

**In scope:** every program that a skill, a recipe, the tests or the pipeline
installs, checks for or runs, and every package that is chosen.

**Out of scope:**

- The packages underneath the chosen ones. The lockfiles hold them.
- The programs that are part of an operating system, such as the shell and its
  standard tools.
- Any program or package not yet chosen.

# 2. The rules

- **Each row holds one exact version.** Never a range, a major version alone or a
  label that moves, such as `latest`.
- **A row is written on the day its program or package is chosen, and not before.**
- **The approved version is the one a full run proved.**
- **Where the build reads a version, the build uses exactly that version**, in
  these forms:
  - the runner by its version label;
  - the .NET SDK in `Code/global.json`, with roll-forward turned off, and
    `actions/setup-dotnet` reading that file;
  - Node as an exact release;
  - CycloneDX installed with its version;
  - each action by its commit, with its version in a comment;
  - each direct npm package with no `^`, `~` or other range;
  - each container image by its exact tag: the integration tests', the local
    database's, and each host's build and runtime images in its `Dockerfile`.

  ```json
  { "sdk": { "version": "10.0.402", "rollForward": "disable" } }
  ```

  ```yaml
  runs-on: ubuntu-26.04
  steps:
    - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
    - uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0
      with:
        global-json-file: Code/global.json
  ```

  ```bash
  dotnet tool install --global CycloneDX --version 6.3.1
  ```

  ```csharp
  new PostgreSqlBuilder("postgres:18.6")
  ```

  ```json
  "react": "19.3.1"
  ```

- **Zapper is API only. Ruled by Francois on 2026-10-22.** The pipeline uses no
  Node, npm or Chromium. The rows for a frontend's programs and packages are kept,
  because `Frontend.md` reads them and is kept for review, but no skill that runs
  installs them.
- **A tool that comes with the runner** reads "the runner image", with the version
  the runner image held when the full run proved it.
- **A program on an engineer's machine** reads "not fixed: the engineer's
  machine", with the version the full run proved. The skills check only that it is
  installed.

# 3. Installed software

| Software | Version | Where it is fixed |
|---|---|---|
| GitHub Actions runner | `ubuntu-26.04` | `.github/workflows/ci.yml`, `runs-on` |
| `actions/checkout` | v7.0.1, commit `3d3c42e5aac5ba805825da76410c181273ba90b1` | `.github/workflows/ci.yml` |
| `actions/setup-dotnet` | v6.0.0, commit `a98b56852c35b8e3190ac28c8c2271da59106c68` | `.github/workflows/ci.yml` |
| `actions/upload-artifact` | v7.0.2, commit `cf430e030ddbb5b0abf93d22962f4752f3646cd9` | `.github/workflows/ci.yml` |
| .NET SDK | 10.0.402 | `Code/global.json` |
| Node | 26.12.0 | `Frontend.md` only, which is not run: Zapper is API only, ruled by Francois on 2026-10-22 |
| npm | 11.20.0 | comes with Node 26.12.0 |
| CycloneDX | 6.3.1 | `.github/workflows/ci.yml`, step "Generate SBOMs" |
| gitleaks | 8.30.1 | `.github/workflows/ci.yml`, step "Secret scan" |
| PostgreSQL, the integration tests' container | `postgres:18.6` | `Code/<Solution>.IntegrationTests/TemplateDatabaseFixture.cs` |
| PostgreSQL, the local database | `postgres:18.6` | `compose.yml` |
| .NET SDK, the build image | `mcr.microsoft.com/dotnet/sdk:10.0.402-noble` | `Code/<Solution>.Migrator/Dockerfile` and each API's `Dockerfile`, the first `FROM` |
| .NET runtime, the Migrator's image | `mcr.microsoft.com/dotnet/runtime:10.0.13-noble-chiseled` | `Code/<Solution>.Migrator/Dockerfile`, the second `FROM` |
| ASP.NET Core runtime, an API's image | `mcr.microsoft.com/dotnet/aspnet:10.0.13-noble-chiseled` | each API's `Dockerfile`, and `Code/<Solution>.Migrator/Dockerfile` once the Migrator references ApiCommon, the second `FROM` |
| Testcontainers' cleanup container | `testcontainers/ryuk:0.14.0` | the `Testcontainers.PostgreSql` package |
| Chromium | 153.0.8010.12, Playwright build 1243 | the `Microsoft.Playwright.Xunit.v3` package |
| git | 2.55.0 | the runner image |
| `gh` | 2.101.0 | the runner image |
| `jq` | 1.8.1 | the runner image |
| PowerShell | 7.6.6 | the runner image |
| Docker | 29.4.2 | the runner image |
| `dotnet-ef` | 10.0.11 | not fixed: the engineer's machine |
| Docker | 28.0.1 | not fixed: the engineer's machine |
| Docker Compose | 2.33.1 | not fixed: the engineer's machine |
| git | 2.50.1 | not fixed: the engineer's machine |
| `gh` | 2.97.0 | not fixed: the engineer's machine |
| `create-vite` | 9.2.1 | the command that runs it, `npm create vite@9.2.1` |
| `shadcn`, the command | 4.21.4 | the command that runs it, `npx --yes shadcn@4.21.4` |

# 4. Installed packages

| Package | Ecosystem | Version | Where it is fixed |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore` | NuGet | 10.0.12 | `Code/Directory.Packages.props` |
| `Microsoft.EntityFrameworkCore.Relational` | NuGet | 10.0.12 | `Code/Directory.Packages.props` |
| `FluentValidation` | NuGet | 12.1.1 | `Code/Directory.Packages.props` |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | NuGet | 10.0.12 | `Code/Directory.Packages.props` |
| `Microsoft.Extensions.Options.DataAnnotations` | NuGet | 10.0.12 | `Code/Directory.Packages.props` |
| `Microsoft.AspNetCore.OpenApi` | NuGet | 10.0.12 | `Code/Directory.Packages.props` |
| `Scalar.AspNetCore` | NuGet | 2.17.10 | `Code/Directory.Packages.props` |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | NuGet | 10.0.3 | `Code/Directory.Packages.props` |
| `Microsoft.EntityFrameworkCore.Design` | NuGet | 10.0.12 | `Code/Directory.Packages.props` |
| `Serilog.Extensions.Hosting` | NuGet | 10.0.0 | `Code/Directory.Packages.props` |
| `Serilog.Settings.Configuration` | NuGet | 10.0.0 | `Code/Directory.Packages.props` |
| `Serilog.Sinks.Console` | NuGet | 6.1.1 | `Code/Directory.Packages.props` |
| `Microsoft.Extensions.Hosting` | NuGet | 10.0.12 | `Code/Directory.Packages.props` |
| `Microsoft.Extensions.Configuration.KeyPerFile` | NuGet | 10.0.12 | `Code/Directory.Packages.props` |
| `xunit.v3` | NuGet | 4.0.1 | `Code/Directory.Packages.props` |
| `Shouldly` | NuGet | 4.3.0 | `Code/Directory.Packages.props` |
| `Microsoft.Extensions.TimeProvider.Testing` | NuGet | 10.11.0 | `Code/Directory.Packages.props` |
| `Testcontainers.PostgreSql` | NuGet | 4.15.0 | `Code/Directory.Packages.props` |
| `Microsoft.AspNetCore.Mvc.Testing` | NuGet | 10.0.12 | `Code/Directory.Packages.props` |
| `Microsoft.Playwright.Xunit.v3` | NuGet | 1.63.0 | `Code/Directory.Packages.props` |
| `Deque.AxeCore.Playwright` | NuGet | 4.13.0 | `Code/Directory.Packages.props` |
| `@fontsource-variable/geist` | npm | 5.3.0 | `Code/<Solution>.<Frontend>/package.json` |
| `@tanstack/react-query` | npm | 5.104.2 | `Code/<Solution>.<Frontend>/package.json` |
| `class-variance-authority` | npm | 0.7.1 | `Code/<Solution>.<Frontend>/package.json` |
| `cn` | npm | 0.4.0 | `Code/<Solution>.<Frontend>/package.json` |
| `lucide-react` | npm | 1.57.0 | `Code/<Solution>.<Frontend>/package.json` |
| `openapi-fetch` | npm | 0.17.0 | `Code/<Solution>.<Frontend>/package.json` |
| `radix-ui` | npm | 1.8.1 | `Code/<Solution>.<Frontend>/package.json` |
| `react` | npm | 19.3.1 | `Code/<Solution>.<Frontend>/package.json` |
| `react-dom` | npm | 19.3.1 | `Code/<Solution>.<Frontend>/package.json` |
| `react-router` | npm | 8.4.0 | `Code/<Solution>.<Frontend>/package.json` |
| `shadcn` | npm | 4.21.5 | `Code/<Solution>.<Frontend>/package.json` |
| `tailwindcss` | npm | 4.3.3 | `Code/<Solution>.<Frontend>/package.json` |
| `tw-animate-css` | npm | 1.4.0 | `Code/<Solution>.<Frontend>/package.json` |
| `@tailwindcss/vite` | npm | 4.3.3 | `Code/<Solution>.<Frontend>/package.json` |
| `@testing-library/jest-dom` | npm | 7.0.1 | `Code/<Solution>.<Frontend>/package.json` |
| `@testing-library/react` | npm | 16.3.3 | `Code/<Solution>.<Frontend>/package.json` |
| `@testing-library/user-event` | npm | 14.6.8 | `Code/<Solution>.<Frontend>/package.json` |
| `@types/node` | npm | 24.19.2 | `Code/<Solution>.<Frontend>/package.json` |
| `@types/react` | npm | 19.3.0 | `Code/<Solution>.<Frontend>/package.json` |
| `@types/react-dom` | npm | 19.3.0 | `Code/<Solution>.<Frontend>/package.json` |
| `@vitejs/plugin-react` | npm | 6.1.2 | `Code/<Solution>.<Frontend>/package.json` |
| `jsdom` | npm | 30.1.2 | `Code/<Solution>.<Frontend>/package.json` |
| `msw` | npm | 3.0.3 | `Code/<Solution>.<Frontend>/package.json` |
| `oxc-transform-react` | npm | 0.152.0 | `Code/<Solution>.<Frontend>/package.json` |
| `typescript` | npm | 6.0.3 | `Code/<Solution>.<Frontend>/package.json` |
| `vite` | npm | 8.3.4 | `Code/<Solution>.<Frontend>/package.json` |
| `vitest` | npm | 5.0.4 | `Code/<Solution>.<Frontend>/package.json` |
| `vitest-axe` | npm | 0.1.0 | `Code/<Solution>.<Frontend>/package.json` |
| `openapi-typescript` | npm | 7.13.0 | `Code/Tools/ContractTypes/package.json` |
| `@types/node` | npm | 26.6.5 | `Code/Tools/ContractTypes/package.json` |
| `typescript` | npm | 5.9.3 | `Code/Tools/ContractTypes/package.json` |

# 5. Enforcers

| Rule | Enforcer |
|---|---|
| Each row holds one exact version | Nothing. A written rule only |
| The approved version is the one a full run proved | Nothing. A written rule only |
| A NuGet package's version | The pipeline's step "Restore, locked", which fails when a `packages.lock.json` changes |
| An npm package's version | The pipeline's step "npm ci, locked", which installs from `Code/package-lock.json` |
| A direct npm package written with no range | Nothing. A range in a `package.json` still installs |
| The runner, each action, Node, CycloneDX and gitleaks | The pipeline file, which installs exactly what it names |
| The .NET SDK | `Code/global.json`: `dotnet` stops with "A compatible .NET SDK was not found." when the SDK it names is not installed |
| The integration tests' container image | The integration tests, which pull the exact tag |
| The local database's image | `docker compose`, which pulls the exact tag `compose.yml` names |
| Each host's build and runtime images | `docker compose`, which builds each host's image from the exact tags its `Dockerfile` names |
| Testcontainers' cleanup container and Chromium | The package that fixes each one, through the step "Restore, locked" |
| A tool that comes with the runner | Nothing. The runner image's release changes under its label |
| A program on an engineer's machine | Nothing. A written rule only |
| A row is written on the day its program or package is chosen, and not before | Nothing |
