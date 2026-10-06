using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Worknexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsAndActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                schema: "worknexa",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "worknexa",
                table: "Notifications",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ReminderDue");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                schema: "worknexa",
                table: "Notifications",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldDefaultValue: "ReminderDue");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                schema: "worknexa",
                table: "ActivityLogs",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_Type_SourceId",
                schema: "worknexa",
                table: "Notifications",
                columns: new[] { "UserId", "Type", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_CreatedBy",
                schema: "worknexa",
                table: "NotificationPreferences",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UpdatedBy",
                schema: "worknexa",
                table: "NotificationPreferences",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId_Type",
                schema: "worknexa",
                table: "NotificationPreferences",
                columns: new[] { "UserId", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreferences",
                schema: "worknexa");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_Type_SourceId",
                schema: "worknexa",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "SourceId",
                schema: "worknexa",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "worknexa",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Metadata",
                schema: "worknexa",
                table: "ActivityLogs");
        }
    }
}
