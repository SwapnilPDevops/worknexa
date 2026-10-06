using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Worknexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Department",
                schema: "worknexa",
                table: "Users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Designation",
                schema: "worknexa",
                table: "Users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "JoinedOn",
                schema: "worknexa",
                table: "Users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerId",
                schema: "worknexa",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "worknexa",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "worknexa",
                table: "Users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                schema: "worknexa",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "worknexa",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "MemberCompetencies",
                schema: "worknexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberCompetencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberCompetencies_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MemberCompetencies_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MemberCompetencies_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "worknexa",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "worknexa",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2d"),
                columns: new[] { "Department", "Designation", "JoinedOn", "ManagerId", "Notes", "Phone", "PhotoUrl", "Status" },
                values: new object[] { null, null, null, null, null, null, null, "Active" });

            migrationBuilder.UpdateData(
                schema: "worknexa",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2e"),
                columns: new[] { "Department", "Designation", "JoinedOn", "ManagerId", "Notes", "Phone", "PhotoUrl", "Status" },
                values: new object[] { null, null, null, null, null, null, null, "Active" });

            migrationBuilder.UpdateData(
                schema: "worknexa",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2f"),
                columns: new[] { "Department", "Designation", "JoinedOn", "ManagerId", "Notes", "Phone", "PhotoUrl", "Status" },
                values: new object[] { null, null, null, null, null, null, null, "Active" });

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "worknexa",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: false,
                oldDefaultValue: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ManagerId",
                schema: "worknexa",
                table: "Users",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberCompetencies_CreatedBy",
                schema: "worknexa",
                table: "MemberCompetencies",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MemberCompetencies_UpdatedBy",
                schema: "worknexa",
                table: "MemberCompetencies",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MemberCompetencies_UserId_Kind_Name",
                schema: "worknexa",
                table: "MemberCompetencies",
                columns: new[] { "UserId", "Kind", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ManagerId",
                schema: "worknexa",
                table: "Users",
                column: "ManagerId",
                principalSchema: "worknexa",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ManagerId",
                schema: "worknexa",
                table: "Users");

            migrationBuilder.DropTable(
                name: "MemberCompetencies",
                schema: "worknexa");

            migrationBuilder.DropIndex(
                name: "IX_Users_ManagerId",
                schema: "worknexa",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Department",
                schema: "worknexa",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Designation",
                schema: "worknexa",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "JoinedOn",
                schema: "worknexa",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                schema: "worknexa",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "worknexa",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "worknexa",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                schema: "worknexa",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "worknexa",
                table: "Users");
        }
    }
}
