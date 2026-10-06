# Worknexa database schema

PostgreSQL database `worknexa_db`, schema `worknexa`. Entity Framework Core is the source of the schema.

## Identifiers and audit

Every table uses a `uniqueidentifier` primary key. Every table stores:

| Column | Purpose |
|---|---|
| `CreatedAt` | When the row was inserted |
| `UpdatedAt` | When the row was last changed |
| `CreatedBy` | Optional user who inserted the row |
| `UpdatedBy` | Optional user who last changed the row |

`CreatedBy` and `UpdatedBy` are nullable so the first user and other system seed rows can exist. The API stamps `CreatedAt` and `UpdatedAt` on save when they are missing, and stamps the user columns from the signed-in session when one exists.

Foreign keys are indexed. Lookup and uniqueness indexes are added where queries need them: task status and assignee, event and meeting start times, notification read state, attachment storage keys, and the join-table pairs listed below.

## Uniqueness

| Table | Unique key |
|---|---|
| `Users` | `Email`, among rows that are not deleted |
| `Roles` | `Name`, among rows that are not deleted |
| `Permissions` | `Key`, among rows that are not deleted |
| `UserRoles` | `UserId`, `RoleId` |
| `RolePermissions` | `RoleId`, `PermissionId` |
| `TeamMembers` | `TeamId`, `UserId` |
| `TaskDependencies` | `TaskId`, `DependsOnTaskId` |
| `TaskTags` | `TaskId`, `TagId` |
| `Tags` | `Name`, among rows that are not deleted |
| `EventParticipants` | `CalendarEventId`, `UserId` |
| `MeetingParticipants` | `MeetingId`, `UserId` |
| `Standups` | `TeamId`, `StandupDate`, among rows that are not deleted |
| `StandupEntries` | `StandupId`, `UserId` |
| `ProjectMembers` | `ProjectId`, `UserId` |
| `LeaveTypes` | `Name`, among rows that are not deleted |
| `NoteTags` | `NoteId`, `TagId` |
| `Attachments` | `StorageKey` |

Check constraints keep a task from depending on itself, keep event and meeting end times at or after the start, and keep leave and project end dates at or after the start.

## Soft delete

These tables have `DeletedAt` and `DeletedBy`, and Entity Framework hides rows where `DeletedAt` is set:

`Users`, `Roles`, `Permissions`, `Teams`, `Tasks`, `TaskComments`, `Tags`, `TodoItems`, `Reminders`, `CalendarEvents`, `Meetings`, `MeetingNotes`, `MeetingActionItems`, `Standups`, `StandupEntries`, `Projects`, `LeaveTypes`, `Leaves`, `LearningTopics`, `LearningResources`, `LearningNotes`, `Notes`, `Notifications`, `Attachments`.

Membership and link rows are removed directly: `UserRoles`, `RolePermissions`, `TeamMembers`, `ProjectMembers`, `EventParticipants`, `MeetingParticipants`, `TaskDependencies`, `TaskTags`, `NoteTags`.

`ActivityLogs` is append-only and is not soft-deleted.

## Relationships that cross aggregates

Deleting a parent removes rows that exist only as part of that parent: role assignments, task comments and tags, meeting participants, notes, and action items, standup entries, learning resources and notes, and note tags.

A task may belong to a project, an assignee, a team, and a parent task. Those links are not cascading deletes, so removing one record does not delete the others. The same rule applies to meeting organizers, project teams, leave reviewers, and reminder targets.

`Attachments` and `ActivityLogs` store `EntityName` plus `EntityId`. That avoids a foreign key from those tables to every other table.

## Seed data

The initial migration inserts:

- Permissions for users, teams, tasks, projects, calendar, meetings, leave, learning, notes, and notifications
- `Admin`, `Manager`, and `TeamMember` roles. `Admin` has every permission. Further roles are additional `Roles` and `RolePermissions` rows
- Development users `admin@worknexa.local`, `manager@worknexa.local`, and `member@worknexa.local`, with password hashes and no plaintext passwords stored
- Leave types `Annual`, `Sick`, and `Unpaid`

No tasks, projects, or meetings are seeded.
