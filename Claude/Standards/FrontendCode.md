# Frontend Code Standards

The implementation standard for web frontend code: React, TypeScript and Vite.
It fixes the technology, the conventions and the practice for every concern a
frontend has, so that an increment can be written without inventing any of them.

Planning reads this before it starts and plans against it. The plan is
written against it, and so is any code example in a plan.
`feature-execution` reads it before writing an increment.
`feature-review` checks the increment against it.

Claude Design reads it as well. Every frontend's design is made there, in a
shape these rules can implement. A design is read out of Claude Design and
rebuilt in React against this document, rather than invented while the code is
written.

Every rule in this document is settled. Each one was argued and decided before it
was written here, and none of them is re-opened during execution. Where this
document states one way to do something, that is the only way, and an equivalent
is not substituted for it.

A code example is shown wherever an example is what makes a rule unambiguous, and
every one of them is bound by the same restriction. A sentence before the example
says it is an example, and the first line inside it repeats that. Every name in an
example, whether it is a component, a hook, a type, a function, a file or a
folder, is there to make the shape readable. The names are illustrations, not
names to look for in a project. Finding one, or not finding one, tells the reader
nothing. Only the shape is the rule.

The codebase must read as though one person wrote it, with the standard not
changing from one part of it to another.

This standard applies to code that already exists. Code written before a rule
existed is still non-conforming, and correcting it is its own increment, never
part of an unrelated one.

Rules that the TypeScript compiler, a lint rule, the formatter or a build setting
enforces are stated here as well. Every rule has a named enforcer in section 17,
including the rules whose enforcer is nothing.

# 1. Scope

This document covers web frontends only. A frontend built on another stack is
outside it and needs a standard of its own. Source layout is its subject. Test
layout is not.

**This document states what a frontend does with what an API sends. It never
states what an API sends.** The shape of a response, the shape of a failure and
how a list is paged are `BackendCode.md`'s, and no rule here restates them.

| Subject | Where it is stated |
|---|---|
| Back end code | `BackendCode.md` |
| What an API sends, including the shape of a failure and how a list is paged | `BackendCode.md` |
| Tests, and where a test file goes | `Testing.md` |
| What counts as sensitive, where secrets live, a vulnerable dependency failing the build | `Security.md` |
| What a screen looks like | Claude Design |
| A decision that affects more than one feature | a decision in a plan |
| What one feature must do | the rules and scenarios in its plan |
| How one feature is built, which modules it has, and where its components go | its plan |

**A security rule is in this document when it constrains how code is written.**
Everything else about security is `Security.md`'s.

# 2. Technology baseline and package management

A frontend is written in React and TypeScript and is built by Vite.

| Concern | Technology |
|---|---|
| User interface | React |
| Language | TypeScript |
| Build tool and development server | Vite |
| Package manager | npm |
| Linter | ESLint |
| Formatter | Prettier |

**That table is the toolchain, and it is not the list of packages a frontend
depends on.** The library a concern uses is stated by the section that governs
that concern, and never here.

**This document is written against React 19, Tailwind CSS 4, TanStack Query 5
and Zod 4.**

**A major version is not changed by an increment.**

**Each frontend is a single-page application.** Nothing is rendered on a server.

**ESLint is the linter**, configured in `eslint.config.js` in flat config form.

**Prettier is the formatter.** ESLint states no formatting rules, and
`eslint-config-prettier` is what keeps that true.

**Every enabled lint rule is an error**, and none is configured as a warning.

# 3. The project: where a frontend lives and how it is built

A frontend is a folder directly under `Code/`, named `<Solution>.<Frontend>`.
`<Solution>` is the solution's name, which `BackendCode.md` section 2 states.
`<Frontend>` is the frontend's own name, which `create-project` asks for and
which takes the same shape `<Solution>` does.

**A frontend calls one or more APIs, and which ones it calls is asked when it is
created.** An API is written `<Name>Api` in this document, which is the API
project `<Solution>.<Name>Api` without the solution's name, both of which
`BackendCode.md` section 3 states. Section 8 states what a frontend holds for
each API it calls.

**It is never inside an API project's folder.**

**A frontend shares no code with another frontend.** It never imports from
another frontend, and no package exists for two frontends to import from.

**Every frontend is an npm workspace**, listed in one `package.json` at `Code/`.
Each one is listed by its exact folder name, never by a glob pattern.

**There is one lockfile, `Code/package-lock.json`, and it is committed.** A
frontend folder never holds its own.

**A frontend's `package.json` `name` is its folder name in npm form**, which is
the folder name lowercased with the dot written as a hyphen, so
`<Solution>.<Frontend>` holds `"name": "<solution>-<frontend>"`. Nothing else
changes, so a name of more than one word runs together and takes no hyphen at
the boundary between its words.

**A frontend is built by `npm run build` with its own workspace named.** From
`Code/`, that is `npm run build --workspace=<folder>`, and the flag takes the
folder name. The package's `name` is never typed on a command line.

The example below shows the workspace list beside the name it points at. Every
name in it is a placeholder, and the shape is the rule. Both files are shown
with only the members these rules fix.

```json
// Example only. Every name in it is a placeholder.

// Code/package.json
{ "workspaces": ["<Solution>.<Frontend>", "<Solution>.<OtherFrontend>"] }

// Code/<Solution>.<Frontend>/package.json
{ "name": "<solution>-<frontend>" }
```

# 4. Folder layout and naming

The tree below is rooted at a frontend's folder, which section 3 names.
`leave`, `LeaveRequest`, `LeaveRequestForm`, `LeaveBalance`, `useLeaveBalance`,
`useLeaveFilters`, `Button`, `Input`, `useDebounce` and `formatDate` in it are
illustrations. They are names this document uses to show the shape, and the shape
is the rule. Every other name in the tree is a file or folder name the rules
below fix.

```
Code/<Solution>.<Frontend>/
    index.html
    package.json
    vite.config.ts
    tsconfig.json
    tsconfig.app.json
    tsconfig.node.json
    components.json
    public/
    src/
        main.tsx
        App.tsx
        index.css
        router.ts
        modules/
            leave/
                pages/
                    leave-request/
                        LeaveRequest.tsx
                        LeaveRequestForm.tsx
                components/
                    LeaveBalance.tsx
                    LeaveBalance.test.tsx
                hooks/
                    useLeaveBalance.ts
                stores/
                    useLeaveFilters.ts
        components/
            ui/
                Button.tsx
                Input.tsx
            layout/
                AppFrame.tsx
                AppErrorBoundary.tsx
        hooks/
            useDebounce.ts
        api/
            <name>-api/
                types.ts
                apiClient.ts
        lib/
            formatDate.ts
        test/
            setup.ts
            server.ts
            renderWithProviders.tsx
            accessibility.json
            axe.ts
            fixtures/
```

**The project root holds `index.html`, `package.json`, `vite.config.ts`,
`tsconfig.json`, `tsconfig.app.json`, `tsconfig.node.json` and
`components.json`, with `public/` beside them.** All application code is under
`src/`.

**A frontend's folder holds no ESLint and no Prettier configuration.** Both sit
at `Code/`.

**`src/` holds `main.tsx`, `App.tsx`, `index.css` and `router.ts` at its top**,
then `modules/`, `components/`, `hooks/`, `api/`, `lib/` and `test/`. `Testing.md`
section 6 states what `src/test/` holds, and puts each test file beside the file
it tests.

**A module is a folder under `src/modules/`**, holding one business area's
`pages/`, `components/`, `hooks/` and `stores/`.

**`src/components/` holds `ui/`, which holds the component primitives, and
`layout/`, which holds the application frame and the one error boundary.** The
application frame is `AppFrame` and the error boundary is `AppErrorBoundary`, one
of each per frontend, and section 9 states what each does.

**A page gets a folder of its own under `pages/`**, holding the page's own file
and any file only that page uses. Everything else is a plain file, with no folder
around it.

**A folder is created when its first file is written**, so the tree above is
larger than what exists on any given day.

**Every folder is lowercase**, kebab-case where the name has more than one word.
That covers every folder inside a frontend's own folder. It does not reach the
frontend's folder or anything above it, and section 3 names the frontend's
folder.

**Every file an author writes is named exactly as the thing it exports.** Where
it also exports something that only supports that thing, such as a props type or
a variant definition, the file is still named after the thing. **Where a file
exports several parts of one thing, it is named for that thing**, which is the
export the other parts are named after. **A generated file is named by the
generator**, and section 8 names the one this document fixes.

The casing follows from that and is not a separate rule. A React component
identifier is PascalCase, so a file exporting `LeaveBalance` is
`LeaveBalance.tsx`. Every other identifier is camelCase, so a file exporting
`useLeaveBalance` is `useLeaveBalance.ts`.

**A component is named for what it renders.** It carries no word saying it is a
component.

**A component carries a word for what kind of thing it is only where the same
subject has more than one component.**

**A page's component is named for what the page shows**, and the page's folder is
that name in kebab-case.

**A hook's name begins with `use`**, then what it gives, or the action it
performs where it gives no value.

**A function that is not a component and not a hook is named for the action it
performs and what that action applies to.**

**A component's props type is exported, and is named the component's name
followed by `Props`.**

The example below shows a file named for what it exports, beside the props type
that only supports it. `LeaveBalance`, `LeaveBalanceProps` and `days` are
illustrations. They are names this document uses to show the shape, and the shape
is the rule.

```tsx
// Example only. The names are illustrations, not names to look for.

// src/modules/leave/components/LeaveBalance.tsx
export type LeaveBalanceProps = {
  days: number;
};

export function LeaveBalance({ days }: LeaveBalanceProps) {
  return <span>{days} days</span>;
}
```

# 5. Modules, and what may import what

**What modules exist, and what goes in each, is decided in planning and recorded
in the plan.** A module is never invented while a screen is being written.

**A module may import another module's `components/`.** It never imports another
module's `pages/`, `hooks/` or `stores/`.

**Nothing under `src/components/`, `src/hooks/` or `src/lib/` knows a business
concept.** A file belongs there only if it would work unchanged in a product that
is not this one. **Nothing under those three folders imports from `src/modules/`
or from `src/api/`.**

**A component goes in a module's `components/` or in `src/components/`. There is
no third place.**

The example below shows these rules as the import lines they govern.
`leave`, `employees`, `LeaveRequest`, `EmployeeCard`, `useEmployee`, `DataTable`,
`LeaveBalance` and `formatDate` are illustrations. They are names this document
uses to show the shape, and the shape is the rule. **The three lines marked below
are the ones these rules refuse**, and they are shown because what may not be
imported is this section's whole subject.

```tsx
// Example only. The names are illustrations, not names to look for.

// src/modules/leave/pages/leave-request/LeaveRequest.tsx
import { EmployeeCard } from '@/modules/employees/components/EmployeeCard';
import { useEmployee } from '@/modules/employees/hooks/useEmployee'; // not allowed

// src/components/ui/DataTable.tsx
import { formatDate } from '@/lib/formatDate';
import type { components } from '@/api/<name>-api/types'; // not allowed
import { LeaveBalance } from '@/modules/leave/components/LeaveBalance'; // not allowed
```

# 6. TypeScript compiler rules

**`tsconfig.app.json` sets `strict`, and these nine settings.**

| Setting | Value |
|---|---|
| `noUncheckedIndexedAccess` | `true` |
| `exactOptionalPropertyTypes` | `true` |
| `noImplicitOverride` | `true` |
| `noFallthroughCasesInSwitch` | `true` |
| `verbatimModuleSyntax` | `true` |
| `erasableSyntaxOnly` | `true` |
| `noUnusedLocals` | `true` |
| `noUnusedParameters` | `true` |
| `moduleResolution` | `"bundler"` |

**`@/` is the import alias for `src/`.** It is declared in `tsconfig.app.json` and
in the root `tsconfig.json`, each as `"paths": { "@/*": ["./src/*"] }`, and in
`vite.config.ts` as a resolve alias. **The three always agree.**

# 7. Configuration

**A setting the frontend needs is read from `public/config.json`.** It is read
once, before the application renders, and it is typed.

**No secret is ever put in a frontend.** Everything in a built frontend,
`config.json` included, is downloadable by anyone who asks for it.

**No `.env` file is committed.**

# 8. The API client and data fetching

**A frontend's API types are generated by `openapi-typescript`, once per API,
from that API's frozen fragments under `Docs/Contracts/<Name>Api/`, into
`src/api/<name>-api/types.ts`, and every generated file is committed.** The
folder per API is that API's name under section 4's casing rule.

**A generated file is never edited and never hand-copied.**

**A generated file covers one API and nothing else.** It is never shared between
frontends, and it never spans two APIs. One file spanning two APIs loses a route
and reports nothing, because every API serves health on every version it serves,
which `BackendCode.md` section 6 requires.

**`openapi-fetch` is the client**, typed by its own API's generated `paths`.
**One client is created per API, at `src/api/<name>-api/apiClient.ts`, exporting
`apiClient`**, and everything that calls that API imports it from there.

**The file name and the export are the same in every API's folder**, so a file
that calls two APIs aliases one of its two imports.

**TanStack Query owns caching, retries, invalidation and loading state.**

**Every call to a server goes through a query or a mutation.**

**A component never calls the client directly.**

**A query and a mutation are each a hook.**

**`useQuery` is the query hook.**

**A query and a mutation each read the client's answer**, return the body when
the call succeeded, and throw the failure when it did not.

**A frontend declares the type of that failure once**, for the whole frontend. No
query and no mutation declares its own.

**A query key is a function, exported from the file of the hook that uses it.**
Anything invalidating that query calls that function, and a key is never typed
out at a call site.

**Every request is given the abort signal its query provides.**

**A component that reads a query returns while the query is pending, returns
when it failed, and renders the data after both.** The two fields it reads are
`isPending` and `isError`.

**After those two returns the data is present**, so it is read directly, with
no optional chain and no fallback value.

**A query's failure is shown by the component that reads the query**, and never
by the `ErrorBoundary` section 9 places.

Section 10's example draws a component reading a query in that shape.

The example below shows one query, one mutation, and the mutation refreshing what
the query holds. `leaveBalanceKey`, `useLeaveBalance`, `useRequestLeave`,
`employeeId`, `leave-balance` and the two routes are illustrations. They are names
this document uses to show the shape, and the shape is the rule.

```tsx
// Example only. The names are illustrations, not names to look for.

// src/modules/leave/hooks/useLeaveBalance.ts
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/api/<name>-api/apiClient';

export function leaveBalanceKey(employeeId: string) {
  return ['leave-balance', employeeId] as const;
}

export function useLeaveBalance(employeeId: string) {
  return useQuery({
    queryKey: leaveBalanceKey(employeeId),
    queryFn: async ({ signal }) => {
      const { data, error } = await apiClient.GET(
        '/v1/employees/{id}/leave-balance',
        { params: { path: { id: employeeId } }, signal },
      );
      if (error) {
        throw error;
      }
      return data;
    },
  });
}

// src/modules/leave/hooks/useRequestLeave.ts
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/api/<name>-api/apiClient';
import { leaveBalanceKey } from '@/modules/leave/hooks/useLeaveBalance';

export function useRequestLeave(employeeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (days: number) => {
      const { error } = await apiClient.POST('/v1/leave-requests', {
        body: { employeeId, days },
      });
      if (error) {
        throw error;
      }
    },
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: leaveBalanceKey(employeeId) }),
  });
}
```

# 9. Routing

**React Router is the router.**

**The route table is one file, `src/router.ts`, exporting `router`**, built with
`createBrowserRouter`. **`App.tsx` renders it.**

**A route names its page component with `Component`.**

**A page's route sits inside the route that draws the application frame,
`AppFrame`.**

**That route carries the application's one error boundary, `AppErrorBoundary`,
on its `ErrorBoundary` property, and no other route carries one.**

**The boundary catches an exception no code expected, and never an API
failure.**

**What it shows is a fixed message, carrying neither the exception's own
message nor its stack.**

**No loaders and no actions.**

**An address is lowercase**, with hyphens where a name has more than one word.

**In-application navigation is `<Link>` or `<NavLink>`.** Never a bare `<a>` for
an address inside the application.

The example below shows the route table, the file that renders it, and a link
from one screen to another. `LeaveRequest`, `leave`, `leave-request` and the
address are illustrations. They are names this document uses to show the shape,
and the shape is the rule. The table below is shown with the page route inside
the frame route, and with the boundary on that route.

```tsx
// Example only. The names are illustrations, not names to look for.

// src/router.ts
import { createBrowserRouter } from 'react-router';
import { AppErrorBoundary } from '@/components/layout/AppErrorBoundary';
import { AppFrame } from '@/components/layout/AppFrame';
import { LeaveRequest } from '@/modules/leave/pages/leave-request/LeaveRequest';

export const router = createBrowserRouter([
  {
    Component: AppFrame,
    ErrorBoundary: AppErrorBoundary,
    children: [{ path: '/leave/requests/new', Component: LeaveRequest }],
  },
]);

// src/App.tsx
import { RouterProvider } from 'react-router';
import { router } from '@/router';

export function App() {
  return <RouterProvider router={router} />;
}

// in a page or a component
<Link to="/leave/requests/new">New request</Link>
```

# 10. State

**Zustand is the store.**

Every value a screen holds lives in one of four places.

| What | Where it lives |
|---|---|
| Data that came from an API | Its query |
| Anything the address should carry | The address |
| Client state more than one part of a screen needs | A store |
| Everything else | The component that uses it |

**Data that came from an API is never copied into a store, into context or into
component state.** It is read from its query where it is needed.

**Filters, sorting, the page and the selected tab live in the address**, through
the router's search parameters.

**Writing one search parameter never removes another.**

**A store is a hook.** It is named the way section 4 names a hook.

**A store goes in a module's `stores/`. There is no other place.**

**Stores are small and named for what they hold.** Many small stores rather than
one large one.

**Context carries a value that does not change. It is not a store.**

**An authentication token is held in a store, in memory only.** It is never
written to the browser's storage and never to a cookie, and it is gone once the
tab closes unless the person asked to stay signed in.

**When a session ends, every query is cleared and every store holding what
arrived during it is reset**, from memory and from anything the browser was
given, even when the server cannot be reached.

**Data at either protection level, as `Security.md` defines them, is never
written to the browser's storage or to a cookie.**

The example below shows a store, and beside it one screen holding a value in each
of the four places. `LeaveSelection`, `useLeaveSelection`, `selectedIds`,
`toggle`, `LeaveRequests`, `useLeaveRequests`, `status`, `approved`,
`isHelpOpen` and the text of both messages are illustrations. They are names this
document uses to show the shape, and the shape is the rule.

```tsx
// Example only. The names are illustrations, not names to look for.

// src/modules/leave/stores/useLeaveSelection.ts
import { create } from 'zustand';

type LeaveSelection = {
  selectedIds: string[];
  toggle: (id: string) => void;
};

export const useLeaveSelection = create<LeaveSelection>((set) => ({
  selectedIds: [],
  toggle: (id) =>
    set((state) => ({
      selectedIds: state.selectedIds.includes(id)
        ? state.selectedIds.filter((selected) => selected !== id)
        : [...state.selectedIds, id],
    })),
}));

// src/modules/leave/pages/leave-requests/LeaveRequests.tsx
import { useState } from 'react';
import { useSearchParams } from 'react-router';
import { useLeaveRequests } from '@/modules/leave/hooks/useLeaveRequests';
import { useLeaveSelection } from '@/modules/leave/stores/useLeaveSelection';

export function LeaveRequests() {
  const [searchParams, setSearchParams] = useSearchParams();

  const status = searchParams.get('status') ?? 'all';            // the address
  const { data, isPending, isError } = useLeaveRequests(status); // its query
  const selectedIds = useLeaveSelection((s) => s.selectedIds);   // a store
  const [isHelpOpen, setIsHelpOpen] = useState(false);           // this component

  function showApproved() {
    setSearchParams((previous) => {
      previous.set('status', 'approved');
      return previous;
    });
  }

  if (isPending) {
    return <p>Loading leave requests</p>;
  }

  if (isError) {
    return <p>Leave requests could not be loaded</p>;
  }

  return (
    <section>
      <button onClick={showApproved}>Approved only</button>
      <button onClick={() => setIsHelpOpen(!isHelpOpen)}>Help</button>
      <p>
        {selectedIds.length} of {data.length} selected
      </p>
    </section>
  );
}
```

# 11. Forms and validation

**React Hook Form is the form library and Zod is the schema library.**

**The server is the authority**, and validation in the browser is for the person
using the screen. Every rule a schema checks is also checked by the API, and a
rule that exists only in the browser is a rule that does not exist.

**A schema is authored. It is never generated from the contract.**

**A schema is constrained to the generated request type with `satisfies`**, never
with a type annotation. That type comes from the generated types and is never
written out by hand.

**There is one schema per form.**

**Every rule in a schema carries a message**, and the message says what to do
rather than what is wrong.

**A form submits through a mutation.**

**A failure carrying a per-field map is shown against the fields it names**, each
entry against the field whose name matches its key.

**A failure shows `detail` as one message on the form.**

**No message a person reads is composed from `code`, and `code` is never shown to
a person.**

**A form that edits something the API returned takes its values from its query**,
so they arrive when the query answers.

**A password field's input has `type="password"`.** Where it also lets the
person show what they typed, the control that does that changes only the
field's `type`, between `password` and `text`.

**A password field and a one-time-code field never refuse a paste, and never
turn off the browser's own filling in.** Neither carries a paste handler that
blocks the paste, and neither carries `autoComplete="off"`. The field's
`autoComplete` names what it holds, such as `current-password`, `new-password`
or `one-time-code`.

The example below shows a schema and the form that uses it, with the schema's own
messages reaching a field, the server's answer reaching a field and the form, and
the result announced either way. `inviteEmployee`, `InviteEmployeeForm`,
`useInviteEmployee`, `workEmail`, `startDate`, the route and the text of every
message are illustrations. They are names this document uses to show the shape, and the
shape is the rule.

```tsx
// Example only. The names are illustrations, not names to look for.

// src/modules/employees/pages/invite-employee/inviteEmployee.ts
import { z } from 'zod';
import type { paths } from '@/api/<name>-api/types';

type InviteEmployee =
  paths['/v1/employees/invitations']['post']['requestBody']['content']['application/json'];

export const inviteEmployee = z.object({
  workEmail: z.email('Enter a valid work email'),
  startDate: z.iso.date('Choose a start date'),
}) satisfies z.ZodType<InviteEmployee>;

// src/modules/employees/pages/invite-employee/InviteEmployeeForm.tsx
import type { z } from 'zod';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { inviteEmployee } from '@/modules/employees/pages/invite-employee/inviteEmployee';
import { useInviteEmployee } from '@/modules/employees/hooks/useInviteEmployee';

type InviteEmployeeValues = z.infer<typeof inviteEmployee>;

export function InviteEmployeeForm() {
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<InviteEmployeeValues>({ resolver: zodResolver(inviteEmployee) });

  const invite = useInviteEmployee();

  const submit = handleSubmit((values) => {
    invite.mutate(values, {
      onError: (failure) => {
        if ('errors' in failure) {
          for (const [field, messages] of Object.entries(failure.errors)) {
            setError(field as keyof InviteEmployeeValues, {
              message: messages.join(' '),
            });
          }
        }

        setError('root', { message: failure.detail });
      },
    });
  });

  return (
    <form
      onSubmit={(event) => {
        void submit(event);
      }}
    >
      <label htmlFor="workEmail">Work email</label>
      <input id="workEmail" {...register('workEmail')} />
      <p>{errors.workEmail?.message}</p>

      <label htmlFor="startDate">Start date</label>
      <input id="startDate" type="date" {...register('startDate')} />
      <p>{errors.startDate?.message}</p>

      <output>
        {invite.isSuccess ? 'Invitation sent' : errors.root?.message}
      </output>
      <button type="submit">Send invitation</button>
    </form>
  );
}
```

# 12. Styling and component primitives

**Tailwind CSS is the styling library.**

**`src/index.css` is the only stylesheet in a frontend.**

**Every design token is declared in `src/index.css`, and nowhere else.**

**A token is a name declared in `@theme`.**

**A token's value is declared in `:root`, and its `@theme` name points at it.**
The `:root` name is the token's name with the namespace removed. A `:root` name
generates no class and is never named by a component.

**A component never writes a raw colour, spacing or font value. It names a
token.** A value in square brackets at a call site, such as `bg-[#ff0000]` or
`p-[13px]`, is that raw value written another way.

**Where the design has a value and no class names it, the value becomes a
token.**

**A token's name begins with the Tailwind namespace for the kind of value it
is.** The rest of the name is lowercase, with hyphens where it has more than one
word.

**The token values come from the design.** `src/index.css` is derived from the
design, and it is never edited to change a design decision.

**A component primitive is taken with shadcn**, and it is built on Radix where
Radix has a primitive for it.

**A copy is taken as shadcn ships it.** On arrival it is renamed to the thing it
exports, the way section 4 names a file, and the import paths that name it are
corrected with it. **Nothing else in it is changed on arrival.**

**A copy the author has not changed is updated by taking it again and renaming
it again. Where the author has changed it, the change is made again on the new
copy.**

**No component library that ships its own appearance is used.**

**`components.json` sits at a frontend's root, one per frontend.** Its `style`
names Radix as the primitive library, its `tailwind.css` names `src/index.css`,
its `tailwind.cssVariables` is on, nothing in it renders on a server, and its
aliases are the `@/` alias and the folders section 4 fixes. **Everything in it
that decides an appearance comes from the design.**

The example below shows the stylesheet's two token blocks, a component naming
those tokens beside a copied primitive, and `components.json`. `brand-500`,
`gutter`, `body`, `LeaveBalance`, `LeaveBalanceProps`, `days` and `Button` are
illustrations. They are names this document uses to show the shape, and the shape
is the rule. Each file is shown with only the parts these rules fix.

```css
/* Example only. The names are illustrations, not names to look for. */

/* src/index.css */
@import "tailwindcss";

:root {
  --brand-500: oklch(0.62 0.19 259);
  --gutter: 1.5rem;
  --body: "Inter", sans-serif;
}

@theme inline {
  --color-brand-500: var(--brand-500);
  --spacing-gutter: var(--gutter);
  --font-body: var(--body);
}
```

```tsx
// Example only. The names are illustrations, not names to look for.

// src/modules/leave/components/LeaveBalance.tsx
import { Button } from '@/components/ui/Button';

export type LeaveBalanceProps = {
  days: number;
};

export function LeaveBalance({ days }: LeaveBalanceProps) {
  return (
    <div className="bg-brand-500 p-gutter font-body">
      <span>{days} days remaining</span>
      <Button>Request leave</Button>
    </div>
  );
}
```

```json
// Example only. The names are illustrations, not names to look for.

// components.json
{
  "rsc": false,
  "tsx": true,
  "tailwind": {
    "config": "",
    "css": "src/index.css",
    "cssVariables": true
  },
  "aliases": {
    "components": "@/components",
    "ui": "@/components/ui",
    "lib": "@/lib",
    "hooks": "@/hooks"
  }
}
```

# 13. Accessibility

**A control that does something is a real control**, being `button`, `a`, `input`
or `select`. Never a `div` or a `span` with a click handler.

**Where something is moved by dragging, the same thing can be done without
dragging.**

**Every input has a label associated with it.** A placeholder is not a label.

**A person is never asked for the same information twice in one process.** Where
a later step needs a value an earlier step took, that value is carried forward.

**An image's alternative text says what the image conveys.** A decorative image
carries an empty one.

**Colour is never the only thing carrying meaning.** Where a colour tells the
reader something, a word, an icon carrying a name, or a shape carries the same
thing beside it.

**Animation honours `prefers-reduced-motion`.** Where a screen animates, that
media query turns the animation off for a person who asked for less motion.

Section 9 states that a page's route sits inside the route that draws the
application frame.

**Every page renders one `<h1>`, and it is that page's own name.**

**Focus moves to that heading when the route changes.**

**Whatever has keyboard focus shows a visible indicator**, and no style removes
one.

**Whatever has keyboard focus is never entirely hidden behind other content**,
including content that stays in place while the page scrolls.

**Every page sets the document title, and it is that page's own name.** The
page renders a `<title>` element, and nothing inside the page renders a second
one.

**The document is given a language**, set on the `<html>` element in
`index.html`.

**A dialog, a menu or any other overlay is built on its Radix primitive**, which
is where taking focus on open, holding it while open and returning it on close
already live. It is not reimplemented.

**A mutation's result is announced, whether it succeeded or failed.**

**The announcement is in a live region, and the region is in the document before
the message arrives.** The element is `<output>`.

**The region belongs to the thing that started the action**, so a form announces
its own result.

**Tab order follows the visual order**, and `tabIndex` is never a positive
number.

The example below shows a page carrying its title and its heading, a decorative
image beside a meaningful one, and a status that a colour and a word both carry.
`LeaveRequests`, `Thandi Mokoena`, `text-destructive`, the two image addresses
and the text of every message are illustrations. They are names this document
uses to show the shape, and the shape is the rule.

```tsx
// Example only. The names are illustrations, not names to look for.

// src/modules/leave/pages/leave-requests/LeaveRequests.tsx
export function LeaveRequests() {
  return (
    <main>
      <title>Leave requests</title>
      <h1>Leave requests</h1>
      <img src="/decoration.svg" alt="" />
      <img src="/leave-balance.png" alt="Leave balance: 12 of 21 days remaining" />
      <ul>
        <li>
          <span>Thandi Mokoena</span>
          <span className="text-destructive">Overdue</span>
        </li>
      </ul>
    </main>
  );
}
```

# 14. What a component reads while it renders

**A component reads nothing that changes on its own while it renders.** The
clock, randomness, and anything the browser holds, including storage and the size
of the window, are read in an effect or in a handler.

**Responsive layout is CSS**, through Tailwind's breakpoints and container
queries. **The size of the window is read only where a layout genuinely cannot
be expressed in CSS.**

The example below shows the two reads this rule refuses, beside the two shapes
that replace them: one for a value that changes while the screen is open, and one
for a value wanted at the moment a person acts. `LeaveSummary`, `useWindowWidth`,
`LeaveExport`, `openedAt`, `width` and `exportedAt` are illustrations. They are
names this document uses to show the shape, and the shape is the rule. **The two
lines marked below are the ones this rule refuses**, and they are shown because
what may not be read is this section's whole subject.

```tsx
// Example only. The names are illustrations, not names to look for.

// src/modules/leave/components/LeaveSummary.tsx
export function LeaveSummary() {
  const openedAt = new Date();      // not allowed
  const width = window.innerWidth;  // not allowed

  return <p>{openedAt.toDateString()} {width}</p>;
}

// src/hooks/useWindowWidth.ts
import { useEffect, useState } from 'react';

export function useWindowWidth() {
  const [width, setWidth] = useState(0);

  useEffect(() => {
    const read = () => {
      setWidth(window.innerWidth);
    };

    read();
    window.addEventListener('resize', read);
    return () => {
      window.removeEventListener('resize', read);
    };
  }, []);

  return width;
}

// src/modules/leave/components/LeaveExport.tsx
import { useState } from 'react';

export function LeaveExport() {
  const [exportedAt, setExportedAt] = useState<string | null>(null);

  return (
    <button onClick={() => { setExportedAt(new Date().toISOString()); }}>
      Export {exportedAt}
    </button>
  );
}
```

# 15. Anti-speculation

**This section governs behaviour and machinery, and never structure.** The folder
layout, what a module holds and what may import what are fixed by sections 4 and
5. The rule that a folder is created when its first file is written is section
4's, and no rule here reaches which folders exist or where a file goes.

**This section reaches code an author writes.** A component shadcn copies is
not code an author writes. Section 12 states what arrives with it and what is
changed on arrival, and nothing here requires any part of it to be removed. **A
change an author later makes to that file for a product reason is reached like
any other code.**

**Nothing is written that no increment needs.** Every part of an increment
answers to a step of the implementation order, and the test for a component, a
hook, a prop, a query or a route is which step asked for it.

**A need nobody has does not buy code now.** Something that might be wanted later
is written when it is wanted, in the increment that wants it.

**Nothing is written for a caller that does not exist.** A component nobody
renders, a hook nobody calls, an optional prop nobody sets, a query nobody calls
and a route for a screen nobody built are the same shape.

**A setting exists because something asked for it.** A value that nothing asked
to vary is written in the code, and does not become a key in
`public/config.json`. Section 7 states where a setting that was asked for is read
from.

**A condition the caller cannot produce gets no check and no branch.** Where the
generated types, a component's props type or a form's schema has already excluded
a value, the code does not test for it again. Section 8 states where the
generated types come from, and section 11 states what a schema checks.

The example below shows a component beside its only call site.
`LeaveEntitlement`, `LeaveEntitlementProps`, `Entitlement`, `entitlement`,
`variant`, `onRefresh` and the route are illustrations. They are names this
document uses to show the shape, and the shape is the rule. **The three lines
marked below are the ones these rules refuse**: the two props are set by no
caller, and the check tests a value the contract makes required.

```tsx
// Example only. The names are illustrations, not names to look for.

// src/modules/leave/components/LeaveEntitlement.tsx
import type { paths } from '@/api/<name>-api/types';

type Entitlement =
  paths['/v1/employees/{id}/leave-balance']['get']['responses']['200']['content']['application/json'];

export type LeaveEntitlementProps = {
  entitlement: Entitlement;
  variant?: 'compact' | 'full';  // not allowed
  onRefresh?: () => void;        // not allowed
};

export function LeaveEntitlement({ entitlement }: LeaveEntitlementProps) {
  if (!entitlement) {            // not allowed
    return null;
  }

  return <span>{entitlement.days} days remaining</span>;
}

// the only place it is rendered
<LeaveEntitlement entitlement={entitlement} />
```

# 16. Banned constructs

**`enum` is never written.**

**`any` is never written as a type.**

**The non-null assertion `!` is never written.**

**A type assertion never escapes a type the compiler worked out.** Where the
compiler says a value may be absent, or may be one of several types, the value is
checked rather than asserted away.

**A value is never substituted for an absent optional property.** Where a
property is optional and the type that receives it is one this product does not
own, the property is passed only when it has a value, and is left out otherwise.

**A comparison is written with `===` or `!==`. `==` and `!=` are never
written.**

**An object received from outside is never merged into another by walking its
keys.** Data received from outside that is used as a lookup by key is held in a
`Map`, never in a plain object.

**A promise nobody waits for is never written.**

**A promise is never given where a value that is not a promise is expected.**

**`dangerouslySetInnerHTML` is never written.**

**Nor is HTML written into the page through the DOM directly**, such as with
`innerHTML`, `outerHTML`, `insertAdjacentHTML` or `document.write`.

**`eval` and `new Function` are never written, and no code is built from data
and run.**

**A default export is never written.** Every export is named. The one exception
is `vite.config.ts`.

**`export *` is never written**, so a frontend has no barrel file.

**An import cycle is never created**, being two files that each import the other,
either directly or through a chain of others.

**`fetch` is never called directly.** The one exception is the code that reads the
settings file before the application renders, and section 7 states where that
setting comes from.

**`import.meta.env` is never read.**

**`window.location` is never used to navigate.** Reading the address is not
navigating, and section 9 states what navigation is.

**A link whose address comes from data is rendered only when its scheme is
`https` or `http`.**

**A file fetched from an API is never shown as a page at the frontend's own
address**, such as by turning it into an object URL and giving that to a
frame, a window or a navigation.

**`useMemo`, `useCallback` and `React.memo` are never written by hand.**

**Cryptography is never written by hand.** Only an industry-validated
implementation performs it.

**A construct this section does not name is not allowed by its absence.**

# 17. Enforcers

Every rule in this document has an enforcer, and this section names it. Where that
thing is nothing, the last list says so, because a column that claims coverage it
does not have is worse than no column.

The three parts below are one question asked three ways: is the rule caught
without anyone reading the increment, only by reading it, or not at all.

## What is caught without reading the increment

| Rule | Enforcer |
|---|---|
| ESLint states no formatting rules | `eslint-config-prettier` |
| A frontend shares no code with another frontend | `import-x/no-restricted-paths`, for an import that crosses two frontend folders, and not for a package under `Code/` that both import from |
| A page sits in its own folder under `pages/` | `eslint-plugin-check-file` |
| A folder is created when its first file is written | git, which does not record an empty folder |
| Every folder is lowercase, kebab-case where the name has more than one word | `eslint-plugin-check-file` |
| A file's name carries the casing of the thing it exports | `eslint-plugin-check-file` |
| A hook's name begins with `use` | `eslint-plugin-react-hooks`, for a hook that calls another hook and not for a store |
| A module never imports another module's `pages/`, `hooks/` or `stores/` | `import-x/no-restricted-paths` |
| Nothing under `src/components/`, `src/hooks/` or `src/lib/` imports from `src/modules/` or from `src/api/` | `import-x/no-restricted-paths` |
| `tsconfig.app.json` sets `strict` and the nine settings | `create-project`, which writes the file, and `feature-review` |
| The `@/` alias is declared in all three files and the three agree | `create-project`, which writes all three declarations, and `tsc -b`, which fails when the `tsconfig.app.json` declaration is missing |
| The API types are generated into `src/api/<name>-api/types.ts`, one file per API, and committed | `create-project`, which wires the frontend to the generator |
| A generated file is never edited | The frontend build, which regenerates before the type check, so code written against an edit fails at `tsc` |
| Every call to a server goes through a query or a mutation | `no-restricted-syntax` |
| After the two returns a query's data is read directly | `@typescript-eslint/no-unnecessary-condition`, which sees the optional chain and the fallback only once the two returns sit above them |
| The route table is `src/router.ts` and `App.tsx` renders it | `create-project`, which writes both |
| The route that draws the application frame carries the application's one `ErrorBoundary` | `create-project`, which writes it onto that route, and `feature-review` |
| What the `ErrorBoundary` shows is a fixed message, carrying neither the exception's message nor its stack | `create-project`, which writes it onto the application frame's `ErrorBoundary`, and `feature-review` |
| No loaders and no actions | `no-restricted-syntax` |
| Never a bare `<a>` for an address inside the application | `no-restricted-syntax` |
| A password field and a one-time-code field never refuse a paste, and never turn off the browser's own filling in | `no-restricted-syntax` |
| A component never writes a raw colour, spacing or font value | `no-restricted-syntax` for a `className` and a `style` prop, and `better-tailwindcss/no-restricted-classes` for a `cva()` call, an arbitrary font family and a colour that is not a hex, everywhere except `src/components/ui/`. Neither sees a class string held in a variable |
| A component shadcn copies is renamed on arrival and its import paths are corrected | `eslint-plugin-check-file` for the name, and `tsc` for the import path |
| A control that does something is a real control | `eslint-plugin-jsx-a11y` |
| Every input has a label associated with it | `eslint-plugin-jsx-a11y` |
| An image carries alternative text | `eslint-plugin-jsx-a11y`, for the presence of the text and not its content |
| Focus moves to the page's heading when the route changes | `create-project`, which writes the application frame, and `feature-review` |
| The document is given a language | `create-project`, which writes `index.html`, and `feature-review` |
| `tabIndex` is never a positive number | `eslint-plugin-jsx-a11y` |
| A component reads nothing that changes on its own while it renders | `eslint-plugin-react-hooks`, for the impure calls it knows by name, and not for a constructor or for anything read off the browser |
| A condition the caller cannot produce gets no check and no branch | `@typescript-eslint/no-unnecessary-condition`, for the half a type carries and not for the half a schema or a caller carries |
| `enum` is never written | The compiler, through `erasableSyntaxOnly` |
| `any` is never written as a type | `@typescript-eslint/no-explicit-any` |
| The non-null assertion `!` is never written | `@typescript-eslint/no-non-null-assertion` |
| A comparison is written with `===` or `!==` | `eqeqeq` |
| A promise nobody waits for is never written | `@typescript-eslint/no-floating-promises` |
| A promise is never given where a value that is not a promise is expected | `@typescript-eslint/no-misused-promises` |
| `dangerouslySetInnerHTML` is never written | `no-restricted-syntax` |
| HTML is never written into the page through the DOM directly | `no-restricted-syntax` |
| `eval` and `new Function` are never written | `no-restricted-syntax` |
| A default export is never written | `no-restricted-syntax` |
| `export *` is never written | `no-restricted-syntax` |
| An import cycle is never created | `import-x/no-cycle` |
| `fetch` is never called directly | `no-restricted-syntax` |
| `import.meta.env` is never read | `no-restricted-syntax` |
| `window.location` is never used to navigate | `no-restricted-syntax` |
| `useMemo`, `useCallback` and `React.memo` are never written by hand | `no-restricted-imports` for the two import shapes, and `no-restricted-syntax` for the third |

## What is caught only by reading the increment

**Every rule this document states that the table above does not name, and that the
list below does not name, is caught by `feature-review` and by nothing else.**
Nothing on the build sees any of them, and no test proves any of them.

That is most of this document. A rule reaching this part is not a weaker rule, and
it is not optional because the build is quiet about it.

## What nothing catches

**One rule has no enforcer at all.** Nothing on the build sees it, and reading the
increment does not show it either.

- **A lint rule configured as a warning.** It reports a finding nobody has to fix,
  the run still succeeds, and nothing reports that the rule is not an error. The
  line sits in the configuration at `Code/`, which is not part of an increment, so
  there is nothing for a reader of one to open. Section 2 states the rule.
