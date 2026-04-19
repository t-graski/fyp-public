using backend.data;
using backend.dtos;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class AuditService(AppDbContext db) : IAuditService
{
    public async Task<IReadOnlyList<AuditEventDto>> SearchAsync(Guid? actorUserId, string? entityType, string? entityId,
        string? action, DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc, int limit, int offset)
    {
        limit = Math.Clamp(limit, 1, 200);
        offset = Math.Max(0, offset);

        var q = db.AuditEvents.AsNoTracking();

        q = q.Where(x => x.Action != "LOGIN");

        if (actorUserId.HasValue) q = q.Where(x => x.ActorUserId == actorUserId);
        if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(x => x.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(entityId)) q = q.Where(x => x.EntityId == entityId);
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(x => x.Action == action);

        return await q
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip(offset)
            .Take(limit)
            .Select(x => new AuditEventDto(
                x.AuditEventId,
                x.OccurredAtUtc,
                x.ActorUserId,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.Summary,
                x.ChangesJson,
                x.MetadataJson
            ))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<LoginEventDto>> SearchLoginAsync(Guid? actorUserId, int limit, int offset)
    {
        limit = Math.Clamp(limit, 1, 200);
        offset = Math.Max(0, offset);

        var q = db.AuditEvents.AsNoTracking();

        q = q.Where(x => x.Action == "LOGIN");

        if (actorUserId.HasValue) q = q.Where(x => x.ActorUserId == actorUserId);

        var auditEvents = await q
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        var userIds = auditEvents
            .Select(x => Guid.Parse(x.EntityId[3..]))
            .Distinct()
            .ToList();

        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email);

        return auditEvents
            .Select(audit =>
            {
                var userId = Guid.Parse(audit.EntityId[3..]);
                var email = users.GetValueOrDefault(userId, "[deleted user]");
                return new LoginEventDto(
                    audit.AuditEventId,
                    email,
                    audit.OccurredAtUtc,
                    audit.ActorUserId
                );
            })
            .ToList();
    }
}