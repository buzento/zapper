# Frontend recipe

`create-project`'s `SKILL.md` reads this file at its step 2, "Ask what is being
created", when the run creates a frontend. **It creates one frontend,
`Code/<Solution>.<Frontend>`, calling one or more of the solution's APIs**, with
everything a frontend needs before its first feature: the project, its packages,
its compiler settings, its styling, one client per API, the settings it reads at
startup, the route table with the application frame and the error boundary, its
two providers, its test support with its first two test files, and the shared
acceptance base class where the solution has none. Every file traces to a sentence
in `FrontendCode.md`, `Testing.md` or `BackendCode.md`, or to a recorded ruling.

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
| `<Frontend>` | asked | the frontend's own name, in the shape `<Solution>` takes, which `FrontendCode.md` section 3 states |
| `<Name>Api`, once per API | asked | each API the frontend calls, in the order given, as `SKILL.md` defines it |
| `<Solution>` | read by `SKILL.md` step 1, "Check the repository" | the solution's name |

**Every other name is derived, and none is asked for:**

| Name | Derived as | Where the rule is |
|---|---|---|
| `<Solution>.<Frontend>` | the frontend's folder, directly under `Code/` | `FrontendCode.md` section 3 |
| `<solution>-<frontend>` | the frontend's npm name: the folder name lowercased, with the dot written as a hyphen and nothing else changed | `FrontendCode.md` section 3. The template writes it, and step 2, "The workspace entry, and the install", checks it |
| `<name>-api` | an API's folder under `src/api/`: `<Name>Api` lowercased, with a hyphen between its words | `FrontendCode.md` sections 4 and 8. The contract type generator writes it |
| `<nameApi>` | an API's key in `public/config.json` and in the settings type: `<Name>Api` with its first letter lowercased, which is camelCase | measured |
| `<address>` | an API's development address, read in step 9, "The settings" | that API's `Properties/launchSettings.json` |
| the first API | the first `<Name>Api` given | step 11, "The failure declaration", which imports one API's generated types |
| `<FrontendPort>` | the development server's port: the first port from 5173 upward that no `Properties/launchSettings.json` under `Code/` and no frontend's `vite.config.ts` names, read in step 5, "`vite.config.ts`" | Francois's ruling of 2026-10-11 |

So the words of an API's name are joined by hyphens in its folder and run together
in its key, and an API whose `<Name>` is one word has no hyphen in its folder.

**`<OtherName>Api` below stands for a second API, `<otherNameApi>` for its key and
`<other-name>-api` for its folder**, where a block shows two.
**A frontend calling one API writes one line where a block shows two, and a
frontend calling three writes three**, in the order the APIs were given.

## What this recipe creates, in this order

1. `Code/<Solution>.<Frontend>`, from the Vite template, with the files no rule
   keeps removed
2. Its entry in `Code/package.json`, and the install
3. Its packages
4. Its TypeScript settings and the `@/` alias in its two `tsconfig` files
5. `vite.config.ts`
6. `src/index.css`, holding Tailwind's import
7. shadcn's `components.json` and its stylesheet
8. The stylesheet's tokens, corrected to `FrontendCode.md` section 12's shape
9. `src/lib/config.ts` and `public/config.json`
10. Each API's pair in the contract type generator's configuration, its
    generated `types.ts`, its `apiClient.ts`, and the build script
11. `src/api/query.d.ts`
12. `src/components/layout/AppFrame.tsx` and
    `src/components/layout/AppErrorBoundary.tsx`
13. `src/router.ts`
14. `src/App.tsx`
15. `src/main.tsx`, and the document's language in `index.html`
16. The frontend's rows in `Docs/InfrastructureInventory.md`
17. Its development address in each called API's `appsettings.Development.json`
18. Its test packages and its `test` script
19. The `test` block in `vite.config.ts`
20. `src/test/server.ts`
21. `src/test/setup.ts`
22. `src/test/renderWithProviders.tsx`
23. `src/test/accessibility.json` and `src/test/axe.ts`
24. `src/components/layout/AppFrame.test.tsx` and
    `src/components/layout/AppErrorBoundary.test.tsx`
25. `Code/<Solution>.IntegrationTests/AcceptanceTestBase.cs` and its two packages,
    where no frontend before this one wrote them

Step 26, "Verify", then verifies all of it.

## What this recipe never creates

- **A module, a page or a page's route.** `FrontendCode.md` section 5 has what
  modules exist decided in planning, and `FrontendCode.md` section 15 writes
  nothing no increment needs, so the first page arrives with the first feature.
- **Any component primitive.** `FrontendCode.md` section 12 has a primitive taken
  with shadcn when something needs it, and shadcn's `init` at the version below
  copies none.
- **ESLint or Prettier configuration.** `FrontendCode.md` section 4 states that a
  frontend's folder holds neither, and both sit at `Code/`.
- **A `.env` file**, which `FrontendCode.md` section 7 states is never committed.
- **A fixture, or the folder `src/test/fixtures/`.** `Testing.md` section 6 writes
  a fixture for a body a default handler returns or more than one test uses, and
  none exists on day one, so the first increment that writes a fixture creates the
  folder.
- **An acceptance test.** `Testing.md` section 7 writes one per scenario in a plan, so the
  first arrives with the first feature that has a scenario.
- **A package nothing on day one imports.** Zustand, React Hook Form and Zod are
  the store, the form library and the schema library, in `FrontendCode.md`
  sections 10 and 11, and the increment that first uses one installs it.

## Preconditions

`SKILL.md`'s step 4, "Ask the recipe's values, and run its preconditions", runs
these, in this order, after it has asked for `<Frontend>` and the APIs. **Where
one fails, stop, say which and why, and write nothing.**

- **Node and npm are installed.**

  ```bash
  node --version
  npm --version
  ```

  Each exits 0 and prints a version. Where either exits non-zero, stop and raise
  it to whoever ran this skill to resolve. This skill installs nothing on the
  machine it runs on.
- **The .NET SDK is installed.**

  ```bash
  dotnet --version
  ```

  It exits 0 and prints a version. Where it exits non-zero, stop and raise it the
  same way. Step 25, "The shared acceptance base class", adds to the solution's C#
  side where no frontend has yet, and step 26, "Verify", restores and builds the
  solution on every run.
- **The contract type generator is at `Code/Tools/ContractTypes/`.**

  ```bash
  test -f Code/Tools/ContractTypes/package.json
  ```

  Exit 0 means it is there. Any other exit means `create-solution` has not
  finished in this repository: stop and say so.
- **`<Frontend>` fits its shape.**

  ```bash
  printf '%s\n' '<Frontend>' | grep -Ex '([A-Z][a-z]+)+'
  ```

  It prints the name back where it fits. **No output means it does not fit**:
  refuse it and ask again, and never adjust it.
- **The frontend does not exist yet**, as a folder or as a workspace entry.

  ```bash
  test -e Code/<Solution>.<Frontend>
  node -p "require('./Code/package.json').workspaces.includes('<Solution>.<Frontend>')"
  ```

  The first exits 1 and the second prints `false` where it does not exist. **Exit
  0 from the first, or `true` from the second, means the name is taken**: say so,
  and ask for another name.
- **Each API is in the solution.** Check that it is one of the APIs `SKILL.md`'s
  step 1, "Check the repository", listed:

  ```bash
  grep -c 'Path="<Solution>.<Name>Api/' Code/<Solution>.slnx
  ```

  It prints `1` where the API is in the solution. **`0` means it is not: refuse
  the frontend for that API**, name the solution's APIs, and say that an API is
  created before a frontend can call it. Never create a frontend for an API that
  does not exist.
- **Each API has left what this recipe reads.** The API's recipe leaves three
  things this recipe needs, and each is checked here rather than found missing
  half way through:

  ```bash
  node -p "require('./Code/Tools/ContractTypes/contract-types.config.json').pairs.filter((pair) => pair.api === '<Name>Api').length"
  find Docs/Contracts/<Name>Api -name '*.openapi.json' | head -1
  node -p "require('./Code/<Solution>.<Name>Api/Properties/launchSettings.json').profiles.https.applicationUrl.split(';').find((address) => address.startsWith('https://'))"
  ```

  **The first prints a number greater than 0**, because the API has at least one
  pair in the generator's configuration. **The second prints one fragment's
  path**, because the generator needs at least one frozen fragment to write the
  API's types. **The third prints the API's `https://` address**, which step 9,
  "The settings", reads. Where any of them prints `0`, nothing, `undefined` or an
  error, stop, name the API and what it is missing, and say that the API's recipe
  leaves it.

---

### 1. The template

`FrontendCode.md` section 2 has a frontend written in React and TypeScript and
built by Vite. **It starts from the Vite `react-ts` template of `create-vite`, at
the version `Versions.md` names**, because the steps below edit what that version
writes.

From `Code/`:

```bash
npm create vite@<Versions.md: create-vite> <Solution>.<Frontend> -- --template react-ts --no-interactive
```

Expect exit 0 and a new folder, `Code/<Solution>.<Frontend>`.

**Then remove what no rule keeps.** `FrontendCode.md` section 4 states what a
frontend's root holds: `index.html`, `package.json`, `vite.config.ts`,
`tsconfig.json`, `tsconfig.app.json`, `tsconfig.node.json` and `components.json`,
with `public/` beside them. The template writes three files outside that list, and
four things that only the template's own `App.tsx` uses, which step 14,
"`App.tsx`", replaces:

```bash
rm -r Code/<Solution>.<Frontend>/README.md \
      Code/<Solution>.<Frontend>/.gitignore \
      Code/<Solution>.<Frontend>/.oxlintrc.json \
      Code/<Solution>.<Frontend>/src/App.css \
      Code/<Solution>.<Frontend>/src/assets \
      Code/<Solution>.<Frontend>/public/icons.svg
```

| Removed | Why |
|---|---|
| `README.md` | outside `FrontendCode.md` section 4's root list, and this skill writes no directions into the repository |
| `.gitignore` | outside `FrontendCode.md` section 4's root list. The repository's own `.gitignore`, which `create-solution` wrote, already covers `node_modules/`, `dist` and `*.tsbuildinfo`, and step 26, "Verify", checks that git sees none of them |
| `.oxlintrc.json` | the configuration of the template's linter, which step 3, "The packages", removes. `FrontendCode.md` section 2 makes ESLint the linter |
| `src/App.css` | `FrontendCode.md` section 12 makes `src/index.css` the only stylesheet |
| `src/assets/` | its three images are imported only by the template's `App.tsx` |
| `public/icons.svg` | referenced only by the template's `App.tsx` |

**`public/favicon.svg` is kept**, because `index.html` references it.

### 2. The workspace entry, and the install

`FrontendCode.md` section 3 makes every frontend a workspace of `Code/package.json`,
listed by its exact folder name and never by a glob, with one lockfile,
`Code/package-lock.json`, committed, and none inside a frontend.

From `Code/`, add the entry at the end of the list, install, then pin the
packages the template of step 1, "The template", wrote:

```bash
npm pkg set "workspaces[]=<Solution>.<Frontend>"
npm install
npm install react@<Versions.md: react> react-dom@<Versions.md: react-dom> @types/node@<Versions.md: @types/node> @types/react@<Versions.md: @types/react> @types/react-dom@<Versions.md: @types/react-dom> @vitejs/plugin-react@<Versions.md: @vitejs/plugin-react> typescript@<Versions.md: typescript> vite@<Versions.md: vite> --save-exact --workspace=<Solution>.<Frontend>
```

Expect exit 0 from each. `Code/package.json` keeps every entry it had, with
`<Solution>.<Frontend>` added last, and no other member changes.

**The template's packages are pinned here, and not in step 1**, because only now
is the frontend a workspace. An install inside the frontend's folder before this
step would write a second lockfile, which `FrontendCode.md` section 3 forbids.
`--save-exact` writes each version with no range, as `Versions.md` section 2,
"The rules", states.

**Check the one lockfile**, from the repository root:

```bash
find Code -name package-lock.json -not -path '*/node_modules/*'
```

It prints `Code/package-lock.json` and nothing else. A second line is a lockfile
inside the frontend: stop and report it.

**Check the two members of the frontend's `package.json` this solution reads**,
from `Code/`:

```bash
npm pkg get name version --workspace=<Solution>.<Frontend>
```

It prints one object, under the frontend's npm name, holding
`"name": "<solution>-<frontend>"` and `"version": "0.0.0"`, both written by the
template. **The name is `FrontendCode.md` section 3's npm form**, and the
template derives it from the folder. **The version is needed**: measured, the pipeline's `npm sbom` exits 1
on any npm package in the workspace that has no `version`.

### 3. The packages

From `Code/`:

```bash
npm uninstall oxlint --workspace=<Solution>.<Frontend>
npm pkg delete scripts.lint --workspace=<Solution>.<Frontend>
npm install react-router@<Versions.md: react-router> @tanstack/react-query@<Versions.md: @tanstack/react-query> openapi-fetch@<Versions.md: openapi-fetch> tailwindcss@<Versions.md: tailwindcss> --save-exact --workspace=<Solution>.<Frontend>
npm install --save-dev @tailwindcss/vite@<Versions.md: @tailwindcss/vite> oxc-transform-react@<Versions.md: oxc-transform-react> --save-exact --workspace=<Solution>.<Frontend>
```

Expect exit 0 from each.

**The template's linter and its script go**, because `FrontendCode.md` section 2
makes ESLint the linter, and the solution-wide enforcer pass configures it at
`Code/`.

| Package | Why | Where |
|---|---|---|
| `react-router` | `FrontendCode.md` section 9 makes React Router the router. **The package is `react-router` and never `react-router-dom`**, which is a different, older package that still installs cleanly | `dependencies` |
| `@tanstack/react-query` | `FrontendCode.md` section 8 has TanStack Query own caching, retries, invalidation and loading state | `dependencies` |
| `openapi-fetch` | `FrontendCode.md` section 8 makes it the client | `dependencies` |
| `tailwindcss` | `FrontendCode.md` section 12 makes Tailwind CSS the styling library, and the stylesheet imports it | `dependencies` |
| `@tailwindcss/vite` | Tailwind's plugin for Vite, which runs only during the build | `devDependencies` |
| `oxc-transform-react` | what `@vitejs/plugin-react` needs to run the React Compiler, which step 5, "`vite.config.ts`", turns on. Measured: installed with no version, npm took the newest version inside the plugin's own range | `devDependencies` |

**The test that splits them**: what the shipped code or stylesheet imports is a
dependency, and a plugin that only runs during the build is a devDependency. The
`init` of step 7, "shadcn", adds seven more, all to `dependencies`, and they stay
there.

### 4. The TypeScript settings, and the alias

`FrontendCode.md` section 6 states that `tsconfig.app.json` sets `strict` and nine
settings, and that `@/` is the import alias for `src/`. **The template sets six of
the nine**, measured, so this
step adds `strict` and the other three, and pins `strict` rather than relying on a
default.

Replace `Code/<Solution>.<Frontend>/tsconfig.app.json` whole:

```json
{
  "compilerOptions": {
    "tsBuildInfoFile": "./node_modules/.tmp/tsconfig.app.tsbuildinfo",
    "target": "es2023",
    "lib": ["ES2023", "DOM"],
    "module": "esnext",
    "types": ["vite/client"],
    "allowArbitraryExtensions": true,
    "skipLibCheck": true,

    /* Bundler mode */
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "verbatimModuleSyntax": true,
    "moduleDetection": "force",
    "noEmit": true,
    "jsx": "react-jsx",

    /* Linting */
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "erasableSyntaxOnly": true,
    "noFallthroughCasesInSwitch": true,
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "exactOptionalPropertyTypes": true,
    "noImplicitOverride": true,

    "paths": {
      "@/*": ["./src/*"]
    }
  },
  "include": ["src"]
}
```

**It is the template's file with four settings and the alias added**, and
nothing the template wrote is changed.

Replace `Code/<Solution>.<Frontend>/tsconfig.json` whole:

```json
{
  "files": [],
  "references": [
    { "path": "./tsconfig.app.json" },
    { "path": "./tsconfig.node.json" }
  ],
  "compilerOptions": {
    "paths": {
      "@/*": ["./src/*"]
    }
  }
}
```

**The alias is declared here as well, because shadcn reads it only here.**
Measured: with the alias in
`tsconfig.app.json` and `vite.config.ts` only, shadcn's `init` passed its alias
check and then wrote into a folder literally named `@` at the frontend's root,
outside `src/`. With this file carrying it too, it wrote under `src/`. **The three
declarations always agree.** Step 5, "`vite.config.ts`", writes the third.

`tsconfig.node.json` is the template's, unchanged. It covers `vite.config.ts`
only.

### 5. `vite.config.ts`

**Find `<FrontendPort>` first**, from the repository root:

```bash
port=5173; while grep -rqsE --include=launchSettings.json --include=vite.config.ts --exclude-dir=node_modules --exclude-dir=bin --exclude-dir=obj "(:|port: )${port}([^0-9]|$)" Code; do port=$((port + 1)); done; echo "$port"
```

It prints `<FrontendPort>`: the first port from 5173 upward that no API's or
host's `Properties/launchSettings.json` under `Code/` names in an address, and no
other frontend's `vite.config.ts` names as its port. This frontend's own
`vite.config.ts` names none yet, since the template's names no port.

Replace `Code/<Solution>.<Frontend>/vite.config.ts` whole:

```ts
import { fileURLToPath } from 'node:url';
import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react({ compiler: { reportDiagnostics: true } }), tailwindcss()],
  server: { port: <FrontendPort>, strictPort: true },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
});
```

**The React Compiler is on**, because `FrontendCode.md` section 16 bans
`useMemo`, `useCallback` and `React.memo` written by hand, so the compiler is what
memoises. **`reportDiagnostics` is on beside it**, because the compiler skips a
component it cannot handle and by default says nothing at all. Measured: with it on, the build and the
development server each name the file, the line and the words "React Compiler
skipped optimizing this component or hook", and the build still exits 0.
`logDiagnostics` reports the same and is deprecated.

**The alias is the third declaration**, and the one Vite reads. Without it,
`tsc -b` and the build both pass and the development server answers HTTP 500 on
every aliased import.

**Tailwind's plugin is added**, which the stylesheet of step 6, "Tailwind's
stylesheet", needs.

**The development server's port is fixed, and it never moves. Ruled by Francois
on 2026-10-11.** `Security.md` section 10 has an API let a page at another origin
read its responses only when that origin is on the API's list, so each API this
frontend calls lists the frontend's development address, which step 17, "Each
API's allowed origin", writes. Left to itself, Vite answers at 5173 unless that
port is taken, and then moves to the next free one, at an address no list holds.
**`strictPort` is what stops it moving**: measured, with the port taken, `npm run dev` printed "Error: Port
5173 is already in use" and exited 1. **The port is the first one no other
project uses**, because the Web SDK's template picks an API's ports at random and
two frontends cannot share one. Measured as well: with the port
free, the page at `http://localhost:<FrontendPort>` read each API's health through
its development list, and the same page at another origin was refused.

**The default export is required by Vite**, which reads the configuration from
this file's default export and from nothing else.

### 6. Tailwind's stylesheet

Replace `Code/<Solution>.<Frontend>/src/index.css` whole:

```css
@import "tailwindcss";
```

**This replaces the template's stylesheet**, and it is what shadcn's `init` looks
for in step 7, "shadcn", before it will run. `FrontendCode.md` section 12 makes
this file the only stylesheet in a frontend.

### 7. shadcn

`FrontendCode.md` section 12 has a component primitive taken with shadcn, built on
Radix, with `components.json` at the frontend's root. **This uses the shadcn CLI
at the version of `Versions.md`'s row "`shadcn`, the command"**, because step 8,
"The stylesheet's tokens", edits what it writes.

From the repository root:

```bash
npx --yes shadcn@<Versions.md: shadcn, the command> init --base radix --preset nova --no-rtl --cwd Code/<Solution>.<Frontend>
```

Expect exit 0, ending "Project initialization completed." **It asks nothing.**
It writes `components.json`, writes `src/lib/utils.ts`, rewrites `src/index.css`,
and installs seven packages into the frontend's `dependencies`: `radix-ui`, `cn`,
`class-variance-authority`, `lucide-react`, `tw-animate-css`,
`@fontsource-variable/geist` and `shadcn`. **It copies no component.**

**Then pin the seven packages**, from `Code/`:

```bash
npm install radix-ui@<Versions.md: radix-ui> cn@<Versions.md: cn> class-variance-authority@<Versions.md: class-variance-authority> lucide-react@<Versions.md: lucide-react> tw-animate-css@<Versions.md: tw-animate-css> @fontsource-variable/geist@<Versions.md: @fontsource-variable/geist> shadcn@<Versions.md: shadcn> --save-exact --workspace=<Solution>.<Frontend>
```

Expect exit 0. The `shadcn` package takes its own row in `Versions.md`, which is
not the row of the command above.

**`--base radix --preset nova` is how this version is told the style.** Measured: `--preset radix-nova` is
refused, and the CLI's own `--defaults` chooses Base UI, which is not Radix.

**Check `components.json`.** It holds `"style": "radix-nova"`, `"rsc": false`,
`"tsx": true`, `"rtl": false`, a `tailwind` block with `"config": ""`,
`"css": "src/index.css"` and `"cssVariables": true`, and these aliases:
`"components": "@/components"`, `"ui": "@/components/ui"`, `"lib": "@/lib"`,
`"hooks": "@/hooks"` and `"utils": "@/lib/utils"`. Each of those is what
`FrontendCode.md` section 12 fixes, or the value ruled for it. **`baseColor`,
`iconLibrary`, `menuColor` and `menuAccent` keep the values the CLI chose**,
being `neutral`, `lucide`, `default` and `subtle`: they decide an appearance, and
the design changes them later. Where any fixed value differs, stop and report it.

**Delete `src/lib/utils.ts`:**

```bash
rm Code/<Solution>.<Frontend>/src/lib/utils.ts
```

It holds only `export { cn } from "cn"`, every component shadcn copies imports `cn`
from the package `cn` instead, and it was measured that a later
`shadcn add` writes a working component without it and does not write it again.
Under `FrontendCode.md` section 4, a file exporting `cn` would be `cn.ts`.

**The seven packages stay in `dependencies`, `shadcn` included.** Measured: the stylesheet `init` writes
imports `shadcn/tailwind.css`, and rules from that file are in the built
frontend. **The `shadcn` package also brings a finding to `npm audit`**, which step
26, "Verify", names. Measured: no version of any package in its
path is free of it.

**Check the one lockfile again**, as step 2, "The workspace entry, and the
install", did. It still prints only `Code/package-lock.json`.

### 8. The stylesheet's tokens

`FrontendCode.md` section 12 states that a token is a name declared in `@theme`,
that a token's value is declared in `:root` under the token's name with the
namespace removed, and that its `@theme` name points at it. **`init` writes nine
lines that do not**, measured: the font token carries its value
straight in `@theme`, the heading font points at another `@theme` name, and the
seven radius tokens are calculations in `@theme`. This step moves each into the
shape. **No value changes**, so nothing looks different.

In `Code/<Solution>.<Frontend>/src/index.css`, inside `@theme inline`, replace
the line on the left with the line on the right, one exact edit for each row,
each line with the file's own four-space indent:

| `init` wrote | Write instead |
|---|---|
| `    --font-heading: var(--font-sans);` | `    --font-heading: var(--heading);` |
| `    --font-sans: 'Geist Variable', sans-serif;` | `    --font-sans: var(--sans);` |
| `    --radius-sm: calc(var(--radius) * 0.6);` | `    --radius-sm: var(--sm);` |
| `    --radius-md: calc(var(--radius) * 0.8);` | `    --radius-md: var(--md);` |
| `    --radius-lg: var(--radius);` | `    --radius-lg: var(--lg);` |
| `    --radius-xl: calc(var(--radius) * 1.4);` | `    --radius-xl: var(--xl);` |
| `    --radius-2xl: calc(var(--radius) * 1.8);` | `    --radius-2xl: var(--2xl);` |
| `    --radius-3xl: calc(var(--radius) * 2.2);` | `    --radius-3xl: var(--3xl);` |
| `    --radius-4xl: calc(var(--radius) * 2.6);` | `    --radius-4xl: var(--4xl);` |

Then, after the line `    --sidebar-ring: oklch(0.708 0 0);`, the last line
inside `:root`, add these nine:

```css
    --heading: var(--sans);
    --sans: 'Geist Variable', sans-serif;
    --sm: calc(var(--radius) * 0.6);
    --md: calc(var(--radius) * 0.8);
    --lg: var(--radius);
    --xl: calc(var(--radius) * 1.4);
    --2xl: calc(var(--radius) * 1.8);
    --3xl: calc(var(--radius) * 2.2);
    --4xl: calc(var(--radius) * 2.6);
```

**Each line on the left, and the line `    --sidebar-ring: oklch(0.708 0 0);`,
occurs exactly once in the file `init` writes.** Measured, with each line's four-space indent. A later block in
the file holds `    --sidebar-ring: oklch(0.556 0 0);`, which is a different
line.

**`--radius` stays in `:root` as `init` wrote it.** It is the value the radius
tokens are calculated from, and no `@theme` name points at it. **Nothing in
`shadcn/tailwind.css` reads any name this step changes**.

**Check every `@theme` line now has the shape:**

```bash
awk '/@theme inline/{f=1;next} /^}/{f=0} f' Code/<Solution>.<Frontend>/src/index.css \
  | sed -E 's/^    --(color|font|radius)-([a-z0-9-]+): var\(--([a-z0-9-]+)\);$/\2 \3/' \
  | awk '$1 != $2'
```

**Expect no output.** Each line inside `@theme inline` becomes the token's name
without its namespace beside the `:root` name it points at, and a line is printed
only where the two differ or the line has another shape. A line printed is a line
step 8, "The stylesheet's tokens", missed: correct it.

### 9. The settings

`FrontendCode.md` section 7 states that a setting the frontend needs is read from
`public/config.json`, once, before the application renders, and typed. The
setting a frontend has on day one is each API's base address, which `openapi-fetch`
needs. **Each address is a whole address on another origin, and never a relative
path**, because each frontend is served at its own address, separate from its
APIs'.

**Read each API's development address.** For each API:

```bash
node -p "require('./Code/<Solution>.<Name>Api/Properties/launchSettings.json').profiles.https.applicationUrl.split(';').find((address) => address.startsWith('https://'))"
```

It prints the API's `https://` address from the `https` profile the Web SDK
template writes, being `https://localhost:` and the port that template chose.
That is `<address>` for that API.

Write `Code/<Solution>.<Frontend>/public/config.json`, one member per API:

```json
{
  "apiBaseAddresses": {
    "<nameApi>": "<address>",
    "<otherNameApi>": "<address>"
  }
}
```

**One nested object, keyed by each API's own name in camelCase.** Measured: an object typed by the exact API
names reads each address as a `string` under `noUncheckedIndexedAccess`, and
asking it for an API it does not hold fails to compile, where a
`Record<string, string>` makes every read `string | undefined`.
`apiBaseAddresses` is the plural of the name this setting had while a frontend
called one API. **Nothing in this file is secret**, which `FrontendCode.md` section
7 requires, since everything in a built frontend can be downloaded.

Write `Code/<Solution>.<Frontend>/src/lib/config.ts`, with one member per API:

```ts
export type Config = {
  apiBaseAddresses: {
    <nameApi>: string;
    <otherNameApi>: string;
  };
};

async function readConfig() {
  const response = await fetch('/config.json');
  return (await response.json()) as Config;
}

export const config = await readConfig();
```

**It is read once, before anything that uses it.** `config` is the module's
top-level `await`, so any module importing it waits until the file has been read,
and a module that creates a client when it loads, as those of step 10, "The
generator, and each API's client", do, is given its address. **`fetch` is called
directly here and nowhere else**, which is the one exception `FrontendCode.md`
section 16 makes. **The cast is forced**: the DOM's `Response.json()` returns
`any`, and the type is what stops every name read off the settings compiling
whether or not it exists.

**The file is named for `config`**, the thing it exports, which
`FrontendCode.md` section 4 states, and `Config` is the type that supports it.
`src/lib/` holds it because it is the one place for code that serves the whole
frontend.

### 10. The generator, and each API's client

`FrontendCode.md` section 8 states that a frontend's API types are generated by
`openapi-typescript`, once per API, from that API's frozen fragments, into
`src/api/<name>-api/types.ts`, and committed. The contract type generator at
`Code/Tools/ContractTypes/` does it.

**Name the frontend in each API's pair**, for each API, from the repository root:

```bash
node -e "const fs = require('node:fs'); const path = 'Code/Tools/ContractTypes/contract-types.config.json'; const config = JSON.parse(fs.readFileSync(path, 'utf8')); const pair = config.pairs.find((pair) => pair.api === '<Name>Api' && pair.frontend === null); if (pair) pair.frontend = 'Code/<Solution>.<Frontend>'; else config.pairs.push({ api: '<Name>Api', frontend: 'Code/<Solution>.<Frontend>' }); fs.writeFileSync(path, JSON.stringify(config, null, 2) + '\n');"
```

**Expect exit 0.** Where the API's only pair carries `"frontend": null`, which is
how its recipe leaves an API no frontend calls, the command sets that pair's
`frontend` to `"Code/<Solution>.<Frontend>"`. Where the API's pairs already name
another frontend, it adds a pair after the last one. No other pair changes, and
the command writes the file back with two spaces of indent and a final line
break.

A pair names one API and at most one frontend, so a frontend calling two APIs is
named by two pairs, and an API two frontends call is named by two pairs.

**Run the generator for this frontend's APIs**, from `Code/`:

```bash
npm run generate-contract-types --workspace=Tools/ContractTypes -- <Name>Api <OtherName>Api
```

Expect one line per pair naming an API given, each reading `contract-types: wrote`,
then the path of the `types.ts` it wrote, then the number of routes, and exit 0.
This frontend's lines end in `src/api/<name>-api/types.ts`, one for each of its
APIs. Each line names the frontend it wrote into, so where an API's pairs
also name another frontend, that frontend's file for the API is written again
from the same fragments. **Anything else is a real failure**: the generator's own
message says what it found and what to do.

**Set the frontend's build to regenerate its types before its type check**, from
`Code/`:

```bash
npm pkg set scripts.build="(cd .. && npm run generate-contract-types --workspace=Tools/ContractTypes -- <Name>Api <OtherName>Api) && tsc -b && vite build" --workspace=<Solution>.<Frontend>
```

**The regeneration has to come first.** `FrontendCode.md` section 17 makes the
build the enforcer of the rule that a generated file is never edited: code written
against an edit fails at `tsc` only because the file was written again before it.
**The generator runs from `Code/`**, in a subshell, because an npm script runs
from its own workspace's folder and the generator's `--workspace` path is
relative to `Code/`. Checked against npm 11.12.1: the same command run from the
frontend's folder fails with "No workspaces found".

Write `Code/<Solution>.<Frontend>/src/api/<name>-api/apiClient.ts`, once per API,
beside that API's `types.ts`:

```ts
import createClient from 'openapi-fetch';
import type { paths } from '@/api/<name>-api/types';
import { config } from '@/lib/config';

export const apiClient = createClient<paths>({
  baseUrl: config.apiBaseAddresses.<nameApi>,
});
```

**One client per API, typed by that API's own `paths` and exporting `apiClient`**,
which `FrontendCode.md` section 8 states. **The file name and the export are the
same in every API's folder**, so a file that calls two APIs aliases one of its two
imports.

**The client is given its base address and nothing else.** `FrontendCode.md`
section 16 allows `fetch` to be called directly only by the code that reads the
settings, so no client is given a `fetch` of its own.

### 11. The failure declaration

`FrontendCode.md` section 8 states that a query and a mutation throw the failure
when the call did not succeed, and that a frontend declares the type of that
failure once, for the whole frontend.

Write `Code/<Solution>.<Frontend>/src/api/query.d.ts`, naming the first API's
folder:

```ts
import type { components } from '@/api/<name>-api/types';

declare module '@tanstack/react-query' {
  interface Register {
    defaultError:
      | components['schemas']['ProblemDetails']
      | components['schemas']['HttpValidationProblemDetails'];
  }
}
```

**It names both problem schemas, as a union.** `BackendCode.md` section 7 declares
every failure other than a 400 with one and every 400 with the other, and only the
validation one carries the per-field map, which `FrontendCode.md` section 11 reads.
**It names one API's generated file, and serves every API.** Every API in the
solution takes both schemas from the same `<Solution>.ApiCommon`, so they are one
type, and measured, one
declaration naming one API's schemas typechecks against two. **Nothing imports
it**: being in the program is enough. **Until an API freezes an operation that
declares a failure, both names are absent from the generated file**, and the
failure type is `never`. Checked against TypeScript 6.0.3: the project still
typechecks.

### 12. The application frame and the error boundary

`FrontendCode.md` section 4 places both in `src/components/layout/` and names them,
and `FrontendCode.md` section 9 states what each does.

Write `Code/<Solution>.<Frontend>/src/components/layout/AppFrame.tsx`:

```tsx
import { useEffect, useRef, type ReactNode } from 'react';
import { Outlet, useLocation } from 'react-router';

export type AppFrameProps = {
  children?: ReactNode;
};

export function AppFrame({ children }: AppFrameProps) {
  const { pathname } = useLocation();
  const shownPathname = useRef(pathname);

  useEffect(() => {
    if (shownPathname.current === pathname) {
      return;
    }

    shownPathname.current = pathname;

    const heading = document.querySelector('h1');

    if (heading !== null) {
      heading.tabIndex = -1;
      heading.focus();
    }
  }, [pathname]);

  return children ?? <Outlet />;
}
```

**It moves focus to the page's heading when the route changes**, which
`FrontendCode.md` section 13 states, and no page writes that code. Measured
2026-09-08: with no focus move, focus ends on `<body>` and a person using a screen
reader is told nothing changed; a plain `<h1>` cannot take focus until it carries
`tabIndex={-1}`, so the frame sets it and no page carries the attribute; and the
trigger has to be the address, because a page component serving two addresses
does not mount again.

**The trigger is the path, and only a change of it.** A change of only the search
parameters is a filter, a sort, a page or a tab, which `FrontendCode.md` section
10 keeps in the address, and taking focus from the control a person just used
would be a change they did not ask for. **The first load is not a change of
route**, so focus stays where the browser puts it. The path last shown is held, so
the development build's second run of every effect moves nothing either.

**The heading is looked for, because a page can be without one for a moment.** A
page that reads a query returns while the query is pending, which
`FrontendCode.md` section 8 states, and the document is read in an effect, which
`FrontendCode.md` section 14 requires.

**On day one the frame draws nothing of its own.** What surrounds the pages is the
design's, and it is added to this file. **`children` exists for the error
boundary**, which renders the frame around its message in place of the page.

Write `Code/<Solution>.<Frontend>/src/components/layout/AppErrorBoundary.tsx`:

```tsx
import { AppFrame } from '@/components/layout/AppFrame';

export function AppErrorBoundary() {
  return (
    <AppFrame>
      <main>
        <title>Something went wrong</title>
        <h1>Something went wrong</h1>
        <p>This page could not be shown.</p>
      </main>
    </AppFrame>
  );
}
```

**It shows a fixed message, carrying neither the exception's own message nor its
stack**, which `FrontendCode.md` section 9 states. **It renders the frame
itself**, because React Router renders a route's boundary in place of that
route's own component, so a boundary that did not would take the frame away with
the page. **It is one component and nothing more**: `FrontendCode.md` section 9
allows one boundary, so nothing else ever renders the message.

**The message stands in for a page, so it carries what a page carries**: its own
`<title>` and one `<h1>`, both its name, which `FrontendCode.md` section 13
states. Its words say what happened without saying why, because they are shown for
an exception nobody expected and for an address no route matches alike.

### 13. The route table

Write `Code/<Solution>.<Frontend>/src/router.ts`:

```ts
import { createBrowserRouter } from 'react-router';
import { AppErrorBoundary } from '@/components/layout/AppErrorBoundary';
import { AppFrame } from '@/components/layout/AppFrame';

export const router = createBrowserRouter([
  {
    Component: AppFrame,
    ErrorBoundary: AppErrorBoundary,
    children: [],
  },
]);
```

**One file, `src/router.ts`, exporting `router`, built with
`createBrowserRouter`**, which `FrontendCode.md` section 9 states. **The route
that draws the frame carries the one error boundary**, and every page's route
goes inside it, in `children`. **It holds no page route on day one**, because a
placeholder page would be code nothing asked for, which `FrontendCode.md` section
15 refuses. Checked against React Router 8.4.0: a route table holding only this
route matches no address, so until the first page route exists every address
shows the boundary's message.

### 14. `App.tsx`

Replace `Code/<Solution>.<Frontend>/src/App.tsx` whole:

```tsx
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router';
import { router } from '@/router';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { throwOnError: false },
    mutations: { throwOnError: false },
  },
});

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  );
}
```

**It replaces the template's whole file**: the starter page, and its default
export, which `FrontendCode.md` section 16 bans. **`App.tsx` renders the route
table**, which `FrontendCode.md` section 9 states.

**It renders TanStack Query's provider around the router**, because no query runs
without a query client above it. Measured 2026-09-08: a component calling
`useQuery` with no provider throws on its first render, and neither `tsc` nor the
lint set reports it. **Nobody imports the query client**: a mutation reads it from
the provider.

**The query client sets `throwOnError` off and nothing else.** `FrontendCode.md`
section 8 shows a query's failure in the component that reads it, and
`FrontendCode.md` section 9 has the error boundary never catch an API failure, so
a failure is never thrown to the boundary. It is TanStack Query's own default, and
it is written so the setting is pinned rather than assumed. **No other option is
set**, because nothing has asked for a stale time or a retry count, and
`FrontendCode.md` section 15 writes no setting nothing asked for.

### 15. `main.tsx`, and the document's language

Replace `Code/<Solution>.<Frontend>/src/main.tsx` whole:

```tsx
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from '@/App';
import '@/index.css';

const root = document.getElementById('root');

if (root === null) {
  throw new Error('index.html holds no element with the id root.');
}

createRoot(root).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
```

**The template's non-null assertion is replaced by a check**, because
`FrontendCode.md` section 16 bans the assertion, and its lint rule would fail the
build on this file. **`App` is imported by name**, since step 14, "`App.tsx`",
removed the default export. Every import uses the `@/` alias, as every other file
does.

**Confirm the document's language.** `FrontendCode.md` section 13 states that the
document is given a language, set on the `<html>` element in `index.html`, and
nothing on the build sees it missing:

```bash
grep -c '<html lang="en">' Code/<Solution>.<Frontend>/index.html
```

It prints `1`: the template sets it, and the file is otherwise the template's.
`0` means it is missing: stop and report it.

### 16. The infrastructure inventory

`Security.md` section 11 keeps every part of the product and every connection
between parts in `Docs/InfrastructureInventory.md`. **This step adds the frontend
and each connection it wires, and fills in nothing it does not know.** Which
environments hold the frontend, the protection level of the data it carries,
whether the internet can reach it, the port each connection uses and how the
frontend proves who it is all depend on features and a deployment not yet
planned, so each of those cells stays empty.

**Each row goes directly after its table's separator line, so the newest row comes
first**, as `Api.md`'s step 15, "The infrastructure inventory", states. Ruled by
Francois on 2026-10-14. **The separator under `## Parts` is also the start and
the end of the separator under `## Connections`**, so an Edit whose text is
`|---|---|---|---|---|` alone matches twice and is refused. The edit's text is
that line with the line break before it and the line break after it, which
occurs once.

In `Docs/InfrastructureInventory.md`, after the line `|---|---|---|---|---|`, add:

```markdown
| `<Solution>.<Frontend>` | A frontend: a single-page application that a browser loads from its own address and runs | | | |
```

Then, after the line `|---|---|---|---|---|---|---|`, add one row per API:

```markdown
| `<Solution>.<Frontend>` | `<Solution>.<Name>Api` | | HTTPS | | | |
```

**The protocol is known**: the client reaches the API at its `https://` address,
and `Security.md` section 11 encrypts every connection. **The last column stays
empty**, because the address comes from the frontend's own settings and not from
data received from outside.

### 17. Each API's allowed origin

`Security.md` section 10 has an API let a page at another origin read its
responses only when that origin is on the API's list, and `BackendCode.md`
section 9 has the API read the list from its own settings. Each API keeps its
development list as `AllowedOrigins`, under `<Solution>:Security`, in its
`appsettings.Development.json`, which its recipe writes empty. **This step adds
the frontend's development address to the list of each API it calls. Ruled by
Francois on 2026-10-11.**

For each API, from the repository root:

```bash
node -e "const fs = require('node:fs'); const path = 'Code/<Solution>.<Name>Api/appsettings.Development.json'; const settings = JSON.parse(fs.readFileSync(path, 'utf8')); const origins = settings['<Solution>'].Security.AllowedOrigins; if (!origins.includes('http://localhost:<FrontendPort>')) origins.push('http://localhost:<FrontendPort>'); fs.writeFileSync(path, JSON.stringify(settings, null, 2) + '\n');"
```

**Expect exit 0**, and the address added at the end of the list, with every other
member of the file as it was. The API's recipe writes the file with two spaces of
indent and a final line break, which is how this command writes it back. **Where an
earlier run already added the address, nothing is added again.**

**The address is `http://localhost:<FrontendPort>`**, the address the development
server answers at. Measured: Vite
on `localhost` listens on `[::1]` only, the page at `http://localhost:<FrontendPort>`
read 200 from each API's health, and the same page at `http://[::1]:<FrontendPort>`,
an origin not on the list, was refused. **Only the development list is written.**
Each deployed environment sets its own list by environment variable, which the
closing report says.

### 18. The test packages, and the `test` script

`Testing.md` section 6 tests a frontend with Vitest and React Testing Library, and
MSW for the network, and `Testing.md` section 9 scans each component with
`vitest-axe`.

From `Code/`:

```bash
npm install --save-dev vitest@<Versions.md: vitest> jsdom@<Versions.md: jsdom> msw@<Versions.md: msw> @testing-library/react@<Versions.md: @testing-library/react> @testing-library/user-event@<Versions.md: @testing-library/user-event> @testing-library/jest-dom@<Versions.md: @testing-library/jest-dom> vitest-axe@<Versions.md: vitest-axe> --save-exact --workspace=<Solution>.<Frontend>
npm pkg set scripts.test="vitest run" --workspace=<Solution>.<Frontend>
```

Expect exit 0 from each.

| Package | Why |
|---|---|
| `vitest` | `Testing.md` section 6 makes it the test runner |
| `jsdom` | the browser document a test renders into, which step 19, "The `test` block in `vite.config.ts`", names. Vitest loads it by that name and does not install it |
| `msw` | `Testing.md` section 6 has the network answered by MSW. **MSW 3 is inside the range `^2.4.9 \|\| ^3.0.0` that Vitest 5's `@vitest/mocker` declares for it.** Measured: with MSW 3.0.3 beside Vitest 5.0.4, `npm ls --all` and `npm sbom` each exit 0, so the pipeline's `npm sbom` step passes |
| `@testing-library/react` | `Testing.md` section 6 renders a component with React Testing Library. npm installs `@testing-library/dom` beside it, as a package it needs, and nothing imports that directly |
| `@testing-library/user-event` | `Testing.md` section 6 writes `userEvent` and never `fireEvent`, and the test of the application frame clicks a link with it |
| `@testing-library/jest-dom` | the Testing Library DOM matchers, which `Testing.md` section 6 names beside Vitest's `expect` |
| `vitest-axe` | `Testing.md` section 9 scans every component with it. It brings `axe-core` with it, and nothing imports `axe-core` directly |

**Every one is a devDependency**, by the test that step 3, "The packages", states:
the shipped code imports none of them. **Each is installed at its version in
`Versions.md`, with `--save-exact`**, as every package this recipe installs.

**This is the last step that adds an npm package, so check every direct package
now**, from `Code/`:

```bash
npm pkg get dependencies devDependencies --workspace=<Solution>.<Frontend>
```

Each package it prints has a row in `Versions.md` section 4, "Installed packages",
whose "Where it is fixed" is `Code/<Solution>.<Frontend>/package.json`, and its
version is that row's version, with no `^`, `~` or other range. **Where a package
has no row, or another version, or a range, stop and report it.**

**The `test` script is `vitest run`**, which runs every test once and exits. Plain
`vitest` watches for changes and never exits, so the pipeline's `npm test` step
would not end.

### 19. The `test` block in `vite.config.ts`

`Testing.md` section 6 names the setup file in the `test` block of
`vite.config.ts`. Replace `Code/<Solution>.<Frontend>/vite.config.ts` whole again:

```ts
import { fileURLToPath } from 'node:url';
import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  plugins: [react({ compiler: { reportDiagnostics: true } }), tailwindcss()],
  server: { port: <FrontendPort>, strictPort: true },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
});
```

**Two things change from step 5, "`vite.config.ts`".** `defineConfig` comes from
`vitest/config`, which types the `test` block, and the `test` block is added.
Measured: with both, `tsc -b`
and `vite build` each exit 0. **It is written here and not in step 5, "`vite.config.ts`"**, because
`vitest/config` is Vitest's, which step 18, "The test packages, and the `test`
script", installs.

**The environment is jsdom**, because a test renders a component into a browser
document. **`setupFiles` names `src/test/setup.ts`**, which `Testing.md` section 6
requires. **No other option is set**, because nothing has asked for one, and
`FrontendCode.md` section 15 writes no setting nothing asked for. So Vitest's
globals stay off, and every test imports `describe`, `it` and `expect` by name.

### 20. The MSW server

`Testing.md` section 6 makes `src/test/server.ts` export `server`, made with
`setupServer` from `msw/node`, holding the default request handlers and exporting
nothing else.

Write `Code/<Solution>.<Frontend>/src/test/server.ts`, with one member per API:

```ts
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import type { Config } from '@/lib/config';

export const server = setupServer(
  http.get('*/config.json', () =>
    HttpResponse.json({
      apiBaseAddresses: {
        <nameApi>: 'https://<name>-api.test',
        <otherNameApi>: 'https://<other-name>-api.test',
      },
    } satisfies Config),
  ),
);
```

**On day one it holds one handler, for the settings file.** `src/lib/config.ts`
fetches `/config.json` when a module importing it loads, so every test of
something that calls an API needs that answer before anything else. Measured: with the server started by the
setup file, MSW answers that relative address. **Nothing on day one calls an
API**, so no handler for an API's operation is written; the increment that first
calls one adds its handler here.

**The address is `*/config.json`**, the shape `Testing.md` section 6 gives every
handler's address, `*` followed by the path, although `config.json` is not a
contract route. **Each API's address is whole, `https://<name>-api.test`**,
because the measurement of 2026-09-15 found `*`
matching no request from a client with no base address. **`.test` is the name RFC
6761 reserves for testing**, so no address reaches a real host, and `Testing.md`
section 2 makes test data synthetic. **The body is typed with `satisfies
Config`**, so a member the settings type gains or loses fails the type check here.
`import type` is removed when the file is compiled, so importing `Config` does not
run the settings reader.

### 21. The Vitest setup file

`Testing.md` section 6 states what surrounds every test: a handler a test replaces
is put back before the next test starts, a request that no handler answers fails
the test, and every store starts each test from its initial state. `Testing.md`
section 2 has the setup file export nothing.

Write `Code/<Solution>.<Frontend>/src/test/setup.ts`:

```ts
import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, expect } from 'vitest';
import * as axeMatchers from 'vitest-axe/matchers';
import type { AxeMatchers } from 'vitest-axe/matchers';
import { server } from '@/test/server';

declare module 'vitest' {
  interface Matchers<R extends void | Promise<void> = void | Promise<void>, T = unknown>
    extends AxeMatchers {}
}

expect.extend(axeMatchers);

const unhandledRequests: string[] = [];

server.listen({
  onUnhandledFrame: async ({ frame }) => {
    unhandledRequests.push(await frame.getUnhandledMessage());
  },
});

afterEach(() => {
  cleanup();
  server.resetHandlers();

  const requests = unhandledRequests.splice(0);

  if (requests.length > 0) {
    throw new Error(`No request handler answered: ${requests.join(', ')}`);
  }
});
```

**The server starts when the file loads, and never in a `beforeAll`.** Each API's
`apiClient.ts` creates its client when its module loads, and `src/lib/config.ts`
fetches its settings the same way, both before any `beforeAll` runs. Measured: started here, MSW answered both,
and it still did on MSW 3.0.3 under Vitest 5.0.4; in a `beforeAll`, a test file importing the settings reader did
not load.

**A request no handler answers fails the test that made it.** MSW calls
`onUnhandledFrame` with one object holding the frame, whose data the callback's
type gives as `unknown`. The callback keeps the message MSW writes for the frame,
which names the method and the address, and `afterEach` throws, naming every
request kept.
Measured: the test fails with
"No request handler answered: intercepted a request without a matching request
handler:", then the request's method and address, then MSW's advice. MSW's own
`'error'` does not fail the test, because the component shows its
failed state and a test waiting for that state passes. **The option is
`onUnhandledFrame`, MSW 3's name.** MSW 2's name, `onUnhandledRequest`, is refused
by the type check with TS2353, while in a test run MSW only warns and the test
passes. So only the type check, which step 26, "Verify", runs,
catches a wrong name.

**`server.resetHandlers()` puts back every handler a test replaced**. **`cleanup()` removes each test's render**, because Testing Library removes it
by itself only where Vitest's globals are on, and step 19, "The `test` block in
`vite.config.ts`", leaves them off.

**The two sets of matchers are registered here.** The import of
`@testing-library/jest-dom/vitest` registers the DOM matchers and their types.
`expect.extend` registers `vitest-axe`'s `toHaveNoViolations`, and the declaration
above gives it a type. Measured: with no declaration, `tsc`
fails on the matcher, and the declaration has to repeat Vitest 5's own two type
parameters on `Matchers` exactly, although it uses neither. `Testing.md` section 6
names every test support file and no declaration file, so the declaration sits
beside the call that registers the matcher.

**No store is reset on day one**, because no store exists, and `FrontendCode.md`
section 15 writes nothing for a caller that does not exist. The increment that
writes the first store adds its reset to this file.

### 22. `renderWithProviders`

`Testing.md` section 6 renders every test's subject with `renderWithProviders`,
inside the providers the application renders it inside, being the query client's
provider and the router, with a query client of its own for each test.

Write `Code/<Solution>.<Frontend>/src/test/renderWithProviders.tsx`:

```tsx
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import type { ReactElement } from 'react';
import { createMemoryRouter, RouterProvider } from 'react-router';

export function renderWithProviders(element: ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { throwOnError: false, retry: false },
      mutations: { throwOnError: false, retry: false },
    },
  });
  const router = createMemoryRouter([{ path: '*', element }]);

  return render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}
```

**It takes the element it renders and nothing else**, because `FrontendCode.md`
section 15 writes no parameter nobody sets. It returns what Testing Library's
`render` returns.

**Each call creates a new query client**, so no two tests share one. **It carries
`App.tsx`'s options, with retries off**: a failed request in a test is shown at
once rather than after TanStack Query's retries, and both settings are written so
they are pinned rather than assumed, as step 14, "`App.tsx`", pins `throwOnError`.

**The router is a memory router whose one route matches every path**, so the
element renders at whatever address a test navigates to. A browser router would
read and change jsdom's own address, which every test in a file shares.

### 23. The accessibility file, and `axe`

`Testing.md` section 9 keeps each frontend's ruleset and exclusion list in
`src/test/accessibility.json`, read by the component scan and by the acceptance
scan, and has the component scan use the `axe` that `src/test/axe.ts` exports.

Write `Code/<Solution>.<Frontend>/src/test/accessibility.json`:

```json
{
  "ruleset": ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"],
  "exclusions": []
}
```

**The ruleset is WCAG 2.2 AA**, which `Testing.md` section 9 states, as the five
tags `axe-core` gives its rules up to that level. Measured: `axe-core` 4.14.0 tags one rule `wcag22aa` and none
`wcag22a`. **The exclusion list is empty on day one.** An entry, added by the
increment that needs it, holds `"ruleId"`, `"selector"` and `"reason"`, which are
`Testing.md` section 9's rule id, CSS selector and written reason.

Write `Code/<Solution>.<Frontend>/src/test/axe.ts`:

```ts
import { configureAxe } from 'vitest-axe';
import accessibility from '@/test/accessibility.json';

const exclusions: { ruleId: string; selector: string }[] = accessibility.exclusions;

const scan = configureAxe({
  runOnly: { type: 'tag', values: accessibility.ruleset },
  elementRef: true,
});

export async function axe(container: Element) {
  const results = await scan(container);

  return {
    ...results,
    violations: results.violations
      .map((violation) => ({
        ...violation,
        nodes: violation.nodes.filter(
          (node) =>
            !exclusions.some(
              (exclusion) =>
                exclusion.ruleId === violation.id && node.element?.matches(exclusion.selector) === true,
            ),
        ),
      }))
      .filter((violation) => violation.nodes.length > 0),
  };
}
```

**`axe` is made with `configureAxe` from the accessibility file**, which
`Testing.md` section 6 states, so the ruleset is configured in one place and a test
cannot quietly weaken it, which `Testing.md` section 9 requires. **A node is
dropped only where an entry's rule id matches and its element matches the entry's
selector**, and a violation left with no node is dropped with it, which
`Testing.md` section 9 states. **`elementRef: true` is what gives each node its
element.** Measured: with it, an entry naming `image-alt` and
one image's selector dropped that image's violation, and a second image the
selector does not name still failed.

**`exclusions` is given its type**, because an empty list in a JSON file is typed
as a list that can hold nothing, and an entry's members could not be read from it.

### 24. The tests of the application frame and the error boundary

`Testing.md` section 6 tests a component rendered, and `Testing.md` section 9
puts one scan in every test file. **The skill writes a test file for each of the
two components it writes**, proving what `FrontendCode.md` sections 9 and 13
require of them, so `npm test` runs real tests from day one.

Write `Code/<Solution>.<Frontend>/src/components/layout/AppFrame.test.tsx`:

```tsx
import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { Link } from 'react-router';
import { AppFrame } from '@/components/layout/AppFrame';
import { axe } from '@/test/axe';
import { renderWithProviders } from '@/test/renderWithProviders';

describe('AppFrame', () => {
  describe('when the path changes', () => {
    it('moves focus to the page heading', async () => {
      const user = userEvent.setup();
      renderWithProviders(
        <AppFrame>
          <main>
            <h1>A page</h1>
            <Link to="/another-page">Another page</Link>
          </main>
        </AppFrame>,
      );

      await user.click(screen.getByRole('link', { name: 'Another page' }));

      expect(screen.getByRole('heading', { level: 1, name: 'A page' })).toHaveFocus();
    });
  });

  describe('when the page first loads', () => {
    it('leaves focus where the browser put it', () => {
      renderWithProviders(
        <AppFrame>
          <main>
            <h1>A page</h1>
          </main>
        </AppFrame>,
      );

      expect(screen.getByRole('heading', { level: 1, name: 'A page' })).not.toHaveFocus();
    });
  });

  it('has no accessibility violations', async () => {
    const { container } = renderWithProviders(
      <AppFrame>
        <main>
          <h1>A page</h1>
        </main>
      </AppFrame>,
    );

    expect(await axe(container)).toHaveNoViolations();
  });
});
```

Write `Code/<Solution>.<Frontend>/src/components/layout/AppErrorBoundary.test.tsx`:

```tsx
import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import { AppErrorBoundary } from '@/components/layout/AppErrorBoundary';
import { axe } from '@/test/axe';
import { renderWithProviders } from '@/test/renderWithProviders';

describe('AppErrorBoundary', () => {
  it('shows the fixed message with its title and its heading', () => {
    renderWithProviders(<AppErrorBoundary />);

    expect(screen.getByRole('heading', { level: 1, name: 'Something went wrong' })).toBeInTheDocument();
    expect(screen.getByText('This page could not be shown.')).toBeInTheDocument();
    expect(document.title).toBe('Something went wrong');
  });

  it('has no accessibility violations', async () => {
    const { container } = renderWithProviders(<AppErrorBoundary />);

    expect(await axe(container)).toHaveNoViolations();
  });
});
```

**Each file sits beside the file it tests, named for it plus `.test`**, which
`Testing.md` sections 2 and 6 state. **The frame's test proves both halves of its
focus rule**, which step 12, "The application frame and the error boundary",
describes: a change of path moves focus to the page's heading, and the first load
does not. The link changes the path inside the memory router, the one route still
matches, and the same heading stays on the screen. **The boundary's test proves
the fixed message**, its heading and its document title, which `FrontendCode.md`
sections 9 and 13 require. **The page each test renders is written inline, and is
synthetic**, as `Testing.md` section 2 has all test data.

**Each file holds one scan**, which `Testing.md` section 9 requires, through the
`axe` of step 23, "The accessibility file, and `axe`". **Everything is found by
role and visible text**, which `Testing.md` section 6 requires; the document
title is read from the document, since a title is not shown on the page.

### 25. The shared acceptance base class

`Testing.md` section 7 runs every acceptance test through
`Microsoft.Playwright.Xunit.v3`, and puts on a shared base class the settings no
test may change, the discriminator each test is supplied, and, with `Testing.md`
section 9, the acceptance scan. **One base class serves every frontend**, so the
first frontend writes it and every later one finds it.

From the repository root:

```bash
test -f Code/<Solution>.IntegrationTests/AcceptanceTestBase.cs
```

**Exit 0 means a frontend before this one wrote it**, with its two packages: write
nothing in this step, and carry on to step 26, "Verify". **Exit 1 means it is
missing**: write the three things below.

**In `Code/Directory.Packages.props`, before the line `  </ItemGroup>`, add:**

```xml
    <PackageVersion Include="Microsoft.Playwright.Xunit.v3" Version="<Versions.md: Microsoft.Playwright.Xunit.v3>" />
    <PackageVersion Include="Deque.AxeCore.Playwright" Version="<Versions.md: Deque.AxeCore.Playwright>" />
```

**In `Code/<Solution>.IntegrationTests/<Solution>.IntegrationTests.csproj`, after
the line `    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />`,
add:**

```xml
    <PackageReference Include="Microsoft.Playwright.Xunit.v3" />
    <PackageReference Include="Deque.AxeCore.Playwright" />
```

That line is the last in the `ItemGroup` that holds `xunit.v3`, written by
`Api.md`'s step 10, "The test project's reference, and its package", for the first
API.

`BackendCode.md` section 2 puts every version in `Code/Directory.Packages.props`
and none in a project file. **Neither file is rewritten, and no existing entry is
touched.** Both versions were the current stable release on 2026-10-10, and
measured, NuGet's audit reports
nothing for either, and the solution restores and builds with 0 warnings and 0
errors. **`Deque.AxeCore.Playwright` carries its own `axe-core`**, so the
acceptance scan runs that version while the component scan runs the one `vitest-axe`
brings; both read the same accessibility file.

Write `Code/<Solution>.IntegrationTests/AcceptanceTestBase.cs`:

```csharp
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Shouldly;
using Xunit;

namespace <Solution>.IntegrationTests;

public abstract class AcceptanceTestBase : PageTest
{
    private static readonly ConcurrentDictionary<string, bool> ScannedUrls = new();

    private readonly string frontendAddress;
    private readonly string accessibilityFilePath;
    private readonly List<string> incompleteResults = [];

    protected AcceptanceTestBase(string frontendAddress, string accessibilityFilePath)
    {
        this.frontendAddress = frontendAddress;
        this.accessibilityFilePath = accessibilityFilePath;
    }

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

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = frontendAddress };

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        BrowserName.ShouldBe(Microsoft.Playwright.BrowserType.Chromium, "Acceptance tests run on Chromium only.");

        await Context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
    }

    public override async ValueTask DisposeAsync()
    {
        if (!TestOk())
        {
            TestContext.Current.AddAttachment("screenshot", await Page.ScreenshotAsync(new() { FullPage = true }), "image/png");

            var tracePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            await Context.Tracing.StopAsync(new() { Path = tracePath });

            // Playwright writes a trace only to a file, so the file is read back and deleted when it closes
            await using var trace = new FileStream(tracePath, new FileStreamOptions { Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose });
            var traceBytes = new byte[trace.Length];
            await trace.ReadExactlyAsync(traceBytes, TestContext.Current.CancellationToken);

            TestContext.Current.AddAttachment("trace", traceBytes, "application/zip");
        }

        if (incompleteResults.Count > 0)
        {
            TestContext.Current.AddAttachment("incomplete results", string.Join(Environment.NewLine, incompleteResults));
        }

        await base.DisposeAsync();
    }

    protected async Task ScanPageAsync()
    {
        if (!ScannedUrls.TryAdd(Page.Url, true))
        {
            return;
        }

        await using var file = File.OpenRead(accessibilityFilePath);
        using var accessibility = await JsonDocument.ParseAsync(file, cancellationToken: TestContext.Current.CancellationToken);
        var ruleset = accessibility.RootElement.GetProperty("ruleset").EnumerateArray().Select(tag => tag.ToString()).ToArray();
        var exclusions = accessibility.RootElement.GetProperty("exclusions").EnumerateArray().ToList();

        var results = await Page.RunAxe(new AxeRunOptions { RunOnly = RunOnlyOptions.Tags(ruleset) });

        List<string> violations = [];

        foreach (var violation in results.Violations)
        {
            var selectors = exclusions
                .Where(exclusion => exclusion.GetProperty("ruleId").ToString() == violation.Id)
                .Select(exclusion => exclusion.GetProperty("selector").ToString())
                .ToList();

            foreach (var node in violation.Nodes)
            {
                if (!await IsExcludedAsync(node, selectors))
                {
                    violations.Add($"accessibility violation: rule {violation.Id}, impact {violation.Impact}, element {node.Target} {node.Html}");
                }
            }
        }

        foreach (var incomplete in results.Incomplete)
        {
            foreach (var node in incomplete.Nodes)
            {
                incompleteResults.Add($"{Page.Url}: rule {incomplete.Id}, impact {incomplete.Impact}, element {node.Target} {node.Html}");
            }
        }

        violations.ShouldBeEmpty($"{Page.Url} has accessibility violations.");
    }

    private async Task<bool> IsExcludedAsync(AxeResultNode node, List<string> selectors)
    {
        // an element inside a frame or a shadow root cannot be located by one selector, so it is never excluded
        if (node.Target.FrameShadowSelectors is not [[var target]])
        {
            return false;
        }

        foreach (var selector in selectors)
        {
            if (await Page.Locator(target).EvaluateAsync<bool>("(element, selector) => element.matches(selector)", selector))
            {
                return true;
            }
        }

        return false;
    }
}
```

**It sits at the root of `<Solution>.IntegrationTests`**, beside the two database
fixtures, for the reason `create-solution` gives for them. **Its name is
`Testing.md` section 7's own**, "the shared acceptance base class", as
`BackendCode.md` section 18 takes a document's own word first. **It is abstract**,
because it is designed for inheritance and nothing runs it alone.

**Its constructor takes two values**, which each acceptance test class passes on
from the fixture that serves the frontend its scenario drives: the address at which
the fixture serves that frontend, which becomes the browser's base address, so a
test navigates by path alone; and the path of that frontend's
`src/test/accessibility.json`. **`client` and `database` do not pass through it**:
`Testing.md` section 7 has the test class take each from the fixture and hold it in
a field.

**The settings `Testing.md` section 7 puts here:**

- **Retries off.** Nothing in Playwright's xUnit classes retries a test, and this
  class turns nothing on. Measured.
- **Chromium only.** Playwright reads the browser from the `BROWSER` environment
  variable or a run settings file, and a class cannot choose it.
  So this class fails any test run on another browser.
- **A trace and a screenshot kept on failure only.** Playwright's xUnit classes keep
  neither. Tracing starts with each test, and before the browser
  context closes, `TestOk()` says whether the test passed. Where it failed, the
  screenshot and the trace are attached to the test with xUnit's
  `TestContext.Current.AddAttachment`, which writes each one under
  `Code/TestResults/`, a folder git already ignores.

**The discriminator** is the first 12 characters, in lowercase, of the hexadecimal
SHA-256 hash of the test class's full name, a dot and the test method's name, so
it is the same on every run of the same test and never a `Guid`, which
`Testing.md` section 7 states. The length is the one settled on 2026-09-25. **It is `static`**, because it reads
xUnit's `TestContext.Current` and nothing of the instance, and
`SonarAnalyzer.CSharp`'s rule `S2325` reports a property that reads nothing of its
instance.

**`ScanPageAsync` is the acceptance scan**, which a test calls on each page it
reaches. It scans a URL once
per run, held on this class, which `Testing.md` section 9 states. It runs the
ruleset's tags from the accessibility file. **A node is dropped only where an
entry's rule id matches and its element matches the entry's selector**: measured, each node's target is a CSS selector, and the element it
locates answers whether it matches the entry's. An element inside a frame or a
shadow root has no single selector, so its violation is never dropped, and fails
the test. **Any violation left fails the test**, with a message saying it is an
accessibility violation and naming the rule, the impact and the element.
**Incomplete results never fail the test**: each is kept, and where a test's scans
returned at least one, they are attached to that test as text, which `Testing.md`
section 9 states and the ruling of 2026-10-10
places. Measured, an xUnit warning is printed nowhere.

### 26. Verify

Run every command below from `Code/`, except where it says the repository root.
**Each one has an expected result, and anything else is a real failure: stop,
report what it said, and do not carry on.**

**Install from the lockfile, the way the pipeline does:**

```bash
npm ci
```

**Expect exit 0.** Then run the lockfile check of step 2, "The workspace entry,
and the install", again from the repository root: it prints
`Code/package-lock.json` and nothing else.

**Run the contract type generator with no argument**, which is the run the
pipeline makes:

```bash
npm run generate-contract-types --workspace=Tools/ContractTypes
```

**Expect one `wrote` line for each pair that names a frontend, and exit 0.** This
frontend's lines name each of its APIs. A pair naming no frontend writes nothing.

**Build every workspace, the way the pipeline does:**

```bash
npm run build --workspaces --if-present
```

**Expect exit 0.** The frontend's build prints its generator lines, then Vite's
list of what it wrote under `dist/`, then the time it took. **No line reads
"React Compiler skipped optimizing"**: the day-one code holds nothing the compiler
skips. The generator's own workspace has no build script, so it is skipped.

**The build's `tsc -b` checks the test files too**, because `tsconfig.app.json`
includes the whole of `src/`, the setup file and the test support files with it.
It is the only thing that checks MSW's option name, since a test run ignores an
option it does not know, and again on MSW 3.0.3 under Vitest
5.0.4.

**Confirm which TypeScript each one runs:**

```bash
npm exec --workspace=<Solution>.<Frontend> -- tsc --version
npx tsc --version
```

**Expect from the first the version of `Versions.md`'s `typescript` row for
`Code/<Solution>.<Frontend>/package.json`, and from the second the version of its
`typescript` row for `Code/Tools/ContractTypes/package.json`.** The frontend's
build runs its own TypeScript, which step 2, "The workspace entry, and the
install", pins. Run from
`Code/` without a workspace, `tsc` is the generator's, which npm put at the root,
measured. So nothing compiles the
frontend from the root with no workspace named.

**Run every workspace's tests:**

```bash
npm test --workspaces --if-present
```

**Expect exit 0, and Vitest's summary reading `Test Files  2 passed (2)` and
`Tests  5 passed (5)`**: three tests in `src/components/layout/AppFrame.test.tsx`
and two in `src/components/layout/AppErrorBoundary.test.tsx`. **jsdom also prints
"Not implemented: HTMLCanvasElement's getContext()" once for each of the two
scans.** `axe-core` asks for a canvas while it checks colour, and jsdom has none. The line
is not a failure. The generator's own workspace has no `test` script, so it is skipped.

**Audit the packages:**

```bash
npm audit
```

**Expect either "found 0 vulnerabilities" and exit 0, or exit 1 and a report.**
Exit 1 is not a reason to stop: **name every finding**, with its advisory, its
severity, the package and the path that brings it in, for the closing report.
Measured, the `shadcn` package
brings seven high findings from one advisory on `braces`, with no fixed version.
**Never run `npm audit fix` with `--force`**: for that finding it installs
`shadcn` 1.0.0, which cannot produce the `radix-nova` style.

**Make the list of software the pipeline makes:**

```bash
npm sbom --sbom-format cyclonedx > /dev/null
```

**Expect exit 0.** It exits 1 with `EINVALIDPURLTYPE` on a package with no
`version`, which step 2, "The workspace entry, and the install",
checked, and with `ESBOMPROBLEMS` on a package outside the range another package
declares for it. That is why step 18, "The test packages, and the `test` script",
names no version for MSW: installed with no version, npm takes one inside the range
Vitest declares for it, and `npm sbom` exits 0, measured on both MSW 2 and
MSW 3.

**Restore the C# solution, and then restore in locked mode:**

```bash
dotnet restore
dotnet restore --locked-mode
```

**Expect exit 0 from both.** Where step 25, "The shared acceptance base class",
added its two packages, the first command writes them into
`<Solution>.IntegrationTests`'s `packages.lock.json`, and the second proves that
every lock file matches what its project asks for, which is how the pipeline
restores. `BackendCode.md` section 2 has every lock file committed and restore run
in locked mode.

**Check that every project in the solution has its lock file**, because locked
mode writes a missing one and still exits 0:

```bash
grep -o 'Path="[^"/]*/' <Solution>.slnx | sed 's/^Path="//; s#/$##' | while read -r project; do test -f "$project/packages.lock.json" || echo "$project"; done
```

**Expect no output.** The first half is the project list of `SKILL.md`'s step 1,
"Check the repository", and the loop prints each project that has no
`packages.lock.json`. A line printed is a lock file the pipeline's own restore
step would fail on: report it.

**Build the C# solution:**

```bash
dotnet build --no-restore
```

**Expect 0 warnings and 0 errors.** `Code/Directory.Build.props` turns any warning
into a failed build, so a build that succeeds has none, and it compiles
`AcceptanceTestBase.cs` against the two packages it uses.

**Check the fixed port, and each called API's entry**, from the repository root:

```bash
grep -c 'server: { port: <FrontendPort>, strictPort: true },' Code/<Solution>.<Frontend>/vite.config.ts
node -p "require('./Code/<Solution>.<Name>Api/appsettings.Development.json')['<Solution>'].Security.AllowedOrigins.includes('http://localhost:<FrontendPort>')"
```

**Expect `1` from the first**: step 19, "The `test` block in `vite.config.ts`",
wrote the file again whole with the port still fixed. **Expect `true` from the
second, once for each API the frontend calls.** Anything else is a step that did
not take: report it.

**Check that git sees nothing the tools wrote**, from the repository root:

```bash
git status --porcelain --untracked-files=all -- Code/<Solution>.<Frontend> | grep -e node_modules -e /dist/
```

**Expect no output and exit 1.** The repository's `.gitignore` covers what the
template's own covered, which step 1, "The template", removed. A line printed is a
file that would be committed: report it.

**Check that no placeholder leaked**, from the repository root:

```bash
grep -rnI -e '<Solution>' -e '<Frontend>' -e '<solution>' -e '<frontend>' \
    -e '<Name>Api' -e '<OtherName>Api' -e '<name>-api' -e '<other-name>-api' \
    -e '<nameApi>' -e '<otherNameApi>' -e '<address>' -e '<FrontendPort>' \
    --exclude-dir=node_modules --exclude-dir=dist \
    --exclude-dir=bin --exclude-dir=obj \
    Code/<Solution>.<Frontend> Code/package.json \
    Code/Tools/ContractTypes/contract-types.config.json \
    Docs/InfrastructureInventory.md Code/Directory.Packages.props \
    Code/<Solution>.IntegrationTests \
    Code/<Solution>.<Name>Api/appsettings.Development.json
```

**This is the one command in this recipe whose placeholders after `-e` are typed
exactly as written and not replaced**, because they are what it looks for. The
paths after them are replaced as everywhere else. **Expect no output and exit 1**,
which is how `grep` reports that it found nothing. A line printed means a value
was never substituted in the file it names: report every line. The paths searched
are everything this recipe writes or changes, apart from the lockfile, which npm
writes, and the C# lock files, which `dotnet restore` writes, with the last path
written once for each API the frontend calls. `node_modules/`, `dist/`, `bin/` and
`obj/` are what the tools write.
