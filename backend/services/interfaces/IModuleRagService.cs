using backend.dtos;

namespace backend.services.interfaces;

public interface IModuleRagService
{
    Task IndexAsync(Guid moduleId, bool rebuild, CancellationToken ct);
    Task<RagChatResponse> ChatAsync(Guid moduleId, string message, int topK, CancellationToken ct);
}