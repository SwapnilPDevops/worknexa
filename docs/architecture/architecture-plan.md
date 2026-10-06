# Worknexa architecture plan

**Worknexa — Your Work. One Intelligent Place.**

Worknexa is a personal work operating system: daily work, todos, tasks, calendar, reminders, meetings, standup notes, meeting notes, projects, team members, team tasks, team leaves, learning, learning notes, knowledge, notifications, activity, and AI-assisted productivity.

This plan describes how those areas attach to the repository that exists today. It does not implement them.

## Current system

```
Browser
  AppShell
    Sidebar          NavId + project list + ThoughtsCard
    Header           search + profile
    Dashboard        project board only
         │
         └── in-memory Task[] and Project[] from src/data/board.ts
```

There is no server, database, session, or external system. That is intentional for this phase. The board UI is the part that must keep working unchanged.

## Target shape

The client remains the Vite app. A server and database appear only in a later phase, and only once.

```
React client (existing)
  layout/        AppShell, Sidebar, Header          reuse
  ui/            buttons, menus, badges, icons      reuse
  features/      one folder per product area        add
  data/          board seed stays; new domains get their own modules
        │
        │  later, one JSON API
        ▼
Worknexa API (does not exist yet)
  identity
  domain records
  activity log
  connector ports ── Outlook, Teams, Google Calendar, GitHub,
                     Azure DevOps, Jira, Gmail, Slack, OpenAI
        │
        ▼
One relational database (does not exist yet)
```

The UI never calls those products directly. Each future connector implements the same port: connect, disconnect, and sync into Worknexa records.

## Product areas and where they live

| Area | Home in the client | Relationship to today’s code |
|---|---|---|
| Projects, project tasks, members on a project | `features/Dashboard` | Already implemented as the Figma board. Keep `Task`, `Project`, `Board`, and `TaskCard`. |
| Daily work and personal todos | `features/Tasks` | New. May reuse `TaskCard` and `PriorityBadge` if the card layout matches. Personal todos are not the same list as the Mobile App board unless the user files them on that project. |
| Calendar, reminders, meetings | `features/Calendar` | New screen inside `AppShell`. The header calendar button should open this area. No calendar library until a view cannot be built with the existing layout components. |
| Standup notes, meeting notes, learning notes, knowledge | `features/Notes` | New. `ThoughtsCard` remains the short sidebar composer. Longer notes do not replace that card. |
| Team members, team tasks, team leaves | `features/Team` | New. `AvatarGroup` and the sidebar Members item are the existing hooks. Member photos stay in `public/assets` until real people exist. |
| Learning | `features/Learning` | New. Learning notes use the notes module rather than a second editor stack. |
| Notifications | Header bell | The button already exists. A later panel lists notification records. Do not add a second bell. |
| Activity | `features/Activity` | A read model of things the user already did (task moved, note written, leave recorded). It is not a separate source of truth. |
| AI assistance | Actions on notes, tasks, and meetings | A later server connector. No chat shell that bypasses `AppShell`. |

`Sidebar` `navItems` grow only when a destination has a screen. Until then, Home, Messages, Tasks, Members, and Settings keep their current labels and icons.

## Domain model (conceptual)

These are the records later phases persist. Names line up with the types that already exist where they overlap.

- **Project** — already `{ id, name, dot }`. Later add nothing visible on the board until a field is shown.
- **Task** — already the board card. A future `source` field (`worknexa`, or a connector id plus external id) lets Jira or Azure DevOps items appear as tasks without a new card.
- **Todo** — a personal task that may not belong to a project column. Do not overload `ColumnId` for reminders.
- **Event** — calendar item, meeting, or reminder. Meetings can point at a note.
- **Note** — standup, meeting, learning, or knowledge. `kind` distinguishes them.
- **Person** — team member. Today’s member list is four image paths, not people.
- **Leave** — person, range, status.
- **Notification** — generated when a record changes. The user can dismiss it.
- **Activity** — append-only description of a change. Connectors write activity when they sync.

`src/data/board.ts` remains the board seed. New domains do not get appended to that file.

## Integration boundary

Not built now. When built, each system maps inward:

| System | Worknexa records it can feed |
|---|---|
| Microsoft Outlook | Events, reminders |
| Microsoft Teams | Meetings, meeting notes metadata |
| Google Calendar | Events |
| Gmail | Messages that the user turns into tasks or notes |
| Slack | Messages the user saves; notifications |
| GitHub | Tasks from issues; activity |
| Azure DevOps | Tasks from work items |
| Jira | Tasks from issues |
| OpenAI | Drafts and summaries attached to a note or task. Not a system of record. |

Sync stores the external id. The board still renders `TaskCard`. Disconnecting a connector stops sync. It does not delete the user’s own notes.

OAuth tokens and the OpenAI key stay on the future server. The Vite app stores neither.

## Boundaries that protect the current UI

- Do not rewrite `Dashboard`, `Board`, or `TaskCard` to host calendar, learning, or AI.
- Do not replace `tokens.css` or restyle the shell to fit a new feature.
- Do not add a second layout component. `AppShell`’s `children` is the extension point.
- Do not move Figma assets out of `public/assets`.
- Unused template files (`src/App.css`, `src/index.css`, `src/assets/*.svg`) can be removed in a cleanup change. They are not part of the running app. That cleanup is optional and separate from feature work.

## State ownership

Today `App.tsx` holds sidebar collapse, nav, projects, tasks, search, priority, period, view mode, and rename mode. That is acceptable while there is one screen.

When the second screen arrives:

- Shell state (collapsed, active nav, search query) stays above the screen.
- Board state moves next to `features/Dashboard`, still as React state.
- The new screen owns its own state.
- Shared records, if any, move to one module both screens import. That module is plain TypeScript until a library is justified in `technology-decisions.md`.

## Error and empty behavior

The board already shows “No tasks” when a filter or another project has no cards. New screens should use the same text color and type scale for empty and error text. They should not introduce a new illustration style unless a later design provides one.

## Security rules for the future API

- Browser holds a session cookie or short-lived token for Worknexa only.
- Connector credentials are server-side and scoped to the Worknexa user.
- AI prompts may include a note the user selected. They must not include every connector token or unrelated mailbox.
- Activity entries store what changed, not secrets.

## Recommended implementation roadmap

Each phase ends with the current board still matching the Figma screen. No phase below is started by this document.

### Phase 0 — Baseline (done)

Figma project board, shell, tokens, and in-memory interactions (search, filter, drag, add task, rename project, sidebar collapse).

### Phase 1 — Navigation without new packages

Give Tasks, Members, and Settings real screens inside `AppShell`, using existing buttons, type, and empty states. Leave Messages as-is until there is a message model. Keep `NavId` in React state. Do not add a router.

### Phase 2 — Personal work on the client

Add todos, reminders, and notes as feature modules with their own types. Reuse badges, menus, and avatars. Persist new personal records in the browser only if refreshing must keep them. Leave the Mobile App board seed intact.

### Phase 3 — Calendar and meetings

Add a calendar screen opened from the existing header calendar button. Model events, meetings, standup notes, and meeting notes. Still no Google or Outlook connection.

### Phase 4 — Team

Add people, team tasks, and leaves. Reuse `AvatarGroup`. Replace the static header profile only when a person record exists.

### Phase 5 — Learning and knowledge

Add learning items and knowledge pages. Notes from Phase 2 are the editor. Do not add a second rich-text stack unless plain text cannot represent the content.

### Phase 6 — Notifications and activity

Feed the existing bell from notification records. Record activity when tasks, notes, events, and leaves change. Activity is derived from those actions.

### Phase 7 — Extract testable rules and add a runner

Move filtering, task moves, and note creation into plain functions. Add Vitest and test those functions. This is the first new dev dependency, and only if Phase 2 or later has logic worth testing.

### Phase 8 — API and database

Introduce one backend and one relational database. Move browser persistence behind that API. Add Worknexa sessions. The React app gains a single client module for HTTP. Components still receive props.

### Phase 9 — Connectors

Add connector ports on the server, one vendor at a time. Suggested first connections, because they match tasks and calendar already in the model: Google Calendar or Outlook for events, then Jira or GitHub for tasks. Teams, Gmail, Slack, and Azure DevOps follow the same port. The UI shows a connection as a status, not as a vendor SDK screen.

### Phase 10 — AI assistance

Add the OpenAI connector on the server. First actions: summarize a meeting note, draft a standup from today’s tasks, suggest a next task. Results are written as notes or task descriptions the user can edit. No autonomous changes to team leaves or external systems.

## Out of scope for every phase until its number is reached

- Installing router, state, UI kit, ORM, auth, or vendor packages “for later”.
- Implementing OAuth or storing API keys.
- Replacing the project board with a different dashboard.
- Building a second shell for mobile apps or a desktop wrapper.
