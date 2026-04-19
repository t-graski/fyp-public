namespace backend.models.@base;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAtUtc { get; set; }
    Guid? DeletedByUserId { get; set; }
}