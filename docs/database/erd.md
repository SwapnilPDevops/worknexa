# Worknexa entity relationship diagram

Primary keys are `uniqueidentifier` values. Every table also has `CreatedAt`, `UpdatedAt`, `CreatedBy`, and `UpdatedBy`. `CreatedBy` and `UpdatedBy` reference `Users` and are omitted below so the business relationships stay readable. Soft-deleted tables add `DeletedAt` and `DeletedBy`.

## Identity and organization

```mermaid
erDiagram
  Users ||--o{ UserRoles : assigned
  Roles ||--o{ UserRoles : granted
  Roles ||--o{ RolePermissions : includes
  Permissions ||--o{ RolePermissions : granted
  Teams ||--o{ TeamMembers : has
  Users ||--o{ TeamMembers : joins
  Teams ||--o{ Projects : owns
  Teams ||--o{ Tasks : scopes
  Teams ||--o{ Standups : runs
  Teams ||--o{ CalendarEvents : scopes
  Teams ||--o{ Meetings : scopes
```

## Productivity

```mermaid
erDiagram
  Projects ||--o{ Tasks : contains
  Users ||--o{ Tasks : assigned
  Tasks ||--o{ Tasks : parent
  Tasks ||--o{ TaskComments : has
  Tasks ||--o{ TaskDependencies : depends
  Tasks ||--o{ TaskDependencies : blocks
  Tasks ||--o{ TaskTags : tagged
  Tags ||--o{ TaskTags : applied
  Users ||--o{ TodoItems : owns
  Users ||--o{ Reminders : receives
  Tasks ||--o{ Reminders : reminds
  CalendarEvents ||--o{ Reminders : reminds
  TodoItems ||--o{ Reminders : reminds
```

## Calendar, meetings, and projects

```mermaid
erDiagram
  Users ||--o{ CalendarEvents : organizes
  CalendarEvents ||--o{ EventParticipants : includes
  Users ||--o{ EventParticipants : attends
  Users ||--o{ Meetings : organizes
  Projects ||--o{ Meetings : schedules
  CalendarEvents ||--o{ Meetings : linked
  Meetings ||--o{ MeetingParticipants : includes
  Users ||--o{ MeetingParticipants : attends
  Meetings ||--o{ MeetingNotes : records
  Meetings ||--o{ MeetingActionItems : produces
  Users ||--o{ MeetingActionItems : assigned
  Tasks ||--o{ MeetingActionItems : tracks
  Standups ||--o{ StandupEntries : collects
  Users ||--o{ StandupEntries : writes
  Projects ||--o{ ProjectMembers : has
  Users ||--o{ ProjectMembers : joins
```

## Leave, learning, knowledge, and system

```mermaid
erDiagram
  Users ||--o{ Leaves : requests
  LeaveTypes ||--o{ Leaves : classifies
  Users ||--o{ Leaves : reviews
  Users ||--o{ LearningTopics : studies
  LearningTopics ||--o{ LearningResources : includes
  LearningTopics ||--o{ LearningNotes : records
  Users ||--o{ Notes : writes
  Projects ||--o{ Notes : scopes
  Notes ||--o{ NoteTags : tagged
  Tags ||--o{ NoteTags : applied
  Users ||--o{ Notifications : receives
```

`ActivityLogs` and `Attachments` point at a record with `EntityName` and `EntityId` instead of a foreign key to every table.
