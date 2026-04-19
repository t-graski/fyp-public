using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class ModifyAuditEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OccuredAt",
                table: "audit_events",
                newName: "OccuredAtUtc");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "audit_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "audit_events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "audit_events",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // ---- Fix AuditEventId: bigint identity -> uuid ----
// 1) Drop identity first (Postgres requires this)
            migrationBuilder.Sql("""ALTER TABLE audit_events ALTER COLUMN "AuditEventId" DROP IDENTITY IF EXISTS;""");

// 2) UUID generator
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS "pgcrypto";""");

// 3) Convert type (dev-safe: new UUIDs for existing rows)
            migrationBuilder.Sql("""
                                     ALTER TABLE audit_events
                                     ALTER COLUMN "AuditEventId" TYPE uuid
                                     USING gen_random_uuid();
                                 """);

// 4) Default for new rows
            migrationBuilder.Sql("""
                                     ALTER TABLE audit_events
                                     ALTER COLUMN "AuditEventId" SET DEFAULT gen_random_uuid();
                                 """);

            migrationBuilder.AddColumn<string>(
                name: "ChangesJson",
                table: "audit_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityTable",
                table: "audit_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "audit_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "audit_events",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_ActorUserId",
                table: "audit_events",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_EntityType_EntityId",
                table: "audit_events",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_OccuredAtUtc",
                table: "audit_events",
                column: "OccuredAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_audit_events_users_ActorUserId",
                table: "audit_events",
                column: "ActorUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_events_users_ActorUserId",
                table: "audit_events");

            migrationBuilder.DropIndex(
                name: "IX_audit_events_ActorUserId",
                table: "audit_events");

            migrationBuilder.DropIndex(
                name: "IX_audit_events_EntityType_EntityId",
                table: "audit_events");

            migrationBuilder.DropIndex(
                name: "IX_audit_events_OccuredAtUtc",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "ChangesJson",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "EntityTable",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "audit_events");

            migrationBuilder.RenameColumn(
                name: "OccuredAtUtc",
                table: "audit_events",
                newName: "OccuredAt");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "audit_events",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "audit_events",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "audit_events",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<long>(
                name: "AuditEventId",
                table: "audit_events",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
