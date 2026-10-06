using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Worknexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationConnections",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Direction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SyncEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalAccountId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalAccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalContainerId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExternalContainerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TimeZone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AccessTokenProtected = table.Column<string>(type: "text", nullable: true),
                    RefreshTokenProtected = table.Column<string>(type: "text", nullable: true),
                    TokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SyncCursor = table.Column<string>(type: "text", nullable: true),
                    CursorIssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextSyncAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalLinks",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LocalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExternalVersion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SyncedHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Origin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalLinks_IntegrationConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalSchema: "worknexa",
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExternalLinks_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExternalLinks_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SyncRuns",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Trigger = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Direction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Imported = table.Column<int>(type: "integer", nullable: false),
                    Updated = table.Column<int>(type: "integer", nullable: false),
                    Exported = table.Column<int>(type: "integer", nullable: false),
                    Removed = table.Column<int>(type: "integer", nullable: false),
                    Skipped = table.Column<int>(type: "integer", nullable: false),
                    Conflicts = table.Column<int>(type: "integer", nullable: false),
                    Failed = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncRuns_IntegrationConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalSchema: "worknexa",
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SyncRuns_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SyncRuns_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SyncFailures",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LocalId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncFailures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncFailures_SyncRuns_RunId",
                        column: x => x.RunId,
                        principalSchema: "worknexa",
                        principalTable: "SyncRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SyncFailures_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SyncFailures_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLinks_ConnectionId_EntityType_ExternalId",
                schema: "worknexa",
                table: "ExternalLinks",
                columns: new[] { "ConnectionId", "EntityType", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLinks_ConnectionId_EntityType_LocalId",
                schema: "worknexa",
                table: "ExternalLinks",
                columns: new[] { "ConnectionId", "EntityType", "LocalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLinks_CreatedBy",
                schema: "worknexa",
                table: "ExternalLinks",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLinks_EntityType_LocalId",
                schema: "worknexa",
                table: "ExternalLinks",
                columns: new[] { "EntityType", "LocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLinks_UpdatedBy",
                schema: "worknexa",
                table: "ExternalLinks",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_CreatedBy",
                schema: "worknexa",
                table: "IntegrationConnections",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_SyncEnabled_Status_NextSyncAt",
                schema: "worknexa",
                table: "IntegrationConnections",
                columns: new[] { "SyncEnabled", "Status", "NextSyncAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_UpdatedBy",
                schema: "worknexa",
                table: "IntegrationConnections",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_UserId_Provider",
                schema: "worknexa",
                table: "IntegrationConnections",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncFailures_CreatedBy",
                schema: "worknexa",
                table: "SyncFailures",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SyncFailures_RunId",
                schema: "worknexa",
                table: "SyncFailures",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncFailures_UpdatedBy",
                schema: "worknexa",
                table: "SyncFailures",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRuns_ConnectionId_StartedAt",
                schema: "worknexa",
                table: "SyncRuns",
                columns: new[] { "ConnectionId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncRuns_CreatedBy",
                schema: "worknexa",
                table: "SyncRuns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRuns_UpdatedBy",
                schema: "worknexa",
                table: "SyncRuns",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalLinks",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "SyncFailures",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "SyncRuns",
                schema: "worknexa");

            migrationBuilder.DropTable(
                name: "IntegrationConnections",
                schema: "worknexa");
        }
    }
}
