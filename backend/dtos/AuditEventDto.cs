namespace backend.dtos;

public record AuditEventDto(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    Guid? ActorUserId,
    string Action,
    string EntityType,
    string EntityId,
    string? Summary,
    string? ChangesJson,
    string? MetadataJson
);

public record LoginEventDto(
    Guid Id,
    string Email,
    DateTimeOffset OccurredAtUtc,
    Guid? ActorUserId
);