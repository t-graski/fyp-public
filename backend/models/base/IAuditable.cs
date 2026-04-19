namespace backend.models.@base;

public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; set; }
    Guid? CreatedByUserId { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}