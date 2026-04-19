using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AuditEventTypoFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OccuredAtUtc",
                table: "audit_events",
                newName: "OccurredAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_audit_events_OccuredAtUtc",
                table: "audit_events",
                newName: "IX_audit_events_OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OccurredAtUtc",
                table: "audit_events",
                newName: "OccuredAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_audit_events_OccurredAtUtc",
                table: "audit_events",
                newName: "IX_audit_events_OccuredAtUtc");
        }
    }
}
