using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_student_attendance_StudentId_Date",
                table: "student_attendance");

            migrationBuilder.DropColumn(
                name: "FirstSeenAtUtc",
                table: "student_attendance");

            migrationBuilder.DropColumn(
                name: "LoginCount",
                table: "student_attendance");

            migrationBuilder.RenameColumn(
                name: "LastSeenAtUtc",
                table: "student_attendance",
                newName: "CheckedInAtUtc");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "students",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModuleId",
                table: "student_attendance",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateOnly>(
                name: "RunsFrom",
                table: "modules",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "RunsTo",
                table: "modules",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "ScheduledDay",
                table: "modules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ScheduledEndLocal",
                table: "modules",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ScheduledStartLocal",
                table: "modules",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.CreateTable(
                name: "attendance_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckInStartLocal = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    CheckInEndLocal = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_student_attendance_ModuleId",
                table: "student_attendance",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_student_attendance_StudentId_ModuleId_Date",
                table: "student_attendance",
                columns: new[] { "StudentId", "ModuleId", "Date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_student_attendance_modules_ModuleId",
                table: "student_attendance",
                column: "ModuleId",
                principalTable: "modules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_student_attendance_modules_ModuleId",
                table: "student_attendance");

            migrationBuilder.DropTable(
                name: "attendance_settings");

            migrationBuilder.DropIndex(
                name: "IX_student_attendance_ModuleId",
                table: "student_attendance");

            migrationBuilder.DropIndex(
                name: "IX_student_attendance_StudentId_ModuleId_Date",
                table: "student_attendance");

            migrationBuilder.DropColumn(
                name: "ModuleId",
                table: "student_attendance");

            migrationBuilder.DropColumn(
                name: "RunsFrom",
                table: "modules");

            migrationBuilder.DropColumn(
                name: "RunsTo",
                table: "modules");

            migrationBuilder.DropColumn(
                name: "ScheduledDay",
                table: "modules");

            migrationBuilder.DropColumn(
                name: "ScheduledEndLocal",
                table: "modules");

            migrationBuilder.DropColumn(
                name: "ScheduledStartLocal",
                table: "modules");

            migrationBuilder.RenameColumn(
                name: "CheckedInAtUtc",
                table: "student_attendance",
                newName: "LastSeenAtUtc");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "students",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstSeenAtUtc",
                table: "student_attendance",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "LoginCount",
                table: "student_attendance",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_student_attendance_StudentId_Date",
                table: "student_attendance",
                columns: new[] { "StudentId", "Date" },
                unique: true);
        }
    }
}
