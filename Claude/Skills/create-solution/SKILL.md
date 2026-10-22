---
name: create-solution
description: >-
  Creates a solution in a repository that has none: the solution file, the
  solution-wide build files, the four shared projects,
  the two test projects, the local database and the Migrator in Docker with
  their compose.yml, the day-one Docs/ inventories and the CI/CD pipeline. Ends
  by committing, pushing and opening a pull request into dev, and deletes the
  local branch once told it is merged. Runs once per repository, ever, and
  creates no API and no frontend. Use when the user says
  "create-solution", "create the solution", "set up the repository" or "start
  a new product". Do NOT use to add an API or a frontend (that is
  create-project), or for writing a feature's own code (that is feature-execution).
---

# Create solution

This skill creates a solution to the standards in `Claude/Standards/`, and never
to habit or to what another product once did. Every file it writes traces to a
sentence in a standard; nothing here is invented.

Every version this skill writes is read from `Claude/Standards/Versions.md`: each
`<Versions.md: ROW>` below is replaced by the version in the row whose first
column reads ROW, without backticks; an action's `<Versions.md: ROW, commit>` and
`<Versions.md: ROW, version>` by that row's commit and its version; and where two
rows share a name, by the row whose "Where it is fixed" names the file being
written.

**It runs once per repository, ever.** It creates the solution and the parts that
every API shares. `create-project` adds one API, every time the product grows
one, and it is the only thing that does.

**It creates no API and no frontend.** Both are named in the list below, so
nobody waits here for one.

**Zapper is API only, so this skill writes nothing a frontend needs.** No npm
workspace root, no `Code/package.json`, no contract type generator, no Node step
in the pipeline and no Node section in `.gitignore`. Ruled by Francois on
2026-10-22. `create-project`'s `Frontend.md` and `FrontendCode.md` are kept for
review, and are not run.

> **Every path in this skill is relative to the repository root of the solution
> being created, never to this file's own location.** `Code/<Solution>.slnx`
> means `<repo root>/Code/<Solution>.slnx`. Resolve every path below against the
> repository root. If you cannot determine it, ask. Do not guess.

## The solution's name, and the values this skill asks for

**The solution's name is written `<Solution>` throughout this document**, and so
is every name derived from it. `BackendCode.md` section 2 states the placeholder
and its shape: one PascalCase token of letters only, carrying no dot, no hyphen,
no underscore, no space and no digit, with the words run together where the
product's name is more than one word. `BackendCode.md` section 18's acronym rule
applies to it, so an acronym arrives as `Api` and not `API`.

**A value that does not fit is refused, and is never adjusted to fit.**
`BackendCode.md` section 2 states that, and states why: `BackendCode.md` section
9 concatenates the value into every registration method's name, so a dot in the
value would make one of those names read to the compiler as namespace-qualified.
Ask for the value again rather than mend it.

| What this skill asks for | What reads it |
|---|---|
| the feature title | the branch, the commit subject and the pull request title, per `Branching.md` section 2 |
| `<Solution>` | every file this skill writes |
| `<DatabaseName>`, the database's name | `<DatabaseName>Context` and `<DatabaseName>ContextDesignTimeFactory` in step 11, the connection strings step 12 writes, and the names of the two test databases step 14 creates |
| `<Port>`, the port the local database listens on | `compose.yml` and the connection string, both of which step 12 writes |

**This table grows as this document does.** Nothing is asked for that no step
below reads.

**`<DatabaseName>` takes the shape `BackendCode.md` section 10 fixes**, which is
the shape `<Solution>` carries above and is there for the same reason, since
`BackendCode.md` section 10 concatenates it into a C# class name. That section
states it in full and it is not restated here. A value that does not fit is
refused, and is never adjusted to fit.

**`<Port>` is asked, with a free port suggested.** It is a whole number from 1
to 65535, and it is free on this machine where this command exits 1, because
nothing answers on it:

```bash
nc -z 127.0.0.1 <Port>
```

**Suggest the first port from 5433 to 5500 that is free**, read with that same
command, and let the engineer accept it or give another. 5432 is not suggested,
because it is PostgreSQL's own default and the port an installed server most
often holds. A port the engineer gives that does not fit, or is not free, is
refused and asked for again, and is never adjusted to fit. **The port is checked
here and not left to step 12**, because by step 12 this skill has written most
of the solution, and the preconditions stop a second run over it. Measured: the
command exited 0 on a port PostgreSQL held in Docker and on a port a local
server held, and 1 on a free one, on macOS on 2026-10-22. Exit 0 also prints
"Connection to 127.0.0.1 port <Port> succeeded!". `nc` replaced a Node command,
because nothing else in this skill needs Node. Ruled by Francois on 2026-10-22.
Not measured on Windows or Linux.

**The Migrator's role is not asked for.** `BackendCode.md` section 18 derives it
from the project's name, so it is `<Solution>MigratorRole`.

**The database and the role are created by step 12**, from `compose.yml`, which
step 12 writes and starts. Step 12's run of `<Solution>.Migrator` is then what
proves the database's name, the port and the role from the host, because nothing
before it connects to the database from outside Docker.

## What this skill creates, in this order

1. `Code/<Solution>.slnx`
2. `.gitignore` and `.gitattributes`, at the repository root
3. `Code/Directory.Build.props`, and `Docs/AcceptedVulnerabilities.md` with its
   heading and an empty table, which every build reads
4. `Code/Directory.Packages.props`
5. `Docs/Contracts/`
6. Nothing: Zapper is API only, so there is no npm workspace root
7. `Code/<Solution>.Domain`, with the two Error entities
8. `Code/<Solution>.Infrastructure`, as a shell
9. `Code/<Solution>.Migrator`, as a shell
10. `Code/<Solution>.ApiCommon`, with the response wiring, validation, the
    published document, versioning, paging, logging, health, the shared
    middleware and the security settings
11. `Code/<Solution>.Infrastructure/Persistence/`, with the schema, the
    certificate loading, the relocated history table and the seed migration
12. `Code/<Solution>.Migrator`'s real `Program.cs`, its committed settings file
    and launch profile, `Code/Certificates/.gitignore`, `Code/.dockerignore`,
    `Code/<Solution>.Migrator/Dockerfile` and `compose.yml`, the local database
    and the Migrator's container they start, and the run that applies the
    migration
13. `Code/<Solution>.UnitTests`, with `Code/global.json` and the unit tests of
    the error message manager
14. `Code/<Solution>.IntegrationTests`, with the database fixture and the four
    tests that cover the whole solution
15. `Docs/CryptographicInventory.md` and `Docs/InfrastructureInventory.md`, each
    with its heading and an empty table
16. `.github/workflows/ci.yml`, the CI/CD pipeline

Step 17 then verifies all of it, and the skill reports what it created and stops.

## What this skill never creates

- **An API project, and a frontend.** `create-project` creates each API, and
  refuses a frontend, because Zapper is API only.
- **`.editorconfig` and `BannedSymbols.txt`.** Both belong to a separate,
  solution-wide enforcer pass that runs once a solution exists to hold them.
  `Code/Directory.Build.props` is that pass's file to add to, and not its file to
  create.
- **A README, and nothing of that kind under `Docs/`.** This skill writes no
  directions into the repository. It states at the end what it reports to whoever
  runs it, and recording those steps in the product's own documentation is
  theirs.
- **Anything a frontend needs.** Step 6 states why.
- **Anything that deploys.** The pipeline step 16 writes has no deploy step and
  names no deployment platform. What a part's deployment must do is reported at
  the end.

## Preconditions

These hold for every use of this skill.

- **Confirm the repository root before writing anything.** Every path in this
  document is resolved against it.
- **Run every command from the repository root**, `cd`-ing into `Code/` only
  where a step says to.
- **Check that `Code/` holds no `.slnx` file.** If one is there, this skill has
  already run in this repository. Stop, say so, and say that adding an API is
  `create-project`'s. This skill never writes over a solution it
  did not create in this run.
- **Check that `dotnet ef` is installed**, with `dotnet ef --version`. Step 11
  generates the seed migration with it and fails without it, so it is checked
  before anything is written. If it is absent, stop and raise it to whoever
  ran this skill to resolve. This skill installs nothing on the machine it runs
  on.
- **Check that Docker is running**, with `docker info`, written plain. Step 12
  runs the local database in it, step 14's
  tests start a PostgreSQL container on every `dotnet test`, and fail without a
  running Docker, and step 17's verify runs them. **Any exit other than 0 means it is not running: stop and raise
  it to whoever ran this skill to resolve.** This skill installs nothing and starts
  nothing on the machine it runs on. A warning on standard error with exit 0 is
  not a failure. **No `--format` is added to the command**, because measured, `docker info --format` exits 0
  with no daemon at all, while plain `docker info` exits 1.
- **Check that Docker is version 26 or later**, with
  `docker version --format '{{.Server.Version}}'`, which prints the engine's
  version alone, such as `28.0.1`. Step 12's `compose.yml` mounts each host's own
  certificate folder by a volume's `subpath`, which needs Docker Engine 26 or
  later. **A first number below 26 means it is too old: stop and raise it to
  whoever ran this skill to resolve.** Measured on Docker Engine 28.0.1 only.
  The check above has already proved the daemon is running, which this command
  does not.
- **Check that Docker Compose is installed**, with `docker compose version`.
  Expect exit 0. Step 12 starts the local database and the Migrator with it.
- **Create the folders under `Code/` that are missing.** Every step below writes
  into `Code/`.
- **Ask for the feature title before writing anything, every run, with no
  exception.** `Branching.md` section 2 derives every branch name, commit subject
  and pull request title from it, and `Branching.md` section 3 states
  that the feature branch is created before any code is written. There is no
  exception for this skill's one run. **`dev` is the one branch this skill
  needs**, on the server, created by hand before this skill is ever invoked,
  because the feature branch is cut from it. This skill reads no other branch.
- **Check that `gh` is signed in**, with `gh auth status`. Expect exit 0. The
  last section opens the pull request with it. Where it fails, stop and say that
  `gh auth login` signs in.
- **Create the feature branch if missing, check it out if present**, per
  `Branching.md` section 3, cut from the current state of `dev` on the server:

  ```
  git fetch origin dev
  git switch <branch> 2>/dev/null || git switch -c <branch> --no-track origin/dev
  ```

  where `<branch>` is `Feature/<FeatureTitle>`, and `<FeatureTitle>` is built
  from the feature title per `Branching.md` section 2's naming scheme: the first
  letter of each word capitalised, every other letter left as it was given, each
  space written as an underscore, and anything that is not a letter, a digit or
  an underscore dropped. A title that leaves no letter and no digit once that is
  done is refused, and asked for again: "!!! ???" leaves `_`, which would cut
  `Feature/_`. Ruled by Francois on 2026-10-21. **If `git fetch origin dev` fails,
  `dev` is not on the server: stop and say so** rather than cut the branch from
  anything else.
- **Never write a file directly on `main`, `dev` or `test`.** Check with
  `git symbolic-ref --short HEAD` and confirm it names the branch just created or
  checked out. If it names `main`, `dev` or `test` instead, stop and say so
  rather than write anything.
- **A step that changes a file that already exists uses exactly one of three
  forms, and names it:**

  1. **Whole.** "Replace `<path>` whole:", followed by the file's full text.
  2. **Command.** The step's own command, run as written, where a tool owns the
     file: dotnet, openssl, `rm`, `cp`. A file a tool later changes is written
     first in the form that tool keeps.
  3. **Exact edit.** "In `<path>`, after the line `<anchor>`", or "before the
     line", or "replace the line `<old>` with", followed by the new lines with
     their indent. The anchor is a whole line, quoted, that occurs exactly once in
     the file, or the end of the file. Nothing else in the file changes. The edit
     is made as one exact text replacement with the Edit tool, never by a script
     written for the run.

---

### 1. The solution file

Write `Code/<Solution>.slnx`:

```xml
<Solution>
  <Project Path="<Solution>.Domain/<Solution>.Domain.csproj" />
  <Project Path="<Solution>.Infrastructure/<Solution>.Infrastructure.csproj" />
  <Project Path="<Solution>.Migrator/<Solution>.Migrator.csproj" />
</Solution>
```

**The opening and closing `<Solution>` tags are the file format's own root
element and are not substituted.** They are the literal text `<Solution>` in
every solution file this skill ever writes, whatever the solution is called. Only
the three `Path` values carry the value asked for. The collision is in the names
alone, since one is a placeholder in this document and the other is XML in the
file, and nothing about it breaks.

`BackendCode.md` section 2 states that the solution file is
`Code/<Solution>.slnx` in the XML solution format, and that this format is what
the SDK produces when nothing asks for the other one, measured and true. The file
is written directly rather than through `dotnet new sln` and `dotnet sln add`,
because the three projects it lists are already known. Measured against SDK
10.0.302: those two commands produce exactly the shape above, an empty
`<Solution></Solution>` element with one `<Project Path="..." />` child per
project.

`<Solution>.ApiCommon` is not listed, because step 1 runs before it exists.
Step 10 creates it and adds it to this file. The two test projects are not listed
for the same reason, and steps 13 and 14 add them.

### 2. `.gitignore` and `.gitattributes`

**At the repository root, a sibling of `Code/`, `Docs/` and `.github/`, never
inside `Code/`.** A `.gitignore` only protects the directory it sits in and
everything below it, so one written at `Code/.gitignore` would leave `Docs/` and
`.github/` uncovered. Measured: `git status` inside a `Docs/` holding a
`.DS_Store` still shows it as untracked when the only `.gitignore` in the
repository is under `Code/`.

No standard states the body of this file. Its first three sections are combined
from three of GitHub's own canonical templates, fetched and used as published
rather than written from memory, because a hand-written list is exactly how a
real exclusion gets missed. They are lightly deduplicated only where two of the
three repeat the same line: the Windows installer extensions are dropped from
the Windows section, being already in the Visual Studio template. **There is no
Node section**, because Zapper is API only. Ruled by Francois on 2026-10-22. Tested
against the day-one file set this skill creates: every real file stages
correctly, `packages.lock.json` is never touched, since `BackendCode.md` section
2 requires it committed, and the files the file exists to catch, being `.env`,
`.DS_Store`, `*.user` and `.vs/`, all do get caught.

**The fourth section is this process's own**, and the two standards are the reason
for it. `BackendCode.md` section 14 states that a certificate file is never
committed, and the ruling behind this skill places the authority certificate and
each database role's certificate and key at `Code/Certificates/`. That folder
carries its own `.gitignore`, written by step 12, which ignores every
file in it but itself, so the folder is committed holding that file alone. The
five patterns catch a certificate or a key anywhere in the repository, that
folder included. **This file does not name the folder**, because git cannot bring
the folder's own `.gitignore` back in once the folder is excluded, and no clone
would then carry the folder. Measured: the templates together cover `*.pfx`
and nothing else certificate-shaped, which is why `*.pfx` is not repeated and the
other five are added.

Write `.gitignore`:

```gitignore
## Visual Studio / .NET, from https://github.com/github/gitignore/blob/main/VisualStudio.gitignore
# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates
*.env

# User-specific files (MonoDevelop/Xamarin Studio)
*.userprefs

# Mono auto generated files
mono_crash.*

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/

[Dd]ebug/x64/
[Dd]ebugPublic/x64/
[Rr]elease/x64/
[Rr]eleases/x64/
bin/x64/
obj/x64/

[Dd]ebug/x86/
[Dd]ebugPublic/x86/
[Rr]elease/x86/
[Rr]eleases/x86/
bin/x86/
obj/x86/

[Ww][Ii][Nn]32/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
[Aa][Rr][Mm]64[Ee][Cc]/
bld/
[Oo]bj/
[Oo]ut/
[Ll]og/
[Ll]ogs/

# Build results on 'Bin' directories
**/[Bb]in/*
# Uncomment if you have tasks that rely on *.refresh files to move binaries
# (https://github.com/github/gitignore/pull/3736)
#!**/[Bb]in/*.refresh

# Visual Studio 2015/2017 cache/options directory
.vs/
# Uncomment if you have tasks that create the project's static files in wwwroot
#wwwroot/

# Visual Studio 2017 auto generated files
Generated\ Files/

# MSTest test Results
[Tt]est[Rr]esult*/
[Bb]uild[Ll]og.*
*.trx

# NUnit
*.VisualState.xml
TestResult.xml
nunit-*.xml

# Approval Tests result files
*.received.*

# Build Results of an ATL Project
[Dd]ebugPS/
[Rr]eleasePS/
dlldata.c

# Benchmark Results
BenchmarkDotNet.Artifacts/

# .NET Core
project.lock.json
project.fragment.lock.json
artifacts/
.artifacts/

# ASP.NET Scaffolding
ScaffoldingReadMe.txt

# StyleCop
StyleCopReport.xml

# Files built by Visual Studio
*_i.c
*_p.c
*_h.h
*.ilk
*.meta
*.obj
*.idb
*.iobj
*.pch
*.pdb
*.ipdb
*.pgc
*.pgd
*.rsp
# but not Directory.Build.rsp, as it configures directory-level build defaults
!Directory.Build.rsp
*.sbr
*.tlb
*.tli
*.tlh
*.tmp
*.tmp_proj
*_wpftmp.csproj
*.log
*.tlog
*.vspscc
*.vssscc
.builds
*.pidb
*.svclog
*.scc

# Chutzpah Test files
_Chutzpah*

# Visual C++ cache files
ipch/
*.aps
*.ncb
*.opendb
*.opensdf
*.sdf
*.cachefile
*.VC.db
*.VC.VC.opendb

# Visual Studio profiler
*.psess
*.vsp
*.vspx
*.sap

# Visual Studio Trace Files
*.e2e

# TFS 2012 Local Workspace
$tf/

# Guidance Automation Toolkit
*.gpState

# ReSharper is a .NET coding add-in
_ReSharper*/
*.[Rr]e[Ss]harper
*.DotSettings.user

# TeamCity is a build add-in
_TeamCity*

# DotCover is a Code Coverage Tool
*.dotCover

# AxoCover is a Code Coverage Tool
.axoCover/*
!.axoCover/settings.json

# Coverlet is a free, cross platform Code Coverage Tool
coverage*.json
coverage*.xml
coverage*.info

# Visual Studio code coverage results
*.coverage
*.coveragexml

# NCrunch
_NCrunch_*
.NCrunch_*
.*crunch*.local.xml
nCrunchTemp_*

# MightyMoose
*.mm.*
AutoTest.Net/

# Web workbench (sass)
.sass-cache/

# Installshield output folder
[Ee]xpress/

# DocProject is a documentation generator add-in
DocProject/buildhelp/
DocProject/Help/*.HxT
DocProject/Help/*.HxC
DocProject/Help/*.hhc
DocProject/Help/*.hhk
DocProject/Help/*.hhp
DocProject/Help/Html2
DocProject/Help/html

# Click-Once directory
publish/

# Publish Web Output
*.[Pp]ublish.xml
*.azurePubxml
# Note: Comment the next line if you want to checkin your web deploy settings,
# but database connection strings (with potential passwords) will be unencrypted
*.pubxml
*.publishproj

# Microsoft Azure Web App publish settings. Comment the next line if you want to
# checkin your Azure Web App publish settings, but sensitive information contained
# in these scripts will be unencrypted
PublishScripts/

# NuGet Packages
*.nupkg
# NuGet Symbol Packages
*.snupkg
# The packages folder can be ignored because of Package Restore
**/[Pp]ackages/*
# except build/, which is used as an MSBuild target.
!**/[Pp]ackages/build/
# Uncomment if necessary however generally it will be regenerated when needed
#!**/[Pp]ackages/repositories.config
# NuGet v3's project.json files produces more ignorable files
*.nuget.props
*.nuget.targets

# Microsoft Azure Build Output
csx/
*.build.csdef

# Microsoft Azure Emulator
ecf/
rcf/

# Windows Store app package directories and files
AppPackages/
BundleArtifacts/
Package.StoreAssociation.xml
_pkginfo.txt
*.appx
*.appxbundle
*.appxupload

# Visual Studio cache files
# files ending in .cache can be ignored
*.[Cc]ache
# but keep track of directories ending in .cache
!?*.[Cc]ache/

# Others
ClientBin/
~$*
*~
*.dbmdl
*.dbproj.schemaview
*.jfm
*.pfx
*.publishsettings
orleans.codegen.cs

# Including strong name files can present a security risk
# (https://github.com/github/gitignore/pull/2483#issue-259490424)
#*.snk

# Since there are multiple workflows, uncomment next line to ignore bower_components
# (https://github.com/github/gitignore/pull/1529#issuecomment-104372622)
#bower_components/

# RIA/Silverlight projects
Generated_Code/

# Backup & report files from converting an old project file
# to a newer Visual Studio version. Backup files are not needed,
# because we have git ;-)
_UpgradeReport_Files/
Backup*/
UpgradeLog*.XML
UpgradeLog*.htm
ServiceFabricBackup/
*.rptproj.bak

# SQL Server files
*.mdf
*.ldf
*.ndf

# Business Intelligence projects
*.rdl.data
*.bim.layout
*.bim_*.settings
*.rptproj.rsuser
*- [Bb]ackup.rdl
*- [Bb]ackup ([0-9]).rdl
*- [Bb]ackup ([0-9][0-9]).rdl

# Microsoft Fakes
FakesAssemblies/

# GhostDoc plugin setting file
*.GhostDoc.xml

# Node.js Tools for Visual Studio
.ntvs_analysis.dat
node_modules/

# Visual Studio 6 build log
*.plg

# Visual Studio 6 workspace options file
*.opt

# Visual Studio 6 auto-generated workspace file (contains which files were open etc.)
*.vbw

# Visual Studio 6 workspace and project file (working project files containing files to include in project)
*.dsw
*.dsp

# Visual Studio 6 technical files
*.ncb
*.aps

# Visual Studio LightSwitch build output
**/*.HTMLClient/GeneratedArtifacts
**/*.DesktopClient/GeneratedArtifacts
**/*.DesktopClient/ModelManifest.xml
**/*.Server/GeneratedArtifacts
**/*.Server/ModelManifest.xml
_Pvt_Extensions

# Paket dependency manager
**/.paket/paket.exe
paket-files/

# FAKE - F# Make
**/.fake/

# CodeRush personal settings
**/.cr/personal

# Python Tools for Visual Studio (PTVS)
**/__pycache__/
*.pyc

# Cake - Uncomment if you are using it
#tools/**
#!tools/packages.config

# Tabs Studio
*.tss

# Telerik's JustMock configuration file
*.jmconfig

# BizTalk build output
*.btp.cs
*.btm.cs
*.odx.cs
*.xsd.cs

# OpenCover UI analysis results
OpenCover/

# Azure Stream Analytics local run output
ASALocalRun/

# MSBuild Binary and Structured Log
*.binlog
MSBuild_Logs/

# AWS SAM Build and Temporary Artifacts folder
.aws-sam

# NVidia Nsight GPU debugger configuration file
*.nvuser

# MFractors (Xamarin productivity tool) working folder
**/.mfractor/

# Local History for Visual Studio
**/.localhistory/

# Visual Studio History (VSHistory) files
.vshistory/

# BeatPulse healthcheck temp database
healthchecksdb

# Backup folder for Package Reference Convert tool in Visual Studio 2017
MigrationBackup/

# Ionide (cross platform F# VS Code tools) working folder
**/.ionide/

# Fody - auto-generated XML schema
FodyWeavers.xsd

# VS Code files for those working on multiple tools
.vscode/*
!.vscode/settings.json
!.vscode/tasks.json
!.vscode/launch.json
!.vscode/extensions.json
!.vscode/*.code-snippets

# Local History for Visual Studio Code
.history/

# Built Visual Studio Code Extensions
*.vsix

# Windows Installer files from build outputs
*.cab
*.msi
*.msix
*.msm
*.msp

# Official VS Code C# Dev Kit Extension exclusion
#*.lscache

## macOS, from https://github.com/github/gitignore/blob/main/Global/macOS.gitignore
.DS_Store
.AppleDouble
.LSOverride
._*

## Windows, from https://github.com/github/gitignore/blob/main/Global/Windows.gitignore
Thumbs.db
Thumbs.db:encryptable
ehthumbs.db
ehthumbs_vista.db
[Dd]esktop.ini
$RECYCLE.BIN/
*.lnk
*.stackdump

## Certificates and keys, this process's own
# Code/Certificates/ holds the database authority's certificate and each
# database role's own certificate and key, and its own .gitignore keeps them
# out. These patterns catch a certificate or a key anywhere in the repository.
# BackendCode.md section 14 states that a certificate file is never committed.
*.crt
*.key
*.pem
*.cer
*.p12

.mcp.json
```

**Almost everything in this file matches nothing yet, and that is fine.** The
Visual Studio template covers far more than this stack uses, Xamarin, F# and
Silverlight among them, none of which this process touches, and an ignore
pattern that never matches costs nothing. What it buys is the same file every
other .NET project already trusts, rather than a shorter list assembled by
guessing what a developer might do wrong.

**`.gitattributes` goes at the repository root too, beside `.gitignore`. Ruled
by Francois on 2026-10-21.** It makes git store every text file, and check it
out on every machine, with a line feed alone at the end of each line. Git for
Windows otherwise checks text files out with a carriage return before each line
feed, and the reviewers are expected to use Windows. The files this repository
runs inside Linux would then carry them: the scripts in `compose.yml`, the
Dockerfiles, and the solution file the `certificates` script reads with `sed`.
`text=auto` leaves a file that git finds to be binary as it is.

Write `.gitattributes`:

```gitattributes
* text=auto eol=lf
```

Measured on 2026-10-21, on clones of a repository holding what this skill and
`create-project` write, with `core.autocrlf=true`, Git for Windows' usual
setting: without this file, `compose.yml`, the solution file, an API's
Dockerfile and the Migrator's `Program.cs` were checked out with a carriage
return on every line; with it, on none. The .NET `web` template, which
`create-project` runs, writes `Properties/launchSettings.json` with carriage
returns, and with this file git stores it with line feeds alone when it is
committed. Not measured: a clone on Windows itself, and whether the carriage
returns would have broken anything.

### 3. `Directory.Build.props`

Write `Code/Directory.Build.props`:

```xml
<Project>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <MSBuildTreatWarningsAsErrors>true</MSBuildTreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>

  <ItemGroup>
    <NuGetAuditSuppress Include="$([MSBuild]::Unescape($([System.Text.RegularExpressions.Regex]::Replace($([System.Text.RegularExpressions.Regex]::Replace($([System.IO.File]::ReadAllText('$(MSBuildThisFileDirectory)../Docs/AcceptedVulnerabilities.md')), '(?m)^\| *(GHSA-[0-9a-z]{4}-[0-9a-z]{4}-[0-9a-z]{4}) *\|.*$|[\s\S]', '$1;')), 'GHSA-', 'https://github.com/advisories/GHSA-'))))" />
  </ItemGroup>

</Project>
```

`BackendCode.md` section 2's settings table states all six of the settings below
`TargetFramework`, and states that every project takes them from this one file
and that no project sets any of them for itself. `TargetFramework` is
centralised here too, the same way and for the same reason: `BackendCode.md`
section 2 also states "every project targets `net10.0`" as a solution-wide fact,
and a value fixed once for the whole solution does not belong copied into every
project file.

**`TreatWarningsAsErrors` and `MSBuildTreatWarningsAsErrors` are both needed and
neither replaces the other.** `BackendCode.md` section 2 states the division:
the first reaches the compiler's warnings and the second reaches the build
engine's, including those NuGet, the SDK and a third-party target raise. A
package version conflict is one of the second kind, so with only the first
setting a build can resolve one project's dependency at a lower version than
another project compiled against and still report success. Measured against SDK
10.0.302: the conflict step 4 prevents was a warning on a build reporting
success with only the first setting, and a build failure with both.

**A warning that turns out to be noise is exempted by name**, and neither
setting is turned off. `BackendCode.md` section 2 states that too.

**The item group reads the Finding column of `Docs/AcceptedVulnerabilities.md`
into `NuGetAuditSuppress`**, so that every build, local ones included, passes a
NuGet finding that file holds and still fails on any other. It reads the file,
keeps the first cell of every row whose first cell is a GHSA identifier and drops
every other character, puts `https://github.com/advisories/` in front of each
identifier, and unescapes the result so that its semicolons split it into items.
It adds no property, so it adds no name. Measured: **a held row suppresses its advisory**, and a finding no
row holds still fails the restore and the build; **the headings alone add no
item**, so a solution with no vulnerable package passes; and **a missing file
fails every restore with `MSB4184`**, "Could not find file". Without
`[MSBuild]::Unescape` the result is one item holding every semicolon, because
MSBuild escapes what a property function returns. **It does not reach a restore
that gets no vulnerability data**, which still fails with `NU1900`.

**Not written here, on purpose:** the analyzer package references, the analysis
level and the documentation-file setting. Those belong to the solution-wide
enforcer pass named above, the same pass that writes `.editorconfig` and
`BannedSymbols.txt`.

Write `Docs/AcceptedVulnerabilities.md` in this step too, because the item group
reads it on every restore and build, and the restore of step 11,
"`<Solution>.Infrastructure/Persistence/`", is the first:

```markdown
# Accepted vulnerabilities

| Finding | Package | Why it cannot be fixed | Date accepted | Date acceptance ends |
|---|---|---|---|---|
```

**`Security.md` section 13 states this file and its rule**, and the columns of its
table are the parts that rule names, in the order the rule gives them. **It stays
empty until a finding cannot be fixed.** `Security.md` section 13 accepts a
vulnerability only by an entry in this file, and the restore and the build of
step 17, "Verify", are the proof that no NuGet package this skill adds carries a known
vulnerability today.

### 4. `Directory.Packages.props`

Write `Code/Directory.Packages.props`:

```xml
<Project>

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="<Versions.md: Microsoft.EntityFrameworkCore>" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="<Versions.md: Microsoft.EntityFrameworkCore.Relational>" />
  </ItemGroup>

</Project>
```

`BackendCode.md` section 2 states both settings. `ManagePackageVersionsCentrally`
puts every package version in this one file, as a `PackageVersion` entry, with a
project's own `PackageReference` naming the package and never carrying a version.
`CentralPackageTransitivePinningEnabled` extends that to the versions a project
resolves through its dependencies rather than names itself.

**The two entries are there because without them the solution builds wrong and
says it succeeded.** Measured against SDK 10.0.302, Npgsql 10.0.3 and EF Core
10.0.12: an API project on the Web SDK referencing `<Solution>.Infrastructure`
resolves EF Core to 10.0.4 in its own graph while Infrastructure compiles against
10.0.12, because Infrastructure's direct reference to
`Microsoft.EntityFrameworkCore.Design` lifts it there and that reference does not
flow. Both settings on, plus these two entries, resolves everything at 10.0.12
and the solution builds with 0 warnings and 0 errors.

**Which packages need an entry is this product's dependency graph and not a
rule**, which is why `BackendCode.md` section 2 states the setting and names no
package. These two are named here because they are the two the projects steps 7
to 9 create need. **Five later steps add fourteen more between them**: step 10,
"`<Solution>.ApiCommon`", adds five, for the packages `<Solution>.ApiCommon`
references; step 11, "`<Solution>.Infrastructure/Persistence/`", adds five, for
the packages `<Solution>.Infrastructure` needs once it holds the schema; step 12,
"`<Solution>.Migrator`'s real `Program.cs`", adds one, for the package that
`Program.cs` needs; step 13, "`<Solution>.UnitTests`", adds two, for the test
library and the assertion library; and step 14, "`<Solution>.IntegrationTests`",
adds one, for Testcontainers. Each replaces this file whole, keeping every entry
before it as it was.

**Both entries are written on day one, although nothing references either
package yet.** Measured against SDK 10.0.302 on the three projects steps 7 to 9
write, before step 10 adds `<Solution>.ApiCommon` and its five entries, with both
settings on and both entries present: `dotnet restore` succeeded,
`dotnet build --no-restore` reported 0 warnings and 0 errors,
`dotnet restore --locked-mode` succeeded against the lock files restore had just
written, and `dotnet test --no-build` exited 0 with no test project in the
solution. So a `PackageVersion` entry for a package nothing references costs
nothing, and neither setting raises a warning in a solution holding no package at
all. The alternative, adding them in the step that first needs them, would put
the fix in the one place a reader of this file would not look for it.

### 5. `Docs/Contracts/`

**Check whether `Docs/Contracts/` exists. If it does not, create it, holding one
empty `.gitkeep`.**

`CLAUDE.md` fixes a contract's path at `Docs/Contracts/<Api>/<version>/<route>/`,
so this folder is the root of something every API will have and no API has yet.
The `.gitkeep` is there because git tracks files and not folders, so without it
the folder does not survive a clone.


### 6. No npm workspace root

**Zapper is API only, so this step writes nothing.** Ruled by Francois on
2026-10-22. It wrote `Code/package.json` and made the contract type generator at
`Code/Tools/ContractTypes/` reachable, and that tool exists only to give a
frontend the types of an API's contracts. **The step keeps its number** so that
every reference to the steps after it stays true.

### 7. `<Solution>.Domain`, with the two Error entities

Write `Code/<Solution>.Domain/<Solution>.Domain.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

</Project>
```

No properties: `TargetFramework`, `Nullable` and `ImplicitUsings` all come from
`Code/Directory.Build.props`. No `ProjectReference`: `BackendCode.md` section 3's
reference graph states that `<Solution>.Domain` references nothing.

**Two entities are written, and nothing else.** `<Solution>.ApiCommon`'s response
wiring, which step 10 writes, reads two tables that the host loads
into memory at startup, and `BackendCode.md` section 7 requires every API to do
that read. Both entities live in `<Solution>.Domain`, the same way every entity
in this codebase does, and `<Solution>.ApiCommon` references Domain to read them
per `BackendCode.md` section 3's reference graph. Neither is mapped here, because
`BackendCode.md` section 3 makes mapping Infrastructure's.

**The schema they share is named `Error`.** Neither entity carries business
meaning of its own, so `BackendCode.md` section 3's rule tying a schema to a
business domain has nothing to give, and the name is derived from the `Errors/`
folder and the `ErrorMessageCode` constants `BackendCode.md` section 7 already
fixes in `<Solution>.ApiCommon`. `BackendCode.md` section 18's folder table names
a folder in `<Solution>.Domain` for its schema, so the folder is `Error/`.

**The classes are `ErrorStatusEntity` and `ErrorMessageEntity`**, per
`BackendCode.md` section 18's rule that an entity's class carries the suffix
`Entity`. **The tables are `ErrorStatus` and `ErrorMessage`**, each taking the
class's own name without that suffix, which the same rule states. The shorter
`Status` and `Message` are refused by `BackendCode.md` section 18's own rule
against a name that says only that there is a category, since the schema
qualifier that would rescue either in SQL never appears in the C# a reader sees.

**Both are ordinary tables with a `Guid` primary key, and neither is a reference
table.** `BackendCode.md` section 13 states that a table qualifies as a reference
table only when its rows ship with the code, nothing creates a row at runtime and
an enum names them. The third fails: `ErrorMessageCode` has to be a class of
string constants and not an enum, because `BackendCode.md` section 7's own worked
example puts a member of it straight into `extensions["code"]` with no conversion
and the wire carries the string, which only happens if it is already one.
`BackendCode.md` section 13 also requires a real foreign key pointing at a
reference table, and nothing in the schema ever points at either of these, since
both are read once into memory at startup and never joined. So `BackendCode.md`
section 11's ordinary shape applies: a `Guid` id made with `Guid.CreateVersion7()`
in the entity's own constructor.

Write `Code/<Solution>.Domain/Error/ErrorStatusEntity.cs`:

```csharp
namespace <Solution>.Domain.Error;

public sealed class ErrorStatusEntity
{
    public ErrorStatusEntity(int statusCode, string code, string title, string detail)
    {
        Id = Guid.CreateVersion7();
        StatusCode = statusCode;
        Code = code;
        Title = title;
        Detail = detail;
    }

    public Guid Id { get; private set; }

    public int StatusCode { get; private set; }

    public string Code { get; private set; }

    public string Title { get; private set; }

    public string Detail { get; private set; }
}
```

Write `Code/<Solution>.Domain/Error/ErrorMessageEntity.cs`:

```csharp
namespace <Solution>.Domain.Error;

public sealed class ErrorMessageEntity
{
    public ErrorMessageEntity(string code, string detail)
    {
        Id = Guid.CreateVersion7();
        Code = code;
        Detail = detail;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; }

    public string Detail { get; private set; }
}
```

**No other folder and no other file.** `BackendCode.md` section 3 places entities
and enums in Domain per schema then per entity, and `BackendCode.md` section 20
refuses a folder or a file nothing yet needs. These two are needed, by the
response wiring every API performs at startup. Nothing else has asked for an
entity.

`Code/<Solution>.slnx` already lists this project, from step 1.

### 8. `<Solution>.Infrastructure`, as a shell

Write `Code/<Solution>.Infrastructure/<Solution>.Infrastructure.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="../<Solution>.Domain/<Solution>.Domain.csproj" />
  </ItemGroup>

</Project>
```

`BackendCode.md` section 3's reference graph states that `<Solution>.Infrastructure`
references Domain.

**No `Persistence/` folder, no `Logging/` folder, no `Integrations/` folder and no
`InfrastructureRegistration.cs`.** `BackendCode.md` section 3 places the schema in
`Persistence/`, the logging pipeline in `Logging/` and each external provider in
its own folder under `Integrations/`, and none has anything to hold yet:
`Persistence/` and `Logging/` are step 11 of this document and no provider exists.
`BackendCode.md` section 9 names a
registration class for the concern it registers, and this project registers
nothing yet, so that file would be the empty, speculative file `BackendCode.md`
section 20 refuses. **Step 11 creates both folders and that file**, because
`Persistence/` and `Logging/` are this project's first concerns and the file then
has concerns to register.

`Code/<Solution>.slnx` already lists this project, from step 1.

### 9. `<Solution>.Migrator`, as a shell

`<Solution>.Migrator`'s whole purpose is to apply migrations, which
`BackendCode.md` section 13 gives it. It does that by calling
`<Solution>.Infrastructure`'s registration method and running the context's
migration call, and neither exists yet: step 11 writes both, when it gives
`<Solution>.Infrastructure` its `Persistence/` folder. So this step creates the
project and leaves its `Program.cs` for step 12 to replace whole.

Write `Code/<Solution>.Migrator/<Solution>.Migrator.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../<Solution>.Infrastructure/<Solution>.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

`BackendCode.md` section 3's reference graph states that `<Solution>.Migrator`
references Infrastructure, and `BackendCode.md` section 13 states that it is a
console application returning an exit code, which is why it carries `OutputType`.

Write `Code/<Solution>.Migrator/Program.cs` as a placeholder:

```csharp
// Replaced whole by step 12 of this skill. This project applies migrations by
// calling <Solution>.Infrastructure's registration method and running the
// migration call on the context BackendCode.md section 10 names, and step 11
// writes both.
return 0;
```

**The placeholder names no class, although this skill now knows the name.** The
table of values above asks for the database's name, so
`BackendCode.md` section 10's `<DatabaseName>Context` could be written here.
Nothing reads it: step 12 replaces this file whole, and this comment survives two
steps. A name written in it would be a name to keep in step with for no gain.

`Code/<Solution>.slnx` already lists this project, from step 1.

### 10. `<Solution>.ApiCommon`

This project holds what every API shares and no API writes for itself. Nothing in
it runs in a host until an API project calls the two methods at the end of this
step, and `create-solution` creates no API, so everything written here is
created and compiled and then waits. Before then only step 13's unit tests run any
of it, being the error message manager.

`BackendCode.md` section 3's reference graph states that `<Solution>.ApiCommon`
references `<Solution>.Domain` and nothing else. In particular it has no path to
`<Solution>.Infrastructure`, which `BackendCode.md` section 7 states outright for
the error lookup, and that single fact shapes three things below: the two Error
tables are read by the host and handed over rather than read here, the health
operation cannot reach the database, and no part of this project touches a
`DbContext`.

The two Error entities this project reads are already written, in step 7.

Write `Code/<Solution>.ApiCommon/<Solution>.ApiCommon.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="../<Solution>.Domain/<Solution>.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
    <PackageReference Include="Microsoft.Extensions.Options.DataAnnotations" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Scalar.AspNetCore" />
  </ItemGroup>

</Project>
```

No property: `TargetFramework`, `Nullable` and `ImplicitUsings` come from
`Code/Directory.Build.props`, which step 3 wrote. No version on any
`PackageReference`: step 4 turned on central package management, so a version
lives only in `Code/Directory.Packages.props`.

**No `FrameworkReference` to `Microsoft.AspNetCore.App`, and none is needed.**
Measured twice against SDK 10.0.302. The plain class library SDK this project
uses reaches no ASP.NET
Core type by itself, and referencing `Microsoft.AspNetCore.OpenApi` is enough
regardless, because that package carries a `FrameworkReference` of its own and a
`FrameworkReference` inside a NuGet package reaches every project that
references that package directly. **It does not reach through a
`ProjectReference`**, measured by building a project that referenced
`<Solution>.ApiCommon` only that way and finding `WebApplication` unavailable
until that project took `Microsoft.NET.Sdk.Web` itself. That is why an API
project needs the Web SDK rather than inheriting ASP.NET Core access from here,
and `create-project` is where that happens.

**Replace `Code/Directory.Packages.props` whole, adding five `PackageVersion`
entries at the end of the `ItemGroup` step 4, "`Directory.Packages.props`",
wrote.** **Neither existing entry changes**: its
`Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational`
entries are what stop an API project
resolving the database library at a lower version than
`<Solution>.Infrastructure` compiled against, which step 4 records as a build
that reports success and runs wrong:

```xml
<Project>

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="<Versions.md: Microsoft.EntityFrameworkCore>" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="<Versions.md: Microsoft.EntityFrameworkCore.Relational>" />
    <PackageVersion Include="FluentValidation" Version="<Versions.md: FluentValidation>" />
    <PackageVersion Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="<Versions.md: Microsoft.Extensions.Options.ConfigurationExtensions>" />
    <PackageVersion Include="Microsoft.Extensions.Options.DataAnnotations" Version="<Versions.md: Microsoft.Extensions.Options.DataAnnotations>" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="<Versions.md: Microsoft.AspNetCore.OpenApi>" />
    <PackageVersion Include="Scalar.AspNetCore" Version="<Versions.md: Scalar.AspNetCore>" />
  </ItemGroup>

</Project>
```

**Two of the five are there for a reason worth knowing, each measured against
SDK 10.0.302.**
`Microsoft.Extensions.Options.ConfigurationExtensions` gives `BindConfiguration`
and `Microsoft.Extensions.Options.DataAnnotations` gives
`ValidateDataAnnotations`, both of which `BackendCode.md` section 14's binding
call needs and neither of which is on `OptionsBuilder<T>` by default: without
the first, `AddOptions<T>().BindConfiguration(...)` fails `CS1061`, and without
the second `.ValidateDataAnnotations()` fails the same way.

**Replace `Code/<Solution>.slnx` whole, adding `<Solution>.ApiCommon`.** Step 1,
"The solution file", wrote that file with three projects and said the step
creating this one adds it. This is that step:

```xml
<Solution>
  <Project Path="<Solution>.Domain/<Solution>.Domain.csproj" />
  <Project Path="<Solution>.Infrastructure/<Solution>.Infrastructure.csproj" />
  <Project Path="<Solution>.ApiCommon/<Solution>.ApiCommon.csproj" />
  <Project Path="<Solution>.Migrator/<Solution>.Migrator.csproj" />
</Solution>
```

The opening and closing `<Solution>` tags are the file format's own root element
and are not substituted, which step 1 states in full.

#### The response wiring

Write `Code/<Solution>.ApiCommon/Errors/ErrorMessageCode.cs`:

```csharp
namespace <Solution>.ApiCommon.Errors;

public static class ErrorMessageCode
{
    public const string BadRequest = nameof(BadRequest);

    public const string Unauthorized = nameof(Unauthorized);

    public const string Forbidden = nameof(Forbidden);

    public const string NotFound = nameof(NotFound);

    public const string MethodNotAllowed = nameof(MethodNotAllowed);

    public const string RequestTooLarge = nameof(RequestTooLarge);

    public const string UnsupportedMediaType = nameof(UnsupportedMediaType);

    public const string TooManyRequests = nameof(TooManyRequests);

    public const string Internal = nameof(Internal);
}
```

Nine constants, matching `BackendCode.md` section 7's table of nine status codes
exactly, and `BackendCode.md` section 7 places the file here.

**These are the constants and not the rows.** `BackendCode.md` section 7 requires
that every constant names a row in one of the two tables and that every row has a
constant. The rows themselves are data, seeded by the migration step 11
generates. That step takes its nine status codes from this file, and nothing
writes the nine constants a second time.

**`ErrorMessageCode` is a class of string constants and never an enum**, which
step 7 already records as the reason neither Error table qualifies as a reference
table: `BackendCode.md` section 7's own worked example puts a member of it
straight into `extensions["code"]` with no conversion, and the wire carries the
string, which only happens if it is already one.

Write `Code/<Solution>.ApiCommon/Managers/ErrorMessage/IErrorMessageManager.cs`:

```csharp
using <Solution>.Domain.Error;

namespace <Solution>.ApiCommon.Managers.ErrorMessage;

public interface IErrorMessageManager
{
    void Populate(IReadOnlyCollection<ErrorStatusEntity> statuses, IReadOnlyCollection<ErrorMessageEntity> messages);

    ErrorStatusEntity GetStatusByStatusCode(int statusCode);

    ErrorMessageEntity? GetMessageByCode(string code);
}
```

Write `Code/<Solution>.ApiCommon/Managers/ErrorMessage/ErrorMessageManager.cs`:

```csharp
using <Solution>.Domain.Error;

namespace <Solution>.ApiCommon.Managers.ErrorMessage;

public sealed class ErrorMessageManager : IErrorMessageManager
{
    private readonly Dictionary<int, ErrorStatusEntity> statusesByCode = new();

    private readonly Dictionary<string, ErrorMessageEntity> messagesByCode = new(StringComparer.Ordinal);

    public void Populate(IReadOnlyCollection<ErrorStatusEntity> statuses, IReadOnlyCollection<ErrorMessageEntity> messages)
    {
        foreach (var status in statuses)
        {
            statusesByCode[status.StatusCode] = status;
        }

        foreach (var message in messages)
        {
            messagesByCode[message.Code] = message;
        }
    }

    public ErrorStatusEntity GetStatusByStatusCode(int statusCode) =>
        statusesByCode.TryGetValue(statusCode, out var status)
            ? status
            : throw new InvalidOperationException($"No ErrorStatus row exists for status {statusCode}.");

    public ErrorMessageEntity? GetMessageByCode(string code) =>
        messagesByCode.TryGetValue(code, out var message) ? message : null;
}
```

`BackendCode.md` section 7 places both files and calls the lookup a manager, and
`BackendCode.md` section 18 states that a class computing something and reaching
nothing outside the process is `<Concern>Manager` with `I<Concern>Manager` beside
it, in a folder named for the class without its suffix.

**`Populate` is called by the host and not from this project.**
`BackendCode.md` section 7 requires each API to load both tables into memory at
startup and states why the host does it: the lookup lives here and this project
may not reference `<Solution>.Infrastructure`, so the host reads the rows and
hands them over. `create-project` writes that call, and this project supplies
the method it calls.

**Neither entity needs an alias here.** Measured against SDK 10.0.302: a file
whose own namespace is
`<Solution>.ApiCommon.Managers.ErrorMessage` cannot refer to a bare type
`ErrorMessage`, because the compiler resolves the name against the current
namespace's own last segment first and fails with `CS0118`, "is a namespace but
is used like a type". `BackendCode.md` section 18's entity suffix rule is what
removes the collision: the class is `ErrorMessageEntity`, so its name no longer
matches the namespace segment this manager's own folder rule puts it in.

**A status with no seeded row throws, deliberately.** `BackendCode.md` section
7's nine rows are the only status codes the framework or an operation ever
produces, so a status this method cannot find is the condition `BackendCode.md`
section 7 calls impossible, and `BackendCode.md` section 7 states that an
exception is for exactly that. The one `IExceptionHandler` below turns it into a
500 for that request alone.

**The two lookups are named by rule rather than by shape.**
`BackendCode.md` section 18 reserves `Get<Noun>` for a lookup by the row's own
id, requires a lookup keyed by any other field to carry that field's name, and
states that what comes back is the row and never one of its fields. So
`GetStatusByStatusCode` and `GetMessageByCode`, each returning the row, and no
method is added per field however many columns either row grows.

Write `Code/<Solution>.ApiCommon/Errors/ProblemDetailsCustomizer.cs`:

```csharp
using <Solution>.ApiCommon.Managers.ErrorMessage;
using <Solution>.Domain.Error;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace <Solution>.ApiCommon.Errors;

public static class ProblemDetailsCustomizer
{
    public static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        var statusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        var manager = context.HttpContext.RequestServices.GetRequiredService<IErrorMessageManager>();
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<ProblemDetailsCustomizerLogCategory>>();

        var status = manager.GetStatusByStatusCode(statusCode);

        if (!problem.Extensions.TryGetValue("code", out var existing) || existing is null)
        {
            problem.Extensions["code"] = status.Code;
        }

        var code = (string)problem.Extensions["code"]!;

        problem.Title = status.Title;
        problem.Detail = GetProblemDetail(code, status, manager, logger);

        if (problem is HttpValidationProblemDetails validation)
        {
            foreach (var field in validation.Errors.Keys.ToList())
            {
                validation.Errors[field] = validation.Errors[field]
                    .Select(fieldCode => GetProblemDetail(fieldCode, status, manager, logger))
                    .ToArray();
            }
        }
    }

    private static string GetProblemDetail(
        string code,
        ErrorStatusEntity status,
        IErrorMessageManager manager,
        ILogger logger)
    {
        if (code == status.Code)
        {
            return status.Detail;
        }

        var message = manager.GetMessageByCode(code);

        if (message is not null)
        {
            return message.Detail;
        }

        logger.LogError("An operation's code {Code} has no message row", code);

        return status.Detail;
    }
}

#pragma warning disable S2094 // ILogger<T> needs a type, and ProblemDetailsCustomizer is static, BackendCode.md section 15
internal sealed class ProblemDetailsCustomizerLogCategory;
#pragma warning restore S2094
```

`BackendCode.md` section 7 places this file and states everything it does: one
shared method fills every failure body, `title` always comes from the status
table by the status of the response, `detail` and every entry in the per-field
map come from the message table by the code, a code an operation set is never
overwritten, a code that is its status row's own takes the baseline detail and
writes no line, and only another code with no message row falls back to the
baseline detail on its status row and logs at `Error` every time.

**Four facts about the framework make this shape work, and none of them is
obvious.** All four were measured against SDK 10.0.302 and
`Microsoft.AspNetCore.OpenApi` 10.0.2, and none is re-derived here. First, one
hook
with three producers: `ProblemHttpResult`, which `TypedResults.Problem` returns,
`UseStatusCodePages()` and `UseExceptionHandler()` all funnel into
`IProblemDetailsService`, which is why one method covers every failure. Second,
the framework's defaults are applied before this callback runs, so `status`,
`type` and `title` are already populated and `traceId` is already in the
extensions, with `detail` null in all three cases. That ordering is the only
reason assigning `Title` here works at all: hooked anywhere earlier, the
framework's English reason phrase overwrites it. Third, the callback receives the
same instance the handler constructed, measured by reference equality, which is
what makes the check for an existing code a plain lookup on the object in hand.
Fourth, `ProblemDetails.Extensions` carries `[JsonExtensionData]`, so its entries
serialise as top-level properties spelled exactly as written, which is why `code`
sits beside `status` rather than nested and is spelled in lowercase by hand, as
`BackendCode.md` section 18 requires of a member added to a response body.

**`ProblemDetailsCustomizerLogCategory` is the log category `BackendCode.md`
section 15 states for a static class that logs.** `ILogger<T>`'s `T` cannot be a
static class, and this class has to stay static because `BackendCode.md` section 9
requires the registration to assign the method rather than hold an instance of its
class. The empty class sits between `#pragma warning disable S2094`, with its
reason, and `#pragma warning restore S2094`, as section 15 states, because
`SonarAnalyzer.CSharp`'s rule `S2094` reports every empty class.

**The callback fires for a validation failure as well as a plain one, measured
rather than assumed**, because `BackendCode.md` does not state it. Measured
against SDK 10.0.302 and `Microsoft.AspNetCore.OpenApi` 10.0.12. With the
callback rewriting `Title` and adding a marker, an endpoint returning
`TypedResults.Problem(...)` and one returning `TypedResults.ValidationProblem(...)`
both came back with the rewritten title and the marker, the runtime type reaching
the callback for the second was `HttpValidationProblemDetails`, which is what the
pattern above relies on, and mutating an entry of `validation.Errors` in place
reached the wire.

Write `Code/<Solution>.ApiCommon/Errors/UnhandledExceptionHandler.cs`:

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace <Solution>.ApiCommon.Errors;

public sealed class UnhandledExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetailsService;

    private readonly ILogger<UnhandledExceptionHandler> logger;

    public UnhandledExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<UnhandledExceptionHandler> logger)
    {
        this.problemDetailsService = problemDetailsService;
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception stopped the request");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception
        });
    }
}
```

`BackendCode.md` section 7 places this file and states that one
`IExceptionHandler` turns an unhandled exception into `ProblemDetails`, and
`BackendCode.md` section 18's table names the class, keeping `Exception` because
that is part of the noun the interface carries.

**It logs the exception once, at `Error`, before it writes the response.**
`BackendCode.md` section 15 lists an unhandled exception at `Error`, states that it
is logged once, by this handler, and states that an exception is passed to the log
call as the exception, so the line carries the exception's type, its message,
every inner exception and its stack trace. The message carries no value, and the
trace id comes with every line written inside a request. Ruled by Francois on
2026-10-13. **Nothing else logs the same exception.** Measured against SDK 10.0.302: the framework's exception
middleware dropped its own line when this handler reported the exception handled,
which it always does, and with that drop turned off it wrote its own line at
`Error` for the same exception. Measured: a route
that raised an exception, and a route the framework failed inside, each answered
500 and wrote this line once, at `Error`, with the exception below it.

Writing to the same `IProblemDetailsService` the framework's own two failure
paths use is what puts this handler's output through
`ProblemDetailsCustomizer.Customize` as well, which is the one hook with three
producers above. So `detail` comes from the status table's baseline for 500 and
never from `exception`, which this class passes only to the log call, and that is what
`BackendCode.md` section 7 requires when it says the unhandled exception handler
writes a fixed message rather than anything taken from the exception.

**The method returns `ValueTask<bool>` although `BackendCode.md` section 16 says
our own signatures return `Task`.** The signature is the framework's, declared by
`IExceptionHandler`, and `BackendCode.md` section 16's own sentence is about
our own signatures.
Nothing here chooses it.

**`UseExceptionHandler()` and `UseStatusCodePages()` are not written here, and
they are not this skill's at all.** `BackendCode.md` section 7 states that every
API makes all three calls, and `AddProblemDetails` is the one of the three this
project makes, below. The other two are middleware on the application rather
than services, so they sit in each host's own `Program.cs` and
`create-project` writes them. **The dependency runs one way and is silent until
a host starts**: measured against SDK 10.0.302, `UseExceptionHandler()` with no
argument throws at
startup unless problem details are registered, failing with
`InvalidOperationException` asking for a path, a handler or `AddProblemDetails`.
So the call below is a precondition of the call order an API's `Program.cs`
fixes, and an API that calls them in the wrong order does not start.

Write `Code/<Solution>.ApiCommon/Errors/ProblemCodeSchemaTransformer.cs`:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace <Solution>.ApiCommon.Errors;

public sealed class ProblemCodeSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type != typeof(ProblemDetails)
            && context.JsonTypeInfo.Type != typeof(HttpValidationProblemDetails))
        {
            return Task.CompletedTask;
        }

        var properties = schema.Properties ?? new Dictionary<string, IOpenApiSchema>();
        properties["code"] = new OpenApiSchema { Type = JsonSchemaType.String };
        schema.Properties = properties;

        var required = schema.Required ?? new HashSet<string>();
        required.Add("code");
        schema.Required = required;

        return Task.CompletedTask;
    }
}
```

`BackendCode.md` section 7 places this file and states what it does: the `code`
member is added to both problem schemas by one schema transformer, registered
once in the wiring every API calls, and no operation declares it.
`BackendCode.md` section 18's table names the class, dropping `OpenApi` from the
interface's name because that is the technology.

**Three details were measured against `Microsoft.AspNetCore.OpenApi` 10.0.12 and
`Microsoft.OpenApi` 2.12.0**, because `BackendCode.md`'s own worked examples
predate that shape. The
OpenAPI model types live in the bare `Microsoft.OpenApi` namespace rather than
`Microsoft.OpenApi.Models`, and the older using fails `CS0234`.
`OpenApiSchema.Properties` is a nullable dictionary and `.Required` a nullable
set, both nullable in a way the compiler tracks through the property getter and
not through a local, so the shorter `schema.Required ??= ...` followed by
`.Add(...)` still fails nullable analysis and the build under
`TreatWarningsAsErrors`; reading into a local first and assigning back is what
compiles clean. And running the generated document out confirmed `code` lands
inside the problem schema marked required, exactly as declared.

#### Validation

Write `Code/<Solution>.ApiCommon/Validation/PropertyNameResolver.cs`:

```csharp
using System.Linq.Expressions;
using System.Reflection;

namespace <Solution>.ApiCommon.Validation;

public static class PropertyNameResolver
{
    public static string Resolve(Type type, MemberInfo? memberInfo, LambdaExpression? expression)
    {
        var name = memberInfo?.Name;

        return name is null or { Length: 0 }
            ? name ?? string.Empty
            : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
```

`BackendCode.md` section 8 places this file and states what it is for: each key
in the per-field map is the request property's name in camelCase, matching the
name that property carries on the wire, produced by the resolver set in the
wiring every API calls, with nothing per validator and nothing per operation
setting it. Without it the keys are the C# property names and a frontend cannot
match an entry to the input it belongs to.

The three parameters are the signature FluentValidation's own hook declares, and
two of them are unused. Nothing here chooses that shape.

Write `Code/<Solution>.ApiCommon/Validation/ValidationFilter.cs`:

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace <Solution>.ApiCommon.Validation;

public sealed class ValidationFilter<TValidator> : IEndpointFilter
    where TValidator : IValidator
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validator = ActivatorUtilities.CreateInstance<TValidator>(context.HttpContext.RequestServices);

        var request = context.Arguments.FirstOrDefault(argument =>
            argument is not null && validator.CanValidateInstancesOfType(argument.GetType()));

        if (request is null)
        {
            return await next(context);
        }

        var result = await validator.ValidateAsync(new ValidationContext<object>(request));

        if (result.IsValid)
        {
            return await next(context);
        }

        var errors = result.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());

        return TypedResults.ValidationProblem(errors);
    }
}
```

Write `Code/<Solution>.ApiCommon/Validation/ValidationExtensions.cs`:

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace <Solution>.ApiCommon.Validation;

public static class ValidationExtensions
{
    public static RouteHandlerBuilder WithValidation<TValidator>(this RouteHandlerBuilder builder)
        where TValidator : IValidator =>
        builder.AddEndpointFilter<ValidationFilter<TValidator>>();
}
```

`BackendCode.md` section 8 places both files, states that a validator is run by
an endpoint filter and never by a handler, and states that an operation attaches
its own with `WithValidation<T>` on the same statement as the route it guards.
`BackendCode.md` section 18's table names the filter class, and
`BackendCode.md` section 18's rule for a class of extension methods names the
other.

**Two sentences of `BackendCode.md` section 8 look like they conflict until
measured**, being that a validator is given nothing except `TimeProvider` and
that validators are not registered and are not discovered.
`ActivatorUtilities.CreateInstance<T>` constructs `T` fresh on every call,
resolving each constructor parameter from the container without `T` itself ever
being registered there, so a validator receives `TimeProvider` with no
registration of its own. Measured end to end against FluentValidation 12.1.1 on
SDK 10.0.302, with a validator taking
`TimeProvider` and a rule that fails on an empty string: a well-formed request
reached the handler, a malformed one came back 400 with its per-field map, the
key already lower-cased by the resolver above once it was set on
FluentValidation's global hook, and the validator's constructor ran with a real
`TimeProvider` both times.

#### The OpenAPI and documentation machinery

Write `Code/<Solution>.ApiCommon/OpenApi/OpenApiOptions.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace <Solution>.ApiCommon.OpenApi;

public sealed class OpenApiOptions
{
    [Required]
    public bool? IsEnabled { get; init; }

    [Required]
    public string Title { get; init; } = string.Empty;

    [Required]
    public string Description { get; init; } = string.Empty;
}
```

Write `Code/<Solution>.ApiCommon/OpenApi/ApiInformationDocumentTransformer.cs`:

```csharp
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace <Solution>.ApiCommon.OpenApi;

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
```

Both files are `BackendCode.md` section 19's own code blocks, which that section
calls the actual ones rather than illustrations, so they are written as they
stand there. `BackendCode.md` section 19 states each member's job, states that
`IsEnabled` is a nullable `bool` because a non-nullable one is never missing and
the host would start on a configuration saying nothing about whether the API
publishes, and states that the document's version is the document's own name and
is not configured. `BackendCode.md` section 14 places an options class of one of
this project's concerns in that concern's folder, which is why both sit under
`OpenApi/`.

Write `Code/<Solution>.ApiCommon/OpenApi/OpenApiExtensions.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace <Solution>.ApiCommon.OpenApi;

public static class OpenApiExtensions
{
    private const string DocumentationPathPrefix = "/scalar";

    public static void MapDocumentation(this WebApplication app)
    {
        if (app.Services.GetRequiredService<IOptions<OpenApiOptions>>().Value.IsEnabled
            is not true)
        {
            return;
        }

        app.UseWhen(
            context => context.Request.Path.StartsWithSegments(DocumentationPathPrefix),
            branch => branch.Use(WrapDocumentationResponseAsync));

        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference().AllowAnonymous();
    }

    private static async Task WrapDocumentationResponseAsync(HttpContext context, Func<Task> next)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.ContentSecurityPolicy =
                "default-src 'self'; script-src 'self'; connect-src 'self'; referrer-policy no-referrer";

            var contentType = context.Response.ContentType;

            if (contentType is not null
                && !contentType.Contains("charset", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = contentType + "; charset=utf-8";
            }

            return Task.CompletedTask;
        });

        await next();
    }
}
```

This is `BackendCode.md` section 19's third code block, extended with the two
things that section's prose requires and its block leaves out: the character-set
wrapper, since the Scalar call takes no configuration, and the documentation
route's own Content-Security-Policy, which `BackendCode.md` section 7 states
separately from the one every other response carries. `BackendCode.md` section 19
states that both route calls live in this one method, that it reads the switch
and registers both or neither rather than registering and hiding, and that
neither route is configured.

**`.AllowAnonymous()` on both calls is not decoration.** Measured against SDK
10.0.302, by running the whole stack together rather than by reading: with the
fallback authorization policy below registered and neither call marked, both
documentation routes fall under that policy exactly as an unguarded operation
does and answer 500 instead of the page. `BackendCode.md` section 19 puts no
sign-in in front of either route, and these two calls are what makes that true in
code.

**The Content-Security-Policy is applied to the page and never to the document
itself.** `BackendCode.md` section 7's wording is the documentation page,
singular, meaning the Scalar user interface rather than the raw document a
browser executes nothing from.

**The shared security-headers middleware below skips that header for this path
prefix rather than competing for it.** Measured against SDK 10.0.302: response
callbacks registered
with `OnStarting` run last registered first, so a middleware that wraps another
and sets the same header in its own callback always loses to the middleware it
wraps, whichever file's code happens to run later in the pipeline. Rather than
depend on which of an API's own two files registers first, the shared middleware
never writes `Content-Security-Policy` for a path under `/scalar`, so there is one
writer and no race to lose.

Write `Code/<Solution>.ApiCommon/OpenApi/OperationSecurityTransformer.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace <Solution>.ApiCommon.OpenApi;

public sealed class OperationSecurityTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        if (metadata.OfType<IAllowAnonymous>().Any())
        {
            operation.Security = [];

            return Task.CompletedTask;
        }

        throw new NotSupportedException(
            "An operation requiring authorization has no scheme for this transformer to name yet. "
            + "The authorization feature's planning fixes the schemes and how a permission is represented, "
            + "and this transformer is revisited once that exists.");
    }
}
```

`BackendCode.md` section 19 places this file and states what it does: one
operation transformer writes each operation's security requirement and the
schemes it names into the generated document, reading the operation's
authorization metadata rather than anything an operation states by hand.
`BackendCode.md` section 18's table names the class.

**It throws rather than guesses, and that is a real gap rather than a
placeholder.** Which schemes exist and how a permission is represented is
settled by the authorization feature's own planning, which has not happened.
Measured against SDK 10.0.302: the metadata collection does carry the
framework's anonymous and
authorize markers as the two interfaces above, confirmed on a scratch endpoint of
each kind, so the public branch is real and tested, and the empty security array
was confirmed to serialise as `"security": []` in the generated document. The
throwing branch is written loudly so that the first operation requiring
authorization fails the build the moment it is added, rather than publishing a
document carrying a `security` field this transformer invented. The one operation
this skill creates, health, is anonymous, so nothing reaches that branch today.

#### Versioning and paging

Write `Code/<Solution>.ApiCommon/Versioning/VersioningExtensions.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace <Solution>.ApiCommon.Versioning;

public static class VersioningExtensions
{
    public static RouteGroupBuilder MapVersion(this WebApplication app, int version) =>
        app.MapGroup($"/v{version}");
}
```

`BackendCode.md` section 6 places this file and states that the version is the
first segment of every route, that the version group is created once in an API's
own `Endpoints.cs`, and that no feature file contains a version string.

Write `Code/<Solution>.ApiCommon/Paging/PagedResponse.cs`:

```csharp
namespace <Solution>.ApiCommon.Paging;

public sealed class PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }
}
```

`BackendCode.md` section 7 places this file and states the whole of it: every
list returns this container holding the operation's own response class, it is the
one response type shared by every API and the exception `BackendCode.md` section
3 names, it is a container with nothing inheriting anything, it carries the rows,
the page number, the page size and the total number of matching records and
nothing derived, and adding a member to it is a decision rather than a
convenience. The four names reach the wire as `items`, `page`, `pageSize` and
`totalCount`, which `BackendCode.md` section 18's casing policy produces with
nothing configuring it.

#### Health

`BackendCode.md` section 6 states that an endpoint every version serves is
provided by `<Solution>.ApiCommon` as one call into the version group, so it is
not written again for each version, and names health as that endpoint, at
`Health/HealthEndpoints.cs` and `Health/GetHealth.cs`. It follows the same two
files a resource follows, in a folder named for its concern.

**The placement was measured as well as read.** Measured against SDK 10.0.302:
health placed in this project and mapped as one call into the version group
produced `/v1/health`, the tag `health`, its summary and an empty security
requirement in the generated document, and needed no package beyond what this
project already references.

**Health is a liveness check and cannot be anything else.**
`BackendCode.md` section 3's reference graph gives this project no path to
`<Solution>.Infrastructure`, so this handler cannot reach the database or
anything else the API depends on. It answers that the host is up and serving. A
host that is down, or that never finished starting, serves nothing at all, and
that absence is what a probe reads. There is no failure for this operation to
return and none is declared.

Write `Code/<Solution>.ApiCommon/Health/HealthEndpoints.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace <Solution>.ApiCommon.Health;

public static class HealthEndpoints
{
    public static void MapHealth(this IEndpointRouteBuilder version)
    {
        var group = version.MapGroup("/health").WithTags("health");

        GetHealth.Map(group);
    }
}
```

**`Microsoft.AspNetCore.Http` is imported for `WithTags`, and this file does not
compile without it.** Measured:
without the import, `<Solution>.ApiCommon` failed `CS1061` on `WithTags`, because
the plain SDK's implicit imports do not carry that namespace. With it, the four
projects built with 0 warnings and 0 errors.

Write `Code/<Solution>.ApiCommon/Health/GetHealth.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace <Solution>.ApiCommon.Health;

public sealed class GetHealth
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle)
             .WithSummary("Reports that the API is running.")
             .Produces(200)
             .AllowAnonymous();

    private static Ok Handle() => TypedResults.Ok();
}
```

**The response is 200 with no body, and the shape is frozen.**
`BackendCode.md` section 6 states that health's response shape is frozen and that
`GetHealth` writes it by hand rather than taking the health checks middleware's
default writer. There is no response class, because `BackendCode.md` section 7
bans every member a health body would carry: a successful response body carries
no success flag, no status code, no message and no timestamp, and a version is
something nothing asked for, which `BackendCode.md` section 20 refuses. A probe
or an orchestrator consumes this response, so an empty 200 is chosen for having
nothing in it that can change later. `Produces(200)` with no type publishes the
status and no body.

**The handler declares no cancellation token, and it is not async.** It performs
no input or output and calls nothing, so `BackendCode.md` section 16 leaves it
nothing to pass a token to, and that section's own rule that a token is never
accepted and then dropped is what decides it. Since the method is not async,
`BackendCode.md` section 18's rule that every async method carries the `Async`
suffix names it `Handle` rather than `HandleAsync`, and it returns its result
rather than a task.

**`.AllowAnonymous()` is required and is not a style choice.**
`BackendCode.md` section 6 states that an operation declaring neither
authorization call is refused by the fallback policy, and health is public, so it
declares the anonymous call on the same statement as the route it guards. It is
also what the operation transformer above reads to publish `"security": []`.

**The framework's own health checks feature is deliberately not registered.**
`AddHealthChecks()` is not called anywhere in this solution. It registers a
service nothing here resolves, and the checks it exists to carry would have to
reach the database, which this project cannot. The route above is this product's
own endpoint and does not depend on that feature in any way, so nobody needs to
wire it up to make health work. `BackendCode.md` section 6 already refuses the
response writer that feature's middleware would use, and `BackendCode.md` section
20 refuses the registration itself.

**Three things about health belong to an API and not to this skill**, and
`create-project` writes all three: the one line in that API's own
`Endpoints.cs` calling `MapHealth` into each version group it creates, that API's
frozen fragment under `Docs/Contracts/`, and that API's contract document. The
summary string above is the one `BackendCode.md` section 19 requires a fragment
to carry, so each API's fragment publishes that same sentence.

**Health is under each API's rate limit, with no exemption and no code. Ruled by
Francois on 2026-10-11.** `BackendCode.md` section 9 has every API read its rate
limit from its own settings, and a probe polling health often enough could be
refused with 429, which an orchestrator may read as an unhealthy host. The limit
is counted for each network address, so a probe polling a few times a minute from
its own address is far below it, and measured, health from one address answered 200 while another address was
over its limit. The subsection below states the limit.

#### The shared security-headers middleware

Write `Code/<Solution>.ApiCommon/Security/SecurityHeadersMiddleware.cs`:

```csharp
using Microsoft.AspNetCore.Http;

namespace <Solution>.ApiCommon.Security;

public static class SecurityHeadersMiddleware
{
    private const string DocumentationPathPrefix = "/scalar";

    public static async Task InvokeAsync(HttpContext context, Func<Task> next)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
            context.Response.Headers.XContentTypeOptions = "nosniff";

            if (!context.Request.Path.StartsWithSegments(DocumentationPathPrefix))
            {
                context.Response.Headers.ContentSecurityPolicy = "frame-ancestors 'none'";
            }

            return Task.CompletedTask;
        });

        await next();
    }
}
```

`BackendCode.md` section 9 places this file and states that it is the one place
that sets these headers, that it runs on every request, and that no operation
sets a header `BackendCode.md` section 7 already assigns.
`BackendCode.md` section 7 states the four headers and their values. The skip for
the documentation prefix is the measured race the previous subsection records,
and it is why there is one writer of `Content-Security-Policy` per response
rather than two.

It is a static method assigned to the application rather than a class
implementing the framework's middleware interface, the same shape the logging
pipeline's `Configure` and the problem details customizer's `Customize` already
take, so nothing registers it as a service.

#### The security settings

`BackendCode.md` section 9 has every API read the origins it lets read its
responses, its request size limit and its rate limit from its own settings, each
a member of an options class read the way `BackendCode.md` section 14 states and
registered where `Add<Solution>ApiCommon` registers the shared machinery. **The
three are members of one options class here, which every API binds, and no API
carries an options class of its own. Ruled by Francois on 2026-10-11.** Each API
supplies its own values under the one section this class binds, and
`create-project` writes them. `BackendCode.md` section 14 places an options
class of one of this project's concerns in that concern's folder, so it sits under
`Security/`, beside the middleware above.

Write `Code/<Solution>.ApiCommon/Security/SecurityOptions.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace <Solution>.ApiCommon.Security;

public sealed class SecurityOptions
{
    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];

    [Range(1, int.MaxValue)]
    public int RequestSizeLimit { get; init; }

    [Range(1, int.MaxValue)]
    public int RateLimit { get; init; }
}
```

`BackendCode.md` section 14 names the class for what it configures plus
`Options` and binds it to `<Solution>:Security`, the class name without
`Options`. **`AllowedOrigins` is the list of addresses whose pages may read an
API's responses**, which `Security.md` section 10 states. **`RequestSizeLimit` is
the largest request body an API accepts, in bytes**, and **`RateLimit` is the
number of requests one network address may send in a minute**. The minute is
written in the code below, because nothing asked for it to vary, which
`BackendCode.md` section 20 states.

**The two limits carry `[Range(1, int.MaxValue)]`, and that is what refuses a
missing one.** A limit nothing sets binds as `0`. Measured: with either limit set to `0`, the host stopped before
it listened, with "DataAnnotation validation failed for 'SecurityOptions'
members" naming the member, and exited 134.

**`AllowedOrigins` carries no `[Required]`.** An API that only a customer's
system calls has no frontend and so no origin to allow. Measured: with no list at all, the host started, and a page at any origin was given no
CORS header, so a missing list refuses every origin and stops nothing. **Each
deployed environment sets its own list by environment variable**, one variable per
address, such as `<Solution>__Security__AllowedOrigins__0`.

Write `Code/<Solution>.ApiCommon/Security/CorsConfigureOptions.cs`:

```csharp
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace <Solution>.ApiCommon.Security;

public sealed class CorsConfigureOptions : IConfigureOptions<CorsOptions>
{
    private readonly SecurityOptions security;

    public CorsConfigureOptions(IOptions<SecurityOptions> security) =>
        this.security = security.Value;

    public void Configure(CorsOptions options) => options.AddDefaultPolicy(ConfigurePolicy);

    private void ConfigurePolicy(CorsPolicyBuilder policy) =>
        policy.WithOrigins([.. security.AllowedOrigins])
              .AllowAnyHeader()
              .AllowAnyMethod();
}
```

Write `Code/<Solution>.ApiCommon/Security/KestrelServerConfigureOptions.cs`:

```csharp
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace <Solution>.ApiCommon.Security;

public sealed class KestrelServerConfigureOptions : IConfigureOptions<KestrelServerOptions>
{
    private readonly SecurityOptions security;

    public KestrelServerConfigureOptions(IOptions<SecurityOptions> security) =>
        this.security = security.Value;

    public void Configure(KestrelServerOptions options) =>
        options.Limits.MaxRequestBodySize = security.RequestSizeLimit;
}
```

Write `Code/<Solution>.ApiCommon/Security/RateLimiterConfigureOptions.cs`:

```csharp
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace <Solution>.ApiCommon.Security;

public sealed class RateLimiterConfigureOptions : IConfigureOptions<RateLimiterOptions>
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly SecurityOptions security;

    private readonly ILogger<RateLimiterConfigureOptions> logger;

    public RateLimiterConfigureOptions(
        IOptions<SecurityOptions> security,
        ILogger<RateLimiterConfigureOptions> logger)
    {
        this.security = security.Value;
        this.logger = logger;
    }

    public void Configure(RateLimiterOptions options)
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(GetPartition);
        options.OnRejected = RejectRequestAsync;
    }

    private RateLimitPartition<string> GetPartition(HttpContext context) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            GetLimiterOptions);

    private FixedWindowRateLimiterOptions GetLimiterOptions(string address) =>
        new() { PermitLimit = security.RateLimit, Window = Window, QueueLimit = 0 };

    private ValueTask RejectRequestAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        logger.LogWarning("A request was refused for being over its rate limit");

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        return ValueTask.CompletedTask;
    }
}
```

**Each of the three gives one framework service its value from
`SecurityOptions`**, because `BackendCode.md` section 9 states that a
registration method assigns and never implements, and that the method a framework
hook takes lives in a named class in its concern folder. **Their names are
derived.** `BackendCode.md` section 18 names a class implementing a framework
interface for what it does plus the noun the interface carries, and carries the
whole name after the `I` where that name is a verb phrase with no noun, which is
what gives `BackendCode.md` section 14 its `DatabaseValidateOptions` for the class
that validates `DatabaseOptions`. `IConfigureOptions<T>` is the same kind of name,
so the class that configures `CorsOptions` is `CorsConfigureOptions`, and the
other two follow from `KestrelServerOptions` and `RateLimiterOptions` the same
way. **`ConfigurePolicy` takes the framework's own word**, the name of the
parameter `AddDefaultPolicy` takes, which `BackendCode.md` section 18 states for a
method that performs a framework action.

**Every origin on the list may read a response, with any header and any method,
and nothing else may.** `Security.md` section 10 states it, and no response lets
every origin read it. **No credential is allowed with a request**, because nothing
asked for one. **The request size limit is the server's own limit on a request
body**, and measured, a body one byte over it answered a 413
problem document carrying `RequestTooLarge`, and a body exactly at it answered
200. **The rate limit is one fixed window of a minute for each network address,
with no queue.** Measured: 300 requests from one address were
served and every later one in that minute answered a 429 problem document carrying
`TooManyRequests`, and a preflight was not counted, because CORS answers a
preflight before the rate limiter runs. The address is the one
`HttpContext.Connection.RemoteIpAddress` carries once `UseForwardedHeaders` has
run, which `BackendCode.md` section 9 states. A test host gives no address, so
every request it sends shares one window. Health is under the limit like every
other route, which the "Health" subsection above states.

**`RateLimiterConfigureOptions` also gives `OnRejected`, the framework's hook for a
request the rate limiter refuses, its method, `RejectRequestAsync`.**
`BackendCode.md` section 9 states that the method a framework hook takes lives in a
named class in its concern folder, and this class already configures
`RateLimiterOptions` and is built by the service container, so it takes
`ILogger<RateLimiterConfigureOptions>` through its constructor as `BackendCode.md`
section 15 requires. The method sets the status to 429 and logs one line at
`Warning`, the level `BackendCode.md` section 15 gives a request refused for being
over its limit, so a caller over its limit writes one line for every refused
request. **The message carries no value.** No operation has run, so there is no
code an operation set and no id it was given, and the network address reaches the
line as the enriched property `BackendCode.md` section 15 puts on every line written
inside a request, once that property exists. Ruled by Francois on 2026-10-13.
**Its name is derived**: `BackendCode.md` section 18 names a method for the action
it performs and what that action applies to, has a method that performs a
framework action take the framework's own word for it, which is the rate limiter's
"reject", and has the method name the request, which its class is not named for.
Measured against SDK 10.0.302:
the framework writes the refusal only at `Debug`. Measured: the
request over the limit answered a 429 problem document carrying `TooManyRequests`
and wrote this line once, at `Warning`.

#### `ApiCommonRegistration.cs`

Write `Code/<Solution>.ApiCommon/ApiCommonRegistration.cs`:

```csharp
using <Solution>.ApiCommon.Errors;
using <Solution>.ApiCommon.Managers.ErrorMessage;
using <Solution>.ApiCommon.OpenApi;
using <Solution>.ApiCommon.Security;
using <Solution>.ApiCommon.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace <Solution>.ApiCommon;

public static class ApiCommonRegistration
{
    public static IServiceCollection Add<Solution>ApiCommon(
        this IServiceCollection services,
        params int[] versions)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IErrorMessageManager, ErrorMessageManager>();

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

        services.AddOptions<OpenApiOptions>()
                .BindConfiguration("<Solution>:OpenApi")
                .ValidateDataAnnotations()
                .ValidateOnStart();

        ValidatorOptions.Global.PropertyNameResolver = PropertyNameResolver.Resolve;

        services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                    .RequireAssertion(AllowRequestWithNoEndpointMetadata)
                    .Build());

        services.AddCors();

        services.AddRateLimiter();

        services.AddOptions<SecurityOptions>()
                .BindConfiguration("<Solution>:Security")
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddSingleton<IConfigureOptions<CorsOptions>, CorsConfigureOptions>();
        services.AddSingleton<IConfigureOptions<KestrelServerOptions>, KestrelServerConfigureOptions>();
        services.AddSingleton<IConfigureOptions<RateLimiterOptions>, RateLimiterConfigureOptions>();

        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

        services.Configure<JsonOptions>(options =>
            options.SerializerOptions.AllowDuplicateProperties = false);

        return services;
    }

    public static void Use<Solution>ApiCommon(this WebApplication app)
    {
        app.Use(SecurityHeadersMiddleware.InvokeAsync);
        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthorization();
    }

    private static bool AllowRequestWithNoEndpointMetadata(AuthorizationHandlerContext context) =>
        context.Resource is not HttpContext httpContext
        || httpContext.GetEndpoint()?.Metadata.Count is null or 0;
}
```

`BackendCode.md` section 9 states the class, its path and both method names: a
shared project that holds something to register owns one extension method, that
method lives in a class named for the project plus `Registration` at the
project's root, and an extension method is named `Add` plus the solution's name
plus its class's name without `Registration`. The same section states that
`Add<Solution>ApiCommon` takes the route versions the API serves and is the one
registration method that takes an argument, because one document is registered
per version and a document is registered before any route exists.

**Where each piece's registration is stated:**

| Line | `BackendCode.md` sections |
|---|---|
| `TimeProvider` | 8, 9, 12 |
| `IErrorMessageManager` | 7, 9 |
| `AddProblemDetails` and `AddExceptionHandler` | 7 |
| The `AddOpenApi` loop and its three transformers | 9, 19 |
| The `OpenApiOptions` binding | 14, 19 |
| The property name resolver assignment | 8 |
| The fallback authorization policy | 6, 9 |
| `AddCors` and `AddRateLimiter` | 9 |
| The `SecurityOptions` binding and its three `ConfigureOptions` classes | 9, 14 |
| `RouteHandlerOptions.ThrowOnBadRequest` | 7 |
| `JsonOptions.AllowDuplicateProperties` | 7, 9 |
| `Use<Solution>ApiCommon` | 7, 9 |

**`AllowRequestWithNoEndpointMetadata` is a named method rather than an inline
lambda**, because `BackendCode.md` section 9's rule that a registration method
assigns and never implements applies to this one line the same as to the problem
details callback and the logging pipeline, however small the body.

**The policy passes any request that did not reach a real endpoint, and refuses
every request that did.** That is the whole of what it is for: an operation
forgetting to declare authorization is refused rather than served, which
`BackendCode.md` section 6 and `BackendCode.md` section 9 both state, while a
request no route matched is left alone because there was never anything to
authorize.

**The test is the endpoint's metadata, and it was read at run time rather than
assumed.** Measured against SDK 10.0.302: an unmatched route gives no endpoint at
all and no metadata;
a wrong method on a route that exists gives a framework placeholder endpoint
carrying no metadata whatsoever; and a real route carries its method metadata and
more. So no endpoint, or an endpoint with no metadata, separates both framework
cases from every real route without matching on a display name, and every route
these standards allow is mapped by a call that adds method metadata. **The method
is named for the branch that returns `true`**, because the framework reads `true`
as authorized and `BackendCode.md` section 9 forbids wrapping a differently named
check in a lambda, and the noun carries the property the body tests because
`BackendCode.md` section 18 refuses a name that says only that there is a
request.

**What this fixes, measured against SDK 10.0.302 on all four shapes in a
deployed configuration.**
Before it, an anonymous endpoint answered 204 with the headers, an endpoint
declaring neither call was refused, and an unmatched route and a wrong method
both answered an empty 500 with no header at all. After it, the first two are
unchanged and the last two answer 404 and 405 with the headers and a proper
problem document carrying `type`, `title`, `status` and the trace id. **Removing
the policy also produces correct 404s and was rejected**, because it gives up
failing closed.

**`UseAuthorization` is called explicitly, and it is the last call in
`Use<Solution>ApiCommon`.** Measured against SDK 10.0.302, and it is the only
arrangement that works.
The framework inserts its own authorization step automatically, and it stops only
when `UseAuthorization` is called directly on the application, so the automatic
step otherwise runs before anything an API's own `Program.cs` adds and before the
security-headers middleware above. Every header was then missing from any
response the policy produced. Calling it here, after the shared middleware,
suppresses the insertion and puts all four headers back on those responses, with
enforcement unchanged. **Authorizing only matched requests through a conditional
branch was measured and changed nothing at all**, because the framework's check
sees a call on a different builder and inserts its own anyway.

**A real authentication scheme is still needed, and this is not it.** Measured
against SDK 10.0.302: with the policy registered and no scheme registered
anywhere, a request to a
route declaring neither authorization call does not answer 401, it throws for a
missing authentication service, and a bare `AddAuthentication()` with no default
scheme throws a different message naming the missing challenge scheme. Nothing
reaches that path in a solution whose only operation is health, which is
anonymous. The authorization feature registers a scheme before any operation may
be added that declares neither call, and this skill reports that at the end.

**`AddCors()` and `AddRateLimiter()` register the two services, and the
three `ConfigureOptions` classes give them and the server their values from
`SecurityOptions`.** `BackendCode.md` section 9 has every API read its allowed
origins, its request size limit and its rate limit from its own settings, and
each API supplies its own values under `<Solution>:Security`, which
`create-project` writes when it creates the API. So nothing here holds a value,
and an API's values reach every one of the three through the one binding above.
**`AddRateLimiter()` takes nothing here**, because `RateLimiterConfigureOptions`
sets both the limiter and the rejection callback, and a refused request still
answers 429 through the same problem details path. Measured, with `RateLimiterConfigureOptions` setting
the limiter: the 429, the 413 and a 404 sent from an allowed origin were each a
problem document carrying `Access-Control-Allow-Origin` and the four security
headers. Measured again, with the rejection callback in that
class: the 429 was a problem document carrying `TooManyRequests` and the four
security headers.
**The request size limit has no service to register**, only the server's own
value to set, which `KestrelServerConfigureOptions` sets.

**One fact about that limit is worth keeping here, measured against SDK
10.0.302**, because it decides
how it is enforced rather than where. An oversized body read by hand, with a
reader over the request's own stream, throws inside the server and comes back as
a 500 rather than a 413. The same request against an endpoint taking a typed
request parameter, which is the shape every handler in this codebase uses,
answers a clean 413 on its own with no custom handling, because the framework's
own body binding catches that condition before an exception reaches application
code. Since no handler here ever reads the raw body, the limit needs nothing
beyond the value.

### 11. `<Solution>.Infrastructure/Persistence/`

This step gives `<Solution>.Infrastructure` the schema, and the logging pipeline
every host writes through. `BackendCode.md` section 3
makes `Persistence` the folder that owns the schema, and `BackendCode.md` section 10
states the actual names of everything in that folder rather than illustrations of
them. `BackendCode.md` section 15 places the pipeline in `Logging/`.

**The two Error entities this step maps are already written, in step 7, and the
nine `ErrorMessageCode` constants are already written, in step 10.** This step
writes the nine rows that match those constants and writes no constant.
`BackendCode.md` section 7 requires that every constant names a row and every row
has a constant, so the two halves have to match and neither is written twice.

Replace `Code/<Solution>.Infrastructure/<Solution>.Infrastructure.csproj` whole,
adding the seven packages this step needs to the shell step 8,
"`<Solution>.Infrastructure`, as a shell", wrote:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="../<Solution>.Domain/<Solution>.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
    <PackageReference Include="Microsoft.Extensions.Options.DataAnnotations" />
    <PackageReference Include="Serilog.Extensions.Hosting" />
    <PackageReference Include="Serilog.Settings.Configuration" />
    <PackageReference Include="Serilog.Sinks.Console" />
  </ItemGroup>

</Project>
```

**Replace `Code/Directory.Packages.props` whole, adding five `PackageVersion`
entries at the end of the `ItemGroup` step 4, "`Directory.Packages.props`",
wrote:**

```xml
<Project>

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="<Versions.md: Microsoft.EntityFrameworkCore>" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="<Versions.md: Microsoft.EntityFrameworkCore.Relational>" />
    <PackageVersion Include="FluentValidation" Version="<Versions.md: FluentValidation>" />
    <PackageVersion Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="<Versions.md: Microsoft.Extensions.Options.ConfigurationExtensions>" />
    <PackageVersion Include="Microsoft.Extensions.Options.DataAnnotations" Version="<Versions.md: Microsoft.Extensions.Options.DataAnnotations>" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="<Versions.md: Microsoft.AspNetCore.OpenApi>" />
    <PackageVersion Include="Scalar.AspNetCore" Version="<Versions.md: Scalar.AspNetCore>" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="<Versions.md: Npgsql.EntityFrameworkCore.PostgreSQL>" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="<Versions.md: Microsoft.EntityFrameworkCore.Design>" />
    <PackageVersion Include="Serilog.Extensions.Hosting" Version="<Versions.md: Serilog.Extensions.Hosting>" />
    <PackageVersion Include="Serilog.Settings.Configuration" Version="<Versions.md: Serilog.Settings.Configuration>" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="<Versions.md: Serilog.Sinks.Console>" />
  </ItemGroup>

</Project>
```

**No existing entry changes.** The file already carries seven entries written by
two earlier steps, and step 4, "`Directory.Packages.props`", records what
losing either of its own two costs, which is a build that reports success and
runs wrong.

**The three Serilog packages are the parts the pipeline calls, and
`Serilog.AspNetCore` is not used.** `Serilog.Extensions.Hosting` gives
`services.AddSerilog(...)`, `Serilog.Settings.Configuration` gives
`ReadFrom.Configuration` and `Serilog.Sinks.Console` gives `WriteTo.Console`, and
`Serilog` itself comes with the first. `Serilog.AspNetCore` carries all three, at
these same versions, together with a `FrameworkReference` to
`Microsoft.AspNetCore.App`, read from its own package, and this project and
`<Solution>.Migrator` are not web projects.

`Microsoft.Extensions.Options.ConfigurationExtensions` and
`Microsoft.Extensions.Options.DataAnnotations` need no entry, because step 10
added one for each. They are referenced here for the reason step 10 gives:
`BackendCode.md` section 14's binding call needs `BindConfiguration` from the
first and `ValidateDataAnnotations` from the second, and neither is on
`OptionsBuilder<T>` by default.

Write `Code/<Solution>.Infrastructure/Persistence/DatabaseOptions.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace <Solution>.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    [Required]
    public string ConnectionString { get; init; } = string.Empty;
}
```

`BackendCode.md` section 14's table places this file at exactly this path and
calls those paths actual rather than illustrations. The same section states the
rest of it: one options class per configuration section, the class named for what
it configures plus `Options`, the connection string a member of the database's
own options class and never in the framework's `ConnectionStrings` section, and
`[Required]` on a member configuration must supply, because a section name that
is missing or misspelled otherwise leaves every member at its type's default and
the host starts anyway.

Write `Code/<Solution>.Infrastructure/Persistence/DatabaseValidateOptions.cs`:

```csharp
using Microsoft.Extensions.Options;
using Npgsql;

namespace <Solution>.Infrastructure.Persistence;

public sealed class DatabaseValidateOptions : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(options.ConnectionString);

        if (connectionString.SslMode is not SslMode.VerifyFull)
        {
            return ValidateOptionsResult.Fail("ConnectionString must set SSL Mode to VerifyFull.");
        }

        return connectionString.Password is null or { Length: 0 }
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("ConnectionString must carry no password.");
    }
}
```

`BackendCode.md` section 14 states both checks and the class itself: anything too
complex for an attribute is an `IValidateOptions<T>` class named for what it
validates plus `ValidateOptions`, registered as a singleton beside the options it
validates, and a failure of either kind stops the host at startup. The first
check is that section's own rule that the connection string sets `SSL Mode` to
`VerifyFull`, which both requires an encrypted connection and checks the server's
certificate. The second is that section's rule that the connection string carries
no password in any environment.

**The password check is new, and the class existed without it.** The text this
step replaces checked `SslMode` alone, so it passed the exact mistake it exists
to catch.

**The order of the two checks is measured and is not cosmetic.** Measured against
Npgsql 10.0.3 and SDK 10.0.302:
a connection string carrying `Password=` was refused by name, and a connection
string that both carried a password and set `SSL Mode` to something else reported
only the `SSL Mode` failure, because the method returns on the first check that
fails. So a connection string wrong in two ways names one of them, and whoever
fixes it meets the second only on the next run. `SSL Mode` is reported first
deliberately, since a connection with no TLS is the worse of the two.

**The password is tested with a pattern and not with a helper.** The parsed value
is `null` where the keyword is absent and an empty string where it is present and
empty, and `BackendCode.md` section 17 requires pattern matching where the older
form would do, which is the same shape `Validation/PropertyNameResolver.cs` uses
in step 10.

Write `Code/<Solution>.Infrastructure/Persistence/<DatabaseName>Context.cs`:

```csharp
using <Solution>.Domain.Error;
using <Solution>.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace <Solution>.Infrastructure.Persistence;

public sealed class <DatabaseName>Context : DbContext
{
    public <DatabaseName>Context(DbContextOptions<<DatabaseName>Context> options)
        : base(options)
    {
    }

    public DbSet<ErrorStatusEntity> ErrorStatuses => Set<ErrorStatusEntity>();

    public DbSet<ErrorMessageEntity> ErrorMessages => Set<ErrorMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ErrorStatusConfiguration());
        modelBuilder.ApplyConfiguration(new ErrorMessageConfiguration());
    }
}
```

`BackendCode.md` section 10 states all of this: there is one `DbContext` for the
solution, its class is named `<DatabaseName>Context` and its file is named for the
class, it declares a `DbSet<T>` property for every entity it maps,
`OnModelCreating` holds one `ApplyConfiguration` call per entity and nothing
else, and `ApplyConfigurationsFromAssembly` is never called.

**The constructor is an ordinary one and not a primary constructor.**
`BackendCode.md` section 17 states that a record declares its members in a
primary constructor and that a class never has one. The text this step replaces
had a primary constructor here, and the standard is right and that text was the
defect. The body is empty because `DbContext` keeps the options itself, through
the base constructor, so there is no field to assign.

**The nested angle brackets in `DbContextOptions<<DatabaseName>Context>` are a
collision between this document and C#, and nothing about it breaks**, the same
way step 1's `<Solution>` tags collide with the solution format's own root
element. Substituting the value closes both brackets correctly.

**Neither table is a reference table, and step 7 records why.** So
`BackendCode.md` section 11's ordinary shape applies rather than
`BackendCode.md` section 13's reference-table shape, which is why both keys are
`Guid` and not `int`.

Write
`Code/<Solution>.Infrastructure/Persistence/Configurations/ErrorStatusConfiguration.cs`:

```csharp
using <Solution>.Domain.Error;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace <Solution>.Infrastructure.Persistence.Configurations;

public sealed class ErrorStatusConfiguration : IEntityTypeConfiguration<ErrorStatusEntity>
{
    public void Configure(EntityTypeBuilder<ErrorStatusEntity> builder)
    {
        builder.ToTable("ErrorStatus", "Error");

        builder.HasKey(errorStatus => errorStatus.Id)
               .HasName("PK_ErrorStatus");

        builder.Property(errorStatus => errorStatus.Id)
               .ValueGeneratedNever();

        builder.Property(errorStatus => errorStatus.StatusCode)
               .IsRequired();

        builder.HasIndex(errorStatus => errorStatus.StatusCode)
               .IsUnique()
               .HasDatabaseName("UX_ErrorStatus_StatusCode");

        builder.Property(errorStatus => errorStatus.Code)
               .HasMaxLength(64)
               .IsRequired();

        builder.Property(errorStatus => errorStatus.Title)
               .HasMaxLength(128)
               .IsRequired();

        builder.Property(errorStatus => errorStatus.Detail)
               .HasMaxLength(500)
               .IsRequired();

        builder.HasData(
            new
            {
                Id = Guid.Parse("b53c6f07-22a7-498c-834b-cdce4fa5913e"),
                StatusCode = 400,
                Code = "BadRequest",
                Title = "Bad Request",
                Detail = "The request was not accepted."
            },
            new
            {
                Id = Guid.Parse("f2d40348-d6b7-4f61-bbb8-b1104c15449e"),
                StatusCode = 401,
                Code = "Unauthorized",
                Title = "Unauthorized",
                Detail = "Sign in to continue."
            },
            new
            {
                Id = Guid.Parse("1d4a8187-a0d7-44ab-96e6-143ec5b64b26"),
                StatusCode = 403,
                Code = "Forbidden",
                Title = "Forbidden",
                Detail = "You do not have permission to do this."
            },
            new
            {
                Id = Guid.Parse("d05419eb-4175-44ff-924b-c45cc3433e25"),
                StatusCode = 404,
                Code = "NotFound",
                Title = "Not Found",
                Detail = "That record could not be found."
            },
            new
            {
                Id = Guid.Parse("bfc0cf2d-96a5-4dc4-8e0d-02af8587622d"),
                StatusCode = 405,
                Code = "MethodNotAllowed",
                Title = "Method Not Allowed",
                Detail = "That method is not allowed for this address."
            },
            new
            {
                Id = Guid.Parse("fa7373fb-9f1b-4da7-b360-f955887d320e"),
                StatusCode = 413,
                Code = "RequestTooLarge",
                Title = "Content Too Large",
                Detail = "The request was too large."
            },
            new
            {
                Id = Guid.Parse("ada439ce-da86-415d-89ed-c228654c13bb"),
                StatusCode = 415,
                Code = "UnsupportedMediaType",
                Title = "Unsupported Media Type",
                Detail = "That content type is not supported."
            },
            new
            {
                Id = Guid.Parse("09908ee6-b837-4d43-93ba-7298b54275aa"),
                StatusCode = 429,
                Code = "TooManyRequests",
                Title = "Too Many Requests",
                Detail = "Too many requests were sent. Try again later."
            },
            new
            {
                Id = Guid.Parse("475d7bb9-b876-4573-b357-5dd9cb36774b"),
                StatusCode = 500,
                Code = "Internal",
                Title = "Internal Server Error",
                Detail = "The request could not be served."
            });
    }
}
```

`BackendCode.md` section 10 states that every entity is mapped by one
`IEntityTypeConfiguration<T>` class named for the entity plus `Configuration`,
that mapping is never written as an attribute on the entity, and that these
classes live in `Persistence/Configurations/`. `BackendCode.md` section 18 states
`ToTable(name, schema)` with the name first, the table singular and the entity's
own name without the `Entity` suffix, every database identifier PascalCase, and
the `PK_<Table>` and `UX_<Table>_<Columns>` patterns, written out even where EF
would produce the same name. `BackendCode.md` section 11 states
`ValueGeneratedNever()` on the key.

**The nine rows are declared with `HasData`.** `BackendCode.md` section 13 states
that rows ship in the entity's own configuration class that way and that
`InsertData` is never written into a migration by hand, and the same section
states that only static data the product owns and ships goes into a migration,
which is what these nine are.

**The nine codes are exactly the nine constants
`Errors/ErrorMessageCode.cs` carries**, which step 10 wrote, and the nine status
codes are `BackendCode.md` section 7's own table of nine. Nothing here invents a
tenth and nothing here writes a constant.

**Each `Id` is a literal that never changes, and that is required rather than
untidy.** `HasData` is read when the migration is generated, so a value made at
that moment would put a different id in every generated migration and the seeded
row would move. `BackendCode.md` section 11's rule that a key is created in code
with `Guid.CreateVersion7()` governs a row the application creates at run time,
and nothing creates one of these: the entity's constructor is never called for
them. **The nine literals are carried across unchanged** from the text this step
replaces, so a solution created before this document changed and one created
after it hold the same nine ids.

**The two unique indexes are what the lookups in step 10 depend on.**
`Managers/ErrorMessage/ErrorMessageManager.cs` keys one dictionary by
`StatusCode` and the other by `Code`, and `BackendCode.md` section 18 requires a
record lookup keyed by a field other than the id to carry that field's name, so
both fields have to be unique for either lookup to mean anything.

Write
`Code/<Solution>.Infrastructure/Persistence/Configurations/ErrorMessageConfiguration.cs`:

```csharp
using <Solution>.Domain.Error;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace <Solution>.Infrastructure.Persistence.Configurations;

public sealed class ErrorMessageConfiguration : IEntityTypeConfiguration<ErrorMessageEntity>
{
    public void Configure(EntityTypeBuilder<ErrorMessageEntity> builder)
    {
        builder.ToTable("ErrorMessage", "Error");

        builder.HasKey(errorMessage => errorMessage.Id)
               .HasName("PK_ErrorMessage");

        builder.Property(errorMessage => errorMessage.Id)
               .ValueGeneratedNever();

        builder.Property(errorMessage => errorMessage.Code)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasIndex(errorMessage => errorMessage.Code)
               .IsUnique()
               .HasDatabaseName("UX_ErrorMessage_Code");

        builder.Property(errorMessage => errorMessage.Detail)
               .HasMaxLength(500)
               .IsRequired();
    }
}
```

**This class declares no row, and the `ErrorMessage` table is empty on day one.**
Every row in it belongs to an operation's own code, and no operation exists yet.
`BackendCode.md` section 7 states what happens until one does: a code the shared
method derived from the status takes the baseline detail on its status row and
writes no line, and an operation's code with no message row falls back to the
baseline detail on its status row and logs at `Error` every time. Step 10's
`Errors/ProblemDetailsCustomizer.cs` already carries both branches. So the empty
table is the working state and not a gap.

Write `Code/<Solution>.Infrastructure/Persistence/DatabaseDataSourceFactory.cs`:

```csharp
using System.Security.Cryptography.X509Certificates;
using Npgsql;

namespace <Solution>.Infrastructure.Persistence;

public static class DatabaseDataSourceFactory
{
    private const string CertificatesFolderName = "Certificates";

    private const string AuthorityFileName = "ca.crt";

    private const string ContainerCertificatesFolder = "/certificates";

    public static NpgsqlDataSource Get(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.ConnectionStringBuilder.GssEncryptionMode = GssEncryptionMode.Disable;

        var folder = GetCertificatesFolder();

        var role = new NpgsqlConnectionStringBuilder(connectionString).Username
            ?? throw new InvalidOperationException(
                "ConnectionString must name this host's own database role in Username.");

        var authorityFile = Path.Combine(folder, AuthorityFileName);
        var certificateFile = Path.Combine(folder, $"{role}.crt");
        var keyFile = Path.Combine(folder, $"{role}.key");

        var missingFile = new[] { authorityFile, certificateFile, keyFile }
            .FirstOrDefault(file => !File.Exists(file));

        if (missingFile is not null)
        {
            throw new InvalidOperationException($"The database certificate file {missingFile} is missing.");
        }

        var authority = X509CertificateLoader.LoadCertificateFromFile(authorityFile);
        var roleCertificate = X509Certificate2.CreateFromPemFile(certificateFile, keyFile);

        builder.UseRootCertificate(authority);
        builder.UseSslClientAuthenticationOptionsCallback(
            sslOptions => sslOptions.ClientCertificates = [roleCertificate]);

        return builder.Build();
    }

    private static string GetCertificatesFolder()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);

        while (folder is not null)
        {
            if (folder.EnumerateFiles("*.slnx").Any())
            {
                return Path.Combine(folder.FullName, CertificatesFolderName);
            }

            folder = folder.Parent;
        }

        return ContainerCertificatesFolder;
    }
}
```

**This class exists because `BackendCode.md` section 14 states that the
connection string carries no certificate path in any environment**, and that
`PersistenceRegistration` gives the certificates to the data source it builds as
objects rather than as paths. Building them is real work rather than an
assignment, and `BackendCode.md` section 9 states that a registration method
assigns and never implements and that the method a framework hook takes lives in
a named class in its concern folder, so the work lives here.

**The class's name is derived and not taken from an example.**
`BackendCode.md` section 18 names a class for what it is about plus what it is,
with what it is coming last, and where no rule states that second word it is
taken from this document's own word for the thing, then the framework's own word,
then the noun formed from the one action. `BackendCode.md` section 13 calls the
class that builds the context a factory, so `Factory` is the document's own word,
and `<DatabaseName>ContextDesignTimeFactory` in this same folder carries it. The
first part is `Database` because `BackendCode.md` section 14's table of actual
paths already names this concern that way, in `DatabaseOptions` and
`DatabaseValidateOptions` beside it. `DataSourceFactory` on its own is refused by
`BackendCode.md` section 18's opening rule, since it says only that there is a
data source without saying of what.

**`Get` is the whole of the method's name, and the second half is left off
deliberately.** `BackendCode.md` section 18 states that `Get` also names
producing a value that is not a record, and that where the method's own class is
named for what the action applies to the method is the action alone. This class is
named for the data source, so the name would otherwise repeat it. That section
also makes the call site the test, and `DatabaseDataSourceFactory.Get(...)` shows
the data source once.

**The walk anchors on `AppContext.BaseDirectory` and on nothing else.** Measured against SDK 10.0.302: across
`dotnet run` from the project folder, `dotnet run --project` from the parent, and
the built binary, `Environment.CurrentDirectory` and
`IHostEnvironment.ContentRootPath` both followed the directory the command was
typed in, while `AppContext.BaseDirectory` was identical in all three. So a
relative path resolved from anywhere else reaches two different places on two
runs of the same binary.

**It looks for any `.slnx` rather than for `<Solution>.slnx`.** The preconditions
of this skill already establish that `Code/` holds exactly one, and matching the
extension keeps the solution's name out of a file that would otherwise carry it
for no reason.

**Where no solution file is found above the binary, the folder is
`/certificates`.** That is where a host running in a container finds its own
role's files, which step 12's `compose.yml` mounts there. **Ruled by Francois on
2026-10-21**: a host in a container finds its certificates at `/certificates`,
the walk to the `.slnx` stays as it is, and no certificate path enters
configuration. **Also ruled by Francois on 2026-10-21: the fallback is not
checked for.** A host with no `.slnx` above it always uses `/certificates`, so
there is no case where nothing is loaded. **It is reached with no code naming an
environment**, which `BackendCode.md` section 19 refuses. Measured on
2026-10-21: the Migrator's image and an API's image, each run with nothing
mounted at `/certificates`, refused to start with "The database certificate file
/certificates/ca.crt is missing." and exited 1, while `dotnet run` on the host
still walked up to the `.slnx` and connected with `Code/Certificates/`.

**Where one of the three files is missing, the host refuses to start and names
the file it wanted.** Measured,
by moving one role's certificate aside: an `InvalidOperationException` carrying
the full path. The check covers the authority, the certificate and the key.

**GSS encryption is turned off on every connection, in code.** Npgsql tries GSS
encryption before TLS, and a chiseled .NET image, which step 12's Dockerfile
runs on, has no Kerberos library. **Ruled by Francois on 2026-10-21: turn it off
in `Get`**, so that no connection string changes and the setting holds
everywhere. Measured on 2026-10-21: without the line, each host in its container
wrote `Cannot load library libgssapi_krb5.so.2` and
`Error: libgssapi_krb5.so.2: cannot open shared object file` on standard error,
outside the log format, and still connected; with it, neither line appeared, and
nothing else changed. `GssEncryptionMode` is in the `Npgsql` namespace, so the
line needs no new `using`.

**The role's three file names are derived from the connection string and are not
asked for.** `BackendCode.md` section 14 has every host connect as the role its
connection string names in `Username=`, and the database maps a client
certificate's own subject to that role, so the role's name is the file's name and
a second value would only be a way of disagreeing with the first. It was measured that the only difference between two hosts' settings files is the
`Username=`, and it was measured that the server refused a role whose
requested name did not match its certificate.

**A connection string with no `Username=` is refused by name rather than
producing a file name of `.crt`.** It is a condition a developer editing a
settings file can produce, so `BackendCode.md` section 20's rule against checking
for a condition the caller cannot produce does not reach it.

**Two loading calls are forced, and the obvious form of each fails the build.**
Measured against Npgsql 10.0.3 and .NET 10:
`UseClientCertificate` is obsolete and fails the build outright under
`TreatWarningsAsErrors`, so the shape is `UseRootCertificate` plus
`UseSslClientAuthenticationOptionsCallback` setting `ClientCertificates`; and the
`X509Certificate2` constructor that takes a path is obsolete the same way, so the
authority is loaded with `X509CertificateLoader.LoadCertificateFromFile`.
`X509Certificate2.CreateFromPemFile` takes a PEM certificate and an unencrypted
PKCS#8 key with no conversion to PFX.

**Npgsql picks up no certificate from a conventional location, so none of this is
belt and braces.** Measured: its assembly contains none of
`.postgresql`, `root.crt`, `postgresql.crt`, `postgresql.key`, `PGSSLROOTCERT` or
`PGSSLCERT`, and only `PGPASSFILE` is present.

Write
`Code/<Solution>.Infrastructure/Persistence/<DatabaseName>ContextDesignTimeFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace <Solution>.Infrastructure.Persistence;

public sealed class <DatabaseName>ContextDesignTimeFactory : IDesignTimeDbContextFactory<<DatabaseName>Context>
{
    public <DatabaseName>Context CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<<DatabaseName>Context>()
            .UseNpgsql(PersistenceRegistration.SetMigrationHistoryTable)
            .Options;

        return new <DatabaseName>Context(options);
    }
}
```

`BackendCode.md` section 10 places this file beside the context, and
`BackendCode.md` section 13 states what it is for and what it applies.
`BackendCode.md` section 18's rule for a class implementing a framework interface
names it, from `IDesignTimeDbContextFactory<T>`, and `CreateDbContext` is the name
that interface declares, which the same section states is carried rather than
chosen.

**This class exists so that `dotnet ef` never builds the real host**, and step
14's database fixture makes every context it hands out through it as well.
Measured against SDK 10.0.302, `dotnet-ef` 10.0.11 and EF Core 10.0.12: without
it, generating a migration fails, because the tool builds the context through
`<Solution>.Migrator`'s own `Program.cs`, which validates `DatabaseOptions` on
start against a connection string nothing configures at design time. It is EF
Core's own documented answer to that shape of problem and is not invented here.

**It takes no connection string at all, and needs neither a connection nor a
certificate.** Measured: this overload applies Npgsql's options
with no connection string, and that is the whole of what `migrations add` and
`migrations script` need. Applying migrations to the solution's own database
through this factory is never the path, because `BackendCode.md` section 13 gives
`<Solution>.Migrator` that job. Step 14's tests migrate only the databases they
create in their own container, which `Testing.md` section 5 requires.

**Step 14's fixture uses this class because it is the only way the history
table's setting reaches a test.** Measured: `SetMigrationHistoryTable`
is `internal` to this project, and a context made here and then given its
connection string carried the setting, so a test database's history went to
`Migration.MigrationHistory`.
**The text this step replaces passed a connection string here**, which was both
unnecessary and a place a product's name could sit in a document that carries
none.

Write `Code/<Solution>.Infrastructure/Persistence/PersistenceRegistration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace <Solution>.Infrastructure.Persistence;

public static class PersistenceRegistration
{
    public static IServiceCollection Add<Solution>Persistence(this IServiceCollection services)
    {
        services.AddOptions<DatabaseOptions>()
                .BindConfiguration("<Solution>:Database")
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseValidateOptions>();

        services.AddDbContext<<DatabaseName>Context>(ConfigureContext);

        return services;
    }

    internal static void SetMigrationHistoryTable(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.MigrationsHistoryTable("MigrationHistory", "Migration");

    private static void ConfigureContext(IServiceProvider provider, DbContextOptionsBuilder options)
    {
        var database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        options.UseNpgsql(
                    DatabaseDataSourceFactory.Get(database.ConnectionString),
                    SetMigrationHistoryTable)
               .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
```

`BackendCode.md` section 9 states the class, its path and its method's name: a
concern's registration class is named for the concern plus `Registration` and
lives in that concern's folder, and an extension method is named `Add` plus the
solution's name plus its class's name without `Registration`. The same section's
table gives this project's method the `DbContext` to register.
`BackendCode.md` section 14 fixes the binding call as `AddOptions<T>`, then
`BindConfiguration`, then `ValidateDataAnnotations`, then `ValidateOnStart`, fixes
the section name as `<Solution>:` plus the class name without `Options`, and
requires the validator registered as a singleton beside the options it validates.
`BackendCode.md` section 10 fixes `QueryTrackingBehavior.NoTracking` on the
context rather than on each query.

**`ConfigureContext` is a named private method rather than a lambda.**
`BackendCode.md` section 9 states that a registration method assigns and never
implements and that no logic is written inline as a lambda, and this body
resolves an options class and makes two calls. It is the same shape
`ApiCommonRegistration`'s own `AllowRequestWithNoEndpointMetadata` takes in step
10. **Its name is derived**: `BackendCode.md` section 18 has a method that
performs a framework action take the framework's own word for it, which is
`Configure` beside `DbContextOptionsBuilder`, the same word
`Logging/LoggingPipeline.cs` below carries for the same kind of hook, and the same
section has the method name what the action applies to where its class does not,
which `PersistenceRegistration` does not.

**`SetMigrationHistoryTable` is the one method both callers call.**
`BackendCode.md` section 13 states the rule and the reason: left to itself the
tool creates the history table with no schema named, which lands it in the
default schema, and `BackendCode.md` section 18 requires every schema to be named
explicitly and never `public`. The tool never goes through this registration, so
the setting lives in one method and the design-time factory above calls the same
one. **Where the two disagree the tool writes to one table and a running API
reads the other**, so every API sees every migration as pending and refuses to
serve, with nothing saying why.

**Its name is derived from `BackendCode.md` section 18 and from no example.**
That section names a method for the action it performs and what the action
applies to; setting a table is none of the four record actions, so the word that
names the action gives `Set`; and the method names what the action applies to
because its class, `PersistenceRegistration`, does not. The Npgsql options call
fixes the parameter's type and not the name.

**It is `internal` because `<DatabaseName>ContextDesignTimeFactory` is its only
other caller and sits in the same assembly.** Step 10's
`ProblemDetailsCustomizerLogCategory` is the same choice for the same reason.

**The relocated table applies, and the two callers agree.** Measured against `dotnet-ef` 10.0.11, EF Core 10.0.12 and PostgreSQL 18.4:
the host created `Migration.MigrationHistory`, `Error.ErrorStatus` and
`Error.ErrorMessage`, the key came out `PK_MigrationHistory` without being asked
for, and `dotnet ef migrations script` and the running host agreed, the script
emitting `CREATE SCHEMA "Migration"` and
`CREATE TABLE IF NOT EXISTS "Migration"."MigrationHistory"`.

**`NpgsqlDbContextOptionsBuilder` is imported from
`Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure`, and that import is
required.** Checked against Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 and SDK
10.0.302: with the import the file builds with 0 warnings and 0 errors under
both of step 3's warning settings, and without it the build fails `CS0246`.

Write `Code/<Solution>.Infrastructure/Logging/LoggingPipeline.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace <Solution>.Infrastructure.Logging;

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
```

This is `BackendCode.md` section 15's own code block, which that section calls
the actual one rather than an illustration, written as it stands there.
`BackendCode.md` section 15 places the file, fixes the output template literally,
states that only the levels come from configuration and are read from the
`Serilog` section, states that this pipeline is the one place in the solution
that reads `IConfiguration`, and states that every host gets the pipeline from
the call it already makes so nothing is written per host.

**Two things this file does not do, and both are reported at the end of this
skill rather than solved here.** `BackendCode.md` section 15 requires every line
written while a request is served to carry the caller and the network address the
request came from, as enriched properties beside the trace id and the
application's name, and the code block that same section fixes enriches the
application's name only. The caller cannot exist until an authentication scheme
does. And `BackendCode.md` section 21 bans `IConfiguration` as a symbol, while
`BackendCode.md` section 15 makes this file the one legitimate reader of it, so
the solution-wide enforcer pass that writes `BannedSymbols.txt` has to allow it
here. `BackendCode.md` section 17 states the form that allowance takes, being a
suppression carrying its reason at the point of use.

Write `Code/<Solution>.Infrastructure/Logging/LoggingRegistration.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace <Solution>.Infrastructure.Logging;

public static class LoggingRegistration
{
    public static IServiceCollection Add<Solution>Logging(this IServiceCollection services) =>
        services.AddSerilog(LoggingPipeline.Configure);
}
```

`BackendCode.md` section 9 names the class and its method: a concern's
registration class is named for the concern plus `Registration` and lives in that
concern's folder, and an extension method is named `Add` plus the solution's name
plus its class's name without `Registration`.

**The pipeline is in `<Solution>.Infrastructure` so that every host reaches it.**
`BackendCode.md` section 9 counts `<Solution>.Migrator` as a host beside each API,
and `BackendCode.md` section 3 gives the Migrator Infrastructure and not
`<Solution>.ApiCommon`, so `Add<Solution>Infrastructure`, the call every host
already makes, is the one call that reaches them all. Ruled by Francois on
2026-10-13. Measured:
the Migrator and an API each wrote every line in the output template above, each
naming itself in `Application`.

Write `Code/<Solution>.Infrastructure/InfrastructureRegistration.cs`:

```csharp
using <Solution>.Infrastructure.Logging;
using <Solution>.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace <Solution>.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection Add<Solution>Infrastructure(this IServiceCollection services) =>
        services.Add<Solution>Persistence()
                .Add<Solution>Logging();
}
```

**This file does not exist before this step, and step 8 says why.** That step
creates `<Solution>.Infrastructure` as a shell and records that the project
registers nothing yet, so the file would have been the empty, speculative file
`BackendCode.md` section 20 refuses. `Persistence/` and `Logging/` are the
project's first concerns, so this is the step that creates it.

`BackendCode.md` section 9 states all of it: a shared project that holds
something to register owns one extension method, in a class named for the project
plus `Registration` at the project's root, and a project whose registrations
cover more than one concern splits them one class per concern with the project's
own method calling each. `Integrations/` has no concern yet, so this method calls
two.

**Every host calls this method and not `Add<Solution>Persistence`.**
`BackendCode.md` section 9 states that `Program.cs` calls one method per project
and knows nothing about the split, and that a host calls the project's method
even where it needs only one concern.

**Restore the solution first**, from `Code/`:

```bash
dotnet restore
```

**`dotnet ef` fails without it, and nothing before this step restores.** Checked
on 2026-10-07 against SDK 10.0.302 and `dotnet-ef` 10.0.11, on the four projects
steps 7 to 11 write: with no restore, `migrations add` failed with `NETSDK1004`,
"Assets file ... project.assets.json not found", and "Unable to retrieve project
metadata". After `dotnet restore` the same command generated the migration.

**Generate the seed migration**, per `BackendCode.md` section 13's own command
shape, from `Code/`:

```bash
dotnet ef migrations add CreateErrorTables \
    --project <Solution>.Infrastructure \
    --startup-project <Solution>.Migrator \
    --output-dir Persistence/Migrations
```

`BackendCode.md` section 13 states that a migration is generated by the tool and
never written by hand from scratch, that the generating command names the project,
the startup project and the output directory, because left to its defaults the
tool writes to a `Migrations` folder at the project's root and nothing fails, and
that the output directory is relative to the project with the model snapshot
written beside the migration.

**`CreateErrorTables` is the migration's name, and it is derived.**
`BackendCode.md` section 13 states that a migration is named as a verb and a
noun, and `BackendCode.md` section 18's opening rule is that every name says what
the thing is. This migration creates the `Error` schema and both tables and
inserts nine rows into one of them, so `Create` is the verb that covers it.
**The text this step replaces called it `SeedErrorTables`**, which named the
smaller half and was false of `ErrorMessage`, which receives no row.

**`<Solution>.Migrator` still holds step 9's placeholder `Program.cs` when this
runs, and that is correct.** The tool builds the startup project and then builds
the context through the design-time factory above, so it never runs the real host.
Step 12 replaces that placeholder, after this.

**Read the generated migration now, before step 12 applies it.**
`BackendCode.md` section 13 states that a generated migration is read before it
is committed, and states the two things it is read for, a `RenameColumn` nobody
asked for and a new column on a populated table carrying a default nobody chose.
**Here it is read before it is applied as well**, because step 12 runs it against
a real database before this skill commits. **Neither of the two can arise
here and both are still checked**: both tables are new, so there is no column to
rename and no existing row for a default to land on.

**What the generated migration holds, checked against
`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, EF Core 10.0.12, `dotnet-ef`
10.0.11 and SDK 10.0.302.** `EnsureSchema` for `Error`, `CreateTable` for
`ErrorMessage` and `ErrorStatus` with exactly the columns, lengths and primary
key names declared above, `InsertData` carrying all nine `ErrorStatus` rows, both
unique indexes, and no `RenameColumn` anywhere. `dotnet ef migrations script`
then emitted `CREATE SCHEMA "Migration"` and
`CREATE TABLE IF NOT EXISTS "Migration"."MigrationHistory"` with
`CONSTRAINT "PK_MigrationHistory"`, which is the shared method above reaching the
tool through the design-time factory.

**`dotnet ef` prints a line saying its tools are older than the runtime, and the
command still succeeds.** It is informational. Anything that ends in a non-zero
exit is a real failure: stop and report it rather than carrying on.

**This migration and its model snapshot are committed exactly as EF generated
them**, in `Code/<Solution>.Infrastructure/Persistence/Migrations/`, and are not
reproduced in this document, because `BackendCode.md` section 13 states that a
migration's file name carries a timestamp and that a migration is never edited by
hand. The snapshot's own name is derived from the context's, so it is
`<DatabaseName>ContextModelSnapshot.cs`, and nobody writes it.

**This migration grants nothing to any database role, and that is a decision
rather than an omission.** `BackendCode.md` section 13 has a migration that
creates a table also grant each API role the privileges its plan states, and
no API role exists: `create-solution` creates no API. The same section permits
a migration that grants and creates nothing, for tables that existed before the
role that needs them, so each API's role gets a grant-only migration of its own.
**`create-project` writes that migration**, granting that API's role `USAGE` on
the `Error` and `Migration` schemas and `SELECT` on `ErrorStatus`, `ErrorMessage`
and `MigrationHistory`, and nothing else. Those five are derived rather than
chosen, because `BackendCode.md` section 7 has every API read both Error tables
at startup and `BackendCode.md` section 13 has it read the history table to
decide whether it is behind. Until that migration runs, only the Migrator's own
role can read any of it, which this skill reports at the end.

### 12. `<Solution>.Migrator`'s real `Program.cs`

This step replaces step 9's placeholder whole, gives the Migrator the three files
it needs to find its own configuration, and then runs it. `BackendCode.md`
section 13 states what it is for: `<Solution>.Migrator` applies migrations,
`BackendCode.md` section 3 gives migrations one owner and states that no API
migrates at startup, and `BackendCode.md` section 9 makes the Migrator a host
like any other, registering shared services once in its own `Program.cs`.

Replace `Code/<Solution>.Migrator/<Solution>.Migrator.csproj` whole:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../<Solution>.Infrastructure/<Solution>.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="appsettings.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup Condition="'$(Configuration)' == 'Debug'">
    <Content Include="appsettings.Development.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

**Replace `Code/Directory.Packages.props` whole, adding one `PackageVersion`
entry at the end of the same `ItemGroup`**, which then holds thirteen:

```xml
<Project>

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="<Versions.md: Microsoft.EntityFrameworkCore>" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="<Versions.md: Microsoft.EntityFrameworkCore.Relational>" />
    <PackageVersion Include="FluentValidation" Version="<Versions.md: FluentValidation>" />
    <PackageVersion Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="<Versions.md: Microsoft.Extensions.Options.ConfigurationExtensions>" />
    <PackageVersion Include="Microsoft.Extensions.Options.DataAnnotations" Version="<Versions.md: Microsoft.Extensions.Options.DataAnnotations>" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="<Versions.md: Microsoft.AspNetCore.OpenApi>" />
    <PackageVersion Include="Scalar.AspNetCore" Version="<Versions.md: Scalar.AspNetCore>" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="<Versions.md: Npgsql.EntityFrameworkCore.PostgreSQL>" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="<Versions.md: Microsoft.EntityFrameworkCore.Design>" />
    <PackageVersion Include="Serilog.Extensions.Hosting" Version="<Versions.md: Serilog.Extensions.Hosting>" />
    <PackageVersion Include="Serilog.Settings.Configuration" Version="<Versions.md: Serilog.Settings.Configuration>" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="<Versions.md: Serilog.Sinks.Console>" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="<Versions.md: Microsoft.Extensions.Hosting>" />
  </ItemGroup>

</Project>
```

**The package is needed and the plain SDK does not carry it.** `Host` and
`HostApplicationBuilderSettings` both come from it, and
`Code/Directory.Build.props` gives this project no framework beyond the base
library. It is the one package this project needs beyond step 9's shell.

**The `Content` item is required, and the Web SDK's own behaviour is what hides
that.** Measured, by listing
both outputs: an API project on the Web SDK carried its
`appsettings.Development.json` into the build output and this project carried
only its `deps.json` and `runtimeconfig.json`, because the plain SDK copies no
settings file at all. `BackendCode.md` section 14 does not state it.

**The `Debug` condition is what keeps that file out of a deployed artefact.**
Measured: a Release publish was carrying
`appsettings.Development.json` for both hosts, which nobody asked for, and
conditioning this item on the Debug configuration left the Debug build output
carrying the file and the Release publish without it, with the solution still
building clean. **Checked again against SDK 10.0.302 on the project set this
document writes**: the Debug output holds `appsettings.Development.json` and the
Release publish holds neither it nor the launch profile below.

Replace `Code/<Solution>.Migrator/Program.cs` whole:

```csharp
using <Solution>.Infrastructure;
using <Solution>.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.Add<Solution>Infrastructure();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    await host.StartAsync();
}
#pragma warning disable CA1031 // Program.cs is not an operation, so BackendCode.md section 7's rule does not reach it
catch (Exception)
#pragma warning restore CA1031
{
    return 1;
}

logger.LogInformation("{Application} has started", builder.Environment.ApplicationName);

try
{
    using var scope = host.Services.CreateScope();

    var database = scope.ServiceProvider.GetRequiredService<<DatabaseName>Context>();

    await database.Database.MigrateAsync();
}
#pragma warning disable CA1031 // Program.cs is not an operation, so BackendCode.md section 7's rule does not reach it
catch (Exception exception)
#pragma warning restore CA1031
{
    logger.LogError(exception, "The migrations could not be applied");

    return 1;
}

await host.StopAsync();

return 0;
```

`BackendCode.md` section 13 states that `<Solution>.Migrator` is a console
application returning an exit code, that zero means the schema is up to date, and
that it holds no list of migrations and does not change when one is added. It
calls `<Solution>.Infrastructure`'s one registration method, which
`BackendCode.md` section 9 requires, and runs the migration call on the context
`BackendCode.md` section 10 names.

**It logs one line when it has started, naming itself, and every line it writes
goes through the pipeline step 11 gave `<Solution>.Infrastructure`.**
`BackendCode.md` section 15 has every host log one startup line naming itself, and
`BackendCode.md` section 9 counts this project as a host. The call to
`Add<Solution>Infrastructure` brings the pipeline, so nothing here configures
logging. Measured: before the
pipeline reached this host, every line it wrote was in the framework's own console
format.

**Both of its failures end in exit 1, with the reason in the log.** `await
host.StartAsync()` sits inside a `catch` that returns 1 and logs nothing, because
the host has already written a failed start at `Error`, and `BackendCode.md`
section 15 lists a failed start under "What is not logged by hand". That is the
shape `create-project` gives an API's `Program.cs`. The migration call sits
inside a second `catch` that logs "The migrations could not be applied" at
`Error`, with the exception, and returns 1, because a migration that fails is not a
failed start and nothing else writes its reason in the log's format.
`BackendCode.md` section 13 has anything other than zero mean the schema is not up
to date. Ruled by Francois on 2026-10-13. Without either `catch`, the runtime
writes its own text for an unhandled exception after the log's lines and the
process exits 134, measured, and the reason a
migration failed reached only that text. Measured: a refused
connection string and a failing migration each ended in exit 1, with the reason
on an `Error` line in the log's format and no text from the runtime. **Each is
written `catch (Exception)`**, which says what it catches. `BackendCode.md`
section 7's rule "No operation catches `Exception`" does not reach `Program.cs`,
which is not an operation. **Each `catch` sits between `#pragma warning disable
CA1031`, with its reason, and `#pragma warning restore CA1031`**, as
`BackendCode.md` section 22 states, because `CA1031`, the rule's enforcer, reaches
every file.

**`StartAsync` is awaited before the scope is created, and that is the fix rather
than a flourish.** Measured: with the host built and never
started, which is how the text this step replaces left it, a refused connection
string surfaced as an unhandled `OptionsValidationException` thrown inside the
data source factory, so `ValidateOnStart` never ran at all. With `StartAsync`
awaited first, the same connection string produced "Hosting failed to start"
carrying the validation failure and the statement after the start never ran, and
on an accepted connection string the host started, the migrations applied and the
host stopped cleanly.

**The content root is set to `AppContext.BaseDirectory`, and without it this host
cannot find its own settings file.** Measured: run from its own
output directory it read the file and connected, and run from the filesystem root
or through `dotnet run --project` from the solution root it bound nothing and
refused to start naming `ConnectionString` as required, which is how a developer
runs it. With it, the host read the file in all three positions.
**It also puts configuration and the certificate walk on the same anchor**, where a measurement shows they otherwise disagree about where they are.

**`HostApplicationBuilderSettings` is the overload that carries it, and `Args`
has to be set on it.** `Host.CreateApplicationBuilder(args)` takes no content
root, so the settings object is used instead, and leaving `Args` out of it would
drop the command line this host is given. Checked against
`Microsoft.Extensions.Hosting` 10.0.12 and SDK 10.0.302: the shape above builds
with 0 warnings and 0 errors under both of step 3's warning settings.

**`MigrateAsync` is called with no cancellation token.**
`BackendCode.md` section 16 names `SaveChangesAsync` and the one explicit
transaction's `CommitAsync` as the two calls that take `CancellationToken.None`
written out, and names no other. This host serves no request, so there is no
request token to pass and none to drop.

**`using` rather than `await using` on both the host and the scope.**
`BackendCode.md` section 16 states that `await using` is used wherever a type
offers `DisposeAsync`, and neither `IHost` nor `IServiceScope` declares one.

Write `Code/<Solution>.Migrator/appsettings.Development.json`:

```json
{
  "<Solution>": {
    "Database": {
      "ConnectionString": "Host=localhost;Port=<Port>;Database=<DatabaseName>;Username=<Solution>MigratorRole;SSL Mode=VerifyFull"
    }
  }
}
```

**This file is committed, and every value in it is the same on every developer
machine.** `BackendCode.md` section 14 states that `appsettings.Development.json`
stays in the repository for local defaults that are not secret, that no secret
appears in any `appsettings` file, that the connection string is a member of the
database's own options class in its own section rather than in the framework's
`ConnectionStrings` section, and that the section is `<Solution>:` followed by
the class name without `Options`.

**It carries no password and no certificate path, in this environment or any
other.** `BackendCode.md` section 14 states both: every host connects as the role
its `Username=` names and proves itself with a client certificate, and
`PersistenceRegistration` gives the certificates to the data source as objects.
`DatabaseValidateOptions` above refuses a connection string that breaks either
half.

**The host is written as it stands and is not asked for, and the port is the one
this skill asked for.** Every value in this file is the same on every developer
machine of this solution, which is what makes the file committable. The port is
asked once, when the solution is created, and every later machine reads it from
this file. They are written out rather than left to Npgsql's own defaults because
`create-project` reads the database, the host and the port out of this file
rather than asking for them again.

**`Username=` is this host's own role and no other.** `BackendCode.md` section 3
and `BackendCode.md` section 13 give `<Solution>.Migrator` the schema, and an
API's role is never the right to change it, so the Migrator cannot borrow one.
Measured: two hosts in one solution each read their own
`appsettings.Development.json`, each connected as its own role, and the database
reported a different client subject for each, with the `Username=` the only
difference between the two files.

Write `Code/<Solution>.Migrator/appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "Microsoft.Hosting.Lifetime": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
      }
    }
  }
}
```

**The two `Serilog` levels keep the log to what a run did.** `BackendCode.md`
section 14's table gives `appsettings.json` the defaults that are the same in every
environment, and `BackendCode.md` section 15 takes only the levels from
configuration, from the `Serilog` section. The framework writes lines of its own
when a host starts and stops, under `Microsoft.Hosting.Lifetime`, at
`Information`, and `BackendCode.md` section 15 has the host's own startup line be
the only one, so that category is raised to `Warning`. EF writes the full SQL text
of every command it runs under `Microsoft.EntityFrameworkCore.Database.Command`, at
`Information`, and that category is raised to `Warning` too, ruled by Francois on
2026-10-13: the SQL is already in the repository, in the migration file of the same
name, and a command that fails is written at `Error`, so it still reaches the log.
`Microsoft.EntityFrameworkCore.Migrations` is not raised, so each run says which
migrations it applied. No other level is set, because nothing has asked for
another, which `BackendCode.md` section 20 states.

**The connection string is still not in this file.** It differs per environment,
and an environment variable supplies it there. It was measured that
`<Solution>__Database__ConnectionString` binds to
`<Solution>:Database:ConnectionString`.

**The `Content` item for this file has no `Debug` condition**, so a Release
publish carries it and a deployed run reads its levels, while the plain SDK copies
no settings file by itself, which was measured. Measured: the Release publish held `appsettings.json` and not
`appsettings.Development.json`.

Write `Code/<Solution>.Migrator/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "<Solution>.Migrator": {
      "commandName": "Project",
      "environmentVariables": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**This file is committed, and it is the only thing that puts this host in the
`Development` environment.** Measured:
`Host.CreateApplicationBuilder` defaults the environment to `Production`, and
`ASPNETCORE_ENVIRONMENT`, which the Web SDK reads, does not apply to a console
host, so without this file the host reads no `Development` settings file at all.
`BackendCode.md` section 19 refuses code that names an environment, so it is set
outside code.

**The profile is not a new mechanism.** The Web SDK's own template writes one for
an API and sets the web equivalent, which is how an API has been getting
`Development` all along, and the console template simply writes none. The profile
is named for the project, which is what that template does, since there is no
console template name to carry over.

**It holds no secret and the `.gitignore` step 2 wrote does not exclude it.**
It was measured that there is no `launchSettings.json` pattern in that
file, and it was measured that committing this one needs no change there.

**It overrides the shell, which is why a deployed environment is defended by
something else.** Measured: `dotnet run` with nothing set came
up `Development`, `dotnet run` with the environment variable set to `Production`
also came up `Development`, and only `--no-launch-profile` respected the shell.
**A published artefact carries no profile at all**, measured on
a Release publish of both hosts, so this file cannot reach a deployed environment
and a published binary with nothing set comes up `Production`.

**Start the local database and the Migrator.** The Migrator's run below needs a
database to apply its migration to, and this is where the solution gets one.

**The database and the Migrator run in Docker, from `compose.yml` at the
repository root, and a machine needs nothing but Docker to run them.** No
PostgreSQL, no OpenSSL, no .NET SDK and no AI tool is installed for it. **Ruled by
Francois on 2026-10-21**: the solution is handed to people who clone it and run
it, and who cannot be assumed to have anything installed but Docker, so every
host runs in a container: a one-off `migrator` service here, and one long-running
service per API, which `create-project` adds. `compose.yml` sits at the root,
beside `.gitignore`, and not under `Code/`, because no `dotnet` build reads it and
because Docker Compose looks for `compose.yml` in the folder it is run from and
then in each folder above it, so the same command works from the root and from
`Code/`.

Write `Code/Certificates/.gitignore`, where it is absent:

```gitignore
*
!.gitignore
```

**It is written before anything can put a key in the folder**, and it is the one
file in the folder that is committed. A clone arrives with the folder holding it
alone, which is why the folder exists on every machine before `compose.yml`
mounts it.

Write `Code/.dockerignore`:

```
**/bin/
**/obj/
Certificates/
```

**Nothing from `Code/Certificates/` reaches an image. Ruled by Francois on
2026-10-21**, and this file is how: every image is built from `Code/`, and Docker
leaves out of the build what this file names. `bin/` and `obj/` are what the
tools write on the host, and the build makes its own. **There is no
`node_modules/` line**, because Zapper is API only and nothing under `Code/` is
installed with npm. Ruled by Francois on 2026-10-22. Measured on
2026-10-21: neither the Migrator's image nor an API's held a key, a
`Certificates/` folder, the `.slnx` or `appsettings.Development.json`.

Write `Code/<Solution>.Migrator/Dockerfile`:

```dockerfile
FROM <Versions.md: .NET SDK, the build image> AS build
WORKDIR /source/Code
COPY --from=docs AcceptedVulnerabilities.md /source/Docs/
COPY . .
RUN dotnet restore <Solution>.Migrator/<Solution>.Migrator.csproj --locked-mode
RUN dotnet publish <Solution>.Migrator/<Solution>.Migrator.csproj --no-restore --configuration Release --output /app

FROM <Versions.md: .NET runtime, the Migrator's image>
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "<Solution>.Migrator.dll"]
```

**The image is built in two stages, and only the second is kept.** The first
restores and publishes on the SDK's image; the second holds the published output
and nothing else, on Microsoft's chiseled runtime image. **Ruled by Francois on
2026-10-21: the images are Microsoft's chiseled .NET images**, which have no
shell and no package manager and run as a user that is not an administrator of
its container, which `Security.md` section 11 requires of every part. Both stages name the same
Ubuntu, 24.04, by the `-noble` in their tags. Measured: the chiseled images run as
user `1654`.

**The restore is in locked mode**, as `BackendCode.md` section 2 has every restore
run, so the image is built from exactly the packages the lock file names.
**A lock file that no longer matches its project fails the build.** Measured: with
one entry removed from the Migrator's `packages.lock.json`, the build failed with
`error NU1004: The package references have changed`, and after a restore on the
host wrote it again, the build passed. That is why the commands below restore
first.

**The image runs the published output**, which carries no launch profile and no
`appsettings.Development.json`, so the Migrator in its container comes up in
`Production` and reads only `appsettings.json`. Measured on 2026-10-21.

**`Docs/AcceptedVulnerabilities.md` is copied in from a second build context.**
Step 3's `Directory.Build.props` reads `../Docs/AcceptedVulnerabilities.md` on
every restore and fails without it, and the build context is `Code/`, which cannot
reach `Docs/`. `compose.yml` below gives the build `Docs/` as a second context
named `docs`, and the Dockerfile copies the one file from it into the place
`Directory.Build.props` looks. This keeps `Code/.dockerignore` as it is. Measured
on 2026-10-21.

**`COPY . .` copies the whole of `Code/` before the restore**, so any change
under `Code/` runs the restore again on the next build. Measured: 33 seconds to
rebuild after a migration was added. Not measured: copying the project files
first, so that the restore is cached.

Write `compose.yml`:

```yaml
services:
  certificates:
    image: <Versions.md: PostgreSQL, the local database>
    user: root
    entrypoint: ["sh", "/certificates.sh"]
    configs:
      - source: certificates
        target: /certificates.sh
    volumes:
      - authority:/authority
      - ./Code/<Solution>.slnx:/solution.slnx:ro
      - ./Code/Certificates:/output
      - certificates:/certificates

  database:
    image: <Versions.md: PostgreSQL, the local database>
    restart: unless-stopped
    depends_on:
      certificates:
        condition: service_completed_successfully
    environment:
      POSTGRES_PASSWORD_FILE: /authority/superuser.password
      POSTGRES_INITDB_ARGS: --encoding=UTF8 --locale=C
    command:
      - -c
      - ssl=on
      - -c
      - ssl_ca_file=/authority/ca.crt
      - -c
      - ssl_cert_file=/authority/server.crt
      - -c
      - ssl_key_file=/authority/server.key
      - -c
      - hba_file=/authority/pg_hba.conf
    ports:
      - 127.0.0.1:<Port>:5432
    volumes:
      - authority:/authority:ro
      - data:/var/lib/postgresql
      - socket:/var/run/postgresql
    healthcheck:
      test: ["CMD", "pg_isready", "--host=/var/run/postgresql"]
      interval: 2s
      timeout: 5s
      retries: 30

  roles:
    image: <Versions.md: PostgreSQL, the local database>
    user: postgres
    depends_on:
      database:
        condition: service_healthy
    entrypoint: ["sh", "/roles.sh"]
    configs:
      - source: roles
        target: /roles.sh
    volumes:
      - ./Code/<Solution>.slnx:/solution.slnx:ro
      - socket:/var/run/postgresql

  migrator:
    build:
      context: ./Code
      dockerfile: <Solution>.Migrator/Dockerfile
      additional_contexts:
        docs: ./Docs
    init: true
    depends_on:
      roles:
        condition: service_completed_successfully
    environment:
      <Solution>__Database__ConnectionString: Host=database;Port=5432;Database=<DatabaseName>;Username=<Solution>MigratorRole;SSL Mode=VerifyFull
    volumes:
      - type: volume
        source: certificates
        target: /certificates
        read_only: true
        volume:
          subpath: <Solution>MigratorRole

configs:
  certificates:
    content: |
      set -eu
      solution=<Solution>
      cd /authority
      if [ ! -f ca.key ]; then
        openssl req -new -x509 -nodes -newkey rsa:4096 -days 3650 \
          -subj "/CN=Local Database Authority" -keyout ca.key -out ca.crt \
          -addext "basicConstraints=critical,CA:TRUE" \
          -addext "keyUsage=critical,keyCertSign,cRLSign" 2>/dev/null
        openssl req -new -nodes -newkey rsa:2048 -subj "/CN=Local Database Server" \
          -keyout server.key -out server.csr 2>/dev/null
        printf '%s\n' "subjectAltName=DNS:localhost,DNS:database,IP:127.0.0.1,IP:::1" \
          "basicConstraints=critical,CA:FALSE" \
          "keyUsage=critical,digitalSignature,keyEncipherment" \
          "extendedKeyUsage=serverAuth" > server.ext
        openssl x509 -req -in server.csr -CA ca.crt -CAkey ca.key -CAcreateserial \
          -days 3650 -extfile server.ext -out server.crt 2>/dev/null
        rm server.csr server.ext
        openssl rand -hex 32 > superuser.password
        printf '%s\n' "local all postgres peer" \
          "hostssl all all 0.0.0.0/0 cert clientcert=verify-full" \
          "hostssl all all ::/0 cert clientcert=verify-full" > pg_hba.conf
        chown 999:999 ca.crt server.crt server.key superuser.password pg_hba.conf
        chmod 600 ca.key server.key superuser.password
        echo "Created the database authority"
      fi
      owner=$$(stat -c %u:%g /output)
      cp ca.crt /output/ca.crt
      chown "$$owner" /output/ca.crt
      for role in "$${solution}MigratorRole" $$(sed -n "s|.*Path=\"$${solution}\.\([A-Z][A-Za-z]*Api\)/.*|$${solution}\1Role|p" /solution.slnx); do
        certificate=/output/$$role.crt
        key=/output/$$role.key
        if [ -f "$$certificate" ] && [ -f "$$key" ] \
          && openssl verify -CAfile ca.crt "$$certificate" >/dev/null 2>&1 \
          && [ "$$(openssl x509 -in "$$certificate" -noout -pubkey)" = "$$(openssl pkey -in "$$key" -pubout 2>/dev/null)" ] \
          && [ "$$(openssl x509 -in "$$certificate" -noout -subject -nameopt multiline | sed -n 's/^ *commonName *= *//p')" = "$$role" ] \
          && openssl x509 -in "$$certificate" -noout -checkend 2592000 >/dev/null; then
          :
        else
        openssl req -new -nodes -newkey rsa:2048 -subj "/CN=$$role" \
          -keyout "$$key" -out /tmp/role.csr 2>/dev/null
        openssl x509 -req -in /tmp/role.csr -CA ca.crt -CAkey ca.key -CAcreateserial \
          -days 365 -out "$$certificate" 2>/dev/null
        rm /tmp/role.csr
        chmod 600 "$$key"
        chown "$$owner" "$$certificate" "$$key"
        echo "Issued the certificate for $$role"
        fi
        folder=/certificates/$$role
        mkdir -p "$$folder"
        cp ca.crt "$$certificate" "$$key" "$$folder/"
        chown -R 1654:1654 "$$folder"
        chmod 700 "$$folder"
        chmod 600 "$$folder/$$role.key"
      done
      for api in $$(sed -n "s|.*Path=\"$${solution}\.\([A-Z][A-Za-z]*Api\)/.*|\1|p" /solution.slnx); do
        folder=/certificates/$${solution}$${api}Role
        certificate=$$folder/server.crt
        key=$$folder/server.key
        if [ -f "$$certificate" ] && [ -f "$$key" ] \
          && openssl verify -CAfile ca.crt "$$certificate" >/dev/null 2>&1 \
          && [ "$$(openssl x509 -in "$$certificate" -noout -pubkey)" = "$$(openssl pkey -in "$$key" -pubout 2>/dev/null)" ] \
          && [ "$$(openssl x509 -in "$$certificate" -noout -ext subjectAltName | tail -n 1 | tr -d ' ')" = "DNS:$$api,DNS:localhost,IPAddress:127.0.0.1,IPAddress:0:0:0:0:0:0:0:1" ] \
          && openssl x509 -in "$$certificate" -noout -checkend 2592000 >/dev/null; then
          continue
        fi
        openssl req -new -nodes -newkey rsa:2048 -subj "/CN=$$api" \
          -keyout "$$key" -out /tmp/server.csr 2>/dev/null
        printf '%s\n' "subjectAltName=DNS:$$api,DNS:localhost,IP:127.0.0.1,IP:::1" \
          "basicConstraints=critical,CA:FALSE" \
          "keyUsage=critical,digitalSignature,keyEncipherment" \
          "extendedKeyUsage=serverAuth" > /tmp/server.ext
        openssl x509 -req -in /tmp/server.csr -CA ca.crt -CAkey ca.key -CAcreateserial \
          -days 365 -extfile /tmp/server.ext -out "$$certificate" 2>/dev/null
        rm /tmp/server.csr /tmp/server.ext
        chown 1654:1654 "$$certificate" "$$key"
        chmod 600 "$$key"
        echo "Issued the server certificate for $$api"
      done

  roles:
    content: |
      set -eu
      solution=<Solution>
      database=<DatabaseName>
      migrator="$${solution}MigratorRole"
      for role in "$$migrator" $$(sed -n "s|.*Path=\"$${solution}\.\([A-Z][A-Za-z]*Api\)/.*|$${solution}\1Role|p" /solution.slnx); do
        if [ -z "$$(psql -d postgres -Atc "SELECT 1 FROM pg_roles WHERE rolname = '$$role'")" ]; then
          psql -d postgres -qc "CREATE ROLE \"$$role\" WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE"
          echo "Created the role $$role"
        fi
      done
      if [ -z "$$(psql -d postgres -Atc "SELECT 1 FROM pg_database WHERE datname = '$$database'")" ]; then
        psql -d postgres -qc "CREATE DATABASE \"$$database\" OWNER \"$$migrator\""
        echo "Created the database $$database"
      fi

volumes:
  authority:
  certificates:
  data:
  socket:
```

**Four services, and three of them run once and stop.** `docker compose up -d`
runs `certificates`, then starts `database` once `certificates` has finished with
0, then runs `roles` once `database` reports healthy, then runs `migrator` once
`roles` has finished with 0. Each API that `create-project` adds is one more
service, started once `migrator` has finished with 0. Each is described below.

**`certificates` makes the authority once, and a role's pair whenever the pair is
not right.** On the first run it creates the authority, the server's certificate,
the server's `pg_hba.conf` and the superuser's password, all inside the
`authority` volume. **The authority's private key never leaves that volume**: it
is not in the repository and not on the host's disk outside Docker. Then, on
every run, it copies the authority's certificate to `Code/Certificates/ca.crt`
and checks each role's pair. It issues a new pair where either file is missing,
where the authority did not sign the certificate, where the key is not the
certificate's own, where the Common Name is not the role, or where the
certificate expires within 30 days. A pair that passes all five is left alone.
**So the same command that sets up a new machine also renews a certificate before
it expires, and repairs a pair left behind by a lost volume.** Measured: with the
volumes removed and the files kept, the next run made a new authority and issued
every role a new pair, because the old ones failed the first check.

**Then it copies each role's three files into that role's own folder in the
`certificates` volume**: `ca.crt`, the role's certificate and its key, in
`/certificates/<Role>/`, the folder owned by `1654`, mode 700, and the key mode
600. **Ruled by Francois on 2026-10-21**: each container's certificates come from
a Docker volume, one folder per role, owned by the container's user and readable
only by it, and each container mounts only its own role's folder, so no container
sees another role's key. `Code/Certificates/` stays as it is, for `dotnet run`
and the tests. `1654` is the user every chiseled .NET image runs as. Measured: a
container running as `999` or as `1000` could not read a key in the volume, and
the Migrator and an API each read their own. **Because every chiseled host runs as
`1654`, the file's owner does not keep one host from another's key**; what does is
that each host mounts only its own folder. **The copy runs on every run, whether
the pair was issued or not**, so a pair reissued in `Code/Certificates/` reaches
the volume on the same run. Measured on 2026-10-21: with the Migrator's two files
deleted, `docker compose run --rm certificates` issued a new pair, the key in the
volume then matched the new one, and the Migrator connected with it.

**Then, for each API in the solution file, it issues the API's server pair** into
that API's role's folder, as `server.crt` and `server.key`, where the pair is not
right. The checks are a role's, except that the Common Name check is replaced by
one that the certificate names exactly `<Name>Api`, `localhost`, `127.0.0.1` and
`::1`, in that order, so a pair whose names differ in any way is issued again.
**Ruled by Francois on 2026-10-21**: an API's container serves HTTPS only, with a
server certificate the local authority issues, as `Security.md` section 11 has
every connection encrypted; the pair is `server.crt` and `server.key` in the
API's role's folder; and the certificate names the API's service, `<Name>Api`,
with `localhost`, `127.0.0.1` and `::1`. `<Name>Api` is taken from the solution
file as it is, in the PascalCase `BackendCode.md` section 3 names an API with.
**On day one the solution has no API, so this loop issues nothing.** Measured on
2026-10-21 with no API in the solution file: `certificates` printed no server
certificate line, and the volume held one folder, `<Solution>MigratorRole`,
holding `ca.crt` and the role's pair. Measured on 2026-10-21 with one API,
`OrdersApi`: it printed `Issued the server certificate for OrdersApi`; `curl
--cacert Code/Certificates/ca.crt` to `https://localhost:<host port>/v1/health`
and to `https://127.0.0.1:<host port>/v1/health` both returned 200; a call from
another container to `https://OrdersApi:8443/v1/health` returned 200, and the
same call checking the name `OrdersApiX` failed with "hostname mismatch"; and the
next run issued nothing. **The API's server pair is only in the volume**, and not
in `Code/Certificates/`, because `dotnet run` of an API on the host uses the .NET
development certificate.

**The authority and the server's certificate are the ones `BackendCode.md`
section 14 requires a connection to check.** The authority is valid for ten
years, a role's certificate for one, and each role's Common Name is the role's
name exactly, which is how the server maps the certificate to the role. **The
server's certificate names `localhost`, `127.0.0.1` and `::1` for a host on this
machine, and `database` for a host running inside the same Docker network**, so
`SSL Mode=VerifyFull` passes for both.

**The files it writes are given the owner of `Code/Certificates/`.** The service
runs as `root` so that it can give the server's own files to the server's user,
999 in the image, and so the files it writes into `Code/Certificates/` are given
the folder's owner, which is whoever cloned the repository. Measured on macOS
with Docker Desktop: the files belonged to the engineer and the key was mode 600.
On Linux, Docker writes to a mounted folder as the user in the container, so
without that step the files would belong to `root` and no host could read its
key. This was not measured on Linux.

**The roles are derived from `Code/<Solution>.slnx`**, by `BackendCode.md`
section 18's rule: the Migrator's role, and one role for each project whose path
starts with `<Solution>.<Name>Api/`. `<Solution>.ApiCommon` does not match,
because its path does not end in `Api/`. **So `create-project` changes neither
script when it adds an API**: the API's line in the solution file is what gives
it a role, its certificate and its server pair on the next run. It adds the API's
own service to this file, which `create-project` states.

**`database` is the server.** It is the image named in `Versions.md`, so the
local database is the same PostgreSQL the integration tests use. Its settings
come from the command line and point at the `authority` volume, so the data
directory holds nothing this file did not decide.

- **It listens on `127.0.0.1:<Port>` and on nothing else outside Docker.** That
  is what keeps it to this machine.
- **`pg_hba.conf` lets the superuser sign in only over the server's own socket,
  as the operating system user `postgres`, and every other sign-in only over TLS
  with a client certificate**, checked in full. Measured: a role signed in from
  the host with its certificate and `verify-full`; the superuser over TCP with
  no certificate was refused with "connection requires a valid client
  certificate"; and a connection with no TLS was refused with "no pg_hba.conf
  entry".
- **The TLS rules accept every address**, because a connection from the host
  reaches the container from the address of Docker's own network and not from
  `127.0.0.1`. Measured: the refused connection above was reported as coming from
  `172.19.0.1`. The published address is what limits who can connect.
- **The superuser's password exists only because the image refuses to set up a
  data directory without one.** It is random, it stays in the `authority` volume,
  and nothing can sign in with it, because `pg_hba.conf` offers the superuser no
  password sign-in.
- **`restart: unless-stopped` brings the server back** whenever Docker starts,
  until someone stops it.
- **The health check reads the server's own socket**, so `roles` starts only
  once the server accepts connections.

**`roles` creates each role and the database, where either is missing.** It
signs in over the socket that `database` shares with it, as the operating system
user `postgres`, which is the only sign-in `pg_hba.conf` gives the superuser. Each
role is created `LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE`, and the database is
created owned by `<Solution>MigratorRole`. **Nothing is granted**: a role's
privileges come from a migration, which `BackendCode.md` section 13 states.

**`migrator` is `<Solution>.Migrator`, built from its Dockerfile and run once.**
Its build context is `Code/`, with `Docs/` as the second context named `docs`
that the Dockerfile copies from. **Its configuration comes from an environment
variable. Ruled by Francois on 2026-10-21**: a host in a container is configured
by environment variables in `compose.yml`, and comes up in `Production`. The
connection string names `Host=database`, the server's name inside Docker's
network, which the server's certificate carries, and port `5432`, the server's
own port inside that network, and not `<Port>`. The rest is the same as the
Migrator's `appsettings.Development.json`.

- **It mounts only its own role's folder, read-only, at `/certificates`**, by
  the volume's `subpath`, which needs Docker Engine 26 or later. That is where
  step 11's `DatabaseDataSourceFactory` looks, since the image holds no `.slnx`.
  Measured on Docker Engine 28.0.1 and Docker Compose 2.33.1, and on nothing
  older.
- **A `subpath` folder that does not exist stops the container from being
  created**: measured, "cannot access path ... no such file or directory". The
  `depends_on` chain is what prevents it, because `certificates` makes every
  role's folder before `database` starts.
- **Measured on 2026-10-21**: in its container, with no `.slnx` in the image, the
  Migrator applied the migrations through this connection string, using
  `/certificates`, and exited 0.
- **`init: true` runs Docker's own small init process as the container's
  process 1, with the host under it. Ruled by Francois on 2026-10-21, for every
  host's service.** A .NET host that fails with an unhandled exception ends
  itself with an abort, and an abort does not end process 1, so without it the
  container would keep running. Measured on 2026-10-21 with an API's image whose
  startup failed before its `try`: without `init: true` the container kept
  running and served nothing; with it, the container exited 134 in 1 second.
  Measured on 2026-10-21, in a full run of this skill with no API: with
  `init: true`, `docker compose run --rm migrator` applied `CreateErrorTables`
  and exited 0, and with a connection string naming a database that does not
  exist, given in a temporary override file, it exited 1 after "The migrations
  could not be applied", as it did without `init: true`.

**`$$` is how Docker Compose writes a `$` the shell is to see.** Compose reads
`${...}` in this file as its own variables, so every shell variable in the two
scripts is written with `$$`. The scripts are in `compose.yml` itself, rather than
in files beside it, so that no checkout can change their line endings.

**Restore, from `Code/`:**

```bash
dotnet restore
```

**Expect exit 0.** This step replaced the Migrator's project file after step
11's restore, and the image's restore runs in locked mode, which fails on a lock
file that does not match, as the Dockerfile's text above records. Measured on
2026-10-21, in this order: this restore rewrote the Migrator's lock file after
this step's replace, adding `Microsoft.Extensions.Hosting`.

**Start the database and run the Migrator, from the repository root:**

```bash
docker compose run --rm migrator
```

**Expect exit 0.** It builds the Migrator's image where it is missing; runs
`certificates`, which writes `Created the database authority` and `Issued the
certificate for <Solution>MigratorRole` to its log; starts the server and waits
until it is healthy; runs `roles`, which writes `Created the role
<Solution>MigratorRole` and `Created the database <DatabaseName>` to its log; and
then runs the Migrator, whose lines this command prints, and returns its exit
code. Then `Code/Certificates/` holds `.gitignore`, `ca.crt`,
`<Solution>MigratorRole.crt` and `<Solution>MigratorRole.key`, and the server
keeps running. Measured on 2026-10-21, with no API: from nothing, with the image
missing and the build cache warm, 6 seconds. The first start on that machine, by
`docker compose up -d --wait` with the two .NET images not yet pulled and the
build cache empty, took 55 seconds, the Migrator's run included.

**`docker compose run --rm migrator` is used, and not `docker compose up -d
--wait`, because on day one no service depends on `migrator`.** Measured on
2026-10-21, with no API: `docker compose up -d --wait` ran every service, the
Migrator applied the migration and exited 0, and the command still exited 1,
printing `container <project>-migrator-1 exited (0)`, because `--wait` counts a
one-off service that nothing depends on as failed once it stops. With one API
beside the Migrator, it exited 0, because the API depends on `migrator`, which
is why `create-project` can use it. `docker compose run --rm migrator` starts
`certificates`, `database` and `roles` first, in the order their `depends_on`
gives, and returns the Migrator's own exit code: measured, 0 when it applied the
migration, and 1, after "The migrations could not be applied", with a
connection string naming a database that does not exist. **A person who clones
the repository runs `docker compose up -d`**, which does the same without
waiting, and keeps the Migrator's container for `docker compose logs`. Measured
on 2026-10-21, with no API: a plain `up -d` exited 0, the Migrator exited 0, and
a plain `up -d` builds an image that is missing.

**Anything other than exit 0 is a real failure: stop, report what it said, and
show what each other service did with
`docker compose logs certificates roles database`.** The one to expect
is `port is already allocated`, where something took `<Port>` after this skill
checked it.

**Expect the schema created.** `BackendCode.md` section 13 states that zero
means the schema is up to date. The `migrator` service applies
`CreateErrorTables`, so the database then holds the `Error` schema with
`ErrorStatus` carrying its nine rows and `ErrorMessage` empty, and the
`Migration` schema with `MigrationHistory` carrying one row. **The command prints
three of the Migrator's lines**: the startup line; EF's
failed read of `MigrationHistory` at `Error`, because that table does not exist
until this run creates it; and "Applying migration" naming `CreateErrorTables`.
The exit code, not that `Error` line, says whether the run succeeded. Measured,
on the host and in the container.

**Run the Migrator from the host**, from the repository root:

```bash
dotnet run --project Code/<Solution>.Migrator
```

**Expect exit 0, and the line "No migrations were applied. The database is
already up to date."** The container has applied the migration already. Measured
on 2026-10-21: this run walked up to the `.slnx`, used `Code/Certificates/`,
connected to `localhost:<Port>` and printed that line.

**This run is what accepts the database's name, the port and the Migrator's
role from the host, and it is the only thing that can.** The local database was
created from the same values, but nothing earlier in this document connects to it
from outside Docker: the `migrator` service connects from inside Docker's
network, to port 5432 and through its own environment variable, so it checks
neither `<Port>` nor the Migrator's `appsettings.Development.json`; and the
design-time factory needs neither a connection nor a
certificate, which was measured, and `migrations add` goes through it. So a
connection string that names another database, port or role, a certificate that
does not match its role, a certificate that is missing, and a connection string
carrying a password or the wrong `SSL Mode` each fail here and nowhere before. **Each one
fails loudly and names its own cause**, which was measured for each in turn, so the failure is read rather than guessed at.

**Anything other than exit 0 is a real failure: stop, report what it said, and
fix the value it names rather than carrying on.** The database's name and the
port are the two values this skill asked for that only this run checks, and a
wrong one is corrected by running this skill again with the right value, not by
editing around it.

**Nothing in this skill applies a migration twice to the solution's database.**
Every later `docker compose up -d` and `docker compose run --rm migrator` runs the
Migrator again, and the run from the host above runs it once more, and each
applies nothing that is already applied, because `MigrateAsync` applies only what
the history table says is missing, which `BackendCode.md` section 13's own
pending-migrations rule rests on. Measured on 2026-10-21, with no API: a plain
`docker compose up -d` after the Migrator had run exited 0 in 2 seconds, the
server kept its container, and the Migrator logged "No migrations were applied".

### 13. `<Solution>.UnitTests`

`Testing.md` section 1 states that the solution holds two C# test projects, and
`<Solution>.UnitTests` holds the C# unit tests. `Testing.md` section 3 states what
it may reference and what it tests. This skill creates both test projects because
every subject they need is already written, by steps 7, 10 and 11, and
`create-project` runs once per API, so it would create them with the first API
and never again.

Write `Code/global.json`:

```json
{
  "sdk": { "version": "<Versions.md: .NET SDK>", "rollForward": "disable" },
  "test": { "runner": "Microsoft.Testing.Platform" }
}
```

**Without this file `dotnet test` does not run a single test.** `Testing.md`
section 3 puts the tests on xUnit v3 and Microsoft.Testing.Platform. Measured against SDK 10.0.302: with no
setting, `dotnet test` exited 1 with "Testing with VSTest target is no longer
supported by Microsoft.Testing.Platform on .NET 10 SDK and later", and setting
`TestingPlatformDotnetTestSupport` in the project gave the same error. The file's
`test` setting is the one setting that worked.

**The file's `sdk` setting fixes the .NET SDK, with roll-forward turned off**, as
`Versions.md` section 2, "The rules", states. A `dotnet` command run where the
SDK it names is not installed stops with "A compatible .NET SDK was not found.".

**It is written in `Code/`, and so `dotnet test` is run from `Code/`.**
`BackendCode.md` section 2 has `Code/` hold every file the build reads. The SDK
looks for this file in the folder the command is typed in and in the folders above
it, and not in the solution's own folder. Measured: from `Code/`,
`dotnet test` exited 0, and from the repository root, both
`dotnet test Code/<Solution>.slnx` and `dotnet test` with no argument exited 1.

**Steps 1 to 12 build on the SDK the machine has, and the SDK this file names
holds from this step on.**

Write `Code/<Solution>.UnitTests/<Solution>.UnitTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../<Solution>.Domain/<Solution>.Domain.csproj" />
    <ProjectReference Include="../<Solution>.ApiCommon/<Solution>.ApiCommon.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Shouldly" />
  </ItemGroup>

</Project>
```

`Testing.md` section 3 states that `<Solution>.UnitTests` references only
`<Solution>.Domain` and `<Solution>.ApiCommon`, and no package other than the
libraries that section names and the analyzer packages `BackendCode.md` section 21
names.

**`OutputType` is `Exe`, and the build fails without it.** Measured: "xUnit.net v3 test projects must be executable (set project property
'<OutputType>Exe</OutputType>')".

**Two packages, and that is the whole runner.** `xunit.v3` brings the
Microsoft.Testing.Platform runner itself, so `Microsoft.NET.Test.Sdk`,
`xunit.runner.visualstudio` and a direct Microsoft.Testing.Platform reference are
not needed, which was measured.

**`Microsoft.Extensions.TimeProvider.Testing` is not referenced yet**, although
`Testing.md` section 3 names `FakeTimeProvider` for time. No test here reads time,
and `BackendCode.md` section 20 refuses a package nothing uses. The first unit test
that needs a `FakeTimeProvider` adds it.

**Replace `Code/Directory.Packages.props` whole, adding two `PackageVersion`
entries at the end of the `ItemGroup` step 4, "`Directory.Packages.props`",
wrote**, which then holds fifteen:

```xml
<Project>

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="<Versions.md: Microsoft.EntityFrameworkCore>" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="<Versions.md: Microsoft.EntityFrameworkCore.Relational>" />
    <PackageVersion Include="FluentValidation" Version="<Versions.md: FluentValidation>" />
    <PackageVersion Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="<Versions.md: Microsoft.Extensions.Options.ConfigurationExtensions>" />
    <PackageVersion Include="Microsoft.Extensions.Options.DataAnnotations" Version="<Versions.md: Microsoft.Extensions.Options.DataAnnotations>" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="<Versions.md: Microsoft.AspNetCore.OpenApi>" />
    <PackageVersion Include="Scalar.AspNetCore" Version="<Versions.md: Scalar.AspNetCore>" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="<Versions.md: Npgsql.EntityFrameworkCore.PostgreSQL>" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="<Versions.md: Microsoft.EntityFrameworkCore.Design>" />
    <PackageVersion Include="Serilog.Extensions.Hosting" Version="<Versions.md: Serilog.Extensions.Hosting>" />
    <PackageVersion Include="Serilog.Settings.Configuration" Version="<Versions.md: Serilog.Settings.Configuration>" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="<Versions.md: Serilog.Sinks.Console>" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="<Versions.md: Microsoft.Extensions.Hosting>" />
    <PackageVersion Include="xunit.v3" Version="<Versions.md: xunit.v3>" />
    <PackageVersion Include="Shouldly" Version="<Versions.md: Shouldly>" />
  </ItemGroup>

</Project>
```

**No existing entry changes**, for the reason step 10, "`<Solution>.ApiCommon`",
gives.

**Replace `Code/<Solution>.slnx` whole, adding `<Solution>.UnitTests`:**

```xml
<Solution>
  <Project Path="<Solution>.Domain/<Solution>.Domain.csproj" />
  <Project Path="<Solution>.Infrastructure/<Solution>.Infrastructure.csproj" />
  <Project Path="<Solution>.ApiCommon/<Solution>.ApiCommon.csproj" />
  <Project Path="<Solution>.Migrator/<Solution>.Migrator.csproj" />
  <Project Path="<Solution>.UnitTests/<Solution>.UnitTests.csproj" />
</Solution>
```

The opening and closing `<Solution>` tags are the file format's own root element
and are not substituted, which step 1 states in full.

Write
`Code/<Solution>.UnitTests/Managers/ErrorMessage/ErrorMessageManagerTests.cs`:

```csharp
using <Solution>.ApiCommon.Managers.ErrorMessage;
using <Solution>.Domain.Error;
using Shouldly;
using Xunit;

namespace <Solution>.UnitTests.Managers.ErrorMessage;

public sealed class ErrorMessageManagerTests
{
    [Fact]
    public void GetStatusByStatusCode_StatusPopulated_ReturnsStatus()
    {
        var status = new ErrorStatusEntity(404, "NotFound", "Not Found", "That record could not be found.");
        var manager = new ErrorMessageManager();
        manager.Populate([status], []);

        var result = manager.GetStatusByStatusCode(404);

        result.ShouldBeSameAs(status);
    }

    [Fact]
    public void GetStatusByStatusCode_StatusNotPopulated_ThrowsInvalidOperationException()
    {
        var manager = new ErrorMessageManager();
        manager.Populate([], []);

        var lookup = () => manager.GetStatusByStatusCode(404);

        lookup.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void GetMessageByCode_MessagePopulated_ReturnsMessage()
    {
        var message = new ErrorMessageEntity("NotFound", "That record could not be found.");
        var manager = new ErrorMessageManager();
        manager.Populate([], [message]);

        var result = manager.GetMessageByCode("NotFound");

        result.ShouldBeSameAs(message);
    }

    [Fact]
    public void GetMessageByCode_MessageNotPopulated_ReturnsNull()
    {
        var manager = new ErrorMessageManager();
        manager.Populate([], []);

        var result = manager.GetMessageByCode("NotFound");

        result.ShouldBeNull();
    }
}
```

**Four tests, because `ErrorMessageManager` makes three computations.**
`Testing.md` section 3 requires a unit test of every computation a manager method
makes. `GetStatusByStatusCode` returns the row or throws where none exists, and
`GetMessageByCode` returns the row or `null` where none exists, so each has two
outcomes. `Populate` returns nothing of its own, so it is proved through the other
two, which find only what it put there.

`Testing.md` section 1's tree places the file, mirroring
`<Solution>.ApiCommon`'s own `Managers/ErrorMessage/`, and `Testing.md` section 3
names the class and each method's three segments. The data is written inline and
is synthetic, and every assertion is Shouldly, which `Testing.md` section 2
states.

**Every test file imports `Xunit`.** xUnit v3 4.0.1 adds no implicit import of it,
and a test file without the import failed `CS0246` on `Fact`, measured.

### 14. `<Solution>.IntegrationTests`

`<Solution>.IntegrationTests` holds every other C# test `Testing.md` names, which
`Testing.md` section 1 states. This step writes the database fixture and the four
tests `Testing.md` section 5 says cover the whole solution. Each API later adds its
own folder to this project, with that API's own fixture built on this one, and
`create-project` writes both.

Write `Code/<Solution>.IntegrationTests/<Solution>.IntegrationTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../<Solution>.Infrastructure/<Solution>.Infrastructure.csproj" />
    <ProjectReference Include="../<Solution>.ApiCommon/<Solution>.ApiCommon.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="Testcontainers.PostgreSql" />
  </ItemGroup>

</Project>
```

**Two project references, and both are derived.** The tests use
`<DatabaseName>Context`, its design-time factory and the migrations, which
`<Solution>.Infrastructure` holds, and `ErrorMessageCode`, which
`<Solution>.ApiCommon` holds. `BackendCode.md` section 3's reference graph forbids
neither. An API project joins them when `create-project` writes the first one.

**Npgsql needs no reference of its own.** It reaches this project through
`<Solution>.Infrastructure`, which was measured.

**Replace `Code/Directory.Packages.props` whole, adding one `PackageVersion`
entry at the end of the same `ItemGroup`.** `xunit.v3` and `Shouldly` already have
theirs, from step 13, "`<Solution>.UnitTests`". The file holds sixteen
entries:

```xml
<Project>

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="<Versions.md: Microsoft.EntityFrameworkCore>" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="<Versions.md: Microsoft.EntityFrameworkCore.Relational>" />
    <PackageVersion Include="FluentValidation" Version="<Versions.md: FluentValidation>" />
    <PackageVersion Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="<Versions.md: Microsoft.Extensions.Options.ConfigurationExtensions>" />
    <PackageVersion Include="Microsoft.Extensions.Options.DataAnnotations" Version="<Versions.md: Microsoft.Extensions.Options.DataAnnotations>" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="<Versions.md: Microsoft.AspNetCore.OpenApi>" />
    <PackageVersion Include="Scalar.AspNetCore" Version="<Versions.md: Scalar.AspNetCore>" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="<Versions.md: Npgsql.EntityFrameworkCore.PostgreSQL>" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="<Versions.md: Microsoft.EntityFrameworkCore.Design>" />
    <PackageVersion Include="Serilog.Extensions.Hosting" Version="<Versions.md: Serilog.Extensions.Hosting>" />
    <PackageVersion Include="Serilog.Settings.Configuration" Version="<Versions.md: Serilog.Settings.Configuration>" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="<Versions.md: Serilog.Sinks.Console>" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="<Versions.md: Microsoft.Extensions.Hosting>" />
    <PackageVersion Include="xunit.v3" Version="<Versions.md: xunit.v3>" />
    <PackageVersion Include="Shouldly" Version="<Versions.md: Shouldly>" />
    <PackageVersion Include="Testcontainers.PostgreSql" Version="<Versions.md: Testcontainers.PostgreSql>" />
  </ItemGroup>

</Project>
```

**No existing entry changes**, for the reason step 10, "`<Solution>.ApiCommon`",
gives. The block shows what
five earlier steps and this one leave there, and nothing in it is written twice.

**Replace `Code/<Solution>.slnx` whole, adding `<Solution>.IntegrationTests`:**

```xml
<Solution>
  <Project Path="<Solution>.Domain/<Solution>.Domain.csproj" />
  <Project Path="<Solution>.Infrastructure/<Solution>.Infrastructure.csproj" />
  <Project Path="<Solution>.ApiCommon/<Solution>.ApiCommon.csproj" />
  <Project Path="<Solution>.Migrator/<Solution>.Migrator.csproj" />
  <Project Path="<Solution>.UnitTests/<Solution>.UnitTests.csproj" />
  <Project Path="<Solution>.IntegrationTests/<Solution>.IntegrationTests.csproj" />
</Solution>
```

#### The database fixture

`Testing.md` section 5 states what the fixture does. It uses a real PostgreSQL
through Testcontainers and never an in-memory provider. It starts one container
for the run, with container reuse off. It runs the migrations once into a template
database. Each test class clones its own database from the template and drops it
with `DROP DATABASE ... WITH (FORCE)` when the class finishes. The migration test
is the exception, and uses an empty database of its own. The database naming test
uses no database. **Two classes do it**, because xUnit v3 holds one object for the
whole run and one for each class,
and the container and the template live once while a clone lives once per class.

**Both sit at the root of `<Solution>.IntegrationTests`.** `Testing.md` section 1's
tree places every test folder and places no folder for a fixture, and
`BackendCode.md` section 20 creates no folder that nothing places. Each API's own
fixture, which `create-project` writes, uses them both.

**Their names are derived.** `Testing.md` calls this thing a fixture, which gives
the second half of each name, as `BackendCode.md` section 18 takes a document's own
word first. The first half is what each one holds, in `Testing.md` section 5's own
words: the template database, and the class's cloned database.

Write `Code/<Solution>.IntegrationTests/TemplateDatabaseFixture.cs`:

```csharp
using <Solution>.Infrastructure.Persistence;
using <Solution>.IntegrationTests;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

[assembly: AssemblyFixture(typeof(TemplateDatabaseFixture))]

namespace <Solution>.IntegrationTests;

public sealed class TemplateDatabaseFixture : IAsyncLifetime
{
    private const string TemplateDatabaseName = "<DatabaseName>Template";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("<Versions.md: PostgreSQL, the integration tests' container>").Build();

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync(TestContext.Current.CancellationToken);

        var connectionString = GetConnectionString(TemplateDatabaseName);
        connectionString.Pooling = false;

        await using var database = GetContext(connectionString);

        await database.Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync() => await container.DisposeAsync();

    public <DatabaseName>Context GetContext(string databaseName) =>
        GetContext(GetConnectionString(databaseName));

    public Task CloneTemplateAsync(string databaseName) =>
        ExecuteAsync($"CREATE DATABASE \"{databaseName}\" TEMPLATE \"{TemplateDatabaseName}\"");

    public Task DropDatabaseAsync(string databaseName) =>
        ExecuteAsync($"DROP DATABASE \"{databaseName}\" WITH (FORCE)");

    private static <DatabaseName>Context GetContext(NpgsqlConnectionStringBuilder connectionString)
    {
        var database = new <DatabaseName>ContextDesignTimeFactory().CreateDbContext([]);

        database.Database.SetConnectionString(connectionString.ConnectionString);
        database.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        return database;
    }

    private NpgsqlConnectionStringBuilder GetConnectionString(string databaseName) =>
        new(container.GetConnectionString()) { Database = databaseName };

    private async Task ExecuteAsync(string statement)
    {
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = new NpgsqlCommand(statement, connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
```

**An assembly fixture holds the one container, and there is no collection
definition.** Measured: the assembly fixture made one container
for the run and let the test classes run side by side, while a collection fixture
also made one but ran the classes one after another, and a class that forgot its
`[Collection]` attribute failed only when it ran. The assembly fixture has no
attribute on each class to forget. **Container reuse stays off because nothing
turns it on**, which was measured, and `DisposeAsync` removes the
container when the run ends.

**The image is named by its exact tag**, as `Versions.md` section 2, "The rules",
states. `Testing.md` section 5 names no version, and `BackendCode.md` section 2's
baseline names PostgreSQL 18. The container is reached
with the username and password Testcontainers gives it and with no TLS, which
`BackendCode.md` section 14's rules on passwords and `SSL Mode` do not reach, by
Francois's ruling of 2026-10-07 that those rules apply to an environment's database
and not to a test database destroyed after use. Nothing here goes through
`DatabaseValidateOptions`, because no context is built through
`Add<Solution>Persistence`.

**The template is named `<DatabaseName>Template`, and `MigrateAsync` creates it.**
Measured: `MigrateAsync` against a connection string naming a
database that does not exist created it and then applied every migration, so
nothing sends `CREATE DATABASE` for the template.

**The template is migrated with `Pooling=false`, and that is required.**
`Testing.md` section 5 states it and quotes the error, and a measurement reproduced the error: with pooling on, a pooled connection the migration had
disposed still held the template open, and the clone failed with
`55006: source database "<template>" is being accessed by other users`. With
pooling off, every clone succeeded.

**A clone and a drop are each an `NpgsqlCommand`, with every name in double
quotes, sent from a connection to the container's own `postgres` database.**
PostgreSQL takes no parameter in place of a database's name. Measured: EF's interpolated form, which `BackendCode.md` section 21 has
written in place of its banned `Raw` methods, turns each value into a parameter,
and both statements failed, so that form cannot do this job. A name without double
quotes is folded to lowercase and is then not found. The connection string the
container gives names its `postgres` database, and it was measured that
only another session on the template blocks a clone.

**Every context the fixture hands out is made through
`<DatabaseName>ContextDesignTimeFactory`, given its connection string afterwards,
and set to `NoTracking`.** Measured: `SetMigrationHistoryTable`
is `internal` to `<Solution>.Infrastructure`, and a context made by that factory
carried the setting, so the template's history went to
`Migration.MigrationHistory`. `BackendCode.md` section 10 sets
`QueryTrackingBehavior.NoTracking` on the context, and it was measured that
the factory's context tracks unless told not to, so a test reading a change back
through `database` would be answered from memory.

**Every async call takes `TestContext.Current.CancellationToken`**, which
`Testing.md` section 2 states for C# test code. `DisposeAsync`, `InitializeAsync`
and their `ValueTask` are xUnit's own signatures, which `BackendCode.md` section 16
does not reach.

**What the template does not hold yet.** An API's grant migration names that API's
role, and the container holds no role. It was measured that the template
then fails to build, and that creating the role in the container before the
migrations run fixes it. `create-project` adds that step to `InitializeAsync`
when it writes the first grant migration.

Write `Code/<Solution>.IntegrationTests/ClonedDatabaseFixture.cs`:

```csharp
using <Solution>.Infrastructure.Persistence;
using Xunit;

namespace <Solution>.IntegrationTests;

public sealed class ClonedDatabaseFixture : IAsyncLifetime
{
    private readonly TemplateDatabaseFixture templateDatabase;

    private readonly string databaseName = Guid.CreateVersion7().ToString("N");

    public ClonedDatabaseFixture(TemplateDatabaseFixture templateDatabase)
    {
        this.templateDatabase = templateDatabase;
        Database = templateDatabase.GetContext(databaseName);
    }

    public <DatabaseName>Context Database { get; }

    public async ValueTask InitializeAsync() => await templateDatabase.CloneTemplateAsync(databaseName);

    public async ValueTask DisposeAsync()
    {
        await Database.DisposeAsync();
        await templateDatabase.DropDatabaseAsync(databaseName);
    }
}
```

**A test class takes this fixture through `IClassFixture<ClonedDatabaseFixture>`,
and holds its `Database` in a field named `database`**, which `Testing.md`
section 5 states. xUnit gives the fixture the assembly fixture through its
constructor, which was measured.

**Each clone is named with a new `Guid`, in its 32-character form, in double
quotes. Ruled by Francois on 2026-10-07.** Nobody reads the name and the clone is
dropped when its class finishes, so it needs only to be unique, and a `Guid` is
unique every time and 32 bytes long, inside PostgreSQL's 63-byte limit on a name.
Measured: without double quotes it failed with
`42601: trailing junk after numeric literal`, and in double quotes every step
succeeded. **It is the one database name here that is not PascalCase**, which
`BackendCode.md` section 18 otherwise requires of every database identifier.

**The drop runs while the class's own context may still hold a connection**, and
`WITH (FORCE)` ends that connection. Measured: a clone was dropped that way, and
no clone was left at the end of the run.

#### The migration test

Write `Code/<Solution>.IntegrationTests/Database/MigrationIntegrationTests.cs`:

```csharp
using <Solution>.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;
using Xunit;

namespace <Solution>.IntegrationTests.Database;

public sealed class MigrationIntegrationTests : IAsyncDisposable
{
    private const string MigrationTestDatabaseName = "<DatabaseName>MigrationTest";

    private readonly TemplateDatabaseFixture templateDatabase;

    private readonly <DatabaseName>Context database;

    public MigrationIntegrationTests(TemplateDatabaseFixture templateDatabase)
    {
        this.templateDatabase = templateDatabase;
        database = templateDatabase.GetContext(MigrationTestDatabaseName);
    }

    [Fact]
    public async Task Migrations_UpThenDownThenUp_DownLeavesNoTableAndUpAppliesEvery()
    {
        var migrator = database.GetService<IMigrator>();

        await database.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await migrator.MigrateAsync(Migration.InitialDatabase, TestContext.Current.CancellationToken);
        var appliedAfterDown = await database.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        var tablesAfterDown = await GetStoredModelTablesAsync();
        await database.Database.MigrateAsync(TestContext.Current.CancellationToken);
        var appliedAfterUp = await database.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        appliedAfterDown.ShouldBeEmpty();
        tablesAfterDown.ShouldBeEmpty();
        appliedAfterUp.ShouldBe(database.Database.GetMigrations());
    }

    public async ValueTask DisposeAsync()
    {
        await database.DisposeAsync();
        await templateDatabase.DropDatabaseAsync(MigrationTestDatabaseName);
    }

    private async Task<List<string>> GetStoredModelTablesAsync()
    {
        var modelTables = database.Model.GetEntityTypes()
            .Select(entity => $"{entity.GetSchema()}.{entity.GetTableName()}")
            .ToList();

        var storedTables = await database.Database
            .SqlQuery<string>($"SELECT table_schema || '.' || table_name AS \"Value\" FROM information_schema.tables")
            .ToListAsync(TestContext.Current.CancellationToken);

        return storedTables.Intersect(modelTables).ToList();
    }
}
```

`Testing.md` section 5 states the test: every migration applied to an empty
database, then every `Down`, then every migration again. **The empty database is
the test's own, and `MigrateAsync` creates it**, which was measured. It
is not a clone, because a clone already holds every migration, and it is dropped
when the test finishes.

**After every `Down` it asserts that no migration is recorded as applied and that
no table the model maps is left. Ruled by Francois on 2026-10-07.** EF never leaves
an empty database: measured, the history table stays, with no
row, and so does every schema a migration created, because EF's generated `Down`
drops tables and never the schema its `EnsureSchema` created. The second `Up` runs
over both with no error. Every `Down` runs through EF's own migrator, migrating to
`Migration.InitialDatabase`, which was measured.

**The database is named `<DatabaseName>MigrationTest`.** There is one in a run, so
its name needs no part that differs, and `BackendCode.md` section 18's rule
applies: PascalCase, and saying what the database is, with `<DatabaseName>` first
as the template's name has it. It was measured that a name longer than 63
bytes is shortened without an error, which costs nothing for a name used once.

**The tables are read with `SqlQuery`, the interpolated form `BackendCode.md`
section 21 has written in place of the banned `SqlQueryRaw`.** The statement
carries no value, so nothing is turned into a parameter.

**The three steps are the act, and the reads between them keep what each step
left**, so every assertion sits after the act, which `Testing.md` section 2
requires.

#### The database naming test

Write `Code/<Solution>.IntegrationTests/Database/DatabaseNamingIntegrationTests.cs`:

```csharp
using <Solution>.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;
using Xunit;

namespace <Solution>.IntegrationTests.Database;

public sealed class DatabaseNamingIntegrationTests : IAsyncDisposable
{
    private readonly <DatabaseName>Context database = new <DatabaseName>ContextDesignTimeFactory().CreateDbContext([]);

    [Fact]
    public void Tables_EveryTable_IsNamedForItsEntity()
    {
        var offenders = database.Model.GetEntityTypes()
            .Where(entity => entity.GetTableName() != entity.ClrType.Name[..^"Entity".Length])
            .Select(entity => $"{entity.ClrType.Name}: {entity.GetTableName()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Schemas_EveryTable_HasNamedSchemaOtherThanPublic()
    {
        var offenders = database.Model.GetEntityTypes()
            .Where(entity => entity.GetSchema() is null or "public")
            .Select(entity => $"{entity.GetTableName()}: {entity.GetSchema()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void PrimaryKeys_EveryKeyColumn_IsNamedId()
    {
        var offenders = database.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetKeys().Where(key => key.IsPrimaryKey()))
            .SelectMany(key => key.Properties
                .Where(column => column.GetColumnName() != "Id")
                .Select(column => $"{key.DeclaringEntityType.GetTableName()}: {column.GetColumnName()}"))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void PrimaryKeys_EveryKey_FollowsPkPattern()
    {
        var offenders = database.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetKeys().Where(key => key.IsPrimaryKey()))
            .Where(key => key.GetName() != $"PK_{key.DeclaringEntityType.GetTableName()}")
            .Select(key => $"{key.DeclaringEntityType.GetTableName()}: {key.GetName()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void ForeignKeys_EveryKey_FollowsFkPattern()
    {
        var offenders = database.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys())
            .Where(foreignKey => foreignKey.GetConstraintName()
                != $"FK_{foreignKey.DeclaringEntityType.GetTableName()}_{GetColumns(foreignKey.Properties)}")
            .Select(foreignKey => $"{foreignKey.DeclaringEntityType.GetTableName()}: {foreignKey.GetConstraintName()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void ForeignKeys_EveryColumn_IsNamedReferencedTableId()
    {
        var offenders = database.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys())
            .Where(foreignKey => GetColumns(foreignKey.Properties)
                != $"{foreignKey.PrincipalEntityType.GetTableName()}Id")
            .Select(foreignKey => $"{foreignKey.DeclaringEntityType.GetTableName()}: {GetColumns(foreignKey.Properties)}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Indexes_EveryIndex_FollowsIxPattern()
    {
        var offenders = database.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetIndexes().Where(index => !index.IsUnique))
            .Where(index => index.GetDatabaseName()
                != $"IX_{index.DeclaringEntityType.GetTableName()}_{GetColumns(index.Properties)}")
            .Select(index => $"{index.DeclaringEntityType.GetTableName()}: {index.GetDatabaseName()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Indexes_EveryUniqueIndex_FollowsUxPattern()
    {
        var offenders = database.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetIndexes().Where(index => index.IsUnique))
            .Where(index => index.GetDatabaseName()
                != $"UX_{index.DeclaringEntityType.GetTableName()}_{GetColumns(index.Properties)}")
            .Select(index => $"{index.DeclaringEntityType.GetTableName()}: {index.GetDatabaseName()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    public ValueTask DisposeAsync() => database.DisposeAsync();

    private static string GetColumns(IEnumerable<IProperty> properties) =>
        string.Join("_", properties.Select(property => property.GetColumnName()));
}
```

`Testing.md` section 5 states the test: it reads EF's built model, needs no
connection and no cloned database, and fails on a name that breaks
`BackendCode.md` section 18. **The model comes from
`<DatabaseName>ContextDesignTimeFactory` with no connection string at all**, and it was measured that it carries every name this test reads, the same as
EF's design-time model. So this class takes no fixture.

**One `[Fact]` for each failure `Testing.md` section 5 lists, and each asserts that
its set of offenders is empty.** On day one the model has no foreign key and no
index that is not unique, so a `[Theory]` fed one case per key would have no case,
and it was measured on 2026-09-25 that such a `[Theory]` fails with "No data found
for ...". A set that is empty passes, and a set holding an offender fails naming
it. No assertion sits inside a loop, which `Testing.md` section 2 forbids.

**A table is named for its entity's class without the suffix `Entity`**, which
`BackendCode.md` section 18 states. A key or an index covering more than one
column joins the column names with underscores, in order, which the same section
states.

#### The reference table test

Write `Code/<Solution>.IntegrationTests/Database/ReferenceTableIntegrationTests.cs`:

```csharp
using <Solution>.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;
using Xunit;

namespace <Solution>.IntegrationTests.Database;

public sealed class ReferenceTableIntegrationTests : IClassFixture<ClonedDatabaseFixture>
{
    private readonly <DatabaseName>Context database;

    public ReferenceTableIntegrationTests(ClonedDatabaseFixture clonedDatabase) =>
        database = clonedDatabase.Database;

    [Fact]
    public async Task ReferenceTables_EveryEnumMember_HasRow()
    {
        var rows = await GetReferenceRowsAsync();

        var offenders = rows
            .SelectMany(table => Enum.GetValues(GetEnum(table.Key)).Cast<object>()
                .Where(member => !table.Value.Select(row => GetColumn(row, "Id")).Contains(member))
                .Select(member => $"{table.Key.GetTableName()}: {member}"))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReferenceTables_EveryRow_HasEnumMember()
    {
        var rows = await GetReferenceRowsAsync();

        var offenders = rows
            .SelectMany(table => table.Value
                .Where(row => !Enum.IsDefined(GetEnum(table.Key), GetColumn(row, "Id")))
                .Select(row => $"{table.Key.GetTableName()}: {GetColumn(row, "Id")}"))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReferenceTables_EveryRowName_IsEnumMemberName()
    {
        var rows = await GetReferenceRowsAsync();

        var offenders = rows
            .SelectMany(table => table.Value
                .Where(row => (string)GetColumn(row, "Name") != Enum.GetName(GetEnum(table.Key), GetColumn(row, "Id")))
                .Select(row => $"{table.Key.GetTableName()}: {GetColumn(row, "Name")}"))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void ReferenceTables_EveryTable_IsNamedForEnumPlusType()
    {
        var offenders = database.Model.GetEntityTypes()
            .Where(IsReferenceTable)
            .Where(table => table.GetTableName() != $"{GetEnum(table).Name}Type")
            .Select(table => $"{GetEnum(table).Name}: {table.GetTableName()}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    private static bool IsReferenceTable(IEntityType table) =>
        table.FindPrimaryKey() is { Properties: [{ ClrType.IsEnum: true }] };

    // IsReferenceTable has already selected a table whose one key column is its enum.
    private static Type GetEnum(IEntityType table) => table.FindPrimaryKey()!.Properties[0].ClrType;

    // BackendCode.md section 13 gives every reference table an Id and a Name.
    private static object GetColumn(object row, string column) => row.GetType().GetProperty(column)!.GetValue(row)!;

    private async Task<Dictionary<IEntityType, List<object>>> GetReferenceRowsAsync()
    {
        var rows = new Dictionary<IEntityType, List<object>>();

        foreach (var table in database.Model.GetEntityTypes().Where(IsReferenceTable))
        {
            // DbContext.Set<T>() returns a set for every mapped entity and never null.
            var set = (IQueryable<object>)typeof(DbContext)
                .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
                .MakeGenericMethod(table.ClrType)
                .Invoke(database, null)!;

            rows[table] = await set.ToListAsync(TestContext.Current.CancellationToken);
        }

        return rows;
    }
}
```

`Testing.md` section 5 states the test: it reads the stored rows of every
reference table the model maps, reads `Id` and `Name` and ignores every other
column, and fails on the four things that section lists. It reads them from the
class's clone, which holds every row the migrations seed.

**A reference table is a mapped entity whose one key column is an enum.**
`BackendCode.md` section 13 declares a reference entity's key property as the enum
and not as `int`, so the key's type is what tells it apart. Each table's rows are
read through EF, with `DbContext.Set<T>()` made for that entity, and `Id` and
`Name` through each property's own getter, which was measured.

**On day one no reference table exists, and all four facts pass.** Each builds an
empty set. Measured: every fact passed with no reference table,
and one planted reference table carrying all four faults failed all four facts,
one each. The shape is the one `Testing.md` section 2 and the zero-case `[Theory]`
leave standing: one `[Fact]` for each failure, each asserting with Shouldly that
its set of offenders is empty, and the filter that builds the set is the rule's own
selection rather than conditional logic in the test.

**The three `!` operators each carry their reason**, which `BackendCode.md` section
17 requires.

#### The error code test

Write `Code/<Solution>.IntegrationTests/Database/ErrorCodeIntegrationTests.cs`:

```csharp
using System.Reflection;
using <Solution>.ApiCommon.Errors;
using <Solution>.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace <Solution>.IntegrationTests.Database;

public sealed class ErrorCodeIntegrationTests : IClassFixture<ClonedDatabaseFixture>
{
    private static readonly List<string> ErrorMessageCodes = typeof(ErrorMessageCode)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral)
        .Select(field => field.GetRawConstantValue())
        .OfType<string>()
        .ToList();

    private readonly <DatabaseName>Context database;

    public ErrorCodeIntegrationTests(ClonedDatabaseFixture clonedDatabase) =>
        database = clonedDatabase.Database;

    [Fact]
    public async Task ErrorMessageCode_EveryConstant_NamesRow()
    {
        var statusCodes = await database.ErrorStatuses.Select(status => status.Code).ToListAsync(TestContext.Current.CancellationToken);
        var messageCodes = await database.ErrorMessages.Select(message => message.Code).ToListAsync(TestContext.Current.CancellationToken);

        var offenders = ErrorMessageCodes
            .Where(code => !statusCodes.Contains(code) && !messageCodes.Contains(code))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public async Task ErrorMessageCode_EveryConstant_NamesRowInOneTableOnly()
    {
        var statusCodes = await database.ErrorStatuses.Select(status => status.Code).ToListAsync(TestContext.Current.CancellationToken);
        var messageCodes = await database.ErrorMessages.Select(message => message.Code).ToListAsync(TestContext.Current.CancellationToken);

        var offenders = ErrorMessageCodes
            .Where(code => statusCodes.Contains(code) && messageCodes.Contains(code))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public async Task ErrorTables_EveryRow_HasConstant()
    {
        var statusCodes = await database.ErrorStatuses.Select(status => status.Code).ToListAsync(TestContext.Current.CancellationToken);
        var messageCodes = await database.ErrorMessages.Select(message => message.Code).ToListAsync(TestContext.Current.CancellationToken);

        var offenders = statusCodes.Concat(messageCodes)
            .Where(code => !ErrorMessageCodes.Contains(code))
            .ToList();

        offenders.ShouldBeEmpty();
    }
}
```

`Testing.md` section 5 states the test: it reads the stored rows of the status
table and the message table, and fails when a constant names no row, when a
constant names a row in both tables, or when a row in either table has no
constant. `BackendCode.md` section 7 states the rule behind it. **The constants
are the public literal fields of `ErrorMessageCode`**, read once for the class,
and the rows come from the class's clone, which holds the nine `ErrorStatus` rows
step 11 seeds and an empty `ErrorMessage` table.

**On day one all three facts pass**, and each planted fault fails its own fact and
no other, which was measured with the failure message Shouldly prints.
It names the local variable `offenders` first and then each code it holds.

**`dotnet test` is run from `Code/`**, for the reason step 13 gives, and it needs
a running Docker, which the preconditions above check. Measured:
it ran both test assemblies and exited 0 when every test passed, and exited 2 when
a test failed.

### 15. The `Docs/` inventories

`Security.md` states two inventories and the rule each one carries: the
cryptographic inventory in `Security.md` section 3, and the infrastructure
inventory in `Security.md` section 11. **This step writes each file's heading and
its empty table, and nothing else.** The columns of each table are the parts that
file's rule names, in the order the rule gives them. Filling a row waits for the
first key or the first part that the file's rule reaches. The third file under
`Docs/` that `Security.md` states, `Docs/AcceptedVulnerabilities.md`, is written by
step 3, "`Directory.Build.props`", because every build reads it.

Write `Docs/CryptographicInventory.md`:

```markdown
# Cryptographic inventory

| Key, algorithm or certificate | Where it may be used | Where it may not be used | What it may protect | What it may not protect |
|---|---|---|---|---|
```

**This file is empty although certificates are already in use, and that is
correct.** `Code/Certificates/` holds the database authority's certificate and the
Migrator's role's certificate and key once step 12 has run, the `certificates`
volume holds copies of them, and none of them gets a row. **Ruled by Francois on 2026-10-06**: they are made on each developer's own
machine, by the `certificates` service in `compose.yml`, for local development
only, they reach no customer data, and they are never committed. `Security.md` section 3 also ties a change to
this file to the increment that lists it, and creating the solution is not an
increment. The certificates a deployed environment uses do belong here, and they
are written by whoever first deploys, which is the infrastructure work.

Write `Docs/InfrastructureInventory.md`:

```markdown
# Infrastructure inventory

## Parts

| Part | What it is | Which environments hold it | Highest protection level of the data it holds or carries | Whether the internet can reach it |
|---|---|---|---|---|

## Connections

| Connects | To | Port | Protocol | Highest protection level of the data it carries | How the connecting part proves who it is | Feature whose plan states what the address may be |
|---|---|---|---|---|---|---|
```

**It holds two tables, because `Security.md` section 11's rule states one thing
for each part and another for each connection.** The last column of
"Connections" is filled in only where the address a connection reaches depends on
data received from outside, which the same rule states.

**No row is written, for the Migrator or for the database.** `Security.md` section
11 has each part state the highest protection level of the data it holds, which
the features not yet planned decide, and which environments hold it, which no
deployment yet decides. A row with those cells guessed would be invented.

### 16. The CI/CD pipeline

Write `.github/workflows/ci.yml`, exactly as follows, with `<Solution>.slnx`
substituted in the step that makes the software bill of materials:

```yaml
name: CI

on:
  push:
    branches:
      - dev
      - test
      - main
  pull_request:

permissions:
  contents: read
  actions: read

jobs:
  checks:
    runs-on: <Versions.md: GitHub Actions runner>
    steps:
      - uses: actions/checkout@<Versions.md: actions/checkout, commit> # <Versions.md: actions/checkout, version>
        with:
          fetch-depth: 0   # gitleaks reads every version, not only the working tree

      - uses: actions/setup-dotnet@<Versions.md: actions/setup-dotnet, commit> # <Versions.md: actions/setup-dotnet, version>
        with:
          global-json-file: Code/global.json

      - name: Restore, locked
        working-directory: Code
        run: |
          dotnet restore --locked-mode
          changed=$(git status --porcelain -- '*packages.lock.json')
          if [ -n "$changed" ]; then
            echo "$changed"
            exit 1
          fi
        # Locked mode writes a missing lock file and exits 0, so a project committed
        # without its packages.lock.json shows here as a new file, and the step fails.
        # Restore also audits every NuGet package, transitive ones included, and fails
        # here first: one NU1902 or NU1903 error per project and advisory on a finding
        # Docs/AcceptedVulnerabilities.md does not hold, and NU1900 or NU1301 when it
        # gets no vulnerability data. Code/Directory.Build.props passes a finding the
        # file holds. Measured.

      - name: Build
        working-directory: Code
        run: dotnet build --no-restore
        # Build audits the NuGet packages again and fails on the same findings, but the
        # Restore, locked step fails on them first. Measured.

      - name: Docker
        run: docker info
        # The integration tests start a PostgreSQL container. Plain docker info exits
        # 1 with no daemon; docker info --format exits 0 with no daemon, so it is
        # never written with --format.

      - name: Test
        working-directory: Code
        run: dotnet test --no-build
        # Run from Code/, where Code/global.json puts dotnet test on
        # Microsoft.Testing.Platform. The first run on a runner pulls <Versions.md: PostgreSQL, the integration tests' container> and
        # the image Testcontainers uses to clean up, and can print nothing for more
        # than ten minutes. Nothing in this file limits how long a step runs.

      - uses: actions/upload-artifact@<Versions.md: actions/upload-artifact, commit> # <Versions.md: actions/upload-artifact, version>
        if: always() && hashFiles('Code/TestResults/**') != ''
        with:
          name: TestResults
          path: Code/TestResults/
          retention-days: 3
        # dotnet test makes Code/TestResults/ even when no test writes into it, so the
        # condition tests for files, not for the folder. always() also runs on a
        # cancelled run, as GitHub's documentation states. retention-days: 3 gave an
        # expires_at three days after the upload, measured.

      - name: Dependency audit
        working-directory: Code
        run: |
          nuget_exit=0
          dotnet package list --vulnerable --include-transitive --format json --no-restore > "$RUNNER_TEMP/nuget-audit.json" || nuget_exit=$?
          failures=$(jq -nr \
            --rawfile nuget "$RUNNER_TEMP/nuget-audit.json" \
            --argjson nuget_exit "$nuget_exit" \
            --rawfile accepted ../Docs/AcceptedVulnerabilities.md '
            def ymd: test("^[0-9]{4}-[0-9]{2}-[0-9]{2}$")
              and ((try (strptime("%Y-%m-%d") | mktime | strftime("%Y-%m-%d")) catch "") == .);
            def day: strptime("%Y-%m-%d") | mktime / 86400;
            (now | strftime("%Y-%m-%d")) as $today
            | ($nuget | try fromjson catch null) as $g
            | [ if $nuget_exit != 0 or ($g | type) != "object" or ($g | has("projects") | not)
                then "dotnet package list returned no vulnerability data" else empty end ] as $nodata
            | [ ($g | .projects[]? | .frameworks[]? | (.topLevelPackages[]?, .transitivePackages[]?)
                  | .id as $p | .vulnerabilities[]
                  | {finding: (.advisoryurl | sub("^.*/"; "")), package: $p, severity: (.severity | ascii_downcase)}) ]
              | unique as $found
            | [ $accepted | split("\n")[] | select(test("^ *\\|")) ][2:]
              | map(sub("^ *\\| *"; "") | sub(" *\\| *$"; "") | [splits(" *(?<!\\\\)\\| *")]) as $rows
            | if ($nodata | length) > 0 then $nodata[] else
                ( $found[] | . as $f
                  | select(any($rows[]; .[0] == $f.finding and .[1] == $f.package) | not)
                  | "\(.finding) in \(.package) is reported and not held in Docs/AcceptedVulnerabilities.md" ),
                ( $rows[] | . as $r
                  | if length != 5 or any(.[]; . == "") then "A row does not fill its five cells: \(join(" | "))"
                    elif (.[0] | test("^GHSA(-[0-9a-z]{4}){3}$") | not) then "\(.[0]) is not a GHSA identifier"
                    elif (.[3] | ymd | not) or (.[4] | ymd | not) then "\(.[0]) in \(.[1]) has a date not written as YYYY-MM-DD"
                    else
                      ( if .[4] < $today then "\(.[0]) in \(.[1]): acceptance ended on \(.[4])" else empty end ),
                      ( [ $found[] | select(.finding == $r[0] and .package == $r[1]) ][0] as $f
                        | if $f == null then "\(.[0]) in \(.[1]) is no longer reported"
                          elif ($f.severity == "moderate" or $f.severity == "low" or $f.severity == "info")
                          then ( if .[4] > "\(.[3][0:4] | tonumber + 1)\(.[3][4:])"
                                 then "\(.[0]) in \(.[1]) is \($f.severity) and accepted for more than 12 months" else empty end )
                          elif (.[4] | day) - (.[3] | day) > 30
                          then "\(.[0]) in \(.[1]) is \($f.severity) and accepted for more than 30 days"
                          else empty end )
                    end ) end
          ')
          if [ -n "$failures" ]; then
            echo "$failures"
            exit 1
          fi
          echo "Every finding is held, and every entry is current."
        # dotnet package list still reports a finding NuGetAuditSuppress suppresses, so the step compares
        # it with Docs/AcceptedVulnerabilities.md itself, and runs it with --no-restore
        # on the assets the Restore, locked step wrote. Measured,
        # 124, 126 and 129.

      - name: Secret scan
        run: |
          curl -sSfL https://github.com/gitleaks/gitleaks/releases/download/v<Versions.md: gitleaks>/gitleaks_<Versions.md: gitleaks>_linux_x64.tar.gz \
            | tar -xz -C /usr/local/bin gitleaks
          if [ -f Code/.gitleaks-baseline.json ]; then
            gitleaks detect --source . --baseline-path Code/.gitleaks-baseline.json -v
          else
            gitleaks detect --source . -v
          fi
        # never --config: a custom config replaces gitleaks's default ruleset rather than
        # adding to it, measured 2026-09-25. --baseline-path is the accepted-findings
        # mechanism, and it is never run with the default rules turned off.
        # gitleaks/gitleaks-action is not used: it is a commercial product, not open
        # source, checked 2026-09-25 against its own licence file.

      - name: Generate SBOMs
        working-directory: Code
        run: |
          dotnet tool install --global CycloneDX --version <Versions.md: CycloneDX>
          mkdir -p ../sbom
          dotnet CycloneDX <Solution>.slnx -o ../sbom -F Json -fn sbom-dotnet.json
          # dotnet CycloneDX, not a bare cyclonedx: the global tool installs as
          # dotnet-CycloneDX, which the dotnet CLI resolves through `dotnet <verb>`.
          # Corrected 2026-09-25: a bare `cyclonedx` was measured against CycloneDX
          # 6.2.0 and does not exist as a command; `dotnet CycloneDX` and
          # `dotnet-CycloneDX` both do.

      - name: Compare SBOMs against the previous run
        if: github.event_name == 'push'
        env:
          GH_TOKEN: ${{ github.token }}
        run: |
          mkdir -p previous-sbom
          PREV_RUN_ID=$(gh run list --branch "${{ github.ref_name }}" --status success \
            --json databaseId --jq '.[0].databaseId')
          if [ -z "$PREV_RUN_ID" ]; then
            echo "No earlier successful run on ${{ github.ref_name }}, so nothing was compared."
            exit 0
          fi
          EXPIRED=$(gh api "repos/${{ github.repository }}/actions/runs/$PREV_RUN_ID/artifacts" \
            --jq '.artifacts[] | select(.name == "sbom") | .expired')
          if [ "$EXPIRED" = "true" ]; then
            echo "Run $PREV_RUN_ID's list has expired, so nothing was compared."
            exit 0
          fi
          gh run download "$PREV_RUN_ID" -n sbom -D previous-sbom
          count=0
          for f in sbom/*.json; do
            name=$(basename "$f")
            if [ -f "previous-sbom/$name" ]; then
              jq -r '.components[] | "\(.name)@\(.version)"' "previous-sbom/$name" | sort > /tmp/prev.txt
              jq -r '.components[] | "\(.name)@\(.version)"' "$f" | sort > /tmp/now.txt
              new=$(comm -13 /tmp/prev.txt /tmp/now.txt)
              if [ -n "$new" ]; then
                echo "::notice::New components in $name, not in the previous run: $new"
                count=$((count + $(printf '%s\n' "$new" | wc -l)))
              fi
            fi
          done
          echo "Compared with run $PREV_RUN_ID: $count new components."

      - uses: actions/upload-artifact@<Versions.md: actions/upload-artifact, commit> # <Versions.md: actions/upload-artifact, version>
        with:
          name: sbom
          path: sbom/
```

**Every step this pipeline takes from its original design was measured.** Every
change made since is listed below, with why it was made. That design settles the
five checks `Security.md` section 14 states, being the build, the tests, the
dependency audit, secret scanning and the install that resolves no version, and the
list of software each build produces. Where a step's own behaviour is not obvious,
the comment beside it records what running it showed. **This file is written once
and never edited by a later run.** Each API joins the .NET steps through the
solution file, so a new API is picked up with no change here.

**It runs on a push to `dev`, `test` or `main` and on a pull request, and on
nothing else.** `Branching.md` section 5 states that nothing builds on a feature
branch and that the whole suite runs once, when the pull request is opened, and
`Branching.md` section 8 names the CI configuration as that rule's enforcer. An
unfiltered `push` builds every push a feature branch receives.

**It deploys nothing, and it names no deployment platform.** A part's deployment
builds from this repository and waits for this pipeline, which the closing report
below states as a requirement.

**It needs Docker and no certificate.** `Testing.md` section 5's tests bring their
own PostgreSQL container, so nothing here makes an authority or issues a
certificate. The only thing that reaches an installed server is the Migrator, which
an engineer runs on their own machine. A GitHub-hosted runner carries Docker, and
the `Docker` step makes a runner without it fail by name rather than in the middle
of the tests.

**What differs from the original design, and why each change was made:**

- **`<Solution>.slnx` in the software bill of materials step** is the one
  substitution. The original named its own solution there.
- **There is no Node, npm or Chromium step. Ruled by Francois on 2026-10-22:
  Zapper is API only.** The original set up Node and ran `npm ci`, the contract
  type generator, `npm run build`, `npm test`, `npm audit` and `npm sbom`, and a
  later change installed Playwright's Chromium for the acceptance tests. Nothing
  in the solution is built with npm, and `Testing.md` section 7's acceptance
  tests call the API without a browser, so all of them are removed.
- **The runner is named by its version label, and each action by its commit with
  its version in a comment**, the `sbom` upload included, where the original had
  `ubuntu-latest` and each action at `@v4`. `Versions.md` section 2, "The rules",
  states both forms. Measured: the runner warned that the actions at `@v4` were forced from Node 20
  onto Node 24, and the steps were run on both Ubuntu 24.04 and Ubuntu
  26.04.
- **`actions/setup-dotnet` reads `Code/global.json`**, rather than naming an SDK of
  its own, so the pipeline builds on the SDK step 13, "`<Solution>.UnitTests`",
  fixes. `Versions.md` section 2, "The rules", states this form.
- **The `Restore, locked` step fails on a new or changed lock file. Approved by
  Francois on 2026-10-08.** Checked on 2026-10-08 against SDK 10.0.302:
  `dotnet restore --locked-mode` on a project with no lock file wrote one and
  exited 0, so a project committed without its lock file resolved its versions
  freely and passed. `Security.md` section 14 makes the install that resolves no
  version of any package for itself one of the five checks, so the step reads git
  after the restore and fails where a lock file appeared or changed. **Its comment
  also records the NuGet audit**, which fails this step first on a finding
  `Docs/AcceptedVulnerabilities.md` does not hold, measured. The
  `Build` step's comment says that `Build` fails on the same findings.
- **The `Docker` step is added**, running plain `docker info`. Measured: `docker info --format` exits 0
  with no daemon, while plain `docker info` exits 1.
- **The `Test` step is run from `Code/`**, as the original already ran it, and that is
  now required rather than incidental. Measured: from the
  repository root `dotnet test` exits 1, because `Code/global.json` is found only
  from `Code/` and below. Its comment adds the first image pull, which took more
  than ten minutes on 2026-10-07 and printed nothing while it waited.
- **The test results are uploaded**, immediately after the `Test` step, as an
  artifact named `TestResults`, so that what a failing test wrote outlives the
  runner. **Ruled by Francois on 2026-10-10: the files are kept for three days**,
  and that period applies to the test results only, so the `sbom` upload sets no
  `retention-days`. **Ruled by Francois on 2026-10-12: the condition is
  `always()`**, with the test for files. Measured: the
  upload ran after a failed `Test` step, was skipped when `Code/TestResults/` was
  empty, and expired three days after it was made.
- **CycloneDX is installed with its version**, as `Versions.md` section 2, "The
  rules", states.
- **The `Dependency audit` step replaces the `npm audit` step**, where that step
  stood. `npm audit` failed on every finding, a held one too, and
  `Security.md` section 13 has a build pass a finding
  `Docs/AcceptedVulnerabilities.md` holds. The step runs the NuGet audit and fails
  on a finding no row holds by both its identifier and its package, a row without
  five filled cells, a Finding that is not a GHSA identifier, a date that is not a
  real day written `YYYY-MM-DD`, an end date before today, a row whose finding and
  package the audit no longer reports, a period longer than the finding's severity
  allows, and the audit returning no vulnerability data. Measured in a local
  Ubuntu 24.04 container, and on the runner, while it also had an npm half.
  **The npm half was removed on 2026-10-22**, and the program without it was
  measured on macOS against this repository's own audit output: it printed
  nothing for no finding, the no-data line for a failed audit, the not-held line
  for a finding no row holds, nothing for a held finding, and the no-longer-reported
  line for a row no finding matches. Not yet measured on the runner. It reads
  dates with `jq` alone and runs no `date` command. It needs the item group of
  step 3, "`Directory.Build.props`", so that the `Restore, locked` step passes a
  held finding before this step reads it.
- **The workflow gives its token `contents: read` and `actions: read`, and
  nothing more. Ruled by Francois on 2026-10-15.** The original file had no
  `permissions:` key. A repository whose workflow permissions default to `read`
  gives the token no `actions: read`, so the step "Compare SBOMs against the
  previous run" got "HTTP 403: Resource not accessible by integration" from
  `gh run list`, and `|| true` hid it, so no list was ever compared. `checkout`
  needs `contents: read`, and the lookup and the download need `actions: read`.
  Measured: the token
  held "Actions: read", "Contents: read" and "Metadata: read", and `checkout` and
  every other step passed.
- **The step "Compare SBOMs against the previous run" fails when it cannot
  compare, and always says what it did. Ruled by Francois on 2026-10-15.**
  Neither `gh run list` nor `gh run download` ends in `|| true`, so under the
  runner's `bash -e` a failed lookup or download fails the step. It prints one
  line: that no earlier successful run exists on the branch, that the run's list
  has expired, or which run it compared with and how many new components it
  found. Each new component still gets its own `::notice::` line. Measured: with no earlier run the step printed its line and exited 0; with a
  token that could not read it exited 1; with an artifact lookup that failed it
  exited 1; with one component added it printed the notice and the count 1; and
  on `dev` after a merge it printed "Compared with run 38622458934: 0 new
  components.", which a comparison of the two lists on the machine confirmed.
- **The comparison runs only on a push to `dev`, `test` or `main`. Ruled by
  Francois on 2026-10-15.** It compares with the latest successful run of the
  same branch, `github.ref_name`. On a pull request it is skipped, since the
  pull request's diff already shows every package a change brings in, and
  `github.ref_name` there is the pull request's merge reference, not a branch.
  Measured: the step was `skipped` on both pull
  requests' runs, and every other step passed.
- **A list that has expired is not compared, and the step passes. Ruled by
  Francois on 2026-10-15.** The `sbom` upload sets no `retention-days`, so
  GitHub keeps each list for 90 days, measured. Without this
  check, once the last successful run's list expired, the download would fail,
  so no later run on that branch could succeed, and each one would look up the
  same expired list again. The step reads the list's `expired` flag before it
  downloads. A run that finds a list missing, rather than marked expired, still
  fails at the download. **This case is not measured**: no list in the run's
  repository expires before 2027-01-12. A run on
  `dev` found the flag not set and compared, and the same lookup on the machine
  printed `false`.
### 17. Verify
### 17. Verify

Run every command below from `Code/`, except the last two, which are run from
the repository root. **Each one has an expected result, and anything else is a
real failure: stop, report what it said, and do not carry on.**

**Restore, and then restore in locked mode:**

```bash
dotnet restore
dotnet restore --locked-mode
```

**Expect exit 0 from both, and a `packages.lock.json` in each of the six
projects.** Step 11's restore ran before the two test projects existed, so the
first command writes their lock files, and the second proves that every lock file
matches what the projects ask for, which is how the pipeline restores.
`BackendCode.md` section 2 has every lock file committed and restore run in locked
mode. **Count the six yourself, because locked mode does not.** Checked on
2026-10-08 against SDK 10.0.302: `dotnet restore --locked-mode` on a project with
no lock file wrote one and exited 0. The pipeline's own restore step fails on
such a file, which step 16 states, and the count here catches it before anything
is committed. **The lock files are not identical**: `<Solution>.Domain` references nothing,
so its own is `{"version": 2, "dependencies": {"net10.0": {}}}`, while every other
project's lists its project references and every package they bring, the whole way
down.

**Build:**

```bash
dotnet build --no-restore
```

**Expect 0 warnings and 0 errors across all six projects**: `<Solution>.Domain`,
`<Solution>.Infrastructure`, `<Solution>.ApiCommon`, `<Solution>.Migrator`,
`<Solution>.UnitTests` and `<Solution>.IntegrationTests`. Step 3's
`TreatWarningsAsErrors` and `MSBuildTreatWarningsAsErrors` turn any warning into a
failed build, so a build that succeeds has none.

**Confirm the migration is present.** `Code/<Solution>.Infrastructure/Persistence/Migrations/`
holds the `CreateErrorTables` migration and `<DatabaseName>ContextModelSnapshot.cs`,
which step 11 generated and step 12 applied.

**Run the tests:**

```bash
dotnet test --no-build
```

**Expect exit 0, with "Test run summary: Passed!", both test assemblies run and 20
tests on day one**: 4 unit tests in `<Solution>.UnitTests` and 16 integration
tests in `<Solution>.IntegrationTests`. Measured: `dotnet test` exits 2 when a test fails. **Exit 2 is a failing
test**: report which test failed and what it printed.

**The first run on a machine can print nothing for more than ten minutes, and that
is not a hang.** It pulls the image step 14, "`<Solution>.IntegrationTests`", names
and the image Testcontainers uses to clean up. With `postgres:18`, and with
`testcontainers/ryuk:0.14.0` under `Testcontainers.PostgreSql` 4.15.0, that took
more than ten minutes on 2026-10-07. Run the command where no time limit
stops it, and wait for its exit code. A later run with both images present takes
seconds.

**Build the Migrator's image again and run the containers**, from the repository
root:

```bash
docker compose run --rm --build migrator
```

**Expect exit 0, and the Migrator's last line printed to be "No migrations were
applied. The database is already up to date."** Steps 13 to 16 wrote files under
`Code/` after step 12 built the image, and `--build` builds it again from the
files that will be committed, which is what a fresh clone builds. It is `run` and
not `up -d --build --wait` for the reason step 12 gives: with no API, `--wait`
exits 1 once the Migrator stops, even with 0. Measured on 2026-10-21: after a
change under `Code/`, `docker compose run --rm --build migrator` built the image
again, printed that line and exited 0, in 15 seconds, and in 21 seconds after
steps 13 to 16. **Then `docker compose ps
-a` lists `certificates` and `roles` as exited with 0 and `database` as
running.** Anything else is a real failure: show
`docker compose logs certificates roles database`.

**Check that no placeholder leaked**, from the repository root:

```bash
grep -rnI -e '<Solution>' -e '<DatabaseName>' -e '<Port>' \
    --exclude-dir=bin --exclude-dir=obj \
    --exclude='*.slnx' \
    .gitignore .gitattributes compose.yml Code Docs .github
```

**This is the one command in this skill whose `<Solution>`, `<DatabaseName>` and
`<Port>` are typed exactly as written and not replaced**, because they are what it
looks for. **Expect no output and exit 1**, which is how `grep` reports that it
found nothing. A line printed means a value was never substituted in the file it
names: report every line.

**The solution file is excluded, and it is the only one.** Measured: the `.slnx` format's own root element is the literal text `<Solution>`, so that
file matches in every repository this skill creates. `Code/` holds one `.slnx`,
which the preconditions check, so `*.slnx` excludes exactly `Code/<Solution>.slnx`.
The six paths searched are everything this skill writes, and `bin/` and `obj/`
are what the tools write. `Code/.dockerignore` and
`Code/<Solution>.Migrator/Dockerfile` are under `Code/`, and `grep -r` reads a
file whose name starts with a dot: measured on 2026-10-21, it found a
placeholder put into `Code/.dockerignore`.

**Nothing here applies the migration again.** Step 12 applied it, and the run
above starts the Migrator a second time, which changes nothing, for the reason
step 12 gives.

---

## Commit, push and open the pull request

Run `Branching.md` section 6, "How a skill ends its run": check that every change
is this run's, then one commit with the subject `<FeatureTitle>_1/1`, the push,
and a pull request into `dev` titled `<FeatureTitle>`. **Its body is the closing
report below.** Ruled by Francois on 2026-10-22.

## Report, and wait for the merge

Say every item of the closing report below, and what step 17, "Verify", found,
then give the pull request's address. **Then wait.** When Francois says in this
conversation that the pull request is merged, run `Branching.md` section 6,
"After the merge", which deletes the local feature branch, and then stop.

The closing report. Say all twelve of these plainly:

- **The solution is complete, and it serves no request.** Every file this skill
  writes is written and step 17 passed. No API exists until `create-project`
  creates one. The solution does apply its migration, which step 12 ran.
- **Step 17 ran both test projects, and every test passed**: 20 tests, being 4
  unit tests and 16 integration tests. `dotnet test` is run from `Code/`, because
  `Code/global.json` is found only from there, and it needs a running Docker,
  because the integration tests start a PostgreSQL container. Every test passes
  on day one, including the reference table test, which has no reference table to
  read until a feature adds one.
- **The database holds the schema, and step 12's run is the proof of every value
  this skill asked for.** The `Error` schema holds `ErrorStatus` with its nine
  rows and `ErrorMessage` empty, and the `Migration` schema holds
  `MigrationHistory` with one row. The Migrator connected as its own role over
  TLS, with no password anywhere.
- **The local database and the Migrator run in Docker, and a fresh clone needs
  Docker 26 or later and nothing else to run them.** From the repository root,
  `docker compose up -d` makes that machine's own authority, the server on
  `127.0.0.1:<Port>`, the database, every role, every role's certificate in
  `Code/Certificates/` and its copy in the `certificates` volume, then builds
  the Migrator's image and runs it, which applies the migrations. Measured on
  2026-10-21: a new clone of the feature branch, with `Code/Certificates/`
  holding only its `.gitignore`, ran `docker compose up -d` and nothing else; it
  exited 0 in 74 seconds, the Migrator applied `CreateErrorTables` and exited 0,
  and the database held the nine `ErrorStatus` rows. Running
  `docker compose up -d` again renews any certificate within 30 days of
  expiring, and replacing one on purpose is deleting its two files in
  `Code/Certificates/` and running `docker compose run --rm certificates`, which
  issues a new pair and replaces its copy in the volume on the same run. After a
  change under `Code/`, `docker compose up -d --build` builds the image again.
  `docker compose logs certificates roles database migrator` shows what each
  service did. `dotnet run --project Code/<Solution>.Migrator` still works on a
  machine with the .NET SDK, using `Code/Certificates/`. The authority's key
  stays in the `authority` volume, and nothing in `Code/Certificates/` but its
  `.gitignore` is committed.
- **No API can read any of those tables yet**, and `create-project` is what
  fixes it. Only the Migrator's own role has any privilege on them, and
  `create-project` writes a grant-only migration for each API role it creates,
  granting `USAGE` on the `Error` and `Migration` schemas and `SELECT` on
  `ErrorStatus`, `ErrorMessage` and `MigrationHistory`. Until it runs for an API,
  that API cannot start, because `BackendCode.md` section 7 has it read both
  Error tables at startup and `BackendCode.md` section 13 has it read the history
  table to decide whether it is behind.
- **A host outside this machine's Docker needs its own role's files at
  `/certificates`, and this repository does not supply them.** Only
  `Code/Certificates/.gitignore` is committed and no image holds a certificate,
  so a host that finds no `.slnx` above it looks in `/certificates`, which step
  11 records, and refuses to start, naming the file, where `ca.crt`, its role's
  certificate or its role's key is missing there. Whatever runs the host puts
  them there, as `compose.yml` does. **Ruled by Francois on 2026-10-21: AWS
  deployment is not a requirement**, so no deployed database is set up.
- **The pipeline is written and has never run.** `.github/workflows/ci.yml` runs
  on a push to `dev`, `test` or `main` and on a pull request, and never on a
  feature branch's own pushes. It needs a runner with Docker and no certificate.
  Its first run pulls two images and can print nothing for more than ten minutes.
  It deploys nothing.
- **Each part needs a deployment, and this repository holds none of it.** Neither
  this skill nor the pipeline names a deployment platform. **A part's deployment
  builds from this repository, and does not deploy a commit until the CI run for
  that commit has passed.** Today the one part that runs is `<Solution>.Migrator`,
  and each API `create-project` creates is another. **A deployment runs the
  published output, and never `dotnet run` against the source.** A launch profile
  puts a host in `Development` and overrides the shell, which step 12 records, and
  a published artefact carries no launch profile, so the published output is the
  only thing that keeps a deployed host out of `Development`. Each part's
  Dockerfile builds an image of that published output, which step 12 records for
  the Migrator.
- **The two `Docs/` inventories and `Docs/AcceptedVulnerabilities.md` are
  empty.** The cryptographic inventory takes
  its first row with the first key or certificate a deployed environment uses,
  which step 15 says is why the local certificates have none. The infrastructure
  inventory takes a row when a feature's plan states a part or a connection,
  and `create-project` adds what it knows when it creates an API or a frontend.
  The accepted vulnerabilities stay empty until a finding cannot be fixed.
- **`.editorconfig` and `BannedSymbols.txt` are still missing**, and the
  solution-wide enforcer pass writes them. Until it runs,
  `EnforceCodeStyleInBuild` in `Code/Directory.Build.props` has no `.editorconfig`
  to read and so enforces nothing. `BackendCode.md` section 2 states the other
  half of the same trap, that a style rule written without an explicit severity
  reports nothing even once the file exists. **That pass also has to allow
  `IConfiguration` in `<Solution>.Infrastructure/Logging/LoggingPipeline.cs`**, which
  `BackendCode.md` section 21 bans as a symbol while `BackendCode.md` section 15
  makes that file the solution's one legitimate reader of it.
  `BackendCode.md` section 17 states the form the allowance takes, being a
  suppression carrying its reason at the point of use. **That pass also gives
  both test projects the test list `Testing.md` section 2 states, beside the
  production list, and defines `XUNIT_GENERATED_DISABLE_WARNINGS` in both.**
  xUnit v3 generates each test project's entry point, and that file calls
  `GetAwaiter().GetResult()`, which the production list bans, so without the
  constant every test project fails to build before a test is written. That was
  measured on 2026-09-15, and the constant is the one way out found. **That pass
  also marks `<Solution>.Infrastructure/Persistence/Migrations/*.cs` as generated
  code**, as `BackendCode.md` section 13 states, because `dotnet ef` writes each
  migration with a block namespace and `///` comments, which `BackendCode.md`
  section 17 refuses everywhere else.
- **The authorization feature still has three things to do**, and none of them
  is this skill's. It registers a real authentication scheme, without which an
  operation declaring neither authorization call answers 500 rather than 401.
  It revisits `OpenApi/OperationSecurityTransformer.cs`, which throws by design
  for the first operation that requires authorization, so that operation fails
  the build rather than publishing a security requirement nobody decided. And it
  supplies the caller and the network address that `BackendCode.md` section 15
  requires on every log line written inside a request, which
  `Logging/LoggingPipeline.cs` cannot enrich while no caller exists.
- **The first API has three things to supply**, and `create-project` creates
  it. `<Solution>:OpenApi:IsEnabled`, `Title` and `Description` are all
  `[Required]` and bound with `ValidateOnStart` by step 10, so that API does not
  start without all three. `<Solution>:Security:RequestSizeLimit` and
  `RateLimit` are each refused at `0` by the `SecurityOptions` of step 10,
  "`<Solution>.ApiCommon`", so it does not start without both, and its
  `AllowedOrigins` holds each frontend that calls it. Health is under that rate
  limit, which the same step states. And step 14's
  database fixture has to create that API's database role in the container before
  it migrates the template, because the API's grant migration names that role and
  the container holds none, so without it every integration test fails.

**The product's own developer setup document records the whole of this list.**
