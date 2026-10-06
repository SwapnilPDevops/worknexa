using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Worknexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE worknexa."LearningTopics" SET "Status" = 'Learning' WHERE "Status" = 'InProgress';
                """);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "worknexa",
                table: "LearningTopics",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Goal",
                schema: "worknexa",
                table: "LearningTopics",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentTopicId",
                schema: "worknexa",
                table: "LearningTopics",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                schema: "worknexa",
                table: "LearningTopics",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Medium");

            migrationBuilder.AddColumn<int>(
                name: "Progress",
                schema: "worknexa",
                table: "LearningTopics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                schema: "worknexa",
                table: "LearningTopics",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TargetDate",
                schema: "worknexa",
                table: "LearningTopics",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningTopics_ParentTopicId",
                schema: "worknexa",
                table: "LearningTopics",
                column: "ParentTopicId");

            migrationBuilder.AddForeignKey(
                name: "FK_LearningTopics_LearningTopics_ParentTopicId",
                schema: "worknexa",
                table: "LearningTopics",
                column: "ParentTopicId",
                principalSchema: "worknexa",
                principalTable: "LearningTopics",
                principalColumn: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                schema: "worknexa",
                table: "LearningTopics",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldDefaultValue: "Medium");

            migrationBuilder.AlterColumn<int>(
                name: "Progress",
                schema: "worknexa",
                table: "LearningTopics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LearningTopics_LearningTopics_ParentTopicId",
                schema: "worknexa",
                table: "LearningTopics");

            migrationBuilder.DropIndex(
                name: "IX_LearningTopics_ParentTopicId",
                schema: "worknexa",
                table: "LearningTopics");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "worknexa",
                table: "LearningTopics");

            migrationBuilder.DropColumn(
                name: "Goal",
                schema: "worknexa",
                table: "LearningTopics");

            migrationBuilder.DropColumn(
                name: "ParentTopicId",
                schema: "worknexa",
                table: "LearningTopics");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "worknexa",
                table: "LearningTopics");

            migrationBuilder.DropColumn(
                name: "Progress",
                schema: "worknexa",
                table: "LearningTopics");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "worknexa",
                table: "LearningTopics");

            migrationBuilder.DropColumn(
                name: "TargetDate",
                schema: "worknexa",
                table: "LearningTopics");
        }
    }
}
