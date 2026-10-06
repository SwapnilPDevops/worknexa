# Worknexa repository analysis

Product: **Worknexa — Your Work. One Intelligent Place.**

This document records what the repository contains today. It is the baseline for later work. Nothing in the current app should be replaced until a later phase has a concrete reason to extend it.

## 1. Frontend technology

The application is a single-page client.

| Piece | Choice |
|---|---|
| UI library | React 19 |
| Language | TypeScript 6 |
| Bundler | Vite 8 with `@vitejs/plugin-react` |
| Entry | `index.html` → `src/main.tsx` → `src/App.tsx` |
| Styling | Global CSS. Design tokens in `src/styles/tokens.css`. Layout and components in `src/styles/app.css` |
| Fonts | Inter and Roboto, loaded from Google Fonts in `index.html` |

There is no router, no CSS framework, and no component library. Navigation is a `NavId` value held in React state. Every nav item still renders the same `Dashboard`.

## 2. Backend technology

None. There is no server project, no HTTP client, and no `fetch` usage in `src`.

## 3. Database

None. Task, project, and member data live in memory. The seed is `src/data/board.ts`. `App` copies that seed into `useState`. Refreshing the page discards edits.

## 4. Folder structure

```
WorkNexa/
  index.html
  package.json
  vite.config.ts
  tsconfig.json
  tsconfig.app.json
  tsconfig.node.json
  .oxlintrc.json
  public/assets/          Figma-exported SVG and PNG files
  scripts/download-assets.mjs
  src/
    main.tsx
    App.tsx               All client state for the board
    data/board.ts         Types and mock data
    styles/tokens.css
    styles/app.css
    components/
      layout/             AppShell, Sidebar, Header
      ui/                 Icon, Badge, Button, Dropdown, AvatarGroup
      features/Dashboard/ Dashboard, Board, TaskCard, ThoughtsCard
    App.css               Unused Vite template stylesheet
    index.css             Unused Vite template stylesheet
    assets/               Unused Vite and React SVGs
```

`src/App.css` and `src/index.css` are not imported. `main.tsx` imports `styles/tokens.css` and `styles/app.css` only.

## 5. Dependencies

Runtime:

- `react`
- `react-dom`

Development:

- `typescript`
- `vite`
- `@vitejs/plugin-react`
- `oxlint`
- `@types/node`, `@types/react`, `@types/react-dom`

Scripts:

- `npm run dev` — Vite dev server
- `npm run build` — `tsc -b` then `vite build`
- `npm run lint` — Oxlint
- `npm run preview` — preview the production build

No test runner is installed.

## 6. Reusable components

These already match the current visual design and should be extended, not copied.

| Component | Role | Reuse |
|---|---|---|
| `AppShell` | Page chrome: gray canvas, white shell, sidebar, header, main slot | Wrap every future screen. Pass new main content as `children`. |
| `Sidebar` | Brand, primary nav, project list, Thoughts Time | Keep as the only navigation surface. Add destinations by extending `navItems`, not by adding a second sidebar. |
| `Header` | Search, calendar, help, notifications, profile | Keep the search contract (`query`, `onQuery`). Future notification and calendar actions belong on these existing buttons. |
| `OutlineButton`, `IconButton` | Bordered and icon actions | Use for Share-style and icon actions. |
| `Dropdown` | Filter and period menus | Use for any menu that matches this control. It already closes on outside click and Escape. |
| `PriorityBadge`, `CountBadge` | Priority and column counts | Use for task priority. Do not invent a second badge style. |
| `AvatarGroup` | Overlapping photos | Use for members on cards and in the header. |
| `Icon` and the named icon wrappers | Figma SVG assets at their intrinsic size | Add icons as files under `public/assets` and render them through `Icon`. |
| `TaskCard`, `Board` | Kanban card and three columns | These are the project-board UI. Personal todos can reuse `TaskCard` only when the visual treatment is the same. |
| `ThoughtsCard` | Sidebar note composer | Keep for short personal notes until a notes feature has its own screen. |
| `Dashboard` | Project title, invite row, toolbar, board | This is the Mobile App project screen from the design. Do not turn it into a generic home page. |

## 7. API patterns

There is no API. The only data pattern is:

1. Types and seed arrays are exported from `src/data/board.ts`.
2. `App` owns the mutable copies.
3. Children receive data and callbacks through props.

Search, priority filter, and project selection are derived in `App` with `useMemo`. Drag-and-drop and the card menu both call `moveTask`.

Future HTTP access should sit behind a small module that returns the same domain types the UI already uses. Components should not call vendor APIs.

## 8. Authentication

None. The profile control shows a fixed person, Anima Agrawal, and opens a local menu. There is no session, token, login route, or protected action.

## 9. Configuration

| File | What it configures |
|---|---|
| `vite.config.ts` | React plugin only. No aliases, proxy, or env schema. |
| `tsconfig.app.json` | Strict unused locals, `verbatimModuleSyntax`, `erasableSyntaxOnly`, JSX `react-jsx`. |
| `tsconfig.node.json` | Typecheck for `vite.config.ts`. |
| `.oxlintrc.json` | React, TypeScript, and Oxc plugins. Hooks rule is an error. |
| `index.html` | Document title `Project M.` and font links. |

There is no `.env` file and no environment variable is read in source.

## 10. Tests

None. There are no `*.test` or `*.spec` files and no test script.

## 11. Coding conventions

Observed in `src`:

- Function components with explicit prop types.
- Domain types are string unions (`Priority`, `ColumnId`, `NavId`), not enums. `erasableSyntaxOnly` rejects enums.
- Type-only imports use `import type` because `verbatimModuleSyntax` is on.
- App source uses double quotes. The Vite config file still uses the template’s single quotes.
- `App` is the only default export. Feature and UI components are named exports.
- Styling is global class names, not CSS modules or inline style objects, except where a dimension must be set from a prop (`Dropdown` width, avatar size).
- State updates for lists are immutable (`map` / spread).
- Relative imports only. There is no `@/` alias.
- `noUnusedLocals` and `noUnusedParameters` are enabled, so unused props and imports fail `npm run build`.

`App.tsx` currently holds shell state, project state, task state, filters, and view state. New features should not keep growing this component. Extract a feature module when a second screen needs its own state.

## 12. UI design system

The visual source is the Figma frame “Project management”. Tokens are CSS variables on `:root` in `src/styles/tokens.css`.

Important tokens:

- Page `#e7e8ea`, surface `#ffffff`, column `#f5f5f5`
- Text `#0d062d` and `#787486`
- Primary `#5030e5`
- Priority colors for Low, High, and Completed
- Column accents for To Do, On Progress, and Done
- Radius: shell 30px, cards 16px, controls 6px, badges 4px
- Shell shadow `0 44px 84px 6px #d8d9db`
- Shell size 1440×932, sidebar 250px, header 88px
- UI font Inter

Responsive behavior is in `src/styles/app.css` at 1280px, 1024px, and 720px. Desktop layout is fixed to the Figma measurements. Smaller widths stack the sidebar and the columns.

Assets in `public/assets` are the Figma exports. `Icon` sets `width` and `height` attributes from the design. New screens should use these tokens and assets. They should not introduce a second color palette or a second icon set.

## What must stay

- The board screen, its measurements, and its mock content for the Mobile App project.
- `AppShell`, `Sidebar`, `Header`, and the `ui` components.
- `src/styles/tokens.css` as the single token file.
- The current dependency set until a later phase needs a capability the repo cannot express.

## What is absent, and should not be invented inside the current files

Worknexa’s product scope (daily work, reminders, meetings, standup notes, leaves, learning, knowledge, activity, and AI) has no modules yet. Those belong in new feature folders that render inside `AppShell`. They should not be folded into `Dashboard` or `board.ts` beyond shared types that are genuinely the same concept.
