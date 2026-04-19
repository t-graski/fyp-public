using backend.dtos;

namespace backend.services.interfaces;

public interface IAuditService
{
    Task<IReadOnlyList<AuditEventDto>> SearchAsync(
        Guid? actorUserId,
        string? entityType,
        string? entityId,
        string? action,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int limit,
        int offset);
    
    Task<IReadOnlyList<LoginEventDto>> SearchLoginAsync(Guid? actorUserId, int limit, int offset);
}