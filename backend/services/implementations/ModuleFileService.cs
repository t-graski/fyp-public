using backend.data;
using backend.dtos;
using backend.errors;
using backend.helpers.interfaces;
using backend.models;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class ModuleFileService(AppDbContext db, IFileStorage storage, IDatabaseGuards guards)
    : IModuleFileService
{
    public async Task<ModuleFileDto> UploadAsync(Guid moduleId, IFormFile file, CancellationToken ct)
    {
        await guards.EnsureTeachingStaffAsync(moduleId);

        if (file is null || file.Length == 0)
        {
            throw new AppException(400, "INVALID_FILE", "File is empty.");
        }

        await using var stream = file.OpenReadStream();
        var storageKey = await storage.SaveAsync(stream, file.ContentType, ct);

        var moduleFile = new ModuleFile
        {
            ModuleId = moduleId,
            OriginalFileName = file.FileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = file.Length,
            StorageKey = storageKey
        };

        db.ModuleFiles.Add(moduleFile);
        await db.SaveChangesAsync(ct);

        return new ModuleFileDto(moduleFile.Id, moduleId, moduleFile.OriginalFileName, moduleFile.ContentType,
            moduleFile.SizeBytes);
    }

    public async Task<(ModuleFile File, Stream Stream)> DownloadAsync(Guid moduleId, Guid fileId, CancellationToken ct)
    {
        await guards.EnsureCanAccessModuleAsync(moduleId);

        var file = await db.ModuleFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId && f.ModuleId == moduleId && !f.IsDeleted, ct);

        if (file is null)
        {
            throw new AppException(404, "FILE_NOT_FOUND", "File does not exist.");
        }

        var exists = await storage.ExistsAsync(file.StorageKey, ct);

        if (!exists)
        {
            throw new AppException(404, "FILE_BLOB_MISSING", "File exists in database but is missing in storage.");
        }

        var (s, _) = await storage.OpenReadAsync(file.StorageKey, ct);
        return (file, s);
    }

    public async Task<ModuleFileExistsDto> ExistsAsync(Guid moduleId, Guid fileId, CancellationToken ct)
    {
        await guards.EnsureCanAccessModuleAsync(moduleId);

        var file = await db.ModuleFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId && f.ModuleId == moduleId && !f.IsDeleted, ct);

        if (file is null)
        {
            return new ModuleFileExistsDto(fileId, false);
        }

        var exists = await storage.ExistsAsync(file.StorageKey, ct);
        return new ModuleFileExistsDto(fileId, exists);
    }
}