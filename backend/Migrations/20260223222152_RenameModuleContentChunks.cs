using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RenameModuleContentChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_module_content_chunk_modules_ModuleId",
                table: "module_content_chunk");

            migrationBuilder.DropPrimaryKey(
                name: "PK_module_content_chunk",
                table: "module_content_chunk");

            migrationBuilder.RenameTable(
                name: "module_content_chunk",
                newName: "module_content_chunks");

            migrationBuilder.RenameIndex(
                name: "IX_module_content_chunk_ModuleId",
                table: "module_content_chunks",
                newName: "IX_module_content_chunks_ModuleId");

            migrationBuilder.RenameIndex(
                name: "IX_module_content_chunk_ModuleFileId",
                table: "module_content_chunks",
                newName: "IX_module_content_chunks_ModuleFileId");

            migrationBuilder.RenameIndex(
                name: "IX_module_content_chunk_ModuleElementId",
                table: "module_content_chunks",
                newName: "IX_module_content_chunks_ModuleElementId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_module_content_chunks",
                table: "module_content_chunks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_module_content_chunks_modules_ModuleId",
                table: "module_content_chunks",
                column: "ModuleId",
                principalTable: "modules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_module_content_chunks_modules_ModuleId",
                table: "module_content_chunks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_module_content_chunks",
                table: "module_content_chunks");

            migrationBuilder.RenameTable(
                name: "module_content_chunks",
                newName: "module_content_chunk");

            migrationBuilder.RenameIndex(
                name: "IX_module_content_chunks_ModuleId",
                table: "module_content_chunk",
                newName: "IX_module_content_chunk_ModuleId");

            migrationBuilder.RenameIndex(
                name: "IX_module_content_chunks_ModuleFileId",
                table: "module_content_chunk",
                newName: "IX_module_content_chunk_ModuleFileId");

            migrationBuilder.RenameIndex(
                name: "IX_module_content_chunks_ModuleElementId",
                table: "module_content_chunk",
                newName: "IX_module_content_chunk_ModuleElementId");

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
    }
}
