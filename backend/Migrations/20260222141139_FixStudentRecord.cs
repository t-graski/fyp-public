using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class FixStudentRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailContact",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "StudentRecordId",
                table: "students",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "student_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonalEmail = table.Column<string>(type: "text", nullable: false),
                    HomeAddress = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    EntryQualifications = table.Column<string[]>(type: "text[]", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_students_StudentRecordId",
                table: "students",
                column: "StudentRecordId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_students_student_records_StudentRecordId",
                table: "students",
                column: "StudentRecordId",
                principalTable: "student_records",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_students_student_records_StudentRecordId",
                table: "students");

            migrationBuilder.DropTable(
                name: "student_records");

            migrationBuilder.DropIndex(
                name: "IX_students_StudentRecordId",
                table: "students");

            migrationBuilder.DropColumn(
                name: "StudentRecordId",
                table: "students");

            migrationBuilder.AddColumn<string>(
                name: "EmailContact",
                table: "users",
                type: "text",
                nullable: true);
        }
    }
}
