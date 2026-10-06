# Worknexa technology decisions

The foundation now includes the approved client and server stack. Decisions below that said “do not add a router, Tailwind, or a server yet” are superseded for tooling only. The project board, CSS tokens, and rule against vendor SDKs still stand. Business modules are still not implemented.

Approved foundation:

- Frontend: React 19, TypeScript, Vite, React Router, Tailwind CSS (utilities only; preflight is off), shadcn/ui, Lucide, TanStack Query, React Hook Form, Zod, Recharts, date-fns.
- Backend: ASP.NET Core, C#, Entity Framework Core, PostgreSQL, clean architecture, FluentValidation, Serilog.

These decisions keep the current Vite + React client and add new technology only when the product cannot be expressed with what is already installed.

Tagline: **Worknexa — Your Work. One Intelligent Place.**

## Decision 1 — Keep the existing frontend stack

**Decision.** React 19, TypeScript, and Vite stay the client. Do not introduce Angular, Blazor, or a second SPA.

**Reason.** The repository is already that stack. The shell, board, and design tokens are written for it.

**Consequence.** New screens are React function components under `src/components/features`.

## Decision 2 — Keep the CSS token system

**Decision.** Do not add Tailwind, CSS-in-JS, or a third-party component kit.

**Reason.** `src/styles/tokens.css` and `src/styles/app.css` already implement the Figma design. A kit would restyle buttons, cards, and the sidebar.

**Consequence.** New UI uses existing components and new classes that reference the current variables. If a token is missing, add it to `tokens.css` instead of hard-coding a one-off color in a feature.

## Decision 3 — Do not add a router yet

**Decision.** Keep navigation as `NavId` state in `App`, passed through `AppShell` and `Sidebar`.

**Reason.** `navItems` has five entries and only one screen. A router package would not change the user’s current experience.

**Revisit when** a screen must be bookmarkable or shareable (a meeting, a note, a project). At that point add one router and map `NavId` to routes. Do not keep both a parallel nav state and a router with different names.

## Decision 4 — Do not add a global store yet

**Decision.** Feature state stays in React `useState` and props. Do not install Redux, Zustand, or a query library for the current board.

**Reason.** One component owns the board. The data set is a single in-memory array.

**Revisit when** two screens edit the same tasks or notes. The first shared client cache should be a small module that exposes the same `Task` and `Project` types. Install a library only if that module becomes hard to keep correct.

## Decision 5 — Domain types stay in TypeScript unions

**Decision.** Continue modeling status, priority, and navigation as string unions. Do not add TypeScript enums.

**Reason.** `tsconfig.app.json` sets `erasableSyntaxOnly`, which rejects enums. `src/data/board.ts` already follows this.

## Decision 6 — UI talks only to a Worknexa boundary

**Decision.** React components never import Outlook, Teams, Google, GitHub, Azure DevOps, Jira, Gmail, Slack, or OpenAI SDKs.

**Reason.** Those systems need secrets, OAuth redirects, and different payloads. Putting them in the browser would scatter credentials and couple screens to vendors.

**Shape, later.** A connector implements one interface:

```ts
type ConnectorId =
  | "outlook"
  | "teams"
  | "google-calendar"
  | "github"
  | "azure-devops"
  | "jira"
  | "gmail"
  | "slack"
  | "openai";

type Connector = {
  id: ConnectorId;
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  sync(): Promise<void>;
};
```

Each vendor maps into Worknexa records (task, event, message, note, activity). The board keeps using `Task`. An external Jira issue becomes a `Task` plus a source reference, not a second card component.

**Now.** Do not create connector packages, OAuth apps, or SDK installs.

## Decision 7 — Persistence waits until a screen needs it

**Decision.** Leave the board on in-memory state seeded from `src/data/board.ts`.

**Reason.** The current screen is a designed mock. Writing it to disk now would add a storage layer with no second consumer.

**Later order.**

1. Browser persistence for personal items the user creates (todos, notes), still without a server.
2. An HTTP API only when data must be shared across devices or when an integration must store tokens.

Do not introduce a database in the repo before that API exists. There is no database to reuse.

## Decision 8 — Backend is a separate process, added once

**Decision.** When the API is needed, add one backend. Do not start a second framework beside it, and do not put secret-bearing integration code in Vite.

**Recommended shape.**

- One process that serves JSON to the existing React app.
- Worknexa user identity is separate from each connector’s OAuth token.
- Tokens for Outlook, Google, GitHub, Jira, Slack, and the others stay on the server.
- OpenAI calls go through that server so the API key is not in the client bundle.

The language of that process is not chosen yet, because the repository has no server and choosing one now would be an unused install. Choose it in the phase that creates the API, using the team’s existing runtime if one is already standard outside this repo. This repository does not contain that runtime today.

## Decision 9 — Database, when the API exists

**Decision.** Use one relational database behind the API. Prefer Postgres if Worknexa is hosted. SQLite is acceptable for a single-user local install. Do not run both.

**Reason.** Tasks, events, notes, members, and leaves are relational. Connector sync needs stable ids and a source reference (`connector`, `externalId`).

**Not now.** No database driver is installed, and none should be until Decision 8 is in progress.

## Decision 10 — Authentication, when the API exists

**Decision.** Worknexa login is an application session. Connecting Outlook or Slack is an additional grant stored against that session. The current static profile menu stays until a real user exists.

**Reason.** The header already shows one person. Replacing that control with a login form before there is an API would be a new screen with nowhere to persist the account.

## Decision 11 — Tests start with the domain, not the pixels

**Decision.** Do not add a test runner in this architecture pass.

**Revisit when** task moves, filters, or note storage are extracted from `App` into plain functions. Test those functions first. Add a runner then (Vitest fits Vite). Do not snapshot the Figma board as the first test suite.

## Decision 12 — Configuration stays explicit

**Decision.** Future secrets go in environment variables read by the server, not by feature components. Vite may read only public values, prefixed and documented. Do not add a config package.

**Now.** `vite.config.ts` stays as it is.

## Decision 13 — AI is a connector, not a screen framework

**Decision.** AI-assisted productivity calls the same server boundary as other connectors. It may summarize notes, draft a standup, or suggest a task. It does not get its own UI toolkit or a direct browser call to OpenAI.

**Now.** No AI SDK.

## Packages that are intentionally not installed

| Package area | Why it waits |
|---|---|
| React Router | One screen. `NavId` already switches the active item. |
| State or data library | One owner for board state. |
| Tailwind or a component kit | Tokens and `ui` components already exist. |
| Vendor SDKs | No integration is being built. |
| ORM, database driver, auth library | No server exists. |
| Test runner | No extracted logic to test yet. |

## Reuse rules for later implementation

1. Render new product areas inside `AppShell`.
2. Import `OutlineButton`, `IconButton`, `Dropdown`, `PriorityBadge`, `CountBadge`, `AvatarGroup`, and `Icon` before creating a lookalike.
3. Extend `src/data/board.ts` only for the project board. Other domains get their own type module so the board seed does not become the whole product model.
4. Keep Figma measurements for the existing board. New screens use the same tokens and do not restyle the shell.
5. Leave `public/assets` as the icon source. The download script `scripts/download-assets.mjs` is how those files were fetched. Do not replace icons with a new icon font.
