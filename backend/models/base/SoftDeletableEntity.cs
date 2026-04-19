namespace backend.models.@base;

public class SoftDeletableEntity<T> : AuditableEntity<T>, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
}