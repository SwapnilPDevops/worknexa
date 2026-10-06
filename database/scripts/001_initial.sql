DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'worknexa') THEN
        CREATE SCHEMA worknexa;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS worknexa."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'worknexa') THEN
        CREATE SCHEMA worknexa;
    END IF;
END $EF$;

CREATE TABLE worknexa."Users" (
    "Id" uuid NOT NULL,
    "Email" character varying(320) NOT NULL,
    "DisplayName" character varying(200) NOT NULL,
    "PasswordHash" character varying(512),
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Users_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Users_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."ActivityLogs" (
    "Id" uuid NOT NULL,
    "EntityName" character varying(64) NOT NULL,
    "EntityId" uuid NOT NULL,
    "Action" character varying(64) NOT NULL,
    "Summary" character varying(2000) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    CONSTRAINT "PK_ActivityLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ActivityLogs_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_ActivityLogs_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."Attachments" (
    "Id" uuid NOT NULL,
    "EntityName" character varying(64) NOT NULL,
    "EntityId" uuid NOT NULL,
    "FileName" character varying(260) NOT NULL,
    "ContentType" character varying(128) NOT NULL,
    "SizeBytes" bigint NOT NULL,
    "StorageKey" character varying(512) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Attachments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Attachments_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Attachments_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Attachments_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."LearningTopics" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Title" character varying(300) NOT NULL,
    "Description" text,
    "Status" character varying(32) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_LearningTopics" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LearningTopics_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_LearningTopics_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_LearningTopics_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_LearningTopics_Users_UserId" FOREIGN KEY ("UserId") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."LeaveTypes" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "IsPaid" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_LeaveTypes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LeaveTypes_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_LeaveTypes_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_LeaveTypes_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."Notifications" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Title" character varying(300) NOT NULL,
    "Body" character varying(2000) NOT NULL,
    "IsRead" boolean NOT NULL,
    "ReadAt" timestamp with time zone,
    "Link" character varying(2048),
    "ReminderId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Notifications_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Notifications_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Notifications_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."Permissions" (
    "Id" uuid NOT NULL,
    "Key" character varying(128) NOT NULL,
    "Description" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Permissions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Permissions_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Permissions_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Permissions_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."Roles" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Roles" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Roles_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Roles_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Roles_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."Tags" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Color" character varying(16),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Tags" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Tags_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Tags_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Tags_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."Teams" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(2000),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Teams" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Teams_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Teams_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Teams_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."TodoItems" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Title" character varying(300) NOT NULL,
    "IsCompleted" boolean NOT NULL,
    "Priority" character varying(32) NOT NULL,
    "Recurrence" character varying(32) NOT NULL,
    "DueAt" timestamp with time zone,
    "SortOrder" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_TodoItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TodoItems_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_TodoItems_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_TodoItems_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_TodoItems_Users_UserId" FOREIGN KEY ("UserId") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."LearningNotes" (
    "Id" uuid NOT NULL,
    "LearningTopicId" uuid NOT NULL,
    "Body" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_LearningNotes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LearningNotes_LearningTopics_LearningTopicId" FOREIGN KEY ("LearningTopicId") REFERENCES worknexa."LearningTopics" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LearningNotes_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_LearningNotes_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_LearningNotes_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."LearningResources" (
    "Id" uuid NOT NULL,
    "LearningTopicId" uuid NOT NULL,
    "Title" character varying(300) NOT NULL,
    "Url" character varying(2048),
    "ResourceType" character varying(32) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_LearningResources" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LearningResources_LearningTopics_LearningTopicId" FOREIGN KEY ("LearningTopicId") REFERENCES worknexa."LearningTopics" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LearningResources_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_LearningResources_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_LearningResources_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."Leaves" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "LeaveTypeId" uuid NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "Status" character varying(32) NOT NULL,
    "Reason" character varying(2000),
    "ReviewedById" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Leaves" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Leaves_DateRange" CHECK ("EndDate" >= "StartDate"),
    CONSTRAINT "FK_Leaves_LeaveTypes_LeaveTypeId" FOREIGN KEY ("LeaveTypeId") REFERENCES worknexa."LeaveTypes" ("Id"),
    CONSTRAINT "FK_Leaves_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Leaves_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Leaves_Users_ReviewedById" FOREIGN KEY ("ReviewedById") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Leaves_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Leaves_Users_UserId" FOREIGN KEY ("UserId") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."RolePermissions" (
    "Id" uuid NOT NULL,
    "RoleId" uuid NOT NULL,
    "PermissionId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RolePermissions_Permissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES worknexa."Permissions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_RolePermissions_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES worknexa."Roles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_RolePermissions_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_RolePermissions_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."UserRoles" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "RoleId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    CONSTRAINT "PK_UserRoles" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES worknexa."Roles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserRoles_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_UserRoles_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_UserRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES worknexa."Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE worknexa."Projects" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" text,
    "Status" character varying(32) NOT NULL,
    "TeamId" uuid,
    "StartDate" date,
    "EndDate" date,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Projects" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Projects_DateRange" CHECK ("EndDate" IS NULL OR "StartDate" IS NULL OR "EndDate" >= "StartDate"),
    CONSTRAINT "FK_Projects_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES worknexa."Teams" ("Id"),
    CONSTRAINT "FK_Projects_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Projects_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Projects_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."TeamMembers" (
    "Id" uuid NOT NULL,
    "TeamId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Role" character varying(32) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    CONSTRAINT "PK_TeamMembers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TeamMembers_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES worknexa."Teams" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TeamMembers_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_TeamMembers_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_TeamMembers_Users_UserId" FOREIGN KEY ("UserId") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."TodoTags" (
    "Id" uuid NOT NULL,
    "TodoItemId" uuid NOT NULL,
    "TagId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    CONSTRAINT "PK_TodoTags" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TodoTags_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES worknexa."Tags" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TodoTags_TodoItems_TodoItemId" FOREIGN KEY ("TodoItemId") REFERENCES worknexa."TodoItems" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TodoTags_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_TodoTags_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."CalendarEvents" (
    "Id" uuid NOT NULL,
    "Title" character varying(300) NOT NULL,
    "Description" text,
    "StartAt" timestamp with time zone NOT NULL,
    "EndAt" timestamp with time zone NOT NULL,
    "IsAllDay" boolean NOT NULL,
    "Location" character varying(300),
    "MeetingLink" character varying(500),
    "Kind" character varying(32) NOT NULL,
    "Recurrence" character varying(32) NOT NULL,
    "RecurrenceInterval" integer,
    "ProjectId" uuid,
    "OrganizerId" uuid NOT NULL,
    "TeamId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_CalendarEvents" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_CalendarEvents_Interval" CHECK ("RecurrenceInterval" IS NULL OR ("RecurrenceInterval" BETWEEN 1 AND 366)),
    CONSTRAINT "CK_CalendarEvents_TimeRange" CHECK ("EndAt" >= "StartAt"),
    CONSTRAINT "FK_CalendarEvents_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES worknexa."Projects" ("Id"),
    CONSTRAINT "FK_CalendarEvents_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES worknexa."Teams" ("Id"),
    CONSTRAINT "FK_CalendarEvents_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_CalendarEvents_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_CalendarEvents_Users_OrganizerId" FOREIGN KEY ("OrganizerId") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_CalendarEvents_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."ProjectMembers" (
    "Id" uuid NOT NULL,
    "ProjectId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Role" character varying(32) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    CONSTRAINT "PK_ProjectMembers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProjectMembers_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES worknexa."Projects" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ProjectMembers_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_ProjectMembers_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_ProjectMembers_Users_UserId" FOREIGN KEY ("UserId") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."EventParticipants" (
    "Id" uuid NOT NULL,
    "CalendarEventId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ResponseStatus" character varying(32) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    CONSTRAINT "PK_EventParticipants" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EventParticipants_CalendarEvents_CalendarEventId" FOREIGN KEY ("CalendarEventId") REFERENCES worknexa."CalendarEvents" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EventParticipants_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_EventParticipants_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_EventParticipants_Users_UserId" FOREIGN KEY ("UserId") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."Meetings" (
    "Id" uuid NOT NULL,
    "Title" character varying(300) NOT NULL,
    "Description" text,
    "StartAt" timestamp with time zone NOT NULL,
    "EndAt" timestamp with time zone NOT NULL,
    "Location" character varying(300),
    "MeetingLink" character varying(500),
    "MeetingType" character varying(32) NOT NULL,
    "Agenda" text,
    "NotesText" text,
    "Decisions" text,
    "FollowUps" text,
    "FollowUpAt" timestamp with time zone,
    "OrganizerId" uuid NOT NULL,
    "TeamId" uuid,
    "ProjectId" uuid,
    "CalendarEventId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Meetings" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Meetings_TimeRange" CHECK ("EndAt" >= "StartAt"),
    CONSTRAINT "FK_Meetings_CalendarEvents_CalendarEventId" FOREIGN KEY ("CalendarEventId") REFERENCES worknexa."CalendarEvents" ("Id"),
    CONSTRAINT "FK_Meetings_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES worknexa."Projects" ("Id"),
    CONSTRAINT "FK_Meetings_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES worknexa."Teams" ("Id"),
    CONSTRAINT "FK_Meetings_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Meetings_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Meetings_Users_OrganizerId" FOREIGN KEY ("OrganizerId") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Meetings_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."MeetingNotes" (
    "Id" uuid NOT NULL,
    "MeetingId" uuid NOT NULL,
    "Body" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_MeetingNotes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MeetingNotes_Meetings_MeetingId" FOREIGN KEY ("MeetingId") REFERENCES worknexa."Meetings" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_MeetingNotes_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_MeetingNotes_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_MeetingNotes_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."MeetingParticipants" (
    "Id" uuid NOT NULL,
    "MeetingId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ResponseStatus" character varying(32) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    CONSTRAINT "PK_MeetingParticipants" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MeetingParticipants_Meetings_MeetingId" FOREIGN KEY ("MeetingId") REFERENCES worknexa."Meetings" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_MeetingParticipants_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_MeetingParticipants_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_MeetingParticipants_Users_UserId" FOREIGN KEY ("UserId") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."Standups" (
    "Id" uuid NOT NULL,
    "TeamId" uuid,
    "OwnerId" uuid,
    "MeetingId" uuid,
    "StandupDate" date NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Standups" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Standups_Scope" CHECK (("TeamId" IS NOT NULL AND "OwnerId" IS NULL) OR ("TeamId" IS NULL AND "OwnerId" IS NOT NULL)),
    CONSTRAINT "FK_Standups_Meetings_MeetingId" FOREIGN KEY ("MeetingId") REFERENCES worknexa."Meetings" ("Id"),
    CONSTRAINT "FK_Standups_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES worknexa."Teams" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Standups_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Standups_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Standups_Users_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Standups_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."Tasks" (
    "Id" uuid NOT NULL,
    "Title" character varying(300) NOT NULL,
    "Description" text,
    "Status" character varying(32) NOT NULL,
    "Priority" character varying(32) NOT NULL,
    "StartAt" timestamp with time zone,
    "EstimatedHours" numeric(8,2),
    "ActualHours" numeric(8,2),
    "ProjectId" uuid,
    "AssigneeId" uuid,
    "TeamId" uuid,
    "ParentTaskId" uuid,
    "MeetingId" uuid,
    "DueAt" timestamp with time zone,
    "CompletedAt" timestamp with time zone,
    "SortOrder" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Tasks" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Tasks_Hours" CHECK (("EstimatedHours" IS NULL OR "EstimatedHours" >= 0) AND ("ActualHours" IS NULL OR "ActualHours" >= 0)),
    CONSTRAINT "CK_Tasks_Schedule" CHECK ("StartAt" IS NULL OR "DueAt" IS NULL OR "StartAt" <= "DueAt"),
    CONSTRAINT "FK_Tasks_Meetings_MeetingId" FOREIGN KEY ("MeetingId") REFERENCES worknexa."Meetings" ("Id"),
    CONSTRAINT "FK_Tasks_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES worknexa."Projects" ("Id"),
    CONSTRAINT "FK_Tasks_Tasks_ParentTaskId" FOREIGN KEY ("ParentTaskId") REFERENCES worknexa."Tasks" ("Id"),
    CONSTRAINT "FK_Tasks_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES worknexa."Teams" ("Id"),
    CONSTRAINT "FK_Tasks_Users_AssigneeId" FOREIGN KEY ("AssigneeId") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Tasks_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Tasks_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Tasks_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."StandupEntries" (
    "Id" uuid NOT NULL,
    "StandupId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Yesterday" text,
    "Today" text,
    "Blockers" text,
    "Notes" text,
    "Status" character varying(32) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_StandupEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_StandupEntries_Standups_StandupId" FOREIGN KEY ("StandupId") REFERENCES worknexa."Standups" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_StandupEntries_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_StandupEntries_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_StandupEntries_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_StandupEntries_Users_UserId" FOREIGN KEY ("UserId") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."MeetingActionItems" (
    "Id" uuid NOT NULL,
    "MeetingId" uuid NOT NULL,
    "Title" character varying(300) NOT NULL,
    "Description" text,
    "AssigneeId" uuid,
    "TaskId" uuid,
    "IsCompleted" boolean NOT NULL,
    "DueAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_MeetingActionItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MeetingActionItems_Meetings_MeetingId" FOREIGN KEY ("MeetingId") REFERENCES worknexa."Meetings" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_MeetingActionItems_Tasks_TaskId" FOREIGN KEY ("TaskId") REFERENCES worknexa."Tasks" ("Id"),
    CONSTRAINT "FK_MeetingActionItems_Users_AssigneeId" FOREIGN KEY ("AssigneeId") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_MeetingActionItems_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_MeetingActionItems_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_MeetingActionItems_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."Notes" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Title" character varying(300) NOT NULL,
    "Body" text NOT NULL,
    "Category" character varying(80),
    "IsFavorite" boolean NOT NULL,
    "IsPinned" boolean NOT NULL,
    "ProjectId" uuid,
    "TaskId" uuid,
    "MeetingId" uuid,
    "LearningTopicId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Notes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Notes_LearningTopics_LearningTopicId" FOREIGN KEY ("LearningTopicId") REFERENCES worknexa."LearningTopics" ("Id"),
    CONSTRAINT "FK_Notes_Meetings_MeetingId" FOREIGN KEY ("MeetingId") REFERENCES worknexa."Meetings" ("Id"),
    CONSTRAINT "FK_Notes_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES worknexa."Projects" ("Id"),
    CONSTRAINT "FK_Notes_Tasks_TaskId" FOREIGN KEY ("TaskId") REFERENCES worknexa."Tasks" ("Id"),
    CONSTRAINT "FK_Notes_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Notes_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Notes_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Notes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."Reminders" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TaskId" uuid,
    "CalendarEventId" uuid,
    "TodoItemId" uuid,
    "Title" character varying(300) NOT NULL DEFAULT 'Reminder',
    "Description" text,
    "RemindAt" timestamp with time zone NOT NULL,
    "Message" character varying(500),
    "Recurrence" character varying(32) NOT NULL,
    "RecurrenceInterval" integer,
    "Priority" character varying(32) NOT NULL,
    "IsCompleted" boolean NOT NULL DEFAULT FALSE,
    "IsSent" boolean NOT NULL,
    "MeetingId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_Reminders" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Reminders_Interval" CHECK ("RecurrenceInterval" IS NULL OR ("RecurrenceInterval" BETWEEN 1 AND 366)),
    CONSTRAINT "FK_Reminders_CalendarEvents_CalendarEventId" FOREIGN KEY ("CalendarEventId") REFERENCES worknexa."CalendarEvents" ("Id"),
    CONSTRAINT "FK_Reminders_Meetings_MeetingId" FOREIGN KEY ("MeetingId") REFERENCES worknexa."Meetings" ("Id"),
    CONSTRAINT "FK_Reminders_Tasks_TaskId" FOREIGN KEY ("TaskId") REFERENCES worknexa."Tasks" ("Id"),
    CONSTRAINT "FK_Reminders_TodoItems_TodoItemId" FOREIGN KEY ("TodoItemId") REFERENCES worknexa."TodoItems" ("Id"),
    CONSTRAINT "FK_Reminders_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Reminders_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Reminders_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_Reminders_Users_UserId" FOREIGN KEY ("UserId") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."TaskComments" (
    "Id" uuid NOT NULL,
    "TaskId" uuid NOT NULL,
    "Body" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    CONSTRAINT "PK_TaskComments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TaskComments_Tasks_TaskId" FOREIGN KEY ("TaskId") REFERENCES worknexa."Tasks" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TaskComments_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_TaskComments_Users_DeletedBy" FOREIGN KEY ("DeletedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_TaskComments_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."TaskDependencies" (
    "Id" uuid NOT NULL,
    "TaskId" uuid NOT NULL,
    "DependsOnTaskId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    CONSTRAINT "PK_TaskDependencies" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_TaskDependencies_NotSelf" CHECK ("TaskId" <> "DependsOnTaskId"),
    CONSTRAINT "FK_TaskDependencies_Tasks_DependsOnTaskId" FOREIGN KEY ("DependsOnTaskId") REFERENCES worknexa."Tasks" ("Id"),
    CONSTRAINT "FK_TaskDependencies_Tasks_TaskId" FOREIGN KEY ("TaskId") REFERENCES worknexa."Tasks" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TaskDependencies_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_TaskDependencies_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."TaskTags" (
    "Id" uuid NOT NULL,
    "TaskId" uuid NOT NULL,
    "TagId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    CONSTRAINT "PK_TaskTags" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TaskTags_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES worknexa."Tags" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TaskTags_Tasks_TaskId" FOREIGN KEY ("TaskId") REFERENCES worknexa."Tasks" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TaskTags_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_TaskTags_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

CREATE TABLE worknexa."NoteTags" (
    "Id" uuid NOT NULL,
    "NoteId" uuid NOT NULL,
    "TagId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedBy" uuid,
    CONSTRAINT "PK_NoteTags" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_NoteTags_Notes_NoteId" FOREIGN KEY ("NoteId") REFERENCES worknexa."Notes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_NoteTags_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES worknexa."Tags" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_NoteTags_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES worknexa."Users" ("Id"),
    CONSTRAINT "FK_NoteTags_Users_UpdatedBy" FOREIGN KEY ("UpdatedBy") REFERENCES worknexa."Users" ("Id")
);

INSERT INTO worknexa."LeaveTypes" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsPaid", "Name", "UpdatedAt", "UpdatedBy")
VALUES ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, TRUE, 'Annual', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."LeaveTypes" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsPaid", "Name", "UpdatedAt", "UpdatedBy")
VALUES ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, TRUE, 'Sick', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."LeaveTypes" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsPaid", "Name", "UpdatedAt", "UpdatedBy")
VALUES ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, FALSE, 'Unpaid', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);

INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000001', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'View users', 'users.read', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000002', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Manage users', 'users.manage', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000003', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'View teams', 'teams.read', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000004', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Manage teams', 'teams.manage', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000005', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'View tasks', 'tasks.read', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000006', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Manage tasks', 'tasks.manage', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000007', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'View projects', 'projects.read', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000008', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Manage projects', 'projects.manage', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000009', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'View calendar events', 'calendar.read', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000010', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Manage calendar events', 'calendar.manage', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000011', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'View meetings', 'meetings.read', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000012', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Manage meetings', 'meetings.manage', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000013', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'View leave', 'leaves.read', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000014', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Manage leave', 'leaves.manage', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000015', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'View learning', 'learning.read', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000016', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Manage learning', 'learning.manage', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000017', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'View notes', 'notes.read', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000018', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Manage notes', 'notes.manage', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Permissions" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy")
VALUES ('c1000001-0000-4000-8000-000000000019', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'View notifications', 'notifications.read', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);

INSERT INTO worknexa."Roles" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Name", "UpdatedAt", "UpdatedBy")
VALUES ('11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Full access to the workspace.', 'Admin', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Roles" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Name", "UpdatedAt", "UpdatedBy")
VALUES ('22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Standard access for day-to-day work.', 'TeamMember', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Roles" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Name", "UpdatedAt", "UpdatedBy")
VALUES ('44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Manages day-to-day work without administering accounts.', 'Manager', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);

INSERT INTO worknexa."Users" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayName", "Email", "IsActive", "PasswordHash", "UpdatedAt", "UpdatedBy")
VALUES ('8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2d', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Worknexa Admin', 'admin@worknexa.local', TRUE, 'AQAAAAIAAYagAAAAEFsHn/Se3GRVZHYexKPeT44ABM36w8JOlC1ahPQnapZRym0hiy4IWg80W0Eg3MZ18g==', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Users" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayName", "Email", "IsActive", "PasswordHash", "UpdatedAt", "UpdatedBy")
VALUES ('8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2e', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Worknexa Manager', 'manager@worknexa.local', TRUE, 'AQAAAAIAAYagAAAAEFsHn/Se3GRVZHYexKPeT44ABM36w8JOlC1ahPQnapZRym0hiy4IWg80W0Eg3MZ18g==', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."Users" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayName", "Email", "IsActive", "PasswordHash", "UpdatedAt", "UpdatedBy")
VALUES ('8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2f', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, NULL, NULL, 'Worknexa Member', 'member@worknexa.local', TRUE, 'AQAAAAIAAYagAAAAEFsHn/Se3GRVZHYexKPeT44ABM36w8JOlC1ahPQnapZRym0hiy4IWg80W0Eg3MZ18g==', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);

INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000001', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000001', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000002', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000002', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000003', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000003', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000004', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000004', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000005', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000005', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000006', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000006', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000007', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000007', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000008', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000008', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000009', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000009', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000010', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000010', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000011', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000011', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000012', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000012', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000013', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000013', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000014', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000014', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000015', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000015', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000016', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000016', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000017', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000017', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000018', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000018', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d1000001-0000-4000-8000-000000000019', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000019', '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d2000001-0000-4000-8000-000000000001', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000001', '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d2000001-0000-4000-8000-000000000002', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000003', '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d2000001-0000-4000-8000-000000000003', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000005', '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d2000001-0000-4000-8000-000000000004', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000006', '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d2000001-0000-4000-8000-000000000005', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000007', '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d2000001-0000-4000-8000-000000000006', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000009', '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d2000001-0000-4000-8000-000000000007', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000011', '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d2000001-0000-4000-8000-000000000008', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000013', '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d2000001-0000-4000-8000-000000000009', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000015', '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d2000001-0000-4000-8000-000000000010', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000016', '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d2000001-0000-4000-8000-000000000011', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000017', '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d2000001-0000-4000-8000-000000000012', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000018', '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d2000001-0000-4000-8000-000000000013', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000019', '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000001', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000001', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000002', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000003', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000003', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000005', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000004', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000006', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000005', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000007', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000006', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000009', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000007', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000011', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000008', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000013', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000009', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000015', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000010', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000016', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000011', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000017', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000012', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000018', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000013', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000019', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000014', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000004', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000015', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000008', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000016', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000010', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000017', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000012', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);
INSERT INTO worknexa."RolePermissions" ("Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy")
VALUES ('d3000001-0000-4000-8000-000000000018', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, 'c1000001-0000-4000-8000-000000000014', '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL);

INSERT INTO worknexa."UserRoles" ("Id", "CreatedAt", "CreatedBy", "RoleId", "UpdatedAt", "UpdatedBy", "UserId")
VALUES ('33333333-3333-4333-8333-333333333333', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, '11111111-1111-4111-8111-111111111111', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, '8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2d');
INSERT INTO worknexa."UserRoles" ("Id", "CreatedAt", "CreatedBy", "RoleId", "UpdatedAt", "UpdatedBy", "UserId")
VALUES ('55555555-5555-4555-8555-555555555555', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, '44444444-4444-4444-8444-444444444444', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, '8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2e');
INSERT INTO worknexa."UserRoles" ("Id", "CreatedAt", "CreatedBy", "RoleId", "UpdatedAt", "UpdatedBy", "UserId")
VALUES ('66666666-6666-4666-8666-666666666666', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, '22222222-2222-4222-8222-222222222222', TIMESTAMPTZ '2026-10-06T00:00:00+00:00', NULL, '8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2f');

CREATE INDEX "IX_ActivityLogs_CreatedAt" ON worknexa."ActivityLogs" ("CreatedAt");

CREATE INDEX "IX_ActivityLogs_CreatedBy" ON worknexa."ActivityLogs" ("CreatedBy");

CREATE INDEX "IX_ActivityLogs_EntityName_EntityId" ON worknexa."ActivityLogs" ("EntityName", "EntityId");

CREATE INDEX "IX_ActivityLogs_UpdatedBy" ON worknexa."ActivityLogs" ("UpdatedBy");

CREATE INDEX "IX_Attachments_CreatedBy" ON worknexa."Attachments" ("CreatedBy");

CREATE INDEX "IX_Attachments_DeletedAt" ON worknexa."Attachments" ("DeletedAt");

CREATE INDEX "IX_Attachments_DeletedBy" ON worknexa."Attachments" ("DeletedBy");

CREATE INDEX "IX_Attachments_EntityName_EntityId" ON worknexa."Attachments" ("EntityName", "EntityId");

CREATE UNIQUE INDEX "IX_Attachments_StorageKey" ON worknexa."Attachments" ("StorageKey");

CREATE INDEX "IX_Attachments_UpdatedBy" ON worknexa."Attachments" ("UpdatedBy");

CREATE INDEX "IX_CalendarEvents_CreatedBy" ON worknexa."CalendarEvents" ("CreatedBy");

CREATE INDEX "IX_CalendarEvents_DeletedAt" ON worknexa."CalendarEvents" ("DeletedAt");

CREATE INDEX "IX_CalendarEvents_DeletedBy" ON worknexa."CalendarEvents" ("DeletedBy");

CREATE INDEX "IX_CalendarEvents_OrganizerId_StartAt" ON worknexa."CalendarEvents" ("OrganizerId", "StartAt");

CREATE INDEX "IX_CalendarEvents_ProjectId" ON worknexa."CalendarEvents" ("ProjectId");

CREATE INDEX "IX_CalendarEvents_StartAt" ON worknexa."CalendarEvents" ("StartAt");

CREATE INDEX "IX_CalendarEvents_TeamId" ON worknexa."CalendarEvents" ("TeamId");

CREATE INDEX "IX_CalendarEvents_UpdatedBy" ON worknexa."CalendarEvents" ("UpdatedBy");

CREATE UNIQUE INDEX "IX_EventParticipants_CalendarEventId_UserId" ON worknexa."EventParticipants" ("CalendarEventId", "UserId");

CREATE INDEX "IX_EventParticipants_CreatedBy" ON worknexa."EventParticipants" ("CreatedBy");

CREATE INDEX "IX_EventParticipants_UpdatedBy" ON worknexa."EventParticipants" ("UpdatedBy");

CREATE INDEX "IX_EventParticipants_UserId" ON worknexa."EventParticipants" ("UserId");

CREATE INDEX "IX_LearningNotes_CreatedBy" ON worknexa."LearningNotes" ("CreatedBy");

CREATE INDEX "IX_LearningNotes_DeletedAt" ON worknexa."LearningNotes" ("DeletedAt");

CREATE INDEX "IX_LearningNotes_DeletedBy" ON worknexa."LearningNotes" ("DeletedBy");

CREATE INDEX "IX_LearningNotes_LearningTopicId" ON worknexa."LearningNotes" ("LearningTopicId");

CREATE INDEX "IX_LearningNotes_UpdatedBy" ON worknexa."LearningNotes" ("UpdatedBy");

CREATE INDEX "IX_LearningResources_CreatedBy" ON worknexa."LearningResources" ("CreatedBy");

CREATE INDEX "IX_LearningResources_DeletedAt" ON worknexa."LearningResources" ("DeletedAt");

CREATE INDEX "IX_LearningResources_DeletedBy" ON worknexa."LearningResources" ("DeletedBy");

CREATE INDEX "IX_LearningResources_LearningTopicId" ON worknexa."LearningResources" ("LearningTopicId");

CREATE INDEX "IX_LearningResources_UpdatedBy" ON worknexa."LearningResources" ("UpdatedBy");

CREATE INDEX "IX_LearningTopics_CreatedBy" ON worknexa."LearningTopics" ("CreatedBy");

CREATE INDEX "IX_LearningTopics_DeletedAt" ON worknexa."LearningTopics" ("DeletedAt");

CREATE INDEX "IX_LearningTopics_DeletedBy" ON worknexa."LearningTopics" ("DeletedBy");

CREATE INDEX "IX_LearningTopics_UpdatedBy" ON worknexa."LearningTopics" ("UpdatedBy");

CREATE INDEX "IX_LearningTopics_UserId_Status" ON worknexa."LearningTopics" ("UserId", "Status");

CREATE INDEX "IX_Leaves_CreatedBy" ON worknexa."Leaves" ("CreatedBy");

CREATE INDEX "IX_Leaves_DeletedAt" ON worknexa."Leaves" ("DeletedAt");

CREATE INDEX "IX_Leaves_DeletedBy" ON worknexa."Leaves" ("DeletedBy");

CREATE INDEX "IX_Leaves_LeaveTypeId" ON worknexa."Leaves" ("LeaveTypeId");

CREATE INDEX "IX_Leaves_ReviewedById" ON worknexa."Leaves" ("ReviewedById");

CREATE INDEX "IX_Leaves_Status" ON worknexa."Leaves" ("Status");

CREATE INDEX "IX_Leaves_UpdatedBy" ON worknexa."Leaves" ("UpdatedBy");

CREATE INDEX "IX_Leaves_UserId_StartDate" ON worknexa."Leaves" ("UserId", "StartDate");

CREATE INDEX "IX_LeaveTypes_CreatedBy" ON worknexa."LeaveTypes" ("CreatedBy");

CREATE INDEX "IX_LeaveTypes_DeletedAt" ON worknexa."LeaveTypes" ("DeletedAt");

CREATE INDEX "IX_LeaveTypes_DeletedBy" ON worknexa."LeaveTypes" ("DeletedBy");

CREATE UNIQUE INDEX "IX_LeaveTypes_Name" ON worknexa."LeaveTypes" ("Name") WHERE "DeletedAt" IS NULL;

CREATE INDEX "IX_LeaveTypes_UpdatedBy" ON worknexa."LeaveTypes" ("UpdatedBy");

CREATE INDEX "IX_MeetingActionItems_AssigneeId" ON worknexa."MeetingActionItems" ("AssigneeId");

CREATE INDEX "IX_MeetingActionItems_CreatedBy" ON worknexa."MeetingActionItems" ("CreatedBy");

CREATE INDEX "IX_MeetingActionItems_DeletedAt" ON worknexa."MeetingActionItems" ("DeletedAt");

CREATE INDEX "IX_MeetingActionItems_DeletedBy" ON worknexa."MeetingActionItems" ("DeletedBy");

CREATE INDEX "IX_MeetingActionItems_MeetingId" ON worknexa."MeetingActionItems" ("MeetingId");

CREATE INDEX "IX_MeetingActionItems_TaskId" ON worknexa."MeetingActionItems" ("TaskId");

CREATE INDEX "IX_MeetingActionItems_UpdatedBy" ON worknexa."MeetingActionItems" ("UpdatedBy");

CREATE INDEX "IX_MeetingNotes_CreatedBy" ON worknexa."MeetingNotes" ("CreatedBy");

CREATE INDEX "IX_MeetingNotes_DeletedAt" ON worknexa."MeetingNotes" ("DeletedAt");

CREATE INDEX "IX_MeetingNotes_DeletedBy" ON worknexa."MeetingNotes" ("DeletedBy");

CREATE INDEX "IX_MeetingNotes_MeetingId" ON worknexa."MeetingNotes" ("MeetingId");

CREATE INDEX "IX_MeetingNotes_UpdatedBy" ON worknexa."MeetingNotes" ("UpdatedBy");

CREATE INDEX "IX_MeetingParticipants_CreatedBy" ON worknexa."MeetingParticipants" ("CreatedBy");

CREATE UNIQUE INDEX "IX_MeetingParticipants_MeetingId_UserId" ON worknexa."MeetingParticipants" ("MeetingId", "UserId");

CREATE INDEX "IX_MeetingParticipants_UpdatedBy" ON worknexa."MeetingParticipants" ("UpdatedBy");

CREATE INDEX "IX_MeetingParticipants_UserId" ON worknexa."MeetingParticipants" ("UserId");

CREATE INDEX "IX_Meetings_CalendarEventId" ON worknexa."Meetings" ("CalendarEventId");

CREATE INDEX "IX_Meetings_CreatedBy" ON worknexa."Meetings" ("CreatedBy");

CREATE INDEX "IX_Meetings_DeletedAt" ON worknexa."Meetings" ("DeletedAt");

CREATE INDEX "IX_Meetings_DeletedBy" ON worknexa."Meetings" ("DeletedBy");

CREATE INDEX "IX_Meetings_MeetingType" ON worknexa."Meetings" ("MeetingType");

CREATE INDEX "IX_Meetings_OrganizerId_StartAt" ON worknexa."Meetings" ("OrganizerId", "StartAt");

CREATE INDEX "IX_Meetings_ProjectId" ON worknexa."Meetings" ("ProjectId");

CREATE INDEX "IX_Meetings_StartAt" ON worknexa."Meetings" ("StartAt");

CREATE INDEX "IX_Meetings_TeamId" ON worknexa."Meetings" ("TeamId");

CREATE INDEX "IX_Meetings_UpdatedBy" ON worknexa."Meetings" ("UpdatedBy");

CREATE INDEX "IX_Notes_CreatedBy" ON worknexa."Notes" ("CreatedBy");

CREATE INDEX "IX_Notes_DeletedAt" ON worknexa."Notes" ("DeletedAt");

CREATE INDEX "IX_Notes_DeletedBy" ON worknexa."Notes" ("DeletedBy");

CREATE INDEX "IX_Notes_LearningTopicId" ON worknexa."Notes" ("LearningTopicId");

CREATE INDEX "IX_Notes_MeetingId" ON worknexa."Notes" ("MeetingId");

CREATE INDEX "IX_Notes_ProjectId" ON worknexa."Notes" ("ProjectId");

CREATE INDEX "IX_Notes_TaskId" ON worknexa."Notes" ("TaskId");

CREATE INDEX "IX_Notes_UpdatedBy" ON worknexa."Notes" ("UpdatedBy");

CREATE INDEX "IX_Notes_UserId" ON worknexa."Notes" ("UserId");

CREATE INDEX "IX_Notes_UserId_IsPinned" ON worknexa."Notes" ("UserId", "IsPinned");

CREATE INDEX "IX_NoteTags_CreatedBy" ON worknexa."NoteTags" ("CreatedBy");

CREATE UNIQUE INDEX "IX_NoteTags_NoteId_TagId" ON worknexa."NoteTags" ("NoteId", "TagId");

CREATE INDEX "IX_NoteTags_TagId" ON worknexa."NoteTags" ("TagId");

CREATE INDEX "IX_NoteTags_UpdatedBy" ON worknexa."NoteTags" ("UpdatedBy");

CREATE INDEX "IX_Notifications_CreatedBy" ON worknexa."Notifications" ("CreatedBy");

CREATE INDEX "IX_Notifications_DeletedAt" ON worknexa."Notifications" ("DeletedAt");

CREATE INDEX "IX_Notifications_DeletedBy" ON worknexa."Notifications" ("DeletedBy");

CREATE INDEX "IX_Notifications_ReminderId" ON worknexa."Notifications" ("ReminderId");

CREATE INDEX "IX_Notifications_UpdatedBy" ON worknexa."Notifications" ("UpdatedBy");

CREATE INDEX "IX_Notifications_UserId_IsRead" ON worknexa."Notifications" ("UserId", "IsRead");

CREATE INDEX "IX_Permissions_CreatedBy" ON worknexa."Permissions" ("CreatedBy");

CREATE INDEX "IX_Permissions_DeletedAt" ON worknexa."Permissions" ("DeletedAt");

CREATE INDEX "IX_Permissions_DeletedBy" ON worknexa."Permissions" ("DeletedBy");

CREATE UNIQUE INDEX "IX_Permissions_Key" ON worknexa."Permissions" ("Key") WHERE "DeletedAt" IS NULL;

CREATE INDEX "IX_Permissions_UpdatedBy" ON worknexa."Permissions" ("UpdatedBy");

CREATE INDEX "IX_ProjectMembers_CreatedBy" ON worknexa."ProjectMembers" ("CreatedBy");

CREATE UNIQUE INDEX "IX_ProjectMembers_ProjectId_UserId" ON worknexa."ProjectMembers" ("ProjectId", "UserId");

CREATE INDEX "IX_ProjectMembers_UpdatedBy" ON worknexa."ProjectMembers" ("UpdatedBy");

CREATE INDEX "IX_ProjectMembers_UserId" ON worknexa."ProjectMembers" ("UserId");

CREATE INDEX "IX_Projects_CreatedBy" ON worknexa."Projects" ("CreatedBy");

CREATE INDEX "IX_Projects_DeletedAt" ON worknexa."Projects" ("DeletedAt");

CREATE INDEX "IX_Projects_DeletedBy" ON worknexa."Projects" ("DeletedBy");

CREATE INDEX "IX_Projects_Status" ON worknexa."Projects" ("Status");

CREATE INDEX "IX_Projects_TeamId_Name" ON worknexa."Projects" ("TeamId", "Name");

CREATE INDEX "IX_Projects_UpdatedBy" ON worknexa."Projects" ("UpdatedBy");

CREATE INDEX "IX_Reminders_CalendarEventId" ON worknexa."Reminders" ("CalendarEventId");

CREATE INDEX "IX_Reminders_CreatedBy" ON worknexa."Reminders" ("CreatedBy");

CREATE INDEX "IX_Reminders_DeletedAt" ON worknexa."Reminders" ("DeletedAt");

CREATE INDEX "IX_Reminders_DeletedBy" ON worknexa."Reminders" ("DeletedBy");

CREATE INDEX "IX_Reminders_IsSent" ON worknexa."Reminders" ("IsSent");

CREATE INDEX "IX_Reminders_MeetingId" ON worknexa."Reminders" ("MeetingId");

CREATE INDEX "IX_Reminders_TaskId" ON worknexa."Reminders" ("TaskId");

CREATE INDEX "IX_Reminders_TodoItemId" ON worknexa."Reminders" ("TodoItemId");

CREATE INDEX "IX_Reminders_UpdatedBy" ON worknexa."Reminders" ("UpdatedBy");

CREATE INDEX "IX_Reminders_UserId_RemindAt" ON worknexa."Reminders" ("UserId", "RemindAt");

CREATE INDEX "IX_RolePermissions_CreatedBy" ON worknexa."RolePermissions" ("CreatedBy");

CREATE INDEX "IX_RolePermissions_PermissionId" ON worknexa."RolePermissions" ("PermissionId");

CREATE UNIQUE INDEX "IX_RolePermissions_RoleId_PermissionId" ON worknexa."RolePermissions" ("RoleId", "PermissionId");

CREATE INDEX "IX_RolePermissions_UpdatedBy" ON worknexa."RolePermissions" ("UpdatedBy");

CREATE INDEX "IX_Roles_CreatedBy" ON worknexa."Roles" ("CreatedBy");

CREATE INDEX "IX_Roles_DeletedAt" ON worknexa."Roles" ("DeletedAt");

CREATE INDEX "IX_Roles_DeletedBy" ON worknexa."Roles" ("DeletedBy");

CREATE UNIQUE INDEX "IX_Roles_Name" ON worknexa."Roles" ("Name") WHERE "DeletedAt" IS NULL;

CREATE INDEX "IX_Roles_UpdatedBy" ON worknexa."Roles" ("UpdatedBy");

CREATE INDEX "IX_StandupEntries_CreatedBy" ON worknexa."StandupEntries" ("CreatedBy");

CREATE INDEX "IX_StandupEntries_DeletedAt" ON worknexa."StandupEntries" ("DeletedAt");

CREATE INDEX "IX_StandupEntries_DeletedBy" ON worknexa."StandupEntries" ("DeletedBy");

CREATE UNIQUE INDEX "IX_StandupEntries_StandupId_UserId" ON worknexa."StandupEntries" ("StandupId", "UserId");

CREATE INDEX "IX_StandupEntries_UpdatedBy" ON worknexa."StandupEntries" ("UpdatedBy");

CREATE INDEX "IX_StandupEntries_UserId" ON worknexa."StandupEntries" ("UserId");

CREATE INDEX "IX_Standups_CreatedBy" ON worknexa."Standups" ("CreatedBy");

CREATE INDEX "IX_Standups_DeletedAt" ON worknexa."Standups" ("DeletedAt");

CREATE INDEX "IX_Standups_DeletedBy" ON worknexa."Standups" ("DeletedBy");

CREATE INDEX "IX_Standups_MeetingId" ON worknexa."Standups" ("MeetingId");

CREATE UNIQUE INDEX "IX_Standups_OwnerId_StandupDate" ON worknexa."Standups" ("OwnerId", "StandupDate") WHERE "DeletedAt" IS NULL AND "OwnerId" IS NOT NULL;

CREATE UNIQUE INDEX "IX_Standups_TeamId_StandupDate" ON worknexa."Standups" ("TeamId", "StandupDate") WHERE "DeletedAt" IS NULL AND "TeamId" IS NOT NULL;

CREATE INDEX "IX_Standups_UpdatedBy" ON worknexa."Standups" ("UpdatedBy");

CREATE INDEX "IX_Tags_CreatedBy" ON worknexa."Tags" ("CreatedBy");

CREATE INDEX "IX_Tags_DeletedAt" ON worknexa."Tags" ("DeletedAt");

CREATE INDEX "IX_Tags_DeletedBy" ON worknexa."Tags" ("DeletedBy");

CREATE UNIQUE INDEX "IX_Tags_Name" ON worknexa."Tags" ("Name") WHERE "DeletedAt" IS NULL;

CREATE INDEX "IX_Tags_UpdatedBy" ON worknexa."Tags" ("UpdatedBy");

CREATE INDEX "IX_TaskComments_CreatedBy" ON worknexa."TaskComments" ("CreatedBy");

CREATE INDEX "IX_TaskComments_DeletedAt" ON worknexa."TaskComments" ("DeletedAt");

CREATE INDEX "IX_TaskComments_DeletedBy" ON worknexa."TaskComments" ("DeletedBy");

CREATE INDEX "IX_TaskComments_TaskId" ON worknexa."TaskComments" ("TaskId");

CREATE INDEX "IX_TaskComments_UpdatedBy" ON worknexa."TaskComments" ("UpdatedBy");

CREATE INDEX "IX_TaskDependencies_CreatedBy" ON worknexa."TaskDependencies" ("CreatedBy");

CREATE INDEX "IX_TaskDependencies_DependsOnTaskId" ON worknexa."TaskDependencies" ("DependsOnTaskId");

CREATE UNIQUE INDEX "IX_TaskDependencies_TaskId_DependsOnTaskId" ON worknexa."TaskDependencies" ("TaskId", "DependsOnTaskId");

CREATE INDEX "IX_TaskDependencies_UpdatedBy" ON worknexa."TaskDependencies" ("UpdatedBy");

CREATE INDEX "IX_Tasks_AssigneeId_Status" ON worknexa."Tasks" ("AssigneeId", "Status");

CREATE INDEX "IX_Tasks_CreatedBy" ON worknexa."Tasks" ("CreatedBy");

CREATE INDEX "IX_Tasks_DeletedAt" ON worknexa."Tasks" ("DeletedAt");

CREATE INDEX "IX_Tasks_DeletedBy" ON worknexa."Tasks" ("DeletedBy");

CREATE INDEX "IX_Tasks_DueAt" ON worknexa."Tasks" ("DueAt");

CREATE INDEX "IX_Tasks_MeetingId" ON worknexa."Tasks" ("MeetingId");

CREATE INDEX "IX_Tasks_ParentTaskId" ON worknexa."Tasks" ("ParentTaskId");

CREATE INDEX "IX_Tasks_ProjectId_Status" ON worknexa."Tasks" ("ProjectId", "Status");

CREATE INDEX "IX_Tasks_Status" ON worknexa."Tasks" ("Status");

CREATE INDEX "IX_Tasks_TeamId" ON worknexa."Tasks" ("TeamId");

CREATE INDEX "IX_Tasks_UpdatedBy" ON worknexa."Tasks" ("UpdatedBy");

CREATE INDEX "IX_TaskTags_CreatedBy" ON worknexa."TaskTags" ("CreatedBy");

CREATE INDEX "IX_TaskTags_TagId" ON worknexa."TaskTags" ("TagId");

CREATE UNIQUE INDEX "IX_TaskTags_TaskId_TagId" ON worknexa."TaskTags" ("TaskId", "TagId");

CREATE INDEX "IX_TaskTags_UpdatedBy" ON worknexa."TaskTags" ("UpdatedBy");

CREATE INDEX "IX_TeamMembers_CreatedBy" ON worknexa."TeamMembers" ("CreatedBy");

CREATE UNIQUE INDEX "IX_TeamMembers_TeamId_UserId" ON worknexa."TeamMembers" ("TeamId", "UserId");

CREATE INDEX "IX_TeamMembers_UpdatedBy" ON worknexa."TeamMembers" ("UpdatedBy");

CREATE INDEX "IX_TeamMembers_UserId" ON worknexa."TeamMembers" ("UserId");

CREATE INDEX "IX_Teams_CreatedBy" ON worknexa."Teams" ("CreatedBy");

CREATE INDEX "IX_Teams_DeletedAt" ON worknexa."Teams" ("DeletedAt");

CREATE INDEX "IX_Teams_DeletedBy" ON worknexa."Teams" ("DeletedBy");

CREATE INDEX "IX_Teams_Name" ON worknexa."Teams" ("Name");

CREATE INDEX "IX_Teams_UpdatedBy" ON worknexa."Teams" ("UpdatedBy");

CREATE INDEX "IX_TodoItems_CreatedBy" ON worknexa."TodoItems" ("CreatedBy");

CREATE INDEX "IX_TodoItems_DeletedAt" ON worknexa."TodoItems" ("DeletedAt");

CREATE INDEX "IX_TodoItems_DeletedBy" ON worknexa."TodoItems" ("DeletedBy");

CREATE INDEX "IX_TodoItems_UpdatedBy" ON worknexa."TodoItems" ("UpdatedBy");

CREATE INDEX "IX_TodoItems_UserId_DueAt" ON worknexa."TodoItems" ("UserId", "DueAt");

CREATE INDEX "IX_TodoItems_UserId_IsCompleted" ON worknexa."TodoItems" ("UserId", "IsCompleted");

CREATE INDEX "IX_TodoTags_CreatedBy" ON worknexa."TodoTags" ("CreatedBy");

CREATE INDEX "IX_TodoTags_TagId" ON worknexa."TodoTags" ("TagId");

CREATE UNIQUE INDEX "IX_TodoTags_TodoItemId_TagId" ON worknexa."TodoTags" ("TodoItemId", "TagId");

CREATE INDEX "IX_TodoTags_UpdatedBy" ON worknexa."TodoTags" ("UpdatedBy");

CREATE INDEX "IX_UserRoles_CreatedBy" ON worknexa."UserRoles" ("CreatedBy");

CREATE INDEX "IX_UserRoles_RoleId" ON worknexa."UserRoles" ("RoleId");

CREATE INDEX "IX_UserRoles_UpdatedBy" ON worknexa."UserRoles" ("UpdatedBy");

CREATE UNIQUE INDEX "IX_UserRoles_UserId_RoleId" ON worknexa."UserRoles" ("UserId", "RoleId");

CREATE INDEX "IX_Users_CreatedBy" ON worknexa."Users" ("CreatedBy");

CREATE INDEX "IX_Users_DeletedAt" ON worknexa."Users" ("DeletedAt");

CREATE INDEX "IX_Users_DeletedBy" ON worknexa."Users" ("DeletedBy");

CREATE UNIQUE INDEX "IX_Users_Email" ON worknexa."Users" ("Email") WHERE "DeletedAt" IS NULL;

CREATE INDEX "IX_Users_IsActive" ON worknexa."Users" ("IsActive");

CREATE INDEX "IX_Users_UpdatedBy" ON worknexa."Users" ("UpdatedBy");

INSERT INTO worknexa."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261006101752_InitialPostgres', '10.0.12');

COMMIT;

