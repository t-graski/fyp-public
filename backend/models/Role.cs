using backend.models.@base;

namespace backend.models;

public class Role : SoftDeletableEntity<Guid>
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Permissions { get; set; }
    public int Rank { get; init; }
    public bool IsSystem { get; init; }
}