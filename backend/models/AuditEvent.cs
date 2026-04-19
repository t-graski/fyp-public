namespace backend.models;

public class AuditEvent
{
    public Guid AuditEventId { get; set; } = Guid.NewGuid();
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string EntityTable { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? Summary { get; set; }
    public string? ChangesJson { get; set; } = string.Empty;
    public string? MetadataJson { get; set; } = string.Empty;
}

public record AuditFieldChange(object? Old, object? New);

public record AuditChanges(
    Dictionary<string, AuditFieldChange> Fields,
    Dictionary<string, object?>? Key = null
);