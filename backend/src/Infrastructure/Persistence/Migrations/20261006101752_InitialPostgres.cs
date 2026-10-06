using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Worknexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "worknexa");

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Attachments_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Attachments_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LearningTopics",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningTopics_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LearningTopics_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LearningTopics_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LearningTopics_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveTypes_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveTypes_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveTypes_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Link = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ReminderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Permissions_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Permissions_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Roles_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Roles_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tags_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tags_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Teams_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Teams_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TodoItems",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Recurrence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoItems_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TodoItems_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TodoItems_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TodoItems_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LearningNotes",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningTopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningNotes_LearningTopics_LearningTopicId",
                        column: x => x.LearningTopicId,
                        principalSchema: "worknexa",
                        principalTable: "LearningTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearningNotes_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LearningNotes_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LearningNotes_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LearningResources",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningTopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningResources_LearningTopics_LearningTopicId",
                        column: x => x.LearningTopicId,
                        principalSchema: "worknexa",
                        principalTable: "LearningTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearningResources_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LearningResources_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LearningResources_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Leaves",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaves", x => x.Id);
                    table.CheckConstraint("CK_Leaves_DateRange", "\"EndDate\" >= \"StartDate\"");
                    table.ForeignKey(
                        name: "FK_Leaves_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalSchema: "worknexa",
                        principalTable: "LeaveTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Leaves_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Leaves_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Leaves_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Leaves_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Leaves_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "worknexa",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "worknexa",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RolePermissions_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "worknexa",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.CheckConstraint("CK_Projects_DateRange", "\"EndDate\" IS NULL OR \"StartDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.ForeignKey(
                        name: "FK_Projects_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "worknexa",
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "worknexa",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamMembers_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamMembers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TodoTags",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TodoItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoTags_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "worknexa",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TodoTags_TodoItems_TodoItemId",
                        column: x => x.TodoItemId,
                        principalSchema: "worknexa",
                        principalTable: "TodoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TodoTags_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TodoTags_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsAllDay = table.Column<bool>(type: "boolean", nullable: false),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MeetingLink = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Recurrence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecurrenceInterval = table.Column<int>(type: "integer", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.Id);
                    table.CheckConstraint("CK_CalendarEvents_Interval", "\"RecurrenceInterval\" IS NULL OR (\"RecurrenceInterval\" BETWEEN 1 AND 366)");
                    table.CheckConstraint("CK_CalendarEvents_TimeRange", "\"EndAt\" >= \"StartAt\"");
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "worknexa",
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "worknexa",
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Users_OrganizerId",
                        column: x => x.OrganizerId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "worknexa",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EventParticipants",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalendarEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventParticipants_CalendarEvents_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalSchema: "worknexa",
                        principalTable: "CalendarEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventParticipants_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventParticipants_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Meetings",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MeetingLink = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MeetingType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Agenda = table.Column<string>(type: "text", nullable: true),
                    NotesText = table.Column<string>(type: "text", nullable: true),
                    Decisions = table.Column<string>(type: "text", nullable: true),
                    FollowUps = table.Column<string>(type: "text", nullable: true),
                    FollowUpAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    CalendarEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meetings", x => x.Id);
                    table.CheckConstraint("CK_Meetings_TimeRange", "\"EndAt\" >= \"StartAt\"");
                    table.ForeignKey(
                        name: "FK_Meetings_CalendarEvents_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalSchema: "worknexa",
                        principalTable: "CalendarEvents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Meetings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "worknexa",
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Meetings_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "worknexa",
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Meetings_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Meetings_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Meetings_Users_OrganizerId",
                        column: x => x.OrganizerId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Meetings_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MeetingNotes",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingNotes_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalSchema: "worknexa",
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingNotes_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MeetingNotes_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MeetingNotes_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MeetingParticipants",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingParticipants_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalSchema: "worknexa",
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingParticipants_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MeetingParticipants_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MeetingParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Standups",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: true),
                    StandupDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Standups", x => x.Id);
                    table.CheckConstraint("CK_Standups_Scope", "(\"TeamId\" IS NOT NULL AND \"OwnerId\" IS NULL) OR (\"TeamId\" IS NULL AND \"OwnerId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Standups_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalSchema: "worknexa",
                        principalTable: "Meetings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Standups_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "worknexa",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Standups_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Standups_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Standups_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Standups_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    ActualHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: true),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.CheckConstraint("CK_Tasks_Hours", "(\"EstimatedHours\" IS NULL OR \"EstimatedHours\" >= 0) AND (\"ActualHours\" IS NULL OR \"ActualHours\" >= 0)");
                    table.CheckConstraint("CK_Tasks_Schedule", "\"StartAt\" IS NULL OR \"DueAt\" IS NULL OR \"StartAt\" <= \"DueAt\"");
                    table.ForeignKey(
                        name: "FK_Tasks_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalSchema: "worknexa",
                        principalTable: "Meetings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "worknexa",
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Tasks_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalSchema: "worknexa",
                        principalTable: "Tasks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "worknexa",
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StandupEntries",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StandupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Yesterday = table.Column<string>(type: "text", nullable: true),
                    Today = table.Column<string>(type: "text", nullable: true),
                    Blockers = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandupEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandupEntries_Standups_StandupId",
                        column: x => x.StandupId,
                        principalSchema: "worknexa",
                        principalTable: "Standups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StandupEntries_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StandupEntries_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StandupEntries_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StandupEntries_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MeetingActionItems",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingActionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingActionItems_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalSchema: "worknexa",
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingActionItems_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "worknexa",
                        principalTable: "Tasks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MeetingActionItems_Users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MeetingActionItems_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MeetingActionItems_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MeetingActionItems_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: true),
                    LearningTopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notes_LearningTopics_LearningTopicId",
                        column: x => x.LearningTopicId,
                        principalSchema: "worknexa",
                        principalTable: "LearningTopics",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notes_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalSchema: "worknexa",
                        principalTable: "Meetings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "worknexa",
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notes_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "worknexa",
                        principalTable: "Tasks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notes_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notes_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notes_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notes_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Reminders",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    CalendarEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    TodoItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: "Reminder"),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RemindAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Recurrence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecurrenceInterval = table.Column<int>(type: "integer", nullable: true),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsSent = table.Column<bool>(type: "boolean", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                    table.CheckConstraint("CK_Reminders_Interval", "\"RecurrenceInterval\" IS NULL OR (\"RecurrenceInterval\" BETWEEN 1 AND 366)");
                    table.ForeignKey(
                        name: "FK_Reminders_CalendarEvents_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalSchema: "worknexa",
                        principalTable: "CalendarEvents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reminders_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalSchema: "worknexa",
                        principalTable: "Meetings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reminders_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "worknexa",
                        principalTable: "Tasks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reminders_TodoItems_TodoItemId",
                        column: x => x.TodoItemId,
                        principalSchema: "worknexa",
                        principalTable: "TodoItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reminders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reminders_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reminders_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reminders_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskComments",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskComments_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "worknexa",
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskComments_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskComments_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskComments_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskDependencies",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    DependsOnTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDependencies", x => x.Id);
                    table.CheckConstraint("CK_TaskDependencies_NotSelf", "\"TaskId\" <> \"DependsOnTaskId\"");
                    table.ForeignKey(
                        name: "FK_TaskDependencies_Tasks_DependsOnTaskId",
                        column: x => x.DependsOnTaskId,
                        principalSchema: "worknexa",
                        principalTable: "Tasks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskDependencies_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "worknexa",
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskDependencies_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskDependencies_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskTags",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskTags_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "worknexa",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskTags_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "worknexa",
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskTags_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskTags_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NoteTags",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoteTags_Notes_NoteId",
                        column: x => x.NoteId,
                        principalSchema: "worknexa",
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NoteTags_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "worknexa",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NoteTags_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NoteTags_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "worknexa",
                table: "LeaveTypes",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsPaid", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, true, "Annual", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, true, "Sick", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, false, "Unpaid", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.InsertData(
                schema: "worknexa",
                table: "Permissions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Key", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("c1000001-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "View users", "users.read", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Manage users", "users.manage", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "View teams", "teams.read", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000004"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Manage teams", "teams.manage", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000005"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "View tasks", "tasks.read", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000006"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Manage tasks", "tasks.manage", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000007"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "View projects", "projects.read", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000008"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Manage projects", "projects.manage", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000009"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "View calendar events", "calendar.read", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000010"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Manage calendar events", "calendar.manage", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000011"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "View meetings", "meetings.read", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000012"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Manage meetings", "meetings.manage", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000013"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "View leave", "leaves.read", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000014"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Manage leave", "leaves.manage", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000015"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "View learning", "learning.read", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000016"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Manage learning", "learning.manage", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000017"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "View notes", "notes.read", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000018"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Manage notes", "notes.manage", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("c1000001-0000-4000-8000-000000000019"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "View notifications", "notifications.read", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.InsertData(
                schema: "worknexa",
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Full access to the workspace.", "Admin", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Standard access for day-to-day work.", "TeamMember", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Manages day-to-day work without administering accounts.", "Manager", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.InsertData(
                schema: "worknexa",
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayName", "Email", "IsActive", "PasswordHash", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2d"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Worknexa Admin", "admin@worknexa.local", true, "AQAAAAIAAYagAAAAEFsHn/Se3GRVZHYexKPeT44ABM36w8JOlC1ahPQnapZRym0hiy4IWg80W0Eg3MZ18g==", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2e"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Worknexa Manager", "manager@worknexa.local", true, "AQAAAAIAAYagAAAAEFsHn/Se3GRVZHYexKPeT44ABM36w8JOlC1ahPQnapZRym0hiy4IWg80W0Eg3MZ18g==", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2f"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Worknexa Member", "member@worknexa.local", true, "AQAAAAIAAYagAAAAEFsHn/Se3GRVZHYexKPeT44ABM36w8JOlC1ahPQnapZRym0hiy4IWg80W0Eg3MZ18g==", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.InsertData(
                schema: "worknexa",
                table: "RolePermissions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("d1000001-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000001"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000002"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000003"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000004"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000004"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000005"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000005"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000006"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000006"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000007"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000007"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000008"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000008"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000009"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000009"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000010"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000010"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000011"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000011"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000012"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000012"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000013"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000013"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000014"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000014"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000015"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000015"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000016"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000016"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000017"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000017"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000018"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000018"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d1000001-0000-4000-8000-000000000019"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000019"), new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d2000001-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000001"), new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d2000001-0000-4000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000003"), new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d2000001-0000-4000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000005"), new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d2000001-0000-4000-8000-000000000004"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000006"), new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d2000001-0000-4000-8000-000000000005"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000007"), new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d2000001-0000-4000-8000-000000000006"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000009"), new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d2000001-0000-4000-8000-000000000007"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000011"), new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d2000001-0000-4000-8000-000000000008"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000013"), new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d2000001-0000-4000-8000-000000000009"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000015"), new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d2000001-0000-4000-8000-000000000010"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000016"), new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d2000001-0000-4000-8000-000000000011"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000017"), new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d2000001-0000-4000-8000-000000000012"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000018"), new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d2000001-0000-4000-8000-000000000013"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000019"), new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000001"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000003"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000005"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000004"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000006"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000005"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000007"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000006"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000009"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000007"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000011"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000008"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000013"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000009"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000015"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000010"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000016"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000011"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000017"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000012"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000018"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000013"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000019"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000014"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000004"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000015"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000008"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000016"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000010"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000017"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000012"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("d3000001-0000-4000-8000-000000000018"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("c1000001-0000-4000-8000-000000000014"), new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.InsertData(
                schema: "worknexa",
                table: "UserRoles",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "RoleId", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-4333-8333-333333333333"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("11111111-1111-4111-8111-111111111111"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2d") },
                    { new Guid("55555555-5555-4555-8555-555555555555"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("44444444-4444-4444-8444-444444444444"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2e") },
                    { new Guid("66666666-6666-4666-8666-666666666666"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("22222222-2222-4222-8222-222222222222"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2f") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_CreatedAt",
                schema: "worknexa",
                table: "ActivityLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_CreatedBy",
                schema: "worknexa",
                table: "ActivityLogs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_EntityName_EntityId",
                schema: "worknexa",
                table: "ActivityLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UpdatedBy",
                schema: "worknexa",
                table: "ActivityLogs",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_CreatedBy",
                schema: "worknexa",
                table: "Attachments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_DeletedAt",
                schema: "worknexa",
                table: "Attachments",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_DeletedBy",
                schema: "worknexa",
                table: "Attachments",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_EntityName_EntityId",
                schema: "worknexa",
                table: "Attachments",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_StorageKey",
                schema: "worknexa",
                table: "Attachments",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_UpdatedBy",
                schema: "worknexa",
                table: "Attachments",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_CreatedBy",
                schema: "worknexa",
                table: "CalendarEvents",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_DeletedAt",
                schema: "worknexa",
                table: "CalendarEvents",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_DeletedBy",
                schema: "worknexa",
                table: "CalendarEvents",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_OrganizerId_StartAt",
                schema: "worknexa",
                table: "CalendarEvents",
                columns: new[] { "OrganizerId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_ProjectId",
                schema: "worknexa",
                table: "CalendarEvents",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_StartAt",
                schema: "worknexa",
                table: "CalendarEvents",
                column: "StartAt");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_TeamId",
                schema: "worknexa",
                table: "CalendarEvents",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_UpdatedBy",
                schema: "worknexa",
                table: "CalendarEvents",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_CalendarEventId_UserId",
                schema: "worknexa",
                table: "EventParticipants",
                columns: new[] { "CalendarEventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_CreatedBy",
                schema: "worknexa",
                table: "EventParticipants",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_UpdatedBy",
                schema: "worknexa",
                table: "EventParticipants",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_UserId",
                schema: "worknexa",
                table: "EventParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningNotes_CreatedBy",
                schema: "worknexa",
                table: "LearningNotes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LearningNotes_DeletedAt",
                schema: "worknexa",
                table: "LearningNotes",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LearningNotes_DeletedBy",
                schema: "worknexa",
                table: "LearningNotes",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LearningNotes_LearningTopicId",
                schema: "worknexa",
                table: "LearningNotes",
                column: "LearningTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningNotes_UpdatedBy",
                schema: "worknexa",
                table: "LearningNotes",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LearningResources_CreatedBy",
                schema: "worknexa",
                table: "LearningResources",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LearningResources_DeletedAt",
                schema: "worknexa",
                table: "LearningResources",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LearningResources_DeletedBy",
                schema: "worknexa",
                table: "LearningResources",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LearningResources_LearningTopicId",
                schema: "worknexa",
                table: "LearningResources",
                column: "LearningTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningResources_UpdatedBy",
                schema: "worknexa",
                table: "LearningResources",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTopics_CreatedBy",
                schema: "worknexa",
                table: "LearningTopics",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTopics_DeletedAt",
                schema: "worknexa",
                table: "LearningTopics",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTopics_DeletedBy",
                schema: "worknexa",
                table: "LearningTopics",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTopics_UpdatedBy",
                schema: "worknexa",
                table: "LearningTopics",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTopics_UserId_Status",
                schema: "worknexa",
                table: "LearningTopics",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_CreatedBy",
                schema: "worknexa",
                table: "Leaves",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_DeletedAt",
                schema: "worknexa",
                table: "Leaves",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_DeletedBy",
                schema: "worknexa",
                table: "Leaves",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_LeaveTypeId",
                schema: "worknexa",
                table: "Leaves",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_ReviewedById",
                schema: "worknexa",
                table: "Leaves",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_Status",
                schema: "worknexa",
                table: "Leaves",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_UpdatedBy",
                schema: "worknexa",
                table: "Leaves",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_UserId_StartDate",
                schema: "worknexa",
                table: "Leaves",
                columns: new[] { "UserId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_CreatedBy",
                schema: "worknexa",
                table: "LeaveTypes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_DeletedAt",
                schema: "worknexa",
                table: "LeaveTypes",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_DeletedBy",
                schema: "worknexa",
                table: "LeaveTypes",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_Name",
                schema: "worknexa",
                table: "LeaveTypes",
                column: "Name",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_UpdatedBy",
                schema: "worknexa",
                table: "LeaveTypes",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingActionItems_AssigneeId",
                schema: "worknexa",
                table: "MeetingActionItems",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingActionItems_CreatedBy",
                schema: "worknexa",
                table: "MeetingActionItems",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingActionItems_DeletedAt",
                schema: "worknexa",
                table: "MeetingActionItems",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingActionItems_DeletedBy",
                schema: "worknexa",
                table: "MeetingActionItems",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingActionItems_MeetingId",
                schema: "worknexa",
                table: "MeetingActionItems",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingActionItems_TaskId",
                schema: "worknexa",
                table: "MeetingActionItems",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingActionItems_UpdatedBy",
                schema: "worknexa",
                table: "MeetingActionItems",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNotes_CreatedBy",
                schema: "worknexa",
                table: "MeetingNotes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNotes_DeletedAt",
                schema: "worknexa",
                table: "MeetingNotes",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNotes_DeletedBy",
                schema: "worknexa",
                table: "MeetingNotes",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNotes_MeetingId",
                schema: "worknexa",
                table: "MeetingNotes",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNotes_UpdatedBy",
                schema: "worknexa",
                table: "MeetingNotes",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipants_CreatedBy",
                schema: "worknexa",
                table: "MeetingParticipants",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipants_MeetingId_UserId",
                schema: "worknexa",
                table: "MeetingParticipants",
                columns: new[] { "MeetingId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipants_UpdatedBy",
                schema: "worknexa",
                table: "MeetingParticipants",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipants_UserId",
                schema: "worknexa",
                table: "MeetingParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_CalendarEventId",
                schema: "worknexa",
                table: "Meetings",
                column: "CalendarEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_CreatedBy",
                schema: "worknexa",
                table: "Meetings",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_DeletedAt",
                schema: "worknexa",
                table: "Meetings",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_DeletedBy",
                schema: "worknexa",
                table: "Meetings",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_MeetingType",
                schema: "worknexa",
                table: "Meetings",
                column: "MeetingType");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_OrganizerId_StartAt",
                schema: "worknexa",
                table: "Meetings",
                columns: new[] { "OrganizerId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_ProjectId",
                schema: "worknexa",
                table: "Meetings",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_StartAt",
                schema: "worknexa",
                table: "Meetings",
                column: "StartAt");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_TeamId",
                schema: "worknexa",
                table: "Meetings",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_UpdatedBy",
                schema: "worknexa",
                table: "Meetings",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_CreatedBy",
                schema: "worknexa",
                table: "Notes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_DeletedAt",
                schema: "worknexa",
                table: "Notes",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_DeletedBy",
                schema: "worknexa",
                table: "Notes",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_LearningTopicId",
                schema: "worknexa",
                table: "Notes",
                column: "LearningTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_MeetingId",
                schema: "worknexa",
                table: "Notes",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_ProjectId",
                schema: "worknexa",
                table: "Notes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_TaskId",
                schema: "worknexa",
                table: "Notes",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_UpdatedBy",
                schema: "worknexa",
                table: "Notes",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_UserId",
                schema: "worknexa",
                table: "Notes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_UserId_IsPinned",
                schema: "worknexa",
                table: "Notes",
                columns: new[] { "UserId", "IsPinned" });

            migrationBuilder.CreateIndex(
                name: "IX_NoteTags_CreatedBy",
                schema: "worknexa",
                table: "NoteTags",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NoteTags_NoteId_TagId",
                schema: "worknexa",
                table: "NoteTags",
                columns: new[] { "NoteId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NoteTags_TagId",
                schema: "worknexa",
                table: "NoteTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteTags_UpdatedBy",
                schema: "worknexa",
                table: "NoteTags",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedBy",
                schema: "worknexa",
                table: "Notifications",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DeletedAt",
                schema: "worknexa",
                table: "Notifications",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DeletedBy",
                schema: "worknexa",
                table: "Notifications",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReminderId",
                schema: "worknexa",
                table: "Notifications",
                column: "ReminderId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UpdatedBy",
                schema: "worknexa",
                table: "Notifications",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                schema: "worknexa",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_CreatedBy",
                schema: "worknexa",
                table: "Permissions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_DeletedAt",
                schema: "worknexa",
                table: "Permissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_DeletedBy",
                schema: "worknexa",
                table: "Permissions",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Key",
                schema: "worknexa",
                table: "Permissions",
                column: "Key",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UpdatedBy",
                schema: "worknexa",
                table: "Permissions",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_CreatedBy",
                schema: "worknexa",
                table: "ProjectMembers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId_UserId",
                schema: "worknexa",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UpdatedBy",
                schema: "worknexa",
                table: "ProjectMembers",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId",
                schema: "worknexa",
                table: "ProjectMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreatedBy",
                schema: "worknexa",
                table: "Projects",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DeletedAt",
                schema: "worknexa",
                table: "Projects",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DeletedBy",
                schema: "worknexa",
                table: "Projects",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status",
                schema: "worknexa",
                table: "Projects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TeamId_Name",
                schema: "worknexa",
                table: "Projects",
                columns: new[] { "TeamId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UpdatedBy",
                schema: "worknexa",
                table: "Projects",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_CalendarEventId",
                schema: "worknexa",
                table: "Reminders",
                column: "CalendarEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_CreatedBy",
                schema: "worknexa",
                table: "Reminders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_DeletedAt",
                schema: "worknexa",
                table: "Reminders",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_DeletedBy",
                schema: "worknexa",
                table: "Reminders",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_IsSent",
                schema: "worknexa",
                table: "Reminders",
                column: "IsSent");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_MeetingId",
                schema: "worknexa",
                table: "Reminders",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_TaskId",
                schema: "worknexa",
                table: "Reminders",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_TodoItemId",
                schema: "worknexa",
                table: "Reminders",
                column: "TodoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_UpdatedBy",
                schema: "worknexa",
                table: "Reminders",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_UserId_RemindAt",
                schema: "worknexa",
                table: "Reminders",
                columns: new[] { "UserId", "RemindAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_CreatedBy",
                schema: "worknexa",
                table: "RolePermissions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "worknexa",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                schema: "worknexa",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_UpdatedBy",
                schema: "worknexa",
                table: "RolePermissions",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CreatedBy",
                schema: "worknexa",
                table: "Roles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_DeletedAt",
                schema: "worknexa",
                table: "Roles",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_DeletedBy",
                schema: "worknexa",
                table: "Roles",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                schema: "worknexa",
                table: "Roles",
                column: "Name",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_UpdatedBy",
                schema: "worknexa",
                table: "Roles",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StandupEntries_CreatedBy",
                schema: "worknexa",
                table: "StandupEntries",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StandupEntries_DeletedAt",
                schema: "worknexa",
                table: "StandupEntries",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StandupEntries_DeletedBy",
                schema: "worknexa",
                table: "StandupEntries",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StandupEntries_StandupId_UserId",
                schema: "worknexa",
                table: "StandupEntries",
                columns: new[] { "StandupId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StandupEntries_UpdatedBy",
                schema: "worknexa",
                table: "StandupEntries",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StandupEntries_UserId",
                schema: "worknexa",
                table: "StandupEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Standups_CreatedBy",
                schema: "worknexa",
                table: "Standups",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Standups_DeletedAt",
                schema: "worknexa",
                table: "Standups",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Standups_DeletedBy",
                schema: "worknexa",
                table: "Standups",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Standups_MeetingId",
                schema: "worknexa",
                table: "Standups",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_Standups_OwnerId_StandupDate",
                schema: "worknexa",
                table: "Standups",
                columns: new[] { "OwnerId", "StandupDate" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"OwnerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Standups_TeamId_StandupDate",
                schema: "worknexa",
                table: "Standups",
                columns: new[] { "TeamId", "StandupDate" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"TeamId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Standups_UpdatedBy",
                schema: "worknexa",
                table: "Standups",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_CreatedBy",
                schema: "worknexa",
                table: "Tags",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_DeletedAt",
                schema: "worknexa",
                table: "Tags",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_DeletedBy",
                schema: "worknexa",
                table: "Tags",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                schema: "worknexa",
                table: "Tags",
                column: "Name",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UpdatedBy",
                schema: "worknexa",
                table: "Tags",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_CreatedBy",
                schema: "worknexa",
                table: "TaskComments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_DeletedAt",
                schema: "worknexa",
                table: "TaskComments",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_DeletedBy",
                schema: "worknexa",
                table: "TaskComments",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_TaskId",
                schema: "worknexa",
                table: "TaskComments",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_UpdatedBy",
                schema: "worknexa",
                table: "TaskComments",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_CreatedBy",
                schema: "worknexa",
                table: "TaskDependencies",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_DependsOnTaskId",
                schema: "worknexa",
                table: "TaskDependencies",
                column: "DependsOnTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_TaskId_DependsOnTaskId",
                schema: "worknexa",
                table: "TaskDependencies",
                columns: new[] { "TaskId", "DependsOnTaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_UpdatedBy",
                schema: "worknexa",
                table: "TaskDependencies",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssigneeId_Status",
                schema: "worknexa",
                table: "Tasks",
                columns: new[] { "AssigneeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CreatedBy",
                schema: "worknexa",
                table: "Tasks",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DeletedAt",
                schema: "worknexa",
                table: "Tasks",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DeletedBy",
                schema: "worknexa",
                table: "Tasks",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DueAt",
                schema: "worknexa",
                table: "Tasks",
                column: "DueAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_MeetingId",
                schema: "worknexa",
                table: "Tasks",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ParentTaskId",
                schema: "worknexa",
                table: "Tasks",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId_Status",
                schema: "worknexa",
                table: "Tasks",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status",
                schema: "worknexa",
                table: "Tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TeamId",
                schema: "worknexa",
                table: "Tasks",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UpdatedBy",
                schema: "worknexa",
                table: "Tasks",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTags_CreatedBy",
                schema: "worknexa",
                table: "TaskTags",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTags_TagId",
                schema: "worknexa",
                table: "TaskTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTags_TaskId_TagId",
                schema: "worknexa",
                table: "TaskTags",
                columns: new[] { "TaskId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskTags_UpdatedBy",
                schema: "worknexa",
                table: "TaskTags",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_CreatedBy",
                schema: "worknexa",
                table: "TeamMembers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId_UserId",
                schema: "worknexa",
                table: "TeamMembers",
                columns: new[] { "TeamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_UpdatedBy",
                schema: "worknexa",
                table: "TeamMembers",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_UserId",
                schema: "worknexa",
                table: "TeamMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CreatedBy",
                schema: "worknexa",
                table: "Teams",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DeletedAt",
                schema: "worknexa",
                table: "Teams",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DeletedBy",
                schema: "worknexa",
                table: "Teams",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Name",
                schema: "worknexa",
                table: "Teams",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_UpdatedBy",
                schema: "worknexa",
                table: "Teams",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_CreatedBy",
                schema: "worknexa",
                table: "TodoItems",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_DeletedAt",
                schema: "worknexa",
                table: "TodoItems",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_DeletedBy",
                schema: "worknexa",
                table: "TodoItems",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UpdatedBy",
                schema: "worknexa",
                table: "TodoItems",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_DueAt",
                schema: "worknexa",
                table: "TodoItems",
                columns: new[] { "UserId", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_IsCompleted",
                schema: "worknexa",
                table: "TodoItems",
                columns: new[] { "UserId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoTags_CreatedBy",
                schema: "worknexa",
                table: "TodoTags",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TodoTags_TagId",
                schema: "worknexa",
                table: "TodoTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoTags_TodoItemId_TagId",
                schema: "worknexa",
                table: "TodoTags",
                columns: new[] { "TodoItemId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoTags_UpdatedBy",
                schema: "worknexa",
                table: "TodoTags",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_CreatedBy",
                schema: "worknexa",
                table: "UserRoles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "worknexa",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UpdatedBy",
                schema: "worknexa",
                table: "UserRoles",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                schema: "worknexa",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedBy",
                schema: "worknexa",
                table: "Users",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DeletedAt",
                schema: "worknexa",
                table: "Users",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DeletedBy",
                schema: "worknexa",
                table: "Users",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "worknexa",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                schema: "worknexa",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UpdatedBy",
                schema: "worknexa",
                table: "Users",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Attachments",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "EventParticipants",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "LearningNotes",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "LearningResources",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Leaves",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "MeetingActionItems",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "MeetingNotes",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "MeetingParticipants",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "NoteTags",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "ProjectMembers",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Reminders",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "StandupEntries",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "TaskComments",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "TaskDependencies",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "TaskTags",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "TeamMembers",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "TodoTags",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "LeaveTypes",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Notes",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Standups",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Tags",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "TodoItems",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "LearningTopics",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Tasks",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Meetings",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "CalendarEvents",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Projects",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Teams",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "worknexa");
        }
    }
}
