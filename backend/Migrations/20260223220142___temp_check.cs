using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class __temp_check : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModuleContentChunks_modules_ModuleId",
                table: "ModuleContentChunks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ModuleContentChunks",
                table: "ModuleContentChunks");

            migrationBuilder.RenameTable(
                name: "ModuleContentChunks",
                newName: "module_content_chunk");

            migrationBuilder.RenameIndex(
                name: "IX_ModuleContentChunks_ModuleId",
                table: "module_content_chunk",
                newName: "IX_module_content_chunk_ModuleId");

            migrationBuilder.RenameIndex(
                name: "IX_ModuleContentChunks_ModuleFileId",
                table: "module_content_chunk",
                newName: "IX_module_content_chunk_ModuleFileId");

            migrationBuilder.RenameIndex(
                name: "IX_ModuleContentChunks_ModuleElementId",
                table: "module_content_chunk",
                newName: "IX_module_content_chunk_ModuleElementId");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddPrimaryKey(
                name: "PK_module_content_chunk",
                table: "module_content_chunk",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_module_content_chunk_modules_ModuleId",
                table: "module_content_chunk",
                column: "ModuleId",
                principalTable: "modules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_module_content_chunk_modules_ModuleId",
                table: "module_content_chunk");

            migrationBuilder.DropPrimaryKey(
                name: "PK_module_content_chunk",
                table: "module_content_chunk");

            migrationBuilder.RenameTable(
                name: "module_content_chunk",
                newName: "ModuleContentChunks");

            migrationBuilder.RenameIndex(
                name: "IX_module_content_chunk_ModuleId",
                table: "ModuleContentChunks",
                newName: "IX_ModuleContentChunks_ModuleId");

            migrationBuilder.RenameIndex(
                name: "IX_module_content_chunk_ModuleFileId",
                table: "ModuleContentChunks",
                newName: "IX_ModuleContentChunks_ModuleFileId");

            migrationBuilder.RenameIndex(
                name: "IX_module_content_chunk_ModuleElementId",
                table: "ModuleContentChunks",
                newName: "IX_ModuleContentChunks_ModuleElementId");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ModuleContentChunks",
                table: "ModuleContentChunks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModuleContentChunks_modules_ModuleId",
                table: "ModuleContentChunks",
                column: "ModuleId",
                principalTable: "modules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
