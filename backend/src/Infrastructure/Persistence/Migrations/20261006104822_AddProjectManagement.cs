using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Worknexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "worknexa",
                table: "Projects",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DocumentationUrl",
                schema: "worknexa",
                table: "Projects",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                schema: "worknexa",
                table: "Projects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                schema: "worknexa",
                table: "Projects",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RepositoryUrl",
                schema: "worknexa",
                table: "Projects",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Technology",
                schema: "worknexa",
                table: "Projects",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE worknexa."Projects" SET "Status" = 'Planning' WHERE "Status" = 'Planned';
                UPDATE worknexa."Projects" SET "Status" = 'Cancelled' WHERE "Status" = 'Archived';
                UPDATE worknexa."Projects" SET "Code" = 'P' || UPPER(SUBSTRING("Id"::text, 1, 8)) WHERE "Code" = '';
                UPDATE worknexa."Projects" SET "Priority" = 'Medium' WHERE "Priority" = '';
                UPDATE worknexa."Projects" SET "OwnerId" = COALESCE("CreatedBy", '8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2d') WHERE "OwnerId" = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "worknexa",
                table: "Projects",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                schema: "worknexa",
                table: "Projects",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                schema: "worknexa",
                table: "Projects",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldDefaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Code",
                schema: "worknexa",
                table: "Projects",
                column: "Code",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerId",
                schema: "worknexa",
                table: "Projects",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_OwnerId",
                schema: "worknexa",
                table: "Projects",
                column: "OwnerId",
                principalSchema: "worknexa",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_OwnerId",
                schema: "worknexa",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Code",
                schema: "worknexa",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerId",
                schema: "worknexa",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "worknexa",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DocumentationUrl",
                schema: "worknexa",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                schema: "worknexa",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "worknexa",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RepositoryUrl",
                schema: "worknexa",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Technology",
                schema: "worknexa",
                table: "Projects");
        }
    }
}
