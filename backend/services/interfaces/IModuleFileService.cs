using backend.dtos;
using backend.models;

namespace backend.services.interfaces;

public interface IModuleFileService
{
    Task<ModuleFileDto> UploadAsync(Guid moduleId, IFormFile file, CancellationToken ct);
    Task<(ModuleFile File, Stream Stream)> DownloadAsync(Guid moduleId, Guid fileId, CancellationToken ct);
    Task<ModuleFileExistsDto> ExistsAsync(Guid moduleId, Guid fileId, CancellationToken ct);
}